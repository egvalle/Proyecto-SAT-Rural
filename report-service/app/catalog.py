import os
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from statistics import mean
from uuid import uuid4

import httpx
from fastapi import HTTPException, status
from jinja2 import Environment, FileSystemLoader, select_autoescape
from pydantic import BaseModel, Field
from starlette.concurrency import run_in_threadpool
from weasyprint import HTML


API_BASE_URL = os.getenv("REPORT_DATA_API_URL", "http://gateway:8080").rstrip("/")
ALERTS_API_URL = os.getenv("ALERTS_API_URL", f"{API_BASE_URL}/api/alerts")
STORAGE_PATH = Path(os.getenv("REPORT_STORAGE_PATH", "/app/storage"))
templates = Environment(
    loader=FileSystemLoader(Path(__file__).parent / "templates"),
    autoescape=select_autoescape(["html", "xml"]),
)


class ReportRequest(BaseModel):
    type: str = Field(pattern="^(communities|sensors|readings|alerts|events|sensor-status|climate|executive)$")
    sensor_id: int | None = None
    community_id: int | None = None
    sensor_type: str | None = None
    from_date: datetime | None = None
    to_date: datetime | None = None
    level: str | None = Field(default=None, pattern="^(YELLOW|ORANGE|RED)$")
    is_active: bool | None = None


async def fetch_items(client: httpx.AsyncClient, path: str, token: str,
                      params: dict | None = None, paged: bool = True) -> list[dict]:
    items = []
    page = 1
    while True:
        query = {**(params or {})}
        if paged:
            query.update(page=page, pageSize=100)
        try:
            response = await client.get(
                path if path.startswith("http") else f"{API_BASE_URL}{path}", params=query,
                headers={"Authorization": f"Bearer {token}"},
            )
            response.raise_for_status()
        except httpx.HTTPError as exc:
            raise HTTPException(status.HTTP_502_BAD_GATEWAY,
                                f"No se pudo consultar {path}.") from exc
        data = response.json()
        batch = data if isinstance(data, list) else data.get("items", data.get("value", []))
        if not isinstance(batch, list):
            raise HTTPException(status.HTTP_502_BAD_GATEWAY,
                                f"Respuesta inesperada de {path}.")
        items.extend(batch)
        if not paged or len(batch) < 100:
            break
        page += 1
    return items


def display(value):
    if value is None:
        return "—"
    if isinstance(value, bool):
        return "Sí" if value else "No"
    if isinstance(value, str) and "T" in value and len(value) >= 19:
        try:
            return datetime.fromisoformat(value.replace("Z", "+00:00")).strftime("%d/%m/%Y %H:%M")
        except ValueError:
            pass
    return str(value)


async def build_report(request: ReportRequest, token: str):
    if request.type == "readings" and (request.from_date is None or request.to_date is None):
        raise HTTPException(400, "El reporte de lecturas requiere fechas de inicio y fin.")
    if request.from_date and request.to_date and request.from_date > request.to_date:
        raise HTTPException(400, "La fecha inicial debe preceder a la final.")
    async with httpx.AsyncClient(timeout=30.0) as client:
        if request.type == "communities":
            data = await fetch_items(client, "/api/communities", token)
            sensors = await fetch_items(client, "/api/sensors", token)
            sensor_counts = Counter(item.get("communityId") for item in sensors)
            for item in data:
                item["sensorCount"] = sensor_counts.get(item.get("id"), 0)
            if request.community_id:
                data = [x for x in data if x.get("id") == request.community_id]
            columns = [("Comunidad", "name"), ("Municipio", "municipality"),
                       ("Departamento", "department"), ("País", "country"),
                       ("Latitud", "latitude"), ("Longitud", "longitude"),
                       ("Estado", "isActive"), ("Sensores", "sensorCount")]
            title = "Reporte de comunidades"
        elif request.type == "sensors":
            data = await fetch_items(client, "/api/sensors", token)
            if request.community_id:
                data = [x for x in data if x.get("communityId") == request.community_id]
            if request.sensor_type:
                data = [x for x in data if x.get("type") == request.sensor_type]
            columns = [("Código", "code"), ("Nombre", "name"), ("Tipo", "type"),
                       ("Comunidad", "communityName"), ("Ubicación", "location"),
                       ("Unidad", "unit"), ("Activo", "isActive"),
                       ("Instalación", "installationDate")]
            title = "Reporte de sensores"
        elif request.type == "readings":
            params = {}
            for key, value in (("sensorId", request.sensor_id),
                               ("communityId", request.community_id),
                               ("from", request.from_date), ("to", request.to_date)):
                if value is not None:
                    params[key] = value.isoformat() if isinstance(value, datetime) else value
            data = await fetch_items(client, "/api/sensor-readings", token, params)
            if request.sensor_type:
                data = [x for x in data if x.get("sensorType") == request.sensor_type]
            columns = [("Sensor", "sensorName"), ("Comunidad", "communityName"),
                       ("Fecha y hora", "recordedAt"), ("Valor", "value"),
                       ("Unidad", "unit"), ("Activo", "sensorIsActive")]
            title = "Reporte de lecturas"
        elif request.type == "alerts":
            params = {}
            if request.level:
                params["level"] = request.level
            if request.is_active is not None:
                params["isActive"] = request.is_active
            data = await fetch_items(client, ALERTS_API_URL, token, params)
            if request.community_id:
                data = [x for x in data if x.get("communityId") == request.community_id]
            columns = [("Fecha", "createdAt"), ("Comunidad", "communityId"),
                       ("Sensor", "sensorId"), ("Fenómeno", "type"),
                       ("Nivel", "level"), ("Valor", "detectedValue"),
                       ("Umbral", "thresholdValue"), ("Mensaje", "message"),
                       ("Activo", "isActive")]
            title = "Reporte de alertas"
        elif request.type == "events":
            params = {}
            if request.community_id:
                params["communityId"] = request.community_id
            if request.from_date:
                params["from"] = request.from_date.date().isoformat()
            if request.to_date:
                params["to"] = request.to_date.date().isoformat()
            data = await fetch_items(client, "/api/events", token, params, False)
            columns = [("Fecha", "occurredAt"), ("Comunidad", "communityName"),
                       ("Fenómeno", "eventType"), ("Nivel", "level"),
                       ("Descripción", "description")]
            title = "Historial de eventos"
        else:
            sensors = await fetch_items(client, "/api/sensors", token)
            readings = await fetch_items(client, "/api/sensor-readings", token)
            alerts = await fetch_items(client, ALERTS_API_URL, token)
            if request.type == "sensor-status":
                latest = {}
                for item in readings:
                    key = item.get("sensorId")
                    if key not in latest or item.get("recordedAt", "") > latest[key].get("recordedAt", ""):
                        latest[key] = item
                now = datetime.now(timezone.utc)
                data = []
                for sensor in sensors:
                    row = dict(sensor)
                    last = latest.get(sensor.get("id"), {})
                    row["lastReading"] = last.get("recordedAt")
                    try:
                        timestamp = datetime.fromisoformat(row["lastReading"].replace("Z", "+00:00"))
                        if timestamp.tzinfo is None:
                            timestamp = timestamp.replace(tzinfo=timezone.utc)
                        stale = (now - timestamp).total_seconds() > 60
                    except (TypeError, ValueError):
                        stale = True
                    row["status"] = "Inactivo" if not row.get("isActive") else (
                        "Sin comunicación" if stale else "Activo")
                    data.append(row)
                columns = [("Sensor", "code"), ("Comunidad", "communityName"),
                           ("Estado", "status"), ("Última lectura", "lastReading")]
                title = "Estado de sensores"
            elif request.type == "climate":
                grouped = defaultdict(list)
                sensor_types = {x.get("id"): x.get("type") for x in sensors}
                for item in readings:
                    sensor_type = item.get("sensorType") or sensor_types.get(item.get("sensorId"))
                    if sensor_type and item.get("value") is not None:
                        grouped[sensor_type].append(float(item["value"]))
                data = [{"type": key, "count": len(values), "average": round(mean(values), 2),
                         "minimum": min(values), "maximum": max(values),
                         "total": round(sum(values), 2) if key == "RAINFALL" else "—"}
                        for key, values in sorted(grouped.items())]
                columns = [("Tipo", "type"), ("Lecturas", "count"),
                           ("Promedio", "average"), ("Mínimo", "minimum"),
                           ("Máximo", "maximum"), ("Lluvia acumulada", "total")]
                title = f"Estadísticas climáticas · {len(alerts)} alertas"
            else:
                communities = await fetch_items(client, "/api/communities", token)
                events = await fetch_items(client, "/api/events", token, paged=False)
                levels = Counter(x.get("level", "SIN NIVEL") for x in alerts if x.get("isActive"))
                data = [
                    {"indicator": "Comunidades", "value": len(communities)},
                    {"indicator": "Sensores", "value": len(sensors)},
                    {"indicator": "Sensores activos", "value": sum(bool(x.get("isActive")) for x in sensors)},
                    {"indicator": "Lecturas", "value": len(readings)},
                    {"indicator": "Alertas activas", "value": sum(levels.values())},
                    {"indicator": "Alertas YELLOW", "value": levels.get("YELLOW", 0)},
                    {"indicator": "Alertas ORANGE", "value": levels.get("ORANGE", 0)},
                    {"indicator": "Alertas RED", "value": levels.get("RED", 0)},
                    {"indicator": "Eventos", "value": len(events)},
                ]
                columns = [("Indicador", "indicator"), ("Valor", "value")]
                title = "Reporte ejecutivo"
    return title, columns, data


async def write_report(report_type: str, title: str, columns: list[tuple[str, str]],
                       data: list[dict], username: str):
    report_id = str(uuid4())
    file_name = f"{report_type}-{report_id}.pdf"
    STORAGE_PATH.mkdir(parents=True, exist_ok=True)
    file_path = STORAGE_PATH / file_name
    html = templates.get_template("table_report.html").render(
        title=title, columns=columns, rows=data, display=display,
        username=username, generated_at=datetime.now(timezone.utc).strftime("%d/%m/%Y %H:%M UTC"),
    )
    await run_in_threadpool(HTML(string=html).write_pdf, target=str(file_path))
    return report_id, file_name, file_path

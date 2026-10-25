import os
from datetime import datetime, timezone
from pathlib import Path
from uuid import uuid4

import httpx
from fastapi import APIRouter, Depends, HTTPException, Query, Request, status
from fastapi.responses import FileResponse
from jinja2 import Environment, FileSystemLoader, select_autoescape
from pydantic import BaseModel, Field
from starlette.concurrency import run_in_threadpool
from weasyprint import HTML

from app.auth import require_roles

router = APIRouter(tags=["reports"])

TEMPLATE_DIRECTORY = Path(__file__).parent / "templates"
template_environment = Environment(
    loader=FileSystemLoader(TEMPLATE_DIRECTORY),
    autoescape=select_autoescape(["html", "xml"]),
)

ALERTS_API_URL = os.getenv(
    "ALERTS_API_URL",
    "http://api:8080/api/alerts",
)
REPORT_STORAGE_PATH = Path(
    os.getenv("REPORT_STORAGE_PATH", "/app/storage")
)

class AlertReportFilters(BaseModel):
    level: str | None = Field(default=None, pattern="^(RED|ORANGE|YELLOW|GREEN)$")
    is_active: bool | None = None


def _extract_alerts(response_data: object) -> list[dict]:
    # La API puede devolver una lista o el objeto paginado { value, Count }.
    if isinstance(response_data, list):
        return response_data

    if isinstance(response_data, dict):
        value = response_data.get("value", response_data.get("Value", []))
        if isinstance(value, list):
            return value

    raise HTTPException(
        status_code=status.HTTP_502_BAD_GATEWAY,
        detail="La API de alertas devolvió una respuesta inesperada.",
    )


async def _fetch_alerts(
    token: str,
    filters: AlertReportFilters,
) -> list[dict]:
    all_alerts: list[dict] = []
    page = 1
    page_size = 100

    params: dict[str, str | int | bool] = {
        "page": page,
        "pageSize": page_size,
    }

    if filters.level:
        params["level"] = filters.level

    if filters.is_active is not None:
        params["isActive"] = filters.is_active

    try:
        async with httpx.AsyncClient(timeout=30.0) as client:
            while True:
                params["page"] = page
                response = await client.get(
                    ALERTS_API_URL,
                    params=params,
                    headers={"Authorization": f"Bearer {token}"},
                )
                response.raise_for_status()

                alerts = _extract_alerts(response.json())
                all_alerts.extend(alerts)

                if len(alerts) < page_size:
                    break

                page += 1

    except httpx.HTTPStatusError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"La API de alertas respondió con HTTP {exc.response.status_code}.",
        ) from exc
    except httpx.RequestError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="No fue posible conectarse con la API de alertas.",
        ) from exc

    return all_alerts

@router.post("/alerts", response_class=FileResponse)
async def generate_alert_report(
    filters: AlertReportFilters,
    request: Request,
    current_user: dict = Depends(
        require_roles("ADMIN", "USERCONSULTA")
    ),
):
    token = request.headers.get("Authorization", "").removeprefix("Bearer ").strip()
    if not token:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="No se recibió el token Bearer.",
        )

    alerts = await _fetch_alerts(token, filters)

    generated_at = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")
    template = template_environment.get_template("alerts_report.html")
    html_content = template.render(
        alerts=alerts,
        generated_at=generated_at,
        generated_by=current_user.get("username") or current_user.get("id", "usuario"),
        filters=filters,
    )

    report_id = str(uuid4())
    file_name = f"alertas-{report_id}.pdf"
    REPORT_STORAGE_PATH.mkdir(parents=True, exist_ok=True)
    file_path = REPORT_STORAGE_PATH / file_name

    try:
        await run_in_threadpool(
            HTML(string=html_content).write_pdf,
            target=str(file_path),
        )
    except Exception as exc:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="No se pudo generar el PDF.",
        ) from exc

    metadata = {
        "reportId": report_id,
        "type": "alerts",
        "fileName": file_name,
        "storagePath": str(file_path),
        "generatedAt": datetime.now(timezone.utc),
        "generatedBy": {
            "userId": current_user.get("id"),
            "username": current_user.get("username"),
            "role": current_user.get("role"),
        },
        "filters": filters.model_dump(),
        "totalAlerts": len(alerts),
    }

    try:
        await request.app.state.database["reports"].insert_one(metadata)
    except Exception as exc:
        file_path.unlink(missing_ok=True)
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="No se pudieron guardar los metadatos del reporte en MongoDB.",
        ) from exc

    return FileResponse(
        path=file_path,
        media_type="application/pdf",
        filename="reporte-alertas-sat-rural.pdf",
        headers={"X-Report-Id": report_id},
    )

@router.get("/history")
async def list_report_history(
    request: Request,
    limit: int = Query(default=20, ge=1, le=100),
    current_user: dict = Depends(
        require_roles("ADMIN", "USERCONSULTA")
    ),
):
    reports = await (
        request.app.state.database["reports"]
        .find(
            {},
            {
                "_id": 0,
                "reportId": 1,
                "type": 1,
                "fileName": 1,
                "generatedAt": 1,
                "generatedBy": 1,
                "filters": 1,
                "totalAlerts": 1,
            },
        )
        .sort("generatedAt", -1)
        .limit(limit)
        .to_list(length=limit)
    )

    return {
        "items": reports,
        "count": len(reports),
    }


@router.get("/{report_id}/download")
async def download_report(
    report_id: str,
    request: Request,
    current_user: dict = Depends(
        require_roles("ADMIN", "USERCONSULTA")
    ),
):
    report = await request.app.state.database["reports"].find_one(
        {"reportId": report_id},
        {"_id": 0},
    )

    if report is None:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Reporte no encontrado.",
        )

    storage_root = REPORT_STORAGE_PATH.resolve()
    file_path = Path(report["storagePath"]).resolve()

    if storage_root not in file_path.parents or not file_path.is_file():
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="El archivo del reporte no está disponible.",
        )

    return FileResponse(
        path=file_path,
        media_type="application/pdf",
        filename=report["fileName"],
    )
import os
import secrets
from datetime import datetime, timezone
from typing import Any

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Request
from pydantic import BaseModel, Field
from pymongo import ReturnDocument

from app.auth import require_admin, require_user

router = APIRouter()


class BinnacleRequest(BaseModel):
    description: str = Field(min_length=1)
    idMovimentType: int
    user: str = ""
    dateHour: datetime


class AuditLogRequest(BaseModel):
    legacyId: int | None = None
    userId: int
    username: str
    fullName: str = ""
    action: str
    entityName: str
    entityId: str
    details: str = ""
    createdAt: datetime | None = None


async def next_id(database: Any, collection_name: str) -> int:
    counter = await database["counters"].find_one_and_update(
        {"_id": collection_name},
        {"$inc": {"value": 1}},
        upsert=True,
        return_document=ReturnDocument.AFTER,
    )
    return counter["value"]


@router.get("/api/binnacle")
async def list_binnacle(
    request: Request,
    limit: int = Query(default=100, ge=1, le=500),
    user: dict = Depends(require_admin),
):
    return await (
        request.app.state.database["binnacle"]
        .find({}, {"_id": 0})
        .sort([("dateHour", -1), ("id", -1)])
        .limit(limit)
        .to_list(length=limit)
    )


@router.post("/api/binnacle", status_code=201)
async def create_binnacle(
    entry: BinnacleRequest,
    request: Request,
    user: dict = Depends(require_user),
):
    database = request.app.state.database
    document = {
        "id": await next_id(database, "binnacle"),
        "description": entry.description.strip(),
        "idMovimentType": entry.idMovimentType,
        "user": user["username"] or entry.user.strip(),
        "dateHour": entry.dateHour,
    }
    await database["binnacle"].insert_one(document)
    return {key: value for key, value in document.items() if key != "_id"}


@router.get("/api/audit-logs")
async def list_audit_logs(
    request: Request,
    userId: int | None = None,
    action: str | None = None,
    entityName: str | None = None,
    fromDate: datetime | None = None,
    toDate: datetime | None = None,
    limit: int = Query(default=100, ge=1, le=500),
    user: dict = Depends(require_admin),
):
    filters: dict[str, Any] = {}
    if userId is not None:
        filters["userId"] = userId
    if action:
        filters["action"] = action
    if entityName:
        filters["entityName"] = entityName
    if fromDate or toDate:
        filters["createdAt"] = {}
        if fromDate:
            filters["createdAt"]["$gte"] = fromDate
        if toDate:
            filters["createdAt"]["$lte"] = toDate

    return await (
        request.app.state.database["audit_logs"]
        .find(filters, {"_id": 0})
        .sort([("createdAt", -1), ("id", -1)])
        .limit(limit)
        .to_list(length=limit)
    )


@router.post("/internal/audit-logs", status_code=201)
async def create_audit_log(
    entry: AuditLogRequest,
    request: Request,
    service_key: str | None = Header(default=None, alias="X-Service-Key"),
):
    expected_key = os.getenv("AUDIT_SERVICE_API_KEY", "")
    if not expected_key or not service_key or not secrets.compare_digest(expected_key, service_key):
        raise HTTPException(status_code=401, detail="Clave de servicio inválida.")

    database = request.app.state.database
    if entry.legacyId is not None:
        existing = await database["audit_logs"].find_one(
            {"legacyId": entry.legacyId}, {"_id": 0}
        )
        if existing:
            return existing
    document = entry.model_dump()
    document["id"] = await next_id(database, "audit_logs")
    document["createdAt"] = entry.createdAt or datetime.now(timezone.utc)
    await database["audit_logs"].insert_one(document)
    return {key: value for key, value in document.items() if key != "_id"}

import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from pymongo import AsyncMongoClient

from app.reports import router as reports_router


@asynccontextmanager
async def lifespan(app: FastAPI):
    mongo_uri = os.getenv("MONGO_URI", "mongodb://mongodb:27017")
    client = AsyncMongoClient(mongo_uri)

    app.state.mongo_client = client
    app.state.database = client["satrural_reports"]

    try:
        await client.admin.command("ping")
        yield
    finally:
        await client.close()


app = FastAPI(
    title="SAT-Rural Report Service",
    lifespan=lifespan,
)

app.include_router(reports_router)


@app.get("/health")
async def health():
    try:
        await app.state.mongo_client.admin.command("ping")
        return {"status": "healthy", "database": "connected"}
    except Exception as exc:
        raise HTTPException(
            status_code=503,
            detail="MongoDB no está disponible",
        ) from exc
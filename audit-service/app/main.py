import os
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException, Request
from pymongo import AsyncMongoClient

from app.routes import router


@asynccontextmanager
async def lifespan(app: FastAPI):
    client = AsyncMongoClient(os.environ["MONGO_URI"])
    app.state.mongo_client = client
    app.state.database = client["satrural_audit"]
    try:
        await client.admin.command("ping")
        yield
    finally:
        await client.close()


app = FastAPI(title="SAT-Rural Audit Service", lifespan=lifespan)
app.include_router(router)


@app.get("/health")
async def health(request: Request):
    try:
        await request.app.state.mongo_client.admin.command("ping")
        return {"status": "healthy"}
    except Exception as exc:
        raise HTTPException(status_code=503, detail="MongoDB no está disponible.") from exc

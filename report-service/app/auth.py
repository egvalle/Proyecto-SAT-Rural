import os
from typing import Any

import jwt
from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jwt import InvalidTokenError


bearer_scheme = HTTPBearer(auto_error=False)

JWT_KEY = os.getenv("JWT_KEY")
JWT_ISSUER = os.getenv("JWT_ISSUER")
JWT_AUDIENCE = os.getenv("JWT_AUDIENCE")

if not JWT_KEY or len(JWT_KEY) < 32:
    raise RuntimeError("JWT_KEY debe existir y tener al menos 32 caracteres.")

if not JWT_ISSUER or not JWT_AUDIENCE:
    raise RuntimeError("JWT_ISSUER y JWT_AUDIENCE deben estar configurados.")


def _get_role(payload: dict[str, Any]) -> str | None:
    # JwtSecurityTokenHandler suele serializar ClaimTypes.Role como "role".
    # Se conserva también la URI por compatibilidad con tokens .NET.
    role = (
        payload.get("role")
        or payload.get("roles")
        or payload.get("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
    )

    if isinstance(role, list):
        return str(role[0]) if role else None

    return str(role) if role is not None else None


async def get_current_user(
    credentials: HTTPAuthorizationCredentials | None = Depends(bearer_scheme),
) -> dict[str, Any]:
    unauthorized = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Se requiere un token Bearer válido.",
        headers={"WWW-Authenticate": "Bearer"},
    )

    if credentials is None or credentials.scheme.lower() != "bearer":
        raise unauthorized

    try:
        payload = jwt.decode(
            credentials.credentials,
            JWT_KEY,
            algorithms=["HS256"],
            issuer=JWT_ISSUER,
            audience=JWT_AUDIENCE,
            options={
                "require": ["sub", "exp", "iss", "aud"],
            },
        )
    except InvalidTokenError:
        raise unauthorized

    return {
        "id": payload["sub"],
        "username": payload.get("unique_name"),
        "name": payload.get("name"),
        "role": _get_role(payload),
    }


def require_roles(*allowed_roles: str):
    async def dependency(
        current_user: dict[str, Any] = Depends(get_current_user),
    ) -> dict[str, Any]:
        if current_user["role"] not in allowed_roles:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail="El rol del usuario no tiene permiso para esta operación.",
            )
        return current_user

    return dependency
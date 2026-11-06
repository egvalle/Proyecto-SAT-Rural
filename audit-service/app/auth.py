import os
from typing import Any

import jwt
from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jwt import InvalidTokenError

bearer_scheme = HTTPBearer(auto_error=False)


def require_user(
    credentials: HTTPAuthorizationCredentials | None = Depends(bearer_scheme),
) -> dict[str, Any]:
    if credentials is None or credentials.scheme.lower() != "bearer":
        raise HTTPException(status_code=401, detail="Se requiere un token Bearer.")

    try:
        payload = jwt.decode(
            credentials.credentials,
            os.environ["JWT_KEY"],
            algorithms=["HS256"],
            issuer=os.environ["JWT_ISSUER"],
            audience=os.environ["JWT_AUDIENCE"],
            options={"require": ["sub", "exp", "iss", "aud"]},
        )
    except (InvalidTokenError, KeyError) as exc:
        raise HTTPException(status_code=401, detail="Token inválido.") from exc

    role = (
        payload.get("role")
        or payload.get("roles")
        or payload.get("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
    )
    if isinstance(role, list):
        role = role[0] if role else None

    return {
        "id": payload["sub"],
        "username": payload.get("unique_name") or payload.get("email"),
        "role": role,
    }


def require_admin(user: dict[str, Any] = Depends(require_user)) -> dict[str, Any]:
    if user["role"] != "ADMIN":
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Acceso denegado.")
    return user

# SAT Rural

Sistema de Alerta Temprana para Riesgos Climáticos en comunidades rurales. Permite monitorear sensores y lecturas, administrar comunidades y reglas, y gestionar alertas.

## Tecnologías

- **Frontend:** Angular 20+, TypeScript, RxJS, Bootstrap 5.3.8, Bootstrap Icons y Nginx.
- **Backend:** ASP.NET Core .NET 10, C#, Web API, Entity Framework Core, JWT y SignalR.
- **Datos e infraestructura:** SQL Server 2022, Docker y Docker Compose.

## Arquitectura y módulos

Aplicación modular full stack: Angular consume la API REST de ASP.NET Core; Entity Framework Core conecta el backend con SQL Server. SignalR habilita actualizaciones en tiempo real.

Módulos principales: **Monitoring**, **Alerts** y **Administration**. Incluyen monitoreo de sensores y lecturas, gestión de comunidades y sensores, reglas y alertas, y administración del sistema.

## Requisitos

- Git
- Docker Desktop con Docker Compose

## Ejecutar con Docker

Desde la carpeta raíz del proyecto (donde está `docker-compose.yml`):

```bash
docker compose up -d --build
```

Al iniciar la API se aplican las migraciones de Entity Framework Core y se ejecuta el seed inicial. El administrador de prueba se crea si aún no existe.

| Servicio | Acceso |
|---|---|
| Frontend | <http://localhost:4200> |
| API | <http://localhost:8080> |
| SQL Server | `localhost:1433` |

## Acceso y roles

La API utiliza autenticación JWT. Usa estas credenciales para iniciar sesión en el frontend:

| Usuario | Contraseña | Rol |
|---|---|---|
| `adminangi@gmail.com` | `adminangi123*` | `ADMIN` |

El proyecto contempla los roles `ADMIN`, `USER` y `USERCONSULTA`.

## Comandos Docker útiles

```bash
docker compose ps                    # Ver estado de los servicios
docker logs sat-rural-api            # Consultar logs de la API
docker compose down                  # Detener los servicios
```

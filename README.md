# SAT-Rural

Sistema de monitoreo y alerta temprana para comunidades rurales. Esta versión ejecuta sus servicios en contenedores Docker y conserva una arquitectura distribuida por servicios.

## Tecnologías

- Frontend: Angular 20 servido por Nginx.
- Gateway: ASP.NET Core 10 y YARP.
- API de monitoreo: ASP.NET Core 10, Entity Framework Core, SQL Server para sensores/lecturas/eventos, PostgreSQL para alertas y reglas, MongoDB para auditoría y SignalR.
- Autenticación: ASP.NET Core 10, JWT y PostgreSQL.
- Comunidades: ASP.NET Core 10, Entity Framework Core y PostgreSQL.
- Reportes: FastAPI, MongoDB, Jinja2 y WeasyPrint; exporta PDF y registra historial.
- Docker Compose administra servicios y volúmenes persistentes.

La migración es gradual: monitoreo, alertas, reglas, lecturas, sensores, eventos y bitácora continúan en la API existente. Autenticación, reportes y comunidades ya cuentan con servicios separados.

## Requisitos

- Docker Desktop con Docker Compose.
- Para desarrollo fuera de Docker: .NET SDK 10, Node.js compatible con Angular 20 y npm.
- MongoDB Compass es opcional para explorar los reportes.

## Configurar y ejecutar con Docker

Desde la raíz del repositorio, crea `.env` a partir de la plantilla solo si aún no existe. No reemplaces tu `.env` actual:

```powershell
if (!(Test-Path .env)) { Copy-Item .env.example .env }
```

La plantilla incluye todas las variables requeridas: `MSSQL_SA_PASSWORD`, `MSSQL_DATABASE`, `JWT_KEY`, `COMMUNITY_DB_USER`, `COMMUNITY_DB_PASSWORD`, `MONGO_ROOT_USERNAME` y `MONGO_ROOT_PASSWORD`. Usa una clave JWT de al menos 32 caracteres y el mismo valor para los servicios que validan o emiten tokens. Las contraseñas de la plantilla son solo para desarrollo local: cámbialas antes de exponer el sistema.

Valida la configuración y construye/inicia el conjunto:

```powershell
docker compose config -q
docker compose up -d --build
docker compose ps
```

La primera compilación puede tardar varios minutos. Abre el frontend en <http://localhost:4200>.

Para reconstruir solo el frontend tras cambios:

```powershell
docker compose up -d --build --force-recreate frontend
```

Para ver los registros de un servicio: `docker compose logs -f gateway` (sustituye `gateway` por `api`, `auth-service`, `community-service` o `report-service`). Para detener los contenedores conservando datos: `docker compose down`. `docker compose down -v` elimina también los volúmenes y los datos persistidos.

## Servicios y direcciones locales

| Servicio | Dirección desde el equipo | Uso |
|---|---|---|
| Frontend | <http://localhost:4200> | Aplicación Angular |
| Gateway | <http://localhost:8081> | Entrada recomendada para las API |
| API de monitoreo | <http://localhost:8080> | API existente |
| AuthService | <http://localhost:8082> | Autenticación y usuarios |
| CommunityService | <http://localhost:8083> | Servicio de comunidades |
| ReportService | <http://localhost:8000> | API FastAPI directa |
| SQL Server | `localhost:1433` | Sensores, lecturas y eventos |
| MongoDB | `localhost:27017` | Auditoría e historial/metadatos de reportes |
| PostgreSQL | Red interna de Docker | Comunidades, autenticación, alertas y reglas |

PostgreSQL y sus bases no publican puertos al equipo anfitrión; los servicios se conectan por la red interna de Compose. Usa el Gateway en `8081` para las llamadas de la aplicación. El cliente Angular tiene configurado `http://localhost:8081` como URL base.

## Rutas principales

- `POST /api/auth/login`, `POST /api/auth/google` y `GET /api/auth/me`: autenticación y perfil, vía Gateway.
- `GET /api/alerts`, `/api/sensors`, `/api/sensor-readings`, `/api/alert-rules`, `/api/events` y `/api/binnacle`: API de monitoreo, vía Gateway; normalmente requieren JWT.
- `GET /api/reports/health`: salud de reportes, sin autenticación.
- `POST /api/reports/alerts`: genera un PDF de alertas. Ejemplo de cuerpo: `{"level":"RED","is_active":true}`. Envía `Authorization: Bearer <token>`.
- `GET /api/reports/history?limit=10`: historial de reportes del usuario autenticado.
- `GET /api/reports/{report_id}/download`: descarga un reporte guardado.
- `GET /community-service/api/communities?page=1&pageSize=10`: integración de CommunityService a través del Gateway, protegida por JWT.

La ruta `/api/communities` existente todavía puede atenderse desde la API de monitoreo. La ruta prefijada `/community-service` es la que dirige al nuevo CommunityService durante la integración.

El acceso con Google requiere habilitar el proveedor Google en Firebase Authentication y agregar el dominio del frontend a los dominios autorizados. Solo pueden ingresar cuentas Google verificadas cuyo correo coincida con el `username` de un usuario SAT Rural activo. La configuración web se encuentra en `frontend/src/environments/environment.ts`; el servicio de autenticación valida las credenciales de Firebase usando `Firebase:ApiKey` en su configuración.

Health checks útiles:

- <http://localhost:8000/health> o <http://localhost:8081/api/reports/health> para reportes.
- <http://localhost:8082/health> para AuthService.
- <http://localhost:8083/health> para CommunityService.

## Usuario de demostración

En una base recién inicializada, el usuario sembrado para demostración es:

- Usuario: `adminangi@gmail.com`
- Contraseña inicial: `adminangi123*`
- Rol: `ADMIN`

La base conserva las cuentas existentes. Si la contraseña fue cambiada previamente, utiliza la contraseña actual; volver a iniciar los contenedores no la restablece. Cambia la contraseña de demostración antes de desplegar el sistema en un entorno accesible públicamente. También existen los roles `USER` y `USERCONSULTA`.

## Ver reportes en MongoDB Compass

Conecta Compass usando la URI siguiente y sustituye los valores por los de tu `.env`:

```text
mongodb://<MONGO_ROOT_USERNAME>:<MONGO_ROOT_PASSWORD>@localhost:27017/?authSource=admin
```

La base usada por el servicio es `satrural_reports` y la colección de historial es `reports`. Si la contraseña contiene caracteres especiales, codifícala para URI.

## Desarrollo local

Para compilar la solución .NET:

```powershell
dotnet build .\backend\SatRural.slnx
```

Para trabajar con Angular localmente:

```powershell
Set-Location .\frontend
npm ci
npm start
```

Con el frontend local, confirma que Gateway y servicios dependientes estén activos. Para ejecutar el conjunto completo y obtener las direcciones configuradas, usa Docker Compose.

## Persistencia y seguridad

SQL Server, PostgreSQL, MongoDB y archivos de reportes usan volúmenes de Docker Compose. Detener o recrear contenedores no elimina esos datos; `docker compose down -v` sí los elimina. No agregues `.env`, tokens JWT ni contraseñas reales al repositorio. Configura secretos nuevos y credenciales seguras para cualquier despliegue.
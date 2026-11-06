# SAT-Rural

Sistema de monitoreo y alerta temprana para comunidades rurales. El entorno local se ejecuta con Docker Compose y expone las API mediante un Gateway en `http://localhost:8081`.

## Servicios

| Servicio | Tecnología | Responsabilidad |
| --- | --- | --- |
| Frontend | Angular 22, Tailwind CSS y Nginx | Panel, simulación y reportes |
| Gateway | ASP.NET Core 10 y YARP | Entrada única, JWT y rutas |
| AuthService | NestJS y PostgreSQL | Usuarios, roles, JWT y acceso OAuth |
| CommunityService | ASP.NET Core 10 y PostgreSQL | Comunidades |
| SensorService | ASP.NET Core 10 y SQL Server | Sensores, lecturas y dashboard |
| AlertService | Spring Boot 4 y PostgreSQL | Reglas, alertas, eventos y avisos Gmail |
| AuditService | FastAPI y MongoDB | Bitácora y auditoría |
| ReportService | FastAPI y MongoDB | PDF e historial de reportes |

Los proyectos de la API y autenticación anteriores ya no forman parte del sistema. La base histórica `SatRuralDb` y los scripts de migración se conservan para consultar o verificar los datos antiguos.

## Ejecutar en localhost

Necesitas Docker Desktop con Docker Compose. Desde la raíz del proyecto, copia `.env.example` a `.env` y completa los valores vacíos. `JWT_KEY` debe tener al menos 32 caracteres. Las claves de dispositivo, AlertService y AuditService deben ser valores diferentes y privados. No publiques `.env`.

```powershell
Copy-Item .env.example .env
docker compose config -q
docker compose up -d --build
docker compose ps
```

La primera construcción puede tardar varios minutos. Abre `http://localhost:4200`. Los servicios Spring Boot y las bases pueden necesitar unos segundos adicionales antes de responder.

Para revisar un servicio usa `docker compose logs --tail=80 gateway` y cambia `gateway` por su nombre. Para detener el sistema sin borrar datos usa `docker compose down`. No uses `docker compose down -v` si deseas conservar las bases y los PDF.

## Direcciones locales

| Servicio | Dirección |
| --- | --- |
| Frontend | `http://localhost:4200` |
| Gateway | `http://localhost:8081` |
| AuthService | `http://localhost:8086` |
| CommunityService | `http://localhost:8083` |
| SensorService | `http://localhost:8084` |
| AlertService | `http://localhost:8085` |
| AuditService | `http://localhost:8001` |
| ReportService | `http://localhost:8000` |
| SQL Server | `localhost:1433` |
| PostgreSQL de alertas | `localhost:5433` |
| PostgreSQL de usuarios | `localhost:5434` |
| MongoDB | `localhost:27017` |

PostgreSQL de comunidades solo está disponible en la red de Compose. El frontend usa el Gateway para acceder a los servicios.

## Comprobar el arranque

```powershell
Invoke-RestMethod http://localhost:8081/health
Invoke-RestMethod http://localhost:8081/api/reports/health
Invoke-RestMethod http://localhost:8084/health
Invoke-RestMethod http://localhost:8085/actuator/health
```

`/api/auth/login` y `/api/auth/me` gestionan el acceso. Los datos del panel salen de `/api/monitoring/dashboard`, `/api/sensors`, `/api/sensor-readings` y `/api/alerts`. Las rutas de datos requieren JWT. El ingreso de lecturas de humedad por `POST /api/sensors/readings` exige `X-Device-Key` y está preparado para el ESP32 con DHT11; la conexión física se realizará después. Los demás sensores se simulan cada cinco segundos.

El sistema permite consultar reportes de comunidades, sensores, lecturas, alertas, eventos, estado de sensores, estadísticas climáticas y resumen ejecutivo. El reporte de lecturas requiere un rango de fechas. Los tres roles `ADMIN`, `OPERATOR` y `VIEWER` pueden descargar PDF.

## Cuenta existente de demostración

En la base migrada del proyecto existe `adminangi@gmail.com` con rol `ADMIN`. Usa la contraseña habitual de esa cuenta. La migración conserva el hash de contraseña, así que reconstruir contenedores no la restablece. En una base nueva, el registro público crea usuarios `VIEWER`; la cuenta administradora existente debe migrarse antes de usarla.

## Migrar una instalación anterior

Estos comandos son para una instalación que ya tiene datos en la API anterior. Conservan usuarios, comunidades, sensores, reglas, alertas, eventos y auditoría. La migración de sensores conserva las lecturas nuevas y descarta las lecturas simuladas copiadas desde la base antigua. Ejecuta los pasos desde la raíz y revisa los conteos antes de activar el nuevo Gateway:

```powershell
docker stop sat-rural-api sat-rural-auth-service
docker compose run --rm --build auth-migrate
docker compose run --rm sensor-migrate
docker compose run --rm data-migrate
docker compose run --rm audit-migrate
docker compose run --rm verify-migration
docker compose up -d --build
```

No ejecutes la migración contra bases de otro proyecto. Las bases y los archivos de reportes están guardados en volúmenes de Docker.

## Correo y acceso externo

Para avisos de alertas, define en `.env` `GMAIL_USERNAME`, `GMAIL_APP_PASSWORD` y `ALERT_NOTIFICATIONS_RECIPIENTS`. El correo se envía al activarse o escalar una alerta YELLOW, ORANGE o RED. No se reenvía por cada lectura que mantiene el mismo nivel.

Google y GitHub necesitan sus Client ID y Client Secret en `.env`. Sin esas credenciales, el inicio de sesión con contraseña sigue disponible. No incluyas credenciales reales en capturas, documentación ni control de versiones.

## Desarrollo

```powershell
dotnet build .\backend\SatRural.slnx
Set-Location .\frontend
npm ci
npm start
```

AlertService se compila con `mvnw.cmd package` desde `backend/SatRural.AlertService`. La ejecución completa y las bases se gestionan con Docker Compose.

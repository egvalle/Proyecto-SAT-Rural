$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$backupRoot = Join-Path $projectRoot 'deploy-backups'
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

function Invoke-Docker {
    param([string[]] $DockerArgs)
    & docker @DockerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Falló el comando Docker: $($DockerArgs[0]) $($DockerArgs[1])"
    }
}

Push-Location $projectRoot
try {
    Invoke-Docker @('compose', 'exec', '-T', 'auth-db', 'pg_dump', '-U', 'satrural_auth', '-Fc', '--no-owner', '--no-acl', '-f', '/tmp/satrural_auth.dump', 'satrural_auth')
    Invoke-Docker @('cp', 'sat-rural-auth-db:/tmp/satrural_auth.dump', (Join-Path $backupRoot 'satrural_auth.dump'))

    Invoke-Docker @('compose', 'exec', '-T', 'community-db', 'pg_dump', '-U', 'satrural_community', '-Fc', '--no-owner', '--no-acl', '-f', '/tmp/satrural_community.dump', 'satrural_community')
    Invoke-Docker @('cp', 'sat-rural-community-db:/tmp/satrural_community.dump', (Join-Path $backupRoot 'satrural_community.dump'))

    Invoke-Docker @('compose', 'exec', '-T', 'alert-db', 'pg_dump', '-U', 'satrural_alerts', '-Fc', '--no-owner', '--no-acl', '-f', '/tmp/satrural_alerts.dump', 'satrural_alerts')
    Invoke-Docker @('cp', 'sat-rural-alert-db:/tmp/satrural_alerts.dump', (Join-Path $backupRoot 'satrural_alerts.dump'))

    Invoke-Docker @('cp', (Join-Path $PSScriptRoot 'backup-mongo.sh'), 'sat-rural-mongodb:/tmp/backup-mongo.sh')
    Invoke-Docker @('exec', 'sat-rural-mongodb', 'sh', '/tmp/backup-mongo.sh')
    Invoke-Docker @('cp', 'sat-rural-mongodb:/tmp/satrural_audit.archive.gz', (Join-Path $backupRoot 'satrural_audit.archive.gz'))
    Invoke-Docker @('cp', 'sat-rural-mongodb:/tmp/satrural_reports.archive.gz', (Join-Path $backupRoot 'satrural_reports.archive.gz'))

    Invoke-Docker @('cp', (Join-Path $PSScriptRoot 'backup-sqlserver.sh'), 'sat-rural-sqlserver:/tmp/backup-sqlserver.sh')
    Invoke-Docker @('exec', 'sat-rural-sqlserver', 'sh', '/tmp/backup-sqlserver.sh')
    Invoke-Docker @('cp', 'sat-rural-sqlserver:/var/opt/mssql/data/satrural_sensors.bak', (Join-Path $backupRoot 'satrural_sensors.bak'))

    Invoke-Docker @('cp', 'sat-rural-report-service:/app/storage', (Join-Path $backupRoot 'report-files'))

    $archive = Join-Path $projectRoot 'sat-rural-data.tar.gz'
    tar -czf $archive -C $backupRoot .
    if ($LASTEXITCODE -ne 0) {
        throw 'No se pudo comprimir el respaldo.'
    }

    Get-ChildItem -LiteralPath $backupRoot -File | Select-Object Name, Length
    Write-Host "Respaldos creados en $backupRoot"
    Write-Host "Paquete para subir a la VM: $archive"
}
finally {
    Pop-Location
}

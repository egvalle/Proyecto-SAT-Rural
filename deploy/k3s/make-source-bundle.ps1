$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$archive = Join-Path $projectRoot 'sat-rural-source.tar.gz'

Push-Location $projectRoot
try {
    tar -czf $archive `
        --exclude='node_modules' `
        --exclude='dist' `
        --exclude='bin' `
        --exclude='obj' `
        --exclude='target' `
        --exclude='.angular' `
        --exclude='__pycache__' `
        auth-service `
        backend/SatRural.AlertService `
        backend/SatRural.CommunityService `
        backend/SatRural.Gateway `
        backend/SatRural.SensorService `
        frontend `
        report-service `
        audit-service `
        deploy/k3s

    if ($LASTEXITCODE -ne 0) {
        throw 'No se pudo crear el paquete de código.'
    }

    Write-Host "Paquete creado: $archive"
    Write-Host 'El archivo .env y las bases de datos no están incluidos.'
}
finally {
    Pop-Location
}

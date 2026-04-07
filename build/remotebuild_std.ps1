# Stoppe Skript bei Fehlern
$ErrorActionPreference = "Stop"

# ================================
# Einstellungen
# ================================
$RemoteUser = "daniel"
$RemoteHost = "csl"
$RemoteDir  = "~/composer"
$ImageName  = "stocktv"

Write-Host "Ermittle neuestes lokales Image..."

# 1. Alle Tags holen
$tags = docker images $ImageName --format "{{.Tag}}"

if (-not $tags) {
    Write-Host "Kein Image mit dem Namen '$ImageName' gefunden!" -ForegroundColor Red
    exit 1
}

# 2. Höchsten Tag bestimmen (Version-Sortierung)
$LatestTag = $tags | Sort-Object { $_ } -Descending | Select-Object -First 1

Write-Host "Gefundenes neuestes Image: ${ImageName}:${LatestTag}"

# TAR-Dateiname
$TarFile = "${ImageName}_${LatestTag}.tar"

# ================================
# Image exportieren
# ================================
Write-Host "Exportiere Image nach $TarFile..."
docker save -o $TarFile "${ImageName}:${LatestTag}"

# ================================
# TAR auf Server kopieren
# ================================
Write-Host "Kopiere TAR auf den Server..."
scp $TarFile "${RemoteUser}@${RemoteHost}:${RemoteDir}/"

# ================================
# Image auf Server laden
# ================================
Write-Host "Lade Image auf dem Server..."
ssh "${RemoteUser}@${RemoteHost}" "docker load -i ${RemoteDir}/${TarFile}"

# ================================
# docker-compose neu starten
# ================================
Write-Host "Starte docker-compose neu..."
ssh "${RemoteUser}@${RemoteHost}" "cd ${RemoteDir} && STOCKTV_TAG=${LatestTag} docker compose up -d --force-recreate stocktvBahn1 stocktvBahn2 stocktvBahn3 stocktvBahn4"

# ================================
# Cleanup
# ================================
Write-Host "Entferne TAR-Datei lokal..."
Remove-Item $TarFile -Force

Write-Host "Entferne TAR-Datei auf dem Server..."
ssh "${RemoteUser}@${RemoteHost}" "rm -f ${RemoteDir}/${TarFile}"

Write-Host "Deployment abgeschlossen!" -ForegroundColor Green

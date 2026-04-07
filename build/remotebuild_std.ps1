# ============================================
#  Build + Deployment Script for StockTV
# ============================================

$ErrorActionPreference = "Stop"

# ================================
# Settings
# ================================
$RemoteUser = "daniel"
$RemoteHost = "csl"
$RemoteDir  = "~/composer"
$ImageName  = "stocktv"

Write-Host "=========================================="
Write-Host "   BUILD + DEPLOY: StockTV"
Write-Host "==========================================`n"

# ================================
# Buildx init
# ================================
Write-Host "Initialize Docker Buildx..."

$builderExists = docker buildx ls | Select-String "devbuilder"

if (-not $builderExists) {
    Write-Host "Create new Buildx builder 'devbuilder'..."
    docker buildx create --name devbuilder --use | Out-Null
} else {
    Write-Host "Buildx builder 'devbuilder' already exists - using it..."
    docker buildx use devbuilder | Out-Null
}

docker buildx inspect --bootstrap | Out-Null

# ================================
# Version tag
# ================================
$VERSION_TAG = "StockTV_v" + (Get-Date -Format "yyyy.MM.dd.HHmm")
Write-Host "Using tag: $VERSION_TAG"

# ================================
# Build image
# ================================
Write-Host "`nBuilding Docker image..."

docker buildx build `
    --platform linux/amd64 `
    -f build/Dockerfile `
    -t "${ImageName}:${VERSION_TAG}" `
    --load `
    .

Write-Host "Build done: ${ImageName}:${VERSION_TAG}"

# ================================
# Export image
# ================================
$TarFile = "${ImageName}_${VERSION_TAG}.tar"
Write-Host "`nExporting image to $TarFile..."

docker save -o $TarFile "${ImageName}:${VERSION_TAG}"

# ================================
# Build SSH/SCP strings
# ================================
$remoteUserHost = "$RemoteUser@$RemoteHost"
$remotePath = "${remoteUserHost}:${RemoteDir}/"


# ================================
# Copy TAR to server
# ================================
Write-Host "Copying TAR to server..."

scp $TarFile $remotePath

# ================================
# Remote commands
# ================================
$remoteCommand = "docker load -i $RemoteDir/$TarFile; cd $RemoteDir; STOCKTV_TAG=$VERSION_TAG docker compose up -d --force-recreate stocktvBahn1 stocktvBahn2 stocktvBahn3 stocktvBahn4; rm -f $RemoteDir/$TarFile"

# ================================
# Execute remote
# ================================
Write-Host "Running deployment on server..."

ssh $remoteUserHost $remoteCommand

# ================================
# Cleanup local
# ================================
Write-Host "Removing local TAR file..."
Remove-Item $TarFile -Force

# ================================
# Start local container with new image
# ================================
Write-Host "Starting local container with new image..."

# Set environment variable for docker compose
$env:STOCKTV_IMAGE = "${ImageName}:${VERSION_TAG}"

# Change to the directory where THIS script is located
Set-Location -Path $PSScriptRoot

# Start local containers
docker compose up -d --build


Write-Host "`n=========================================="
Write-Host "Deployment completed successfully!"
Write-Host "=========================================="

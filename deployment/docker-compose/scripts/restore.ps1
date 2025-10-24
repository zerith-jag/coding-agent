# Restore volumes from backup
# Usage: ./restore.ps1 -BackupTimestamp "yyyyMMdd-HHmmss"
# Example: ./restore.ps1 -BackupTimestamp "20251024-120000"

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupTimestamp
)

$ErrorActionPreference = "Stop"

$backupDir = Join-Path $PSScriptRoot ".." "backups"

if (-not (Test-Path $backupDir)) {
    Write-Host "✗ Backup directory not found: $backupDir" -ForegroundColor Red
    exit 1
}

Write-Host "`n⚠️  WARNING: This will restore data and overwrite current data!" -ForegroundColor Yellow
Write-Host "Backup timestamp: $BackupTimestamp" -ForegroundColor Cyan
$confirmation = Read-Host "`nAre you sure you want to continue? (yes/no)"

if ($confirmation -ne "yes") {
    Write-Host "Restore cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host "`n🔄 Starting restore process..." -ForegroundColor Cyan

# Check if services are running
Write-Host "`n📋 Checking service status..." -ForegroundColor Yellow
$servicesRunning = docker compose -f docker-compose.yml ps --services --filter "status=running"

if ($servicesRunning) {
    Write-Host "Services are running. They need to be stopped for restore." -ForegroundColor Yellow
    $stopConfirm = Read-Host "Stop services now? (yes/no)"
    
    if ($stopConfirm -eq "yes") {
        Write-Host "Stopping services..." -ForegroundColor Yellow
        docker compose -f docker-compose.yml down
        Write-Host "✓ Services stopped" -ForegroundColor Green
    } else {
        Write-Host "✗ Cannot restore while services are running. Exiting." -ForegroundColor Red
        exit 1
    }
}

# Start services
Write-Host "`n🚀 Starting services..." -ForegroundColor Yellow
docker compose -f docker-compose.yml up -d

# Wait for services to be healthy
Write-Host "⏳ Waiting for services to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Restore PostgreSQL
$postgresBackupFile = Join-Path $backupDir "postgres-$BackupTimestamp.sql"
if (Test-Path $postgresBackupFile) {
    Write-Host "`n📦 Restoring PostgreSQL..." -ForegroundColor Yellow
    Get-Content $postgresBackupFile | docker compose -f docker-compose.yml exec -T postgres psql -U postgres
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ PostgreSQL restored successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ PostgreSQL restore failed" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  PostgreSQL backup file not found: $postgresBackupFile" -ForegroundColor Yellow
}

# Restore Redis
$redisBackupFile = Join-Path $backupDir "redis-$BackupTimestamp.rdb"
if (Test-Path $redisBackupFile) {
    Write-Host "`n📦 Restoring Redis..." -ForegroundColor Yellow
    
    # Stop Redis
    docker compose -f docker-compose.yml stop redis
    
    # Copy backup file
    docker compose -f docker-compose.yml cp $redisBackupFile redis:/data/dump.rdb
    
    # Restart Redis
    docker compose -f docker-compose.yml start redis
    
    Write-Host "✓ Redis restored successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  Redis backup file not found: $redisBackupFile" -ForegroundColor Yellow
}

# Restore RabbitMQ
$rabbitmqBackupFile = Join-Path $backupDir "rabbitmq-$BackupTimestamp.json"
if (Test-Path $rabbitmqBackupFile) {
    Write-Host "`n📦 Restoring RabbitMQ definitions..." -ForegroundColor Yellow
    
    Get-Content $rabbitmqBackupFile | docker compose -f docker-compose.yml exec -T rabbitmq rabbitmqadmin import
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ RabbitMQ restored successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ RabbitMQ restore failed" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  RabbitMQ backup file not found: $rabbitmqBackupFile" -ForegroundColor Yellow
}

Write-Host "`n✅ Restore process completed!" -ForegroundColor Green
Write-Host "`n📋 Service status:" -ForegroundColor Cyan
docker compose -f docker-compose.yml ps

Write-Host "`n✨ Done!" -ForegroundColor Green

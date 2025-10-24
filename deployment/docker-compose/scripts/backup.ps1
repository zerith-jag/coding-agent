# Backup all volumes
# Usage: ./backup.ps1

$ErrorActionPreference = "Stop"

# Create backups directory if it doesn't exist
$backupDir = Join-Path $PSScriptRoot ".." "backups"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
    Write-Host "✓ Created backups directory: $backupDir" -ForegroundColor Green
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "`n🔄 Starting backup process..." -ForegroundColor Cyan
Write-Host "Timestamp: $timestamp`n" -ForegroundColor Gray

# Backup PostgreSQL
Write-Host "📦 Backing up PostgreSQL..." -ForegroundColor Yellow
$postgresBackupFile = Join-Path $backupDir "postgres-$timestamp.sql"
docker compose -f docker-compose.yml exec -T postgres pg_dumpall -U postgres > $postgresBackupFile

if ($LASTEXITCODE -eq 0) {
    $fileSize = (Get-Item $postgresBackupFile).Length / 1KB
    Write-Host "✓ PostgreSQL backup created: postgres-$timestamp.sql ($([math]::Round($fileSize, 2)) KB)" -ForegroundColor Green
} else {
    Write-Host "✗ PostgreSQL backup failed" -ForegroundColor Red
    exit 1
}

# Backup Redis (RDB snapshot)
Write-Host "`n📦 Backing up Redis..." -ForegroundColor Yellow
docker compose -f docker-compose.yml exec -T redis redis-cli --no-auth-warning -a redis SAVE | Out-Null

if ($LASTEXITCODE -eq 0) {
    $redisBackupFile = Join-Path $backupDir "redis-$timestamp.rdb"
    docker compose -f docker-compose.yml cp redis:/data/dump.rdb $redisBackupFile
    
    if (Test-Path $redisBackupFile) {
        $fileSize = (Get-Item $redisBackupFile).Length / 1KB
        Write-Host "✓ Redis backup created: redis-$timestamp.rdb ($([math]::Round($fileSize, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Host "✗ Redis backup copy failed" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Redis backup failed" -ForegroundColor Red
}

# Backup RabbitMQ definitions
Write-Host "`n📦 Backing up RabbitMQ definitions..." -ForegroundColor Yellow
$rabbitmqBackupFile = Join-Path $backupDir "rabbitmq-$timestamp.json"

try {
    $rabbitmqDefs = docker compose -f docker-compose.yml exec -T rabbitmq rabbitmqadmin export
    $rabbitmqDefs | Out-File -FilePath $rabbitmqBackupFile -Encoding utf8
    
    $fileSize = (Get-Item $rabbitmqBackupFile).Length / 1KB
    Write-Host "✓ RabbitMQ backup created: rabbitmq-$timestamp.json ($([math]::Round($fileSize, 2)) KB)" -ForegroundColor Green
} catch {
    Write-Host "✗ RabbitMQ backup failed: $_" -ForegroundColor Red
}

Write-Host "`n✅ Backup process completed!" -ForegroundColor Green
Write-Host "Backup location: $backupDir" -ForegroundColor Cyan

# List all backups
Write-Host "`n📋 Available backups:" -ForegroundColor Cyan
Get-ChildItem -Path $backupDir | Sort-Object LastWriteTime -Descending | Select-Object -First 10 | Format-Table Name, Length, LastWriteTime -AutoSize

# Cleanup old backups (keep last 30 days)
Write-Host "`n🧹 Cleaning up old backups (older than 30 days)..." -ForegroundColor Yellow
$oldBackups = Get-ChildItem -Path $backupDir | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }

if ($oldBackups.Count -gt 0) {
    $oldBackups | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "  Removed: $($_.Name)" -ForegroundColor Gray
    }
    Write-Host "✓ Cleaned up $($oldBackups.Count) old backup(s)" -ForegroundColor Green
} else {
    Write-Host "  No old backups to clean up" -ForegroundColor Gray
}

Write-Host "`n✨ Done!" -ForegroundColor Green

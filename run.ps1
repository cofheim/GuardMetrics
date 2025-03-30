#!/usr/bin/env pwsh

# Проверка наличия Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker не установлен. Пожалуйста, установите Docker и Docker Compose"
    exit 1
}

# Создание защищенных паролей, если не установлены
if (-not $env:POSTGRES_PASSWORD) {
    $env:POSTGRES_PASSWORD = [System.Guid]::NewGuid().ToString("N").Substring(0, 16)
    Write-Host "Генерация пароля PostgreSQL: $env:POSTGRES_PASSWORD"
}

if (-not $env:REDIS_PASSWORD) {
    $env:REDIS_PASSWORD = [System.Guid]::NewGuid().ToString("N").Substring(0, 16)
    Write-Host "Генерация пароля Redis: $env:REDIS_PASSWORD"
}

if (-not $env:JWT_SECRET) {
    $env:JWT_SECRET = [System.Guid]::NewGuid().ToString("N") + [System.Guid]::NewGuid().ToString("N")
    Write-Host "Генерация JWT секретного ключа: $env:JWT_SECRET"
}

# Использование предоставленных API ключей
$env:VIRUSTOTAL_API_KEY = "02852f4c4b4e046c859634fedb75617ad5ab0157a8b0cf94825c27b0bfa6b417"
$env:TELEGRAM_BOT_TOKEN = "6782495011:AAG_fEsMcW-hv4G3RO50QvXJrx01VWMHJ_8"
$env:TELEGRAM_CHAT_ID = "1628591115"

# Запуск базовых сервисов
Write-Host "Запуск PostgreSQL, Redis и Grafana..."
docker-compose up -d postgres redis grafana

# Применение миграций
Write-Host "Настройка базы данных..."
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=guardmetrics;Username=postgres;Password=$env:POSTGRES_PASSWORD"
$env:ConnectionStrings__HangfireConnection = "Host=localhost;Database=guardmetrics_hangfire;Username=postgres;Password=$env:POSTGRES_PASSWORD"

try {
    # Проверка установлен ли уже dotnet-ef
    & dotnet tool list --global | Select-String -Pattern "dotnet-ef"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Установка dotnet-ef..."
        & dotnet tool install --global dotnet-ef --version 6.0.0
    }
    
    Write-Host "Применение миграций..."
    & dotnet ef database update --framework net6.0
}
catch {
    Write-Error "Ошибка при применении миграций: $_"
}

# Запуск основного сервиса
Write-Host "Запуск GuardMetrics..."
docker-compose up -d guardmetrics

Write-Host @"

GuardMetrics система запущена!

Доступ к интерфейсам:
- API Swagger: http://localhost:5000/swagger
- Панель Hangfire: http://localhost:5000/hangfire
- Grafana: http://localhost:3000 (admin/admin)

Используемые ключи:
- JWT Secret: $env:JWT_SECRET
- PostgreSQL Password: $env:POSTGRES_PASSWORD
- Redis Password: $env:REDIS_PASSWORD
- VirusTotal API Key: $env:VIRUSTOTAL_API_KEY
- Telegram Bot Token: $env:TELEGRAM_BOT_TOKEN
- Telegram Chat ID: $env:TELEGRAM_CHAT_ID

"@ 
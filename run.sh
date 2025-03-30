#!/bin/bash

# Проверка наличия Docker
if ! command -v docker &> /dev/null; then
    echo "Docker не установлен. Пожалуйста, установите Docker и Docker Compose"
    exit 1
fi

# Создание защищенных паролей, если не установлены
if [ -z "$POSTGRES_PASSWORD" ]; then
    export POSTGRES_PASSWORD=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 16 | head -n 1)
    echo "Генерация пароля PostgreSQL: $POSTGRES_PASSWORD"
fi

if [ -z "$REDIS_PASSWORD" ]; then
    export REDIS_PASSWORD=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 16 | head -n 1)
    echo "Генерация пароля Redis: $REDIS_PASSWORD"
fi

if [ -z "$JWT_SECRET" ]; then
    export JWT_SECRET=$(cat /dev/urandom | tr -dc 'a-zA-Z0-9' | fold -w 32 | head -n 1)
    echo "Генерация JWT секретного ключа: $JWT_SECRET"
fi

# Использование предоставленных API ключей
export VIRUSTOTAL_API_KEY="02852f4c4b4e046c859634fedb75617ad5ab0157a8b0cf94825c27b0bfa6b417"
export TELEGRAM_BOT_TOKEN="6782495011:AAG_fEsMcW-hv4G3RO50QvXJrx01VWMHJ_8"
export TELEGRAM_CHAT_ID="1628591115"

# Запуск базовых сервисов
echo "Запуск PostgreSQL, Redis и Grafana..."
docker-compose up -d postgres redis grafana

# Ожидание готовности сервисов
echo "Ожидание готовности PostgreSQL..."
until docker-compose exec postgres pg_isready -U postgres > /dev/null 2>&1; do
    sleep 1
done

# Применение миграций
echo "Настройка базы данных..."
export ConnectionStrings__DefaultConnection="Host=localhost;Database=guardmetrics;Username=postgres;Password=$POSTGRES_PASSWORD"
export ConnectionStrings__HangfireConnection="Host=localhost;Database=guardmetrics_hangfire;Username=postgres;Password=$POSTGRES_PASSWORD"

# Проверка установлен ли уже dotnet-ef
if ! dotnet tool list --global | grep -q "dotnet-ef"; then
    echo "Установка dotnet-ef..."
    dotnet tool install --global dotnet-ef --version 6.0.0
fi

echo "Применение миграций..."
dotnet ef database update --framework net6.0

# Запуск основного сервиса
echo "Запуск GuardMetrics..."
docker-compose up -d guardmetrics

cat << EOF

GuardMetrics система запущена!

Доступ к интерфейсам:
- API Swagger: http://localhost:5000/swagger
- Панель Hangfire: http://localhost:5000/hangfire
- Grafana: http://localhost:3000 (admin/admin)

Используемые ключи:
- JWT Secret: $JWT_SECRET
- PostgreSQL Password: $POSTGRES_PASSWORD
- Redis Password: $REDIS_PASSWORD
- VirusTotal API Key: $VIRUSTOTAL_API_KEY
- Telegram Bot Token: $TELEGRAM_BOT_TOKEN
- Telegram Chat ID: $TELEGRAM_CHAT_ID

EOF 
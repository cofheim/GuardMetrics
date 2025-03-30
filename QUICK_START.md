# Быстрый старт GuardMetrics

Это руководство поможет вам быстро запустить и протестировать систему GuardMetrics.

## Шаг 1: Подготовка окружения

### Необходимые компоненты:
- Docker и Docker Compose
- .NET 6.0 SDK
- PowerShell или Bash

## Шаг 2: Клонирование репозитория

```bash
git clone https://github.com/your-username/GuardMetrics.git
cd GuardMetrics
```

## Шаг 3: Запуск системы

### Windows:

```powershell
# Запуск скрипта автоматической настройки
.\run.ps1
```

### Linux/macOS:

```bash
# Сделать скрипт исполняемым
chmod +x run.sh

# Запуск скрипта автоматической настройки
./run.sh
```

## Шаг 4: Проверка работоспособности

После успешного запуска, вы получите доступ к:

- **API Swagger**: http://localhost:5000/swagger
- **Панель Hangfire**: http://localhost:5000/hangfire
- **Grafana**: http://localhost:3000 (login: admin, password: admin)

## Шаг 5: Тестирование API

### 1. Получение JWT токена

```bash
curl -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"ApiKey":"your_api_key","AgentId":"agent1"}'
```

Ответ:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsIn...",
  "expiration": "2023-06-01T13:00:00Z"
}
```

### 2. Отправка метрик процесса

```bash
curl -X POST http://localhost:5000/api/metrics/process \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsIn..." \
  -H "Content-Type: application/json" \
  -d '{
    "ProcessName": "chrome",
    "ProcessId": 1234,
    "CpuUsagePercent": 5.2,
    "MemoryUsageBytes": 102400,
    "FileHash": "abcd1234",
    "Timestamp": "2023-06-01T12:00:00Z"
  }'
```

### 3. Отправка сетевых метрик

```bash
curl -X POST http://localhost:5000/api/metrics/network \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsIn..." \
  -H "Content-Type: application/json" \
  -d '{
    "ProcessName": "chrome",
    "LocalAddress": "127.0.0.1",
    "LocalPort": 8080,
    "RemoteAddress": "8.8.8.8",
    "RemotePort": 443,
    "Protocol": "TCP",
    "BytesSent": 1024,
    "BytesReceived": 2048,
    "Timestamp": "2023-06-01T12:00:00Z"
  }'
```

### 4. Отправка метрик безопасности

```bash
curl -X POST http://localhost:5000/api/metrics/security \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsIn..." \
  -H "Content-Type: application/json" \
  -d '{
    "AgentId": "agent1",
    "Source": "Windows Security",
    "Type": "UserLogin",
    "Name": "Successful login",
    "Value": 1,
    "Description": "User logged in successfully",
    "Timestamp": "2023-06-01T12:05:00Z",
    "Metadata": {
      "username": "john.doe",
      "ipAddress": "192.168.1.100",
      "loginType": "interactive"
    }
  }'
```

### 5. Проверка состояния системы

```bash
curl -X GET http://localhost:5000/health
```

Ожидаемый ответ:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "PostgreSQL",
      "status": "Healthy",
      "description": "PostgreSQL connection is working"
    },
    {
      "name": "Redis",
      "status": "Healthy",
      "description": "Redis connection is working"
    }
  ],
  "timestamp": "2023-06-01T12:10:00Z"
}
```

## Шаг 6: Проверка уведомлений в Telegram

1. Отправьте подозрительную метрику (например, с высоким использованием CPU):

```bash
curl -X POST http://localhost:5000/api/metrics/process \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsIn..." \
  -H "Content-Type: application/json" \
  -d '{
    "ProcessName": "xmrig",
    "ProcessId": 5678,
    "CpuUsagePercent": 95.2,
    "MemoryUsageBytes": 1024000,
    "FileHash": "abcd1234",
    "Timestamp": "2023-06-01T12:15:00Z"
  }'
```

2. Проверьте, что уведомление появилось в указанном Telegram чате.

## Шаг 7: Проверка Grafana дашбордов

1. Откройте Grafana: http://localhost:3000
2. Войдите с логином `admin` и паролем `admin`
3. Перейдите в раздел Dashboards и выберите один из доступных дашбордов:
   - System Metrics
   - Process Metrics
   - Network Activity
   - Security Metrics

## Шаг 8: Остановка системы

```bash
docker-compose down
```

С этими параметрами данные сохраняются в Docker volumes и будут доступны при следующем запуске.

Для полной очистки всех данных:

```bash
docker-compose down -v
```

## Устранение неполадок

### Проблема: Миграции не применяются

**Решение:**
```bash
dotnet tool install --global dotnet-ef --version 6.0.0
dotnet ef database update --framework net6.0
```

### Проблема: Не удается подключиться к API

**Решение:**
Проверьте, что контейнер с GuardMetrics запущен:
```bash
docker ps | grep guardmetrics
```

### Проблема: Telegram уведомления не приходят

**Решение:**
Проверьте, что токен бота и ID чата правильно указаны в `.env` файле или переменных окружения. 
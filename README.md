# GuardMetrics

Система мониторинга безопасности для Windows и Linux, анализирующая процессы, сетевые соединения и системные события безопасности.

## Возможности

- Мониторинг процессов и их ресурсов
- Мониторинг сетевых соединений
- Анализ метрик безопасности
- Обнаружение аномалий с помощью ML.NET
- Проверка файлов через VirusTotal API
- Уведомления о угрозах через Telegram
- Хранение и визуализация данных с Grafana

## Требования

- .NET 6.0 SDK или новее
- PostgreSQL (для хранения метрик)
- Redis (для кэширования и обмена сообщениями)
- Docker и Docker Compose (опционально, но рекомендуется)

## Установка

### Без Docker

1. Клонируйте репозиторий и перейдите в его директорию

```bash
git clone https://github.com/yourusername/GuardMetrics.git
cd GuardMetrics
```

2. Настройте PostgreSQL и создайте базы данных

```bash
createdb guardmetrics
createdb guardmetrics_hangfire
```

3. Настройте Redis
   - Для Windows: установите Redis из [Redis Windows](https://github.com/tporadowski/redis/releases)
   - Для Linux: `sudo apt install redis-server`

4. Настройте переменные окружения или appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=guardmetrics;Username=postgres;Password=postgres",
    "Redis": "localhost:6379",
    "HangfireConnection": "Host=localhost;Database=guardmetrics_hangfire;Username=postgres;Password=postgres"
  },
  "JWT": {
    "Secret": "ваш_секретный_ключ",
    "ValidIssuer": "http://localhost:5000",
    "ValidAudience": "http://localhost:4200",
    "TokenValidityInMinutes": 1440
  },
  "VirusTotal": {
    "ApiKey": "ваш_api_ключ_virustotal",
    "ApiUrl": "https://www.virustotal.com/vtapi/v2"
  },
  "Telegram": {
    "BotToken": "ваш_токен_telegram_бота",
    "ChatId": "ваш_chat_id"
  }
}
```

5. Выполните миграции базы данных и запустите приложение

```bash
dotnet ef database update
dotnet run
```

### С Docker

1. Клонируйте репозиторий и перейдите в его директорию

```bash
git clone https://github.com/yourusername/GuardMetrics.git
cd GuardMetrics
```

2. Настройте файл .env с вашими параметрами и запустите Docker Compose:

```bash
docker-compose up -d
```

## Использование

1. Получите токен доступа через `/api/auth/token`
2. Отправляйте метрики через соответствующие API-эндпоинты
3. Просматривайте данные в Grafana (порт 3000)
4. Управляйте фоновыми задачами через Hangfire Dashboard (/hangfire)

## Поддерживаемые агенты для сбора метрик

- Windows Agent (.NET)
- Linux Agent (Python или .NET)
- Docker Agent (для контейнеризованных приложений)

## Лицензия

MIT 
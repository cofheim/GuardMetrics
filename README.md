# GuardMetrics

Система мониторинга безопасности для Windows и Linux, анализирующая процессы, сетевые соединения и системные события безопасности.

## Последние улучшения

Недавно в проекте был произведен ряд значительных улучшений для повышения стабильности, производительности и читаемости кода:

1. **Оптимизация архитектуры приложения**:
   - Упрощение структуры кода в `Program.cs` путем выделения логических блоков в отдельные методы
   - Повышение устойчивости к ошибкам за счет улучшенной обработки исключений
   - Детальное логирование процесса запуска и работы приложения

2. **Улучшение сервиса обнаружения аномалий**:
   - Введено перечисление `MetricType` для улучшения типизации
   - Устранение дублирования кода за счет создания универсальных методов
   - Более надежная обработка ошибок и улучшенное логирование

3. **Оптимизация анализатора метрик**:
   - Исправлена асинхронная обработка данных с async/await
   - Безопасное взаимодействие с Redis для хранения метрик
   - Расширенные проверки входных данных и обработка исключений

Для подробного описания всех улучшений смотрите файл [IMPROVEMENTS.md](IMPROVEMENTS.md).

## Возможности

- Мониторинг процессов и их ресурсов
- Мониторинг сетевых соединений
- Анализ метрик безопасности
- Обнаружение аномалий с помощью ML.NET
- Проверка файлов через VirusTotal API
- Уведомления о угрозах через Telegram
- Хранение и визуализация данных с Grafana

## Требования

- .NET 8.0 SDK или новее
- PostgreSQL (для хранения метрик)
- Redis (для кэширования и обмена сообщениями)
- Docker и Docker Compose (опционально, но рекомендуется)

## Установка

### Без Docker

1. Клонируйте репозиторий и перейдите в его директорию

```bash
git clone https://github.com/cofheim/GuardMetrics.git
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
git clone https://github.com/cofheim/GuardMetrics.git
cd GuardMetrics
```

2. Настройте файл .env с вашими параметрами и запустите Docker Compose:

```bash
docker-compose up -d
```

## Использование

1. Получите токен доступа через `/api/auth/token`
2. Отправляйте метрики через соответствующие API-эндпоинты
3. Просматривайте данные в веб-интерфейсе или в Grafana (порт 3000)
4. Управляйте фоновыми задачами через Hangfire Dashboard (/hangfire)

## Веб-интерфейс

Проект включает простой веб-интерфейс для быстрого доступа к основным функциям:

- **Дашборд** - общий обзор состояния системы
- **Метрики** - подробный анализ собранных метрик
- **Оповещения** - просмотр и настройка оповещений о безопасности

## Поддерживаемые агенты для сбора метрик

- Windows Agent (.NET)
- Linux Agent (Python или .NET)
- Docker Agent (для контейнеризованных приложений)

## Лицензия

MIT 
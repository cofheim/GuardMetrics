# Руководство для разработчиков GuardMetrics

## Известные проблемы и их решения

### 1. Несовместимость версий пакетов

В проекте есть проблема совместимости между .NET 6.0 и некоторыми пакетами версии 9.0.0. При запуске миграций могут возникать предупреждения:

```
System.Text.Encodings.Web 9.0.0 doesn't support net6.0 and has not been tested with it.
```

**Решение:**

1. Добавьте в файл проекта `.csproj` следующую настройку:

```xml
<PropertyGroup>
  <SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>
</PropertyGroup>
```

2. Или обновите проект до .NET 8.0, изменив целевую платформу:

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

### 2. Проблемы с миграцией БД

При создании миграций может возникать ошибка конфликта версий Entity Framework Core.

**Решение:**

Используйте конкретную версию инструмента миграции:

```bash
dotnet tool install --global dotnet-ef --version 6.0.0
dotnet ef migrations add InitialCreate --framework net6.0
dotnet ef database update --framework net6.0
```

### 3. Проблемы с Helm charts

В Helm чартах есть ссылки на переменные в формате `${APP_NAME}`, что не соответствует синтаксису Helm.

**Решение:**

Замените ссылки на переменные в Helm charts на формат Go Template:

```yaml
# Было
- name: ${APP_NAME}

# Надо
- name: {{ .Release.Name }}
```

### 4. Ошибки при подключении к Redis

Если Redis требует пароль, но он не указан в строке подключения, будут возникать ошибки аутентификации.

**Решение:**

Убедитесь, что строка подключения включает пароль:

```
ConnectionStrings__Redis=localhost:6379,password=your_password
```

### 5. Отсутствие тестов для критических компонентов

В проекте отсутствуют тесты для критических компонентов, таких как `MetricAnalyzer` и `AnomalyDetectionService`.

**Рекомендации:**

1. Создайте тесты для `AnomalyDetectionService`:

```csharp
[Fact]
public void DetectCpuAnomaly_ReturnsCorrectResult()
{
    // Arrange
    var service = new AnomalyDetectionService();
    var historical = new List<float> { 10f, 15f, 20f, 25f, 90f, 30f, 20f, 15f };
    service.TrainCpuModel(historical);
    
    // Act
    var result = service.DetectCpuAnomaly(85f);
    
    // Assert
    Assert.True(result.IsAnomaly);
    Assert.True(result.Score > 0);
}
```

2. Создайте тесты для `MetricAnalyzer` с использованием моков для зависимостей.

### 6. Проблемы с Dockerfile

Текущий Dockerfile использует .NET 8.0, в то время как проект нацелен на .NET 6.0.

**Решение:**

Обновите Dockerfile:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GuardMetrics.csproj", "./"]
RUN dotnet restore "GuardMetrics.csproj"
COPY . .
RUN dotnet build "GuardMetrics.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GuardMetrics.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GuardMetrics.dll"]
```

## Предложения по улучшению архитектуры

1. **Разделение проекта на микросервисы:**
   - API Gateway для аутентификации и маршрутизации запросов
   - Сервис сбора метрик
   - Сервис анализа метрик и обнаружения аномалий
   - Сервис уведомлений

2. **Улучшение хранения метрик:**
   - Использовать временные ряды в ClickHouse или TimescaleDB для более эффективного хранения и запросов метрик
   - Реализовать партиционирование данных для улучшения производительности запросов

3. **Расширение ML модели:**
   - Добавить поддержку для других алгоритмов обнаружения аномалий (не только IID Spike Detection)
   - Использовать обучение с подкреплением для уменьшения количества ложных срабатываний

4. **Улучшение безопасности:**
   - Использовать более сложную модель аутентификации с OAuth 2.0
   - Добавить поддержку авторизации на основе ролей
   - Реализовать проверку подлинности запросов с использованием HMAC

## Управление техническим долгом

1. **Миграция на .NET 8.0:**
   - Обновить целевую платформу до последней версии .NET
   - Обновить все пакеты до последних совместимых версий

2. **Добавление тестов:**
   - Добавить модульные тесты для всех сервисов
   - Добавить интеграционные тесты для ключевых сценариев
   - Настроить непрерывное тестирование в CI/CD

3. **Улучшение документации:**
   - Добавить документацию по API с использованием Swagger
   - Создать диаграммы архитектуры и потоков данных
   - Документировать алгоритмы обнаружения аномалий 
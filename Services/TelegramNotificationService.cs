using Telegram.Bot;

namespace GuardMetrics.Services;

public class TelegramNotificationService
{
    private readonly ITelegramBotClient _botClient;
    private readonly string _chatId;
    private readonly ILogger<TelegramNotificationService> _logger;

    public TelegramNotificationService(IConfiguration configuration, ILogger<TelegramNotificationService> logger)
    {
        var botToken = configuration["Telegram:BotToken"] ?? throw new ArgumentNullException("Telegram bot token is not configured");
        _chatId = configuration["Telegram:ChatId"] ?? throw new ArgumentNullException("Telegram chat ID is not configured");
        _botClient = new TelegramBotClient(botToken);
        _logger = logger;
    }

    public async Task SendThreatAlertAsync(string processName, string threatType, double severity, string recommendation)
    {
        try
        {
            var message = $"🚨 *Обнаружена угроза!*\n\n" +
                         $"*Процесс:* `{processName}`\n" +
                         $"*Тип угрозы:* {threatType}\n" +
                         $"*Уровень опасности:* {severity:P0}\n\n" +
                         $"*Рекомендации:*\n{recommendation}";

            await _botClient.SendTextMessageAsync(
                chatId: _chatId,
                text: message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

            _logger.LogInformation("Sent threat alert to Telegram for process: {ProcessName}", processName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Telegram notification for process: {ProcessName}", processName);
        }
    }

    public async Task SendAnomalyAlertAsync(string metricType, string details, double anomalyScore)
    {
        try
        {
            var message = $"⚠️ *Обнаружена аномалия!*\n\n" +
                         $"*Тип метрики:* {metricType}\n" +
                         $"*Детали:* {details}\n" +
                         $"*Оценка аномалии:* {anomalyScore:P0}";

            await _botClient.SendTextMessageAsync(
                chatId: _chatId,
                text: message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

            _logger.LogInformation("Sent anomaly alert to Telegram: {Details}", details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Telegram notification for anomaly: {Details}", details);
        }
    }
} 
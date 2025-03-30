using System.Net.Http.Headers;
using System.Text.Json;

namespace GuardMetrics.Services;

public class VirusTotalService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<VirusTotalService> _logger;

    public VirusTotalService(IConfiguration configuration, ILogger<VirusTotalService> logger)
    {
        _apiKey = configuration["VirusTotal:ApiKey"] ?? throw new ArgumentNullException("VirusTotal API key is not configured");
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://www.virustotal.com/vtapi/v2/")
        };
        _logger = logger;
    }

    public async Task<(bool ismalicious, int detections)> CheckFileHashAsync(string fileHash)
    {
        try
        {
            var response = await _httpClient.GetAsync($"file/report?apikey={_apiKey}&resource={fileHash}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("VirusTotal API returned status code: {StatusCode}", response.StatusCode);
                return (false, 0);
            }

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("response_code", out var responseCode) && responseCode.GetInt32() == 0)
            {
                _logger.LogInformation("File hash not found in VirusTotal database: {Hash}", fileHash);
                return (false, 0);
            }

            var positives = root.GetProperty("positives").GetInt32();
            var total = root.GetProperty("total").GetInt32();

            _logger.LogInformation("VirusTotal results for {Hash}: {Positives}/{Total} detections", 
                fileHash, positives, total);

            return (positives > 0, positives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file hash with VirusTotal: {Hash}", fileHash);
            return (false, 0);
        }
    }
} 
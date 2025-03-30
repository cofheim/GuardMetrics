using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using Serilog;

namespace GuardMetrics.Services;

// Перечисление типов метрик
public enum MetricType
{
    Cpu,
    Memory,
    Network
}

public class AnomalyDetectionService
{
    private readonly MLContext _mlContext;
    private readonly int _windowSize = 60; // 1 час при сборе метрик каждую минуту
    private readonly Dictionary<MetricType, ITransformer?> _models = new();

    public AnomalyDetectionService()
    {
        _mlContext = new MLContext(seed: 1);
        // Инициализация моделей значениями по умолчанию
        foreach (MetricType type in Enum.GetValues(typeof(MetricType)))
        {
            _models[type] = null;
        }
    }

    public class MetricData
    {
        [LoadColumn(0)]
        public float Value { get; set; }
    }

    public class MetricPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; } = new double[3];
    }

    // Универсальный метод обучения модели
    public void TrainModel(MetricType metricType, IEnumerable<float> historicalData)
    {
        try
        {
            if (historicalData == null || !historicalData.Any())
            {
                Log.Warning("Попытка обучения модели {MetricType} с пустыми данными", metricType);
                return;
            }

            var dataView = _mlContext.Data.LoadFromEnumerable(
                historicalData.Select(x => new MetricData { Value = x }));

            var pipeline = _mlContext.Transforms.DetectIidSpike(
                outputColumnName: "Prediction",
                inputColumnName: nameof(MetricData.Value),
                confidence: 95.0,
                pvalueHistoryLength: _windowSize);

            _models[metricType] = pipeline.Fit(dataView);
            Log.Information("Модель для {MetricType} успешно обучена на {Count} точках данных", 
                metricType, historicalData.Count());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при обучении модели {MetricType}", metricType);
        }
    }

    // Универсальный метод определения аномалий
    public (bool IsAnomaly, double Score) DetectAnomaly(MetricType metricType, float value)
    {
        try
        {
            var model = _models[metricType];
            if (model == null)
            {
                Log.Warning("Попытка определения аномалии {MetricType} с необученной моделью", metricType);
                return (false, 0);
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<MetricData, MetricPrediction>(model);
            var prediction = predictionEngine.Predict(new MetricData { Value = value });

            // prediction[0] - alert score
            // prediction[1] - p-value
            // prediction[2] - 1 если обнаружен всплеск, 0 если нет
            bool isAnomaly = prediction.Prediction[2] == 1;
            
            if (isAnomaly)
            {
                Log.Information("Обнаружена аномалия в метрике {MetricType}: значение {Value}, оценка {Score}", 
                    metricType, value, prediction.Prediction[0]);
            }
            
            return (isAnomaly, prediction.Prediction[0]);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при определении аномалии {MetricType}", metricType);
            return (false, 0);
        }
    }

    // Методы-обертки для обратной совместимости
    
    public void TrainCpuModel(IEnumerable<float> historicalData)
    {
        TrainModel(MetricType.Cpu, historicalData);
    }

    public void TrainMemoryModel(IEnumerable<float> historicalData)
    {
        TrainModel(MetricType.Memory, historicalData);
    }

    public void TrainNetworkModel(IEnumerable<float> historicalData)
    {
        TrainModel(MetricType.Network, historicalData);
    }

    public (bool IsAnomaly, double Score) DetectCpuAnomaly(float value)
    {
        return DetectAnomaly(MetricType.Cpu, value);
    }

    public (bool IsAnomaly, double Score) DetectMemoryAnomaly(float value)
    {
        return DetectAnomaly(MetricType.Memory, value);
    }

    public (bool IsAnomaly, double Score) DetectNetworkAnomaly(float value)
    {
        return DetectAnomaly(MetricType.Network, value);
    }
} 
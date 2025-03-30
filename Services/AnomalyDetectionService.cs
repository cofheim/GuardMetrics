using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;

namespace GuardMetrics.Services;

public class AnomalyDetectionService
{
    private readonly MLContext _mlContext;
    private readonly int _windowSize = 60; // 1 час при сборе метрик каждую минуту
    private ITransformer _cpuModel;
    private ITransformer _memoryModel;
    private ITransformer _networkModel;

    public AnomalyDetectionService()
    {
        _mlContext = new MLContext(seed: 1);
    }

    public class MetricData
    {
        [LoadColumn(0)]
        public float Value { get; set; }
    }

    public class MetricPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; }
    }

    public void TrainCpuModel(IEnumerable<float> historicalData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(
            historicalData.Select(x => new MetricData { Value = x }));

        var pipeline = _mlContext.Transforms.DetectIidSpike(
            outputColumnName: "Prediction",
            inputColumnName: nameof(MetricData.Value),
            confidence: 95,
            pvalueHistoryLength: _windowSize);

        _cpuModel = pipeline.Fit(dataView);
    }

    public void TrainMemoryModel(IEnumerable<float> historicalData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(
            historicalData.Select(x => new MetricData { Value = x }));

        var pipeline = _mlContext.Transforms.DetectIidSpike(
            outputColumnName: "Prediction",
            inputColumnName: nameof(MetricData.Value),
            confidence: 95,
            pvalueHistoryLength: _windowSize);

        _memoryModel = pipeline.Fit(dataView);
    }

    public void TrainNetworkModel(IEnumerable<float> historicalData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(
            historicalData.Select(x => new MetricData { Value = x }));

        var pipeline = _mlContext.Transforms.DetectIidSpike(
            outputColumnName: "Prediction",
            inputColumnName: nameof(MetricData.Value),
            confidence: 95,
            pvalueHistoryLength: _windowSize);

        _networkModel = pipeline.Fit(dataView);
    }

    public (bool IsAnomaly, double Score) DetectCpuAnomaly(float value)
    {
        if (_cpuModel == null)
        {
            return (false, 0);
        }

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<MetricData, MetricPrediction>(_cpuModel);
        var prediction = predictionEngine.Predict(new MetricData { Value = value });

        // prediction[0] - alert score
        // prediction[1] - p-value
        // prediction[2] - 1 if spike, 0 if not
        return (prediction.Prediction[2] == 1, prediction.Prediction[0]);
    }

    public (bool IsAnomaly, double Score) DetectMemoryAnomaly(float value)
    {
        if (_memoryModel == null)
        {
            return (false, 0);
        }

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<MetricData, MetricPrediction>(_memoryModel);
        var prediction = predictionEngine.Predict(new MetricData { Value = value });

        return (prediction.Prediction[2] == 1, prediction.Prediction[0]);
    }

    public (bool IsAnomaly, double Score) DetectNetworkAnomaly(float value)
    {
        if (_networkModel == null)
        {
            return (false, 0);
        }

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<MetricData, MetricPrediction>(_networkModel);
        var prediction = predictionEngine.Predict(new MetricData { Value = value });

        return (prediction.Prediction[2] == 1, prediction.Prediction[0]);
    }
} 
namespace NzbDrone.Core.Configuration
{
    public class OpenTelemetryOptions
    {
        public string ServiceName { get; set; }
        public string ServiceVersion { get; set; }
        public double SamplingRate { get; set; } = 1.0;
        public OtlpExporterOptions Exporter { get; set; }
        public string TracesExporter { get; set; }
        public string MetricsExporter { get; set; }
        public string LogsExporter { get; set; }
        public bool EnableCustomMetrics { get; set; } = true;
        public bool EnableApiMetrics { get; set; } = true;
        public bool EnableConsoleExporter { get; set; }
    }

    public class OtlpExporterOptions
    {
        public string Endpoint { get; set; }
        public string Headers { get; set; }
    }
}

using System;
using System.Collections.Generic;
using DryIoc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NzbDrone.Core.Instrumentation.Extensions
{
    public static class OpenTelemetryExtensions
    {
        public static IContainer AddOpenTelemetry(this IContainer container)
        {
            container.Register<ReadarrMetrics>(Reuse.Singleton);
            return container;
        }

        public static IServiceCollection AddReadarrOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OpenTelemetryOptions>(configuration.GetSection("Otel"));

            var options = new OpenTelemetryOptions();
            configuration.GetSection("Otel").Bind(options);

            var isEnabled =
                (options.Exporter != null && !string.IsNullOrWhiteSpace(options.Exporter.Endpoint)) ||
                !string.IsNullOrWhiteSpace(options.TracesExporter) ||
                !string.IsNullOrWhiteSpace(options.MetricsExporter) ||
                !string.IsNullOrWhiteSpace(options.LogsExporter);

            if (!isEnabled)
            {
                return services;
            }

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName: options.ServiceName ?? "Readarr",
                    serviceVersion: options.ServiceVersion ?? BuildInfo.Version.ToString(),
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new ("environment", RuntimeInfo.IsProduction ? "production" : "development"),
                    new ("runtime", ".NET"),
                    new ("os", OsInfo.Os.ToString())
                });

            // Manual OpenTelemetry setup without DependencyInjection package
            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .SetSampler(new TraceIdRatioBasedSampler(options.SamplingRate))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Readarr.Database")
                .AddSource("Readarr.Command")
                .AddSource("Readarr.Event")
                .AddSource("Readarr.HealthCheck")
                .AddSource("Readarr.Metrics");

            if (options.EnableConsoleExporter)
            {
                tracerProvider.AddConsoleExporter();
            }

            if (options.Exporter != null && !string.IsNullOrWhiteSpace(options.Exporter.Endpoint))
            {
                tracerProvider.AddOtlpExporter();
            }

            tracerProvider.Build();

            var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Readarr");

            if (options.EnableConsoleExporter)
            {
                meterProvider.AddConsoleExporter();
            }

            if (options.Exporter != null && !string.IsNullOrWhiteSpace(options.Exporter.Endpoint))
            {
                meterProvider.AddOtlpExporter();
            }

            meterProvider.Build();

            if (options.EnableCustomMetrics)
            {
                services.AddSingleton<ReadarrMetrics>();
            }

            return services;
        }
    }
}

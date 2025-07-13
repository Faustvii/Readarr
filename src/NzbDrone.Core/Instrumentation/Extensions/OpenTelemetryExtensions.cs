using System;
using System.Collections.Generic;
using DryIoc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Configuration;
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
                    new ("environment", "production"),
                    new ("runtime", ".NET"),
                    new ("os", "unknown")
                });

            var builder = services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .SetResourceBuilder(resourceBuilder)
                    .SetSampler(new TraceIdRatioBasedSampler(options.SamplingRate))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Readarr.Database")
                    .AddSource("Readarr.Command")
                    .AddSource("Readarr.Event")
                    .AddSource("Readarr.HealthCheck")
                    .AddSource("Readarr.Metrics"))
                .WithMetrics(metrics => metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("Readarr"));

            if (options.EnableConsoleExporter)
            {
                builder.WithTracing(tracing => tracing.AddConsoleExporter())
                       .WithMetrics(metrics => metrics.AddConsoleExporter());
            }

            if (options.Exporter != null && !string.IsNullOrWhiteSpace(options.Exporter.Endpoint))
            {
                builder.WithTracing(tracing => tracing.AddOtlpExporter())
                       .WithMetrics(metrics => metrics.AddOtlpExporter());
            }

            if (options.EnableCustomMetrics)
            {
                services.AddSingleton<ReadarrMetrics>();
            }

            return services;
        }
    }
}

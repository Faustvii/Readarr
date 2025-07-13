using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Instrumentation;
using Readarr.Http.Extensions;

namespace Readarr.Http.Middleware
{
    public class OpenTelemetryApiMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OpenTelemetryApiMiddleware> _logger;
        private readonly IOptions<OpenTelemetryOptions> _options;
        private readonly ReadarrMetrics _metrics;

        public OpenTelemetryApiMiddleware(RequestDelegate next, ILogger<OpenTelemetryApiMiddleware> logger, IOptions<OpenTelemetryOptions> options, ReadarrMetrics metrics)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _metrics = metrics;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var options = _options.Value;
            var isEnabled =
                (options.Exporter != null && !string.IsNullOrWhiteSpace(options.Exporter.Endpoint)) ||
                !string.IsNullOrWhiteSpace(options.TracesExporter) ||
                !string.IsNullOrWhiteSpace(options.MetricsExporter) ||
                !string.IsNullOrWhiteSpace(options.LogsExporter);

            if (!isEnabled || !options.EnableApiMetrics || !context.Request.IsApiRequest())
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                RecordApiMetrics(context, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        private void RecordApiMetrics(HttpContext context, double durationMilliseconds)
        {
            try
            {
                var endpoint = GetEndpoint(context);
                var method = context.Request.Method;
                var statusCode = context.Response.StatusCode;

                _metrics.RecordApiRequest(endpoint, method, statusCode, durationMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record API metrics");
            }
        }

        private static string GetEndpoint(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            var query = context.Request.QueryString.Value ?? "";

            var endpoint = path;

            if (!string.IsNullOrEmpty(query) && query != "?")
            {
                var truncatedQuery = query.Length > 50 ? query[..50] + "..." : query;
                endpoint = path + truncatedQuery;
            }

            if (endpoint.EndsWith("/"))
            {
                endpoint = endpoint.TrimEnd('/');
            }

            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = "root";
            }

            return endpoint;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.AuthorStats;
using NzbDrone.Core.Books;
using NzbDrone.Core.Jobs;

namespace NzbDrone.Core.Instrumentation
{
    public class ReadarrMetrics : IDisposable
    {
        public static ReadarrMetrics Instance { get; set; }
        private readonly Meter _meter;
        private readonly ILogger<ReadarrMetrics> _logger;
        private readonly IAuthorStatisticsService _authorStatisticsService;
        private readonly ITaskManager _taskManager;
        private readonly IBookRepository _bookRepository;

        // API Metrics
        private readonly Counter<long> _apiRequestCounter;
        private readonly Histogram<double> _apiRequestDuration;
        private readonly Counter<long> _apiErrorCounter;

        // Database Metrics
        private readonly Counter<long> _databaseQueryCounter;
        private readonly Histogram<double> _databaseQueryDuration;
        private readonly Counter<long> _databaseErrorCounter;

        // Library Metrics
        private readonly ObservableGauge<long> _libraryBookCount;
        private readonly ObservableGauge<long> _libraryAuthorCount;
        private readonly ObservableGauge<long> _libraryBookFileCount;
        private readonly ObservableGauge<long> _librarySizeOnDisk;
        private readonly ObservableGauge<long> _libraryMonitoredBookCount;
        private readonly ObservableGauge<long> _libraryMonitoredAuthorCount;

        // Background Job Metrics
        private readonly Counter<long> _backgroundJobCounter;
        private readonly Histogram<double> _backgroundJobDuration;
        private readonly Counter<long> _backgroundJobErrorCounter;
        private readonly ObservableGauge<long> _pendingBackgroundJobs;

        private static readonly ActivitySource MetricsActivitySource = new ActivitySource("Readarr.Metrics");

        public ReadarrMetrics(
            ILogger<ReadarrMetrics> logger,
            IAuthorStatisticsService authorStatisticsService,
            ITaskManager taskManager,
            IBookRepository bookRepository)
        {
            _logger = logger;
            _authorStatisticsService = authorStatisticsService;
            _taskManager = taskManager;
            _bookRepository = bookRepository;

            _meter = new Meter("Readarr", BuildInfo.Version.ToString());

            // API Metrics
            _apiRequestCounter = _meter.CreateCounter<long>("api_requests_total", "Total number of API requests");
            _apiRequestDuration = _meter.CreateHistogram<double>("api_request_duration_ms", "API request duration in milliseconds");
            _apiErrorCounter = _meter.CreateCounter<long>("api_errors_total", "Total number of API errors");

            // Database Metrics
            _databaseQueryCounter = _meter.CreateCounter<long>("database_queries_total", "Total number of database queries");
            _databaseQueryDuration = _meter.CreateHistogram<double>("database_query_duration_ms", "Database query duration in milliseconds");
            _databaseErrorCounter = _meter.CreateCounter<long>("database_errors_total", "Total number of database errors");

            // Library Metrics
            _libraryBookCount = _meter.CreateObservableGauge<long>("library_book_count", () => GetLibraryBookCount(), "Total number of books in library");
            _libraryAuthorCount = _meter.CreateObservableGauge<long>("library_author_count", () => GetLibraryAuthorCount(), "Total number of authors in library");
            _libraryBookFileCount = _meter.CreateObservableGauge<long>("library_book_file_count", () => GetLibraryBookFileCount(), "Total number of book files in library");
            _librarySizeOnDisk = _meter.CreateObservableGauge<long>("library_size_on_disk_bytes", () => GetLibrarySizeOnDisk(), "Total library size on disk in bytes");
            _libraryMonitoredBookCount = _meter.CreateObservableGauge<long>("library_monitored_book_count", () => GetLibraryMonitoredBookCount(), "Total number of monitored books");
            _libraryMonitoredAuthorCount = _meter.CreateObservableGauge<long>("library_monitored_author_count", () => GetLibraryMonitoredAuthorCount(), "Total number of monitored authors");

            // Background Job Metrics
            _backgroundJobCounter = _meter.CreateCounter<long>("background_jobs_total", "Total number of background jobs executed");
            _backgroundJobDuration = _meter.CreateHistogram<double>("background_job_duration_seconds", "Background job duration in seconds");
            _backgroundJobErrorCounter = _meter.CreateCounter<long>("background_job_errors_total", "Total number of background job errors");
            _pendingBackgroundJobs = _meter.CreateObservableGauge<long>("pending_background_jobs", () => GetPendingBackgroundJobs(), "Number of pending background jobs");

            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void RecordApiRequest(string endpoint, string method, int statusCode, double durationMilliseconds)
        {
            var tags = new KeyValuePair<string, object>[]
            {
                new ("endpoint", endpoint),
                new ("method", method),
                new ("status_code", statusCode)
            };

            _apiRequestCounter.Add(1, tags);
            _apiRequestDuration.Record(durationMilliseconds, tags);

            if (statusCode >= 400)
            {
                _apiErrorCounter.Add(1, tags);
            }
        }

        public void RecordDatabaseQuery(string queryType, double durationMilliseconds, bool success = true)
        {
            var tags = new KeyValuePair<string, object>[]
            {
                new ("query_type", queryType),
                new ("success", success)
            };

            _databaseQueryCounter.Add(1, tags);
            _databaseQueryDuration.Record(durationMilliseconds, tags);

            if (!success)
            {
                _databaseErrorCounter.Add(1, tags);
            }
        }

        public void RecordBackgroundJob(string jobType, double durationSeconds, bool success = true)
        {
            var tags = new KeyValuePair<string, object>[]
            {
                new ("job_type", jobType),
                new ("success", success)
            };

            _backgroundJobCounter.Add(1, tags);
            _backgroundJobDuration.Record(durationSeconds, tags);

            if (!success)
            {
                _backgroundJobErrorCounter.Add(1, tags);
            }
        }

        private long GetLibraryBookCount()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetLibraryBookCount");
            activity?.SetTag("metric.name", "library_book_count");
            try
            {
                var count = _bookRepository.Count();
                activity?.SetTag("metric.value", count);
                return count;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get library book count");
                return 0;
            }
        }

        private long GetLibraryAuthorCount()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetLibraryAuthorCount");
            activity?.SetTag("metric.name", "library_author_count");
            try
            {
                var stats = _authorStatisticsService.AuthorStatistics();
                activity?.SetTag("metric.value", stats.Count);
                return stats.Count;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get library author count");
                return 0;
            }
        }

        private long GetLibraryBookFileCount()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetLibraryBookFileCount");
            activity?.SetTag("metric.name", "library_book_file_count");
            try
            {
                var stats = _authorStatisticsService.AuthorStatistics();
                activity?.SetTag("metric.value", stats.Sum(x => x.BookFileCount));
                return stats.Sum(x => x.BookFileCount);
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get library book file count");
                return 0;
            }
        }

        private long GetLibrarySizeOnDisk()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetLibrarySizeOnDisk");
            activity?.SetTag("metric.name", "library_size_on_disk_bytes");
            try
            {
                var stats = _authorStatisticsService.AuthorStatistics();
                activity?.SetTag("metric.value", stats.Sum(x => x.SizeOnDisk));
                return stats.Sum(x => x.SizeOnDisk);
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get library size on disk");
                return 0;
            }
        }

        private long GetLibraryMonitoredBookCount()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetLibraryMonitoredBookCount");
            activity?.SetTag("metric.name", "library_monitored_book_count");
            try
            {
                var count = _bookRepository.All().Count(x => x.Monitored);
                activity?.SetTag("metric.value", count);
                return count;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get monitored book count");
                return 0;
            }
        }

        private long GetLibraryMonitoredAuthorCount()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetLibraryMonitoredAuthorCount");
            activity?.SetTag("metric.name", "library_monitored_author_count");
            try
            {
                var stats = _authorStatisticsService.AuthorStatistics();
                activity?.SetTag("metric.value", stats.Count(x => x.BookCount > 0));
                return stats.Count(x => x.BookCount > 0); // Authors with books are considered monitored
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get monitored author count");
                return 0;
            }
        }

        private long GetPendingBackgroundJobs()
        {
            using var activity = MetricsActivitySource.StartActivity("Metrics.GetPendingBackgroundJobs");
            activity?.SetTag("metric.name", "pending_background_jobs");
            try
            {
                var pending = _taskManager.GetPending();
                activity?.SetTag("metric.value", pending.Count);
                return pending.Count;
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", ex.Message);
                _logger.LogError(ex, "Failed to get pending background jobs count");
                return 0;
            }
        }

        public void Dispose()
        {
            _meter?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Messaging;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.HealthCheck
{
    public interface IHealthCheckService
    {
        List<HealthCheck> Results();
    }

    public class HealthCheckService : IHealthCheckService,
                                      IExecute<CheckHealthCommand>,
                                      IHandleAsync<ApplicationStartedEvent>,
                                      IHandleAsync<IEvent>
    {
        private readonly DateTime _startupGracePeriodEndTime;
        private readonly IProvideHealthCheck[] _healthChecks;
        private readonly IProvideHealthCheck[] _startupHealthChecks;
        private readonly IProvideHealthCheck[] _scheduledHealthChecks;
        private readonly Dictionary<Type, IEventDrivenHealthCheck[]> _eventDrivenHealthChecks;
        private readonly IServerSideNotificationService _serverSideNotificationService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICacheManager _cacheManager;
        private readonly Logger _logger;

        private readonly ICached<HealthCheck> _healthCheckResults;
        private static readonly ActivitySource HealthCheckActivitySource = new ActivitySource("Readarr.HealthCheck");

        private bool _hasRunHealthChecksAfterGracePeriod = false;
        private bool _isRunningHealthChecksAfterGracePeriod = false;

        public HealthCheckService(IEnumerable<IProvideHealthCheck> healthChecks,
                                  IServerSideNotificationService serverSideNotificationService,
                                  IEventAggregator eventAggregator,
                                  ICacheManager cacheManager,
                                  IRuntimeInfo runtimeInfo,
                                  Logger logger)
        {
            _healthChecks = healthChecks.ToArray();
            _serverSideNotificationService = serverSideNotificationService;
            _eventAggregator = eventAggregator;
            _cacheManager = cacheManager;
            _logger = logger;

            _healthCheckResults = _cacheManager.GetCache<HealthCheck>(GetType());

            _startupHealthChecks = _healthChecks.Where(v => v.CheckOnStartup).ToArray();
            _scheduledHealthChecks = _healthChecks.Where(v => v.CheckOnSchedule).ToArray();
            _eventDrivenHealthChecks = GetEventDrivenHealthChecks();
            _startupGracePeriodEndTime = runtimeInfo.StartTime + TimeSpan.FromMinutes(15);
        }

        public List<HealthCheck> Results()
        {
            return _healthCheckResults.Values.ToList();
        }

        private Dictionary<Type, IEventDrivenHealthCheck[]> GetEventDrivenHealthChecks()
        {
            return _healthChecks
                .SelectMany(h => h.GetType().GetAttributes<CheckOnAttribute>().Select(a =>
                {
                    var eventDrivenType = typeof(EventDrivenHealthCheck<>).MakeGenericType(a.EventType);
                    var eventDriven = (IEventDrivenHealthCheck)Activator.CreateInstance(eventDrivenType, h, a.Condition);

                    return Tuple.Create(a.EventType, eventDriven);
                }))
                .GroupBy(t => t.Item1, t => t.Item2)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        private void PerformHealthCheck(IProvideHealthCheck[] healthChecks, IEvent message = null, bool performServerChecks = false)
        {
            using var activity = HealthCheckActivitySource.StartActivity("HealthCheck.PerformHealthCheck");
            activity?.SetTag("healthcheck.count", healthChecks.Length);
            activity?.SetTag("healthcheck.perform_server_checks", performServerChecks);
            activity?.SetTag("healthcheck.has_message", message != null);
            if (message != null)
            {
                activity?.SetTag("healthcheck.message_type", message.GetType().FullName);
            }

            var results = new List<HealthCheck>();

            foreach (var healthCheck in healthChecks)
            {
                using var checkActivity = HealthCheckActivitySource.StartActivity($"HealthCheck.{healthCheck.GetType().Name}");
                checkActivity?.SetTag("healthcheck.type", healthCheck.GetType().FullName);
                checkActivity?.SetTag("healthcheck.name", healthCheck.GetType().Name);
                checkActivity?.SetTag("healthcheck.check_on_startup", healthCheck.CheckOnStartup);
                checkActivity?.SetTag("healthcheck.check_on_schedule", healthCheck.CheckOnSchedule);
                checkActivity?.SetTag("healthcheck.has_message", message != null);

                var success = false;
                HealthCheck result = null;
                try
                {
                    if (healthCheck is IProvideHealthCheckWithMessage && message != null)
                    {
                        result = ((IProvideHealthCheckWithMessage)healthCheck).Check(message);
                    }
                    else
                    {
                        result = healthCheck.Check();
                    }

                    success = true;
                    checkActivity?.SetTag("healthcheck.result_type", result.Type.ToString());
                    checkActivity?.SetTag("healthcheck.result_message", result.Message ?? "");
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Health check {0} failed", healthCheck.GetType().Name);
                    checkActivity?.SetTag("error", true);
                    checkActivity?.SetTag("error.message", e.Message);
                    throw;
                }
                finally
                {
                    checkActivity?.SetTag("healthcheck.status", success ? "success" : "failure");
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
            }

            if (performServerChecks)
            {
                using var serverActivity = HealthCheckActivitySource.StartActivity("HealthCheck.ServerChecks");
                try
                {
                    var serverResults = _serverSideNotificationService.GetServerChecks();
                    results.AddRange(serverResults);
                    serverActivity?.SetTag("healthcheck.server_count", serverResults.Count);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Server health checks failed");
                    serverActivity?.SetTag("error", true);
                    serverActivity?.SetTag("error.message", e.Message);
                }
            }

            foreach (var result in results)
            {
                if (result.Type == HealthCheckResult.Ok)
                {
                    _healthCheckResults.Remove(result.Source.Name);
                }
                else
                {
                    if (_healthCheckResults.Find(result.Source.Name) == null)
                    {
                        _eventAggregator.PublishEvent(new HealthCheckFailedEvent(result, !_hasRunHealthChecksAfterGracePeriod));
                    }

                    _healthCheckResults.Set(result.Source.Name, result);
                }
            }

            _eventAggregator.PublishEvent(new HealthCheckCompleteEvent());
        }

        public void Execute(CheckHealthCommand message)
        {
            using var activity = HealthCheckActivitySource.StartActivity("HealthCheck.Execute");
            activity?.SetTag("healthcheck.trigger", message.Trigger.ToString());
            activity?.SetTag("healthcheck.manual", message.Trigger == CommandTrigger.Manual);

            if (message.Trigger == CommandTrigger.Manual)
            {
                PerformHealthCheck(_healthChecks, null, true);
            }
            else
            {
                PerformHealthCheck(_scheduledHealthChecks, null, true);
            }
        }

        public void HandleAsync(ApplicationStartedEvent message)
        {
            using var activity = HealthCheckActivitySource.StartActivity("HealthCheck.ApplicationStarted");
            activity?.SetTag("healthcheck.startup_checks_count", _startupHealthChecks.Length);

            PerformHealthCheck(_startupHealthChecks, null, true);
        }

        public void HandleAsync(IEvent message)
        {
            if (message is HealthCheckCompleteEvent)
            {
                return;
            }

            // If we haven't previously re-run health checks after startup grace period run startup checks again and track so they aren't run again.
            // Return early after re-running checks to avoid triggering checks multiple times.
            if (!_hasRunHealthChecksAfterGracePeriod && !_isRunningHealthChecksAfterGracePeriod && DateTime.UtcNow > _startupGracePeriodEndTime)
            {
                _isRunningHealthChecksAfterGracePeriod = true;

                using var activity = HealthCheckActivitySource.StartActivity("HealthCheck.AfterGracePeriod");
                activity?.SetTag("healthcheck.grace_period_ended", true);
                activity?.SetTag("healthcheck.startup_checks_count", _startupHealthChecks.Length);

                PerformHealthCheck(_startupHealthChecks);

                // Update after running health checks so new failure notifications aren't sent 2x.
                _hasRunHealthChecksAfterGracePeriod = true;

                // Explicitly notify for any failed checks since existing failed results would not have sent events.
                var results = _healthCheckResults.Values.ToList();

                foreach (var result in results)
                {
                    _eventAggregator.PublishEvent(new HealthCheckFailedEvent(result, false));
                }

                _isRunningHealthChecksAfterGracePeriod = false;
            }

            if (!_eventDrivenHealthChecks.TryGetValue(message.GetType(), out var checks))
            {
                return;
            }

            using var eventActivity = HealthCheckActivitySource.StartActivity("HealthCheck.EventDriven");
            eventActivity?.SetTag("healthcheck.event_type", message.GetType().FullName);
            eventActivity?.SetTag("healthcheck.event_name", message.GetType().Name);
            eventActivity?.SetTag("healthcheck.checks_count", checks.Length);

            var filteredChecks = new List<IProvideHealthCheck>();
            var healthCheckResults = _healthCheckResults.Values.ToList();

            foreach (var eventDrivenHealthCheck in checks)
            {
                var healthCheckType = eventDrivenHealthCheck.HealthCheck.GetType();
                var previouslyFailed = healthCheckResults.Any(r => r.Source == healthCheckType);

                if (eventDrivenHealthCheck.ShouldExecute(message, previouslyFailed))
                {
                    filteredChecks.Add(eventDrivenHealthCheck.HealthCheck);
                    continue;
                }
            }

            // TODO: Add debounce
            PerformHealthCheck(filteredChecks.ToArray(), message);
        }
    }
}

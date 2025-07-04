using DryIoc;
using NLog;
using Octokit;

namespace NzbDrone.Common.Instrumentation.Extensions
{
    public static class CompositionExtensions
    {
        private const string ReadarrAppName = "ForkReadarr";
        public static IContainer AddNzbDroneLogger(this IContainer container)
        {
            container.Register(Made.Of<Logger>(() => LogManager.GetLogger(Arg.Index<string>(0)), r => r.Parent.ImplementationType.Name.ToString()), reuse: Reuse.Transient);
            return container;
        }

        public static IContainer AddGithubClient(this IContainer container)
        {
            container.Register(made: Made.Of(() => new ProductHeaderValue(ReadarrAppName)), reuse: Reuse.Singleton);
            container.Register<IGitHubClient>(
                made: Made.Of(
                    () => new GitHubClient(Arg.Of<ProductHeaderValue>()),
                    r => new ProductHeaderValue(ReadarrAppName)),
                reuse: Reuse.Singleton);
            return container;
        }
    }
}

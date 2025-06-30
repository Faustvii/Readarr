using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Configuration;
using Octokit;

namespace NzbDrone.Core.Update
{
    public interface IUpdatePackageProvider
    {
        UpdatePackage GetLatestUpdate(string branch, Version currentVersion);
        List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion = null);
    }

    public class UpdatePackageProvider : IUpdatePackageProvider
    {
        private const string Owner = "faustvii";
        private const string Repo = "Readarr";
        private readonly IGitHubClient _githubClient;
        private readonly IDeploymentInfoProvider _deploymentInfoProvider;

        public UpdatePackageProvider(IDeploymentInfoProvider deploymentInfoProvider)
        {
            _deploymentInfoProvider = deploymentInfoProvider;
            _githubClient = new GitHubClient(new ProductHeaderValue("ForkReadarr"));
        }

        public UpdatePackage GetLatestUpdate(string branch, Version currentVersion)
        {
            var owner = _deploymentInfoProvider.PackageAuthor ?? Owner;
            var releases = _githubClient.Repository.Release.GetAll(owner, Repo).GetAwaiter().GetResult();
            var latestRelease = releases
                .Where(r => !r.Draft && !r.Prerelease)
                .Select(r => new { Release = r, Version = TryParseVersion(r.TagName) })
                .Where(x => x.Version != null)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (latestRelease == null)
            {
                return null;
            }

            if (latestRelease.Version <= currentVersion)
            {
                return null;
            }

            var tarAsset = latestRelease.Release.Assets.FirstOrDefault(a => a.Name.EndsWith("linux-musl-x64.tar.gz"));
            var shaAsset = latestRelease.Release.Assets.FirstOrDefault(a => a.Name.EndsWith("linux-musl-x64.tar.gz.sha256"));
            string hash = null;
            if (shaAsset != null)
            {
                // Download the .sha256 file and parse the hash
                var shaContent = _githubClient.Connection.Get<string>(new Uri(shaAsset.BrowserDownloadUrl), TimeSpan.FromSeconds(15)).GetAwaiter().GetResult().Body;
                if (!string.IsNullOrWhiteSpace(shaContent))
                {
                    hash = shaContent.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                }
            }

            return new UpdatePackage
            {
                Version = latestRelease.Version,
                ReleaseDate = latestRelease.Release.CreatedAt.DateTime,
                Url = tarAsset?.BrowserDownloadUrl ?? latestRelease.Release.HtmlUrl,
                Branch = branch,
                Changes = ParseReleaseNotes(latestRelease.Release.Body),
            };
        }

        public List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion)
        {
            var releases = _githubClient.Repository.Release.GetAll(Owner, Repo).GetAwaiter().GetResult();
            var recentReleases = releases
                .Where(r => !r.Draft && !r.Prerelease)
                .Select(r => new { Release = r, Version = TryParseVersion(r.TagName) })
                .Where(x => x.Version != null)
                .OrderByDescending(x => x.Release.CreatedAt)
                .Take(10)
                .Select(x =>
                {
                    var asset = x.Release.Assets.FirstOrDefault(a => a.Name.EndsWith("linux-musl-x64.tar.gz"));
                    return new UpdatePackage
                    {
                        Version = x.Version,
                        Branch = branch,
                        Url = asset?.BrowserDownloadUrl ?? x.Release.HtmlUrl,
                        ReleaseDate = x.Release.CreatedAt.DateTime,
                        Changes = ParseReleaseNotes(x.Release.Body),
                        Hash = x.Release.CreatedAt.DateTime.ToString(),
                        FileName = asset?.Name ?? "Readarr.develop.0.0.0-linux-musl-x64.tar.gz"
                    };
                })
                .ToList();

            return recentReleases;
        }

        private static Version TryParseVersion(string versionString)
        {
            return Version.TryParse(versionString.TrimStart('v'), out var version) ? version : null;
        }

        private static UpdateChanges ParseReleaseNotes(string body)
        {
            var newChanges = new List<string>();
            var fixedChanges = new List<string>();

            var currentSection = "";

            foreach (var line in body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("### Features", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "new";
                    continue;
                }

                if (line.StartsWith("### Bug Fixes", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = "fixed";
                    continue;
                }

                if (line.StartsWith("* ", StringComparison.OrdinalIgnoreCase))
                {
                    var cleanLine = line[2..].Split(new[] { " (" }, StringSplitOptions.None)[0];

                    if (currentSection == "new")
                    {
                        newChanges.Add(cleanLine);
                    }
                    else if (currentSection == "fixed")
                    {
                        fixedChanges.Add(cleanLine);
                    }
                }
            }

            return new UpdateChanges { New = newChanges, Fixed = fixedChanges };
        }
    }
}

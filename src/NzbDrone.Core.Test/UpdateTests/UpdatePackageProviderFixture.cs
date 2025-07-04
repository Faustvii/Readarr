using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;
using Octokit;

namespace NzbDrone.Core.Test.UpdateTests
{
    public class UpdatePackageProviderFixture : CoreTest<UpdatePackageProvider>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IPlatformInfo>().SetupGet(c => c.Version).Returns(new Version("9.9.9"));
            Mocker.GetMock<IGitHubClient>().Setup(c => c.Repository.Release.GetAll(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new[]
                {
                    new Release(
                    url: "https://api.github.com/repos/Readarr/Readarr/releases/1",
                    htmlUrl: "https://example.com/url1",
                    assetsUrl: "https://api.github.com/repos/Readarr/Readarr/releases/1/assets",
                    id: 1,
                    nodeId: "MDc6UmVsZWFzZTE=",
                    uploadUrl: "https://uploads.github.com/repos/Readarr/Readarr/releases/1/assets{?name,label}",
                    tarballUrl: "https://api.github.com/repos/Readarr/Readarr/tarball/develop",
                    zipballUrl: "https://api.github.com/repos/Readarr/Readarr/zipball/develop",
                    tagName: "0.6.0",
                    targetCommitish: "develop",
                    name: "Readarr.develop.0.6.0",
                    body: "notes1",
                    draft: false,
                    prerelease: false,
                    createdAt: DateTimeOffset.UtcNow.AddDays(-10),
                    publishedAt: DateTimeOffset.UtcNow.AddDays(-10),
                    author: null,
                    assets: new System.Collections.Generic.List<ReleaseAsset>()
                    {
                        new ReleaseAsset(
                            url: "https://api.github.com/repos/Readarr/Readarr/releases/assets/1",
                            id: 1,
                            nodeId: "MDc6QXNzZXQx",
                            name: "Readarr.develop.0.6.0.linux-musl-x64.tar.gz",
                            label: "linux-musl-x64",
                            contentType: "application/gzip",
                            state: ItemState.Open.ToString(),
                            size: 123456,
                            downloadCount: 100,
                            createdAt: DateTimeOffset.UtcNow.AddDays(-10),
                            updatedAt: DateTimeOffset.UtcNow.AddDays(-10),
                            uploader: null,
                            browserDownloadUrl: "https://example.com/download1")
                    })
                });
        }

        [Test]
        public void no_update_when_version_higher()
        {
            Subject.GetLatestUpdate("develop", new Version(10, 0)).Should().BeNull();
        }

        [Test]
        public void finds_update_when_version_lower()
        {
            Subject.GetLatestUpdate("develop", new Version(0, 1)).Should().NotBeNull();
        }

        [Test]
        [Ignore("Ignore until we actually release something on Master")]
        public void should_get_master_if_branch_doesnt_exit()
        {
            UseRealHttp();
            Subject.GetLatestUpdate("invalid_branch", new Version(0, 2)).Should().NotBeNull();
        }

        [Test]
        public void should_get_recent_updates()
        {
            const string branch = "develop";
            UseRealHttp();
            var recent = Subject.GetRecentUpdates(branch, new Version(0, 1), null);

            recent.Should().NotBeEmpty();
            recent.Should().OnlyContain(c => c.Hash.IsNotNullOrWhiteSpace());
            recent.Should().OnlyContain(c => c.FileName.Contains("Readarr.develop.0"));
            recent.Should().OnlyContain(c => c.ReleaseDate.Year >= 2014);
            recent.Where(c => c.Changes != null).Should().OnlyContain(c => c.Changes.New != null);
            recent.Where(c => c.Changes != null).Should().OnlyContain(c => c.Changes.Fixed != null);
            recent.Should().OnlyContain(c => c.Branch == branch);
        }
    }
}

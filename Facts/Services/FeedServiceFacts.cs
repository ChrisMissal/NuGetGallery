﻿using System.Collections.Generic;
using System.Linq;
using System.Web;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace NuGetGallery
{
    public class FeedServiceFacts
    {
        [Theory]
        [InlineData("http://nuget.org", "http://nuget.org/")]
        [InlineData("http://nuget.org/", "http://nuget.org/")]
        public void SiteRootAddsTrailingSlashes(string siteRoot, string expected)
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(s => s.GetSiteRoot(false)).Returns(siteRoot);
            var feed = new V2Feed(entities: null, repo: null, configuration: config.Object, searchService: null);
            feed.HttpContext = GetContext();

            // Act
            var actual = feed.SiteRoot;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SiteRootUsesCurrentRequestToDetermineSiteRoot()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(s => s.GetSiteRoot(true)).Returns("https://nuget.org").Verifiable();
            var feed = new V2Feed(entities: null, repo: null, configuration: config.Object, searchService: null);
            feed.HttpContext = GetContext(isSecure: true);

            // Act
            var actual = feed.SiteRoot;

            // Assert
            Assert.Equal("https://nuget.org/", actual);
            config.Verify();
        }

        [Fact]
        public void V1FeedSearchDoesNotReturnPrereleasePackages()
        {
            // Arrange
            var packageRegistration = new PackageRegistration { Id = "Foo" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package
                            {
                                PackageRegistration = packageRegistration,
                                Version = "1.0.0",
                                IsPrerelease = false,
                                Listed = true,
                                DownloadStatistics = new List<PackageStatistics>()
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistration,
                                Version = "1.0.1-a",
                                IsPrerelease = true,
                                Listed = true,
                                DownloadStatistics = new List<PackageStatistics>()
                            },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(It.IsAny<bool>())).Returns("https://localhost:8081/");
            var searchService = new Mock<ISearchService>(MockBehavior.Strict);
            int total;
            searchService.Setup(s => s.Search(It.IsAny<IQueryable<Package>>(), It.IsAny<SearchFilter>(), out total)).Returns
                <IQueryable<Package>, string>((_, __) => _);
            var v1Service = new TestableV1Feed(repo.Object, configuration.Object, searchService.Object);

            // Act
            var result = v1Service.Search(null, null);

            // Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Foo", result.First().Id);
            Assert.Equal("1.0.0", result.First().Version);
            Assert.Equal("https://localhost:8081/packages/Foo/1.0.0", result.First().GalleryDetailsUrl);
        }

        [Fact]
        public void V1FeedFindPackagesByIdReturnsUnlistedPackagesButNotPrereleasePackages()
        {
            // Arrange
            var packageRegistration = new PackageRegistration { Id = "Foo" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package
                            {
                                PackageRegistration = packageRegistration,
                                Version = "1.0.0",
                                IsPrerelease = false,
                                Listed = false,
                                DownloadStatistics = new List<PackageStatistics>()
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistration,
                                Version = "1.0.1-a",
                                IsPrerelease = true,
                                Listed = true,
                                DownloadStatistics = new List<PackageStatistics>()
                            },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(It.IsAny<bool>())).Returns("https://localhost:8081/");
            var v1Service = new TestableV1Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v1Service.FindPackagesById("Foo");

            // Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Foo", result.First().Id);
            Assert.Equal("1.0.0", result.First().Version);
            Assert.Equal("https://localhost:8081/packages/Foo/1.0.0", result.First().GalleryDetailsUrl);
        }

        [Fact]
        public void V2FeedFindPackagesByIdReturnsUnlistedAndPrereleasePackages()
        {
            // Arrange
            var packageRegistration = new PackageRegistration { Id = "Foo" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package
                            {
                                PackageRegistration = packageRegistration,
                                Version = "1.0.0",
                                IsPrerelease = false,
                                Listed = false,
                                DownloadStatistics = new List<PackageStatistics>()
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistration,
                                Version = "1.0.1-a",
                                IsPrerelease = true,
                                Listed = true,
                                DownloadStatistics = new List<PackageStatistics>()
                            },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(It.IsAny<bool>())).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v2Service.FindPackagesById("Foo");

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Equal("Foo", result.First().Id);
            Assert.Equal("1.0.0", result.First().Version);

            Assert.Equal("Foo", result.Last().Id);
            Assert.Equal("1.0.1-a", result.Last().Version);
        }

        [Theory]
        [InlineData(null, "1.0.0|0.9")]
        [InlineData("", "1.0.0|0.9")]
        [InlineData("   ", "1.0.0|0.9")]
        [InlineData("|   ", "1.0.0|0.9")]
        [InlineData("A", null)]
        [InlineData("A", "")]
        [InlineData("A", "   |")]
        [InlineData("A", "|  ")]
        [InlineData("A|B", "1.0|")]
        public void V2FeedGetUpdatesReturnsEmptyResultsIfInputIsMalformed(string id, string version)
        {
            // Arrange
            var repo = Mock.Of<IEntityRepository<Package>>();
            var configuration = Mock.Of<IConfiguration>();
            var v2Service = new TestableV2Feed(repo, configuration, null);

            // Act
            var result = v2Service.GetUpdates(id, version, includePrerelease: false, includeAllVersions: true, targetFrameworks: null)
                .ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void V2FeedGetUpdatesIgnoresItemsWithMalformedVersions()
        {
            // Arrange
            var packageRegistrationA = new PackageRegistration { Id = "Foo" };
            var packageRegistrationB = new PackageRegistration { Id = "Qux" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.0.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.1.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0-alpha", IsPrerelease = true, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationB, Version = "2.0", IsPrerelease = false, Listed = true },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(false)).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v2Service.GetUpdates("Foo|Qux", "1.0.0|abcd", includePrerelease: false, includeAllVersions: false, targetFrameworks: null)
                .ToList();

            // Assert
            Assert.Equal(1, result.Count);
            AssertPackage(new { Id = "Foo", Version = "1.2.0" }, result[0]);
        }

        [Fact]
        public void V2FeedGetUpdatesReturnsVersionsNewerThanListedVersion()
        {
            // Arrange
            var packageRegistrationA = new PackageRegistration { Id = "Foo" };
            var packageRegistrationB = new PackageRegistration { Id = "Qux" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.0.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.1.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0-alpha", IsPrerelease = true, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationB, Version = "2.0", IsPrerelease = false, Listed = true },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(false)).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v2Service.GetUpdates("Foo|Qux", "1.0.0|0.9", includePrerelease: false, includeAllVersions: true, targetFrameworks: null)
                .ToList();

            // Assert
            Assert.Equal(3, result.Count);
            AssertPackage(new { Id = "Foo", Version = "1.1.0" }, result[0]);
            AssertPackage(new { Id = "Foo", Version = "1.2.0" }, result[1]);
            AssertPackage(new { Id = "Qux", Version = "2.0" }, result[2]);
        }

        [Fact]
        public void V2FeedGetUpdatesReturnsPrereleasePackages()
        {
            // Arrange
            var packageRegistrationA = new PackageRegistration { Id = "Foo" };
            var packageRegistrationB = new PackageRegistration { Id = "Qux" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.0.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.1.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0-alpha", IsPrerelease = true, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationB, Version = "2.0", IsPrerelease = false, Listed = true },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(false)).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v2Service.GetUpdates("Foo|Qux", "1.1.0|0.9", includePrerelease: true, includeAllVersions: true, targetFrameworks: null)
                .ToList();

            // Assert
            Assert.Equal(3, result.Count);
            AssertPackage(new { Id = "Foo", Version = "1.2.0-alpha" }, result[0]);
            AssertPackage(new { Id = "Foo", Version = "1.2.0" }, result[1]);
            AssertPackage(new { Id = "Qux", Version = "2.0" }, result[2]);
        }

        [Fact]
        public void V2FeedGetUpdatesIgnoresDuplicatesInPackageList()
        {
            // Arrange
            var packageRegistrationA = new PackageRegistration { Id = "Foo" };
            var packageRegistrationB = new PackageRegistration { Id = "Qux" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.0.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.1.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0-alpha", IsPrerelease = true, Listed = true },
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.2.0", IsPrerelease = false, Listed = true },
                        new Package { PackageRegistration = packageRegistrationB, Version = "2.0", IsPrerelease = false, Listed = true },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(false)).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result =
                v2Service.GetUpdates("Foo|Qux|Foo", "0.9|1.5|1.1.2", includePrerelease: false, includeAllVersions: true, targetFrameworks: null)
                    .ToList();

            // Assert
            Assert.Equal(2, result.Count);
            AssertPackage(new { Id = "Foo", Version = "1.2.0" }, result[0]);
            AssertPackage(new { Id = "Qux", Version = "2.0" }, result[1]);
        }

        [Fact]
        public void V2FeedGetUpdatesFiltersByTargetFramework()
        {
            // Arrange
            var packageRegistrationA = new PackageRegistration { Id = "Foo" };
            var packageRegistrationB = new PackageRegistration { Id = "Qux" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.0.0", IsPrerelease = false, Listed = true },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "1.1.0",
                                IsPrerelease = false,
                                Listed = true,
                                SupportedFrameworks =
                                    new[]
                                        { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "Net40-Full" } }
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "1.3.0-alpha",
                                IsPrerelease = true,
                                Listed = true,
                                SupportedFrameworks =
                                    new[]
                                        { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "Net40-Full" } }
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "2.0.0",
                                IsPrerelease = false,
                                Listed = true,
                                SupportedFrameworks =
                                    new[] { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "WinRT" } }
                            },
                        new Package { PackageRegistration = packageRegistrationB, Version = "2.0", IsPrerelease = false, Listed = true },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(false)).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v2Service.GetUpdates("Foo|Qux", "1.0|1.5", includePrerelease: false, includeAllVersions: true, targetFrameworks: "net40")
                .ToList();

            // Assert
            Assert.Equal(2, result.Count);
            AssertPackage(new { Id = "Foo", Version = "1.1.0" }, result[0]);
            AssertPackage(new { Id = "Qux", Version = "2.0" }, result[1]);
        }

        [Fact]
        public void V2FeedGetUpdatesFiltersIncludesHighestPrereleasePackage()
        {
            // Arrange
            var packageRegistrationA = new PackageRegistration { Id = "Foo" };
            var packageRegistrationB = new PackageRegistration { Id = "Qux" };
            var repo = new Mock<IEntityRepository<Package>>(MockBehavior.Strict);
            repo.Setup(r => r.GetAll()).Returns(
                new[]
                    {
                        new Package { PackageRegistration = packageRegistrationA, Version = "1.0.0", IsPrerelease = false, Listed = true },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "1.1.0",
                                IsPrerelease = false,
                                Listed = true,
                                SupportedFrameworks =
                                    new[]
                                        { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "Net40-Full" } }
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "1.2.0",
                                IsPrerelease = false,
                                Listed = true,
                                SupportedFrameworks =
                                    new[]
                                        { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "Net40-Full" } }
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "1.3.0-alpha",
                                IsPrerelease = true,
                                Listed = true,
                                SupportedFrameworks =
                                    new[]
                                        { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "Net40-Full" } }
                            },
                        new Package
                            {
                                PackageRegistration = packageRegistrationA,
                                Version = "2.0.0",
                                IsPrerelease = false,
                                Listed = true,
                                SupportedFrameworks =
                                    new[] { new PackageFramework { TargetFramework = "SL5" }, new PackageFramework { TargetFramework = "WinRT" } }
                            },
                        new Package { PackageRegistration = packageRegistrationB, Version = "2.0", IsPrerelease = false, Listed = true },
                    }.AsQueryable());
            var configuration = new Mock<IConfiguration>(MockBehavior.Strict);
            configuration.Setup(c => c.GetSiteRoot(false)).Returns("https://localhost:8081/");
            var v2Service = new TestableV2Feed(repo.Object, configuration.Object, null);

            // Act
            var result = v2Service.GetUpdates("Foo|Qux", "1.0|1.5", includePrerelease: true, includeAllVersions: false, targetFrameworks: "net40")
                .ToList();

            // Assert
            Assert.Equal(2, result.Count);
            AssertPackage(new { Id = "Foo", Version = "1.3.0-alpha" }, result[0]);
            AssertPackage(new { Id = "Qux", Version = "2.0" }, result[1]);
        }

        private static HttpContextBase GetContext(bool isSecure = false)
        {
            var httpRequest = new Mock<HttpRequestBase>();
            httpRequest.Setup(s => s.IsSecureConnection).Returns(isSecure);
            var httpContext = new Mock<HttpContextBase>();
            httpContext.Setup(s => s.Request).Returns(httpRequest.Object);

            return httpContext.Object;
        }


        private static void AssertPackage(dynamic expected, V2FeedPackage package)
        {
            Assert.Equal(expected.Id, package.Id);
            Assert.Equal(expected.Version, package.Version);
        }

        public class TestableV1Feed : V1Feed
        {
            public TestableV1Feed(
                IEntityRepository<Package> repo,
                IConfiguration configuration,
                ISearchService searchService)
                : base(new Mock<IEntitiesContext>().Object, repo, configuration, searchService)
            {
            }

            protected internal override HttpContextBase HttpContext
            {
                get { return GetContext(); }
            }
        }

        public class TestableV2Feed : V2Feed
        {
            public TestableV2Feed(
                IEntityRepository<Package> repo,
                IConfiguration configuration,
                ISearchService searchService)
                : base(new Mock<IEntitiesContext>().Object, repo, configuration, searchService)
            {
            }

            protected internal override HttpContextBase HttpContext
            {
                get { return GetContext(); }
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace NuGetGallery {
    public class DisplayPackageViewModel {
        public DisplayPackageViewModel(Package package) {
            Id = package.PackageRegistration.Id;
            Version = package.Version;
            Description = package.Description;
            Authors = package.Authors;
            Owners = package.PackageRegistration.Owners;
            ProjectUrl = package.ProjectUrl;
            LicenseUrl = package.LicenseUrl;
            LatestVersion = package.IsLatest;
            Prerelease = package.IsPrerelease;
            RatingCount = package.Reviews.Count();
            RatingSum = package.Reviews.Sum(r => r.Rating);
            DownloadCount = package.DownloadCount;
            LastUpdated = package.LastUpdated;
            Tags = package.Tags != null ? package.Tags.Trim().Split(' ') : null;
            PackageVersions = from p in package.PackageRegistration.Packages
                              select new DisplayPackageViewModel(p);
        }

        public string Id { get; set; }
        public string Version { get; set; }

        public IEnumerable<PackageAuthor> Authors { get; set; }
        public ICollection<User> Owners { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string LicenseUrl { get; set; }

        public DateTime LastUpdated { get; set; }
        public bool LatestVersion { get; set; }
        public bool Prerelease { get; set; }

        public int DownloadCount { get; set; }
        public int RatingCount { get; set; }
        public int RatingSum { get; set; }
        public float RatingAverage {
            get {
                if (RatingCount > 0) {
                    return (float)RatingSum / (float)RatingCount;
                }
                return 0;
            }
        }

        public IEnumerable<DisplayPackageViewModel> PackageVersions { get; set; }

        public bool IsOwner(IPrincipal user) {
            if (user == null || user.Identity == null) {
                return false;
            }
            return Owners.Any(u => u.Username == user.Identity.Name);
        }
    }
}
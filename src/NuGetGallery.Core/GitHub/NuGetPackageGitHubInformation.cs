﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGetGallery.GitHub
{
    public class NuGetPackageGitHubInformation
    {
        public const int ReposPerPackage = 10;

        public readonly static NuGetPackageGitHubInformation Empty = new NuGetPackageGitHubInformation(
                                                            0,
                                                            new List<RepositoryInformation>());

        public NuGetPackageGitHubInformation(int totalRepos, IReadOnlyList<RepositoryInformation> repos)
        {
            if (totalRepos < 0)
            {
                throw new IndexOutOfRangeException(string.Format("{0} cannot have a negative value!", nameof(totalRepos)));
            }

            if( repos == null)
            {
                throw new ArgumentNullException(nameof(repos));
            }

            TotalRepos = totalRepos;
            Repos = repos
                .OrderByDescending(x => x.Stars)
                .ThenBy(x => x.Id)
                .Take(ReposPerPackage)
                .ToList();
        }

        public int TotalRepos { get; }
        public IReadOnlyList<RepositoryInformation> Repos { get; }
    }
}

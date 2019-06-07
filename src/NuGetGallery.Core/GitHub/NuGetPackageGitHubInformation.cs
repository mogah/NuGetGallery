﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGetGallery.GitHub
{
    public class NuGetPackageGitHubInformation
    {
        public readonly static NuGetPackageGitHubInformation Empty = new NuGetPackageGitHubInformation(
                                                            0,
                                                            new List<RepositoryInformation>());

        public NuGetPackageGitHubInformation(int totalRepos, IReadOnlyList<RepositoryInformation> repos)
        {
            if (totalRepos < 0)
            {
                throw new IndexOutOfRangeException(string.Format("{0} cannot have a negative value!", nameof(totalRepos)));
            }

            TotalRepos = totalRepos;
            Repos = repos ?? throw new ArgumentNullException(nameof(repos));
        }

        public int TotalRepos { get; }
        public IReadOnlyList<RepositoryInformation> Repos { get; }
    }
}

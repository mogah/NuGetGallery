﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NuGet.Services.Entities;
using NuGetGallery.Framework;
using Xunit;

namespace NuGetGallery.Services
{
    public class PackageDeprecationServiceFacts
    {
        public class TheUpdateDeprecationMethod : TestContainer
        {
            public static IEnumerable<object[]> ThrowsIfPackagesEmpty_Data => MemberDataHelper.AsDataSet(null, new Package[0]);

            [Theory]
            [MemberData(nameof(ThrowsIfPackagesEmpty_Data))]
            public async Task ThrowsIfPackagesEmpty(IReadOnlyList<Package> packages)
            {
                var service = Get<PackageDeprecationService>();

                var user = new User { Key = 1 };
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    service.UpdateDeprecation(
                        packages,
                        PackageDeprecationStatus.NotDeprecated,
                        alternatePackageRegistration: null,
                        alternatePackage: null,
                        customMessage: null,
                        shouldUnlist: false,
                        user: user));
            }

            [Fact]
            public async Task ThrowsIfPackagesHaveDifferentRegistrations()
            {
                var service = Get<PackageDeprecationService>();

                var packages = new[]
                {
                    new Package { PackageRegistrationKey = 1 },
                    new Package { PackageRegistrationKey = 2 },
                };

                var user = new User { Key = 1 };
                await Assert.ThrowsAsync<ArgumentException>(() =>
                    service.UpdateDeprecation(
                        packages,
                        PackageDeprecationStatus.NotDeprecated,
                        alternatePackageRegistration: null,
                        alternatePackage: null,
                        customMessage: null,
                        shouldUnlist: false,
                        user: user));
            }

            [Fact]
            public async Task DeletesExistingDeprecationsIfStatusNotDeprecated()
            {
                // Arrange
                var registration = new PackageRegistration { Id = "theId" };
                var packageWithDeprecation1 = new Package
                {
                    PackageRegistration = registration,
                    Deprecations = new List<PackageDeprecation> { new PackageDeprecation() }
                };

                var packageWithoutDeprecation1 = new Package
                {
                    PackageRegistration = registration
                };

                var packageWithDeprecation2 = new Package
                {
                    PackageRegistration = registration,
                    Deprecations = new List<PackageDeprecation> { new PackageDeprecation() }
                };

                var packageWithoutDeprecation2 = new Package
                {
                    PackageRegistration = registration
                };

                var packages = new[]
                {
                    packageWithDeprecation1,
                    packageWithoutDeprecation1,
                    packageWithDeprecation2,
                    packageWithoutDeprecation2
                };

                var databaseMock = new Mock<IDatabase>();
                databaseMock
                    .Setup(x => x.BeginTransaction())
                    .Returns(Mock.Of<IDbContextTransaction>());

                var context = GetFakeContext();
                context.SetupDatabase(databaseMock.Object);
                context.Deprecations.AddRange(
                    packages
                        .Select(p => p.Deprecations.SingleOrDefault())
                        .Where(d => d != null));

                var packageUpdateService = GetMock<IPackageUpdateService>();
                packageUpdateService
                    .Setup(b => b.UpdatePackagesAsync(packages, null, true))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                var user = new User { Key = 1 };
                var service = Get<PackageDeprecationService>();

                // Act
                await service.UpdateDeprecation(
                    packages,
                    PackageDeprecationStatus.NotDeprecated,
                    alternatePackageRegistration: null,
                    alternatePackage: null,
                    customMessage: null,
                    shouldUnlist: false,
                    user: user);

                // Assert
                context.VerifyCommitChanges();
                Assert.Equal(0, context.Deprecations.Count());
                packageUpdateService.Verify();

                foreach (var package in packages)
                {
                    Assert.Empty(package.Deprecations);
                }
            }

            public static IEnumerable<object[]> ReplacesExistingDeprecations_Data => MemberDataHelper.BooleanDataSet();

            [Theory]
            [MemberData(nameof(ReplacesExistingDeprecations_Data))]
            public async Task ReplacesExistingDeprecations(bool shouldUnlist)
            {
                // Arrange
                var registration = new PackageRegistration { Id = "theId" };
                var lastTimestamp = new DateTime(2019, 3, 4);

                var packageWithDeprecation1 = new Package
                {
                    PackageRegistration = registration,
                    Deprecations = new List<PackageDeprecation> { new PackageDeprecation() }
                };

                var packageWithoutDeprecation1 = new Package
                {
                    PackageRegistration = registration
                };

                var packageWithDeprecation2 = new Package
                {
                    PackageRegistration = registration,
                    Deprecations = new List<PackageDeprecation>
                    {
                        new PackageDeprecation
                        {
                        }
                    }
                };

                var packageWithoutDeprecation2 = new Package
                {
                    PackageRegistration = registration
                };

                var packageWithDeprecation3 = new Package
                {
                    PackageRegistration = registration,
                    Deprecations = new List<PackageDeprecation>
                    {
                        new PackageDeprecation
                        {
                        }
                    }
                };

                var packages = new[]
                {
                    packageWithDeprecation1,
                    packageWithoutDeprecation1,
                    packageWithDeprecation2,
                    packageWithoutDeprecation2,
                    packageWithDeprecation3
                };

                var transactionMock = new Mock<IDbContextTransaction>();
                transactionMock
                    .Setup(x => x.Commit())
                    .Verifiable();

                var databaseMock = new Mock<IDatabase>();
                databaseMock
                    .Setup(x => x.BeginTransaction())
                    .Returns(transactionMock.Object);

                var context = GetFakeContext();
                context.SetupDatabase(databaseMock.Object);
                context.Deprecations.AddRange(
                    packages
                        .Select(p => p.Deprecations.SingleOrDefault())
                        .Where(d => d != null));

                var packageUpdateService = GetMock<IPackageUpdateService>();
                packageUpdateService
                    .Setup(b => b.UpdatePackagesAsync(packages, shouldUnlist ? false : (bool?)null, true))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                var service = Get<PackageDeprecationService>();

                var status = (PackageDeprecationStatus)99;

                var alternatePackageRegistration = new PackageRegistration();
                var alternatePackage = new Package();

                var customMessage = "message";
                var user = new User { Key = 1 };

                // Act
                await service.UpdateDeprecation(
                    packages,
                    status,
                    alternatePackageRegistration,
                    alternatePackage,
                    customMessage,
                    shouldUnlist,
                    user);

                // Assert
                context.VerifyCommitChanges();
                databaseMock.Verify();
                transactionMock.Verify();

                packageUpdateService.Verify();

                Assert.Equal(packages.Count(), context.Deprecations.Count());
                foreach (var package in packages)
                {
                    var deprecation = package.Deprecations.Single();
                    Assert.Contains(deprecation, context.Deprecations);
                    Assert.Equal(status, deprecation.Status);
                    Assert.Equal(alternatePackageRegistration, deprecation.AlternatePackageRegistration);
                    Assert.Equal(alternatePackage, deprecation.AlternatePackage);
                    Assert.Equal(customMessage, deprecation.CustomMessage);
                }
            }
        }

        public class TheGetDeprecationByPackageMethod : TestContainer
        {
            [Fact]
            public void GetsDeprecationOfPackage()
            {
                // Arrange
                var key = 190304;
                var package = new Package
                {
                    Key = key
                };

                var differentDeprecation = new PackageDeprecation
                {
                    PackageKey = 9925
                };

                var matchingDeprecation = new PackageDeprecation
                {
                    PackageKey = key
                };

                var context = GetFakeContext();
                context.Deprecations.AddRange(
                    new[] { differentDeprecation, matchingDeprecation });

                // Act
                var deprecation = Get<PackageDeprecationService>()
                    .GetDeprecationByPackage(package);

                // Assert
                Assert.Equal(matchingDeprecation, deprecation);
            }

            [Fact]
            public void ThrowsIfMultipleDeprecationsOfPackage()
            {
                // Arrange
                var key = 190304;
                var package = new Package
                {
                    Key = key
                };

                var matchingDeprecation1 = new PackageDeprecation
                {
                    PackageKey = key
                };

                var matchingDeprecation2 = new PackageDeprecation
                {
                    PackageKey = key
                };

                var context = GetFakeContext();
                context.Deprecations.AddRange(
                    new[] { matchingDeprecation1, matchingDeprecation2 });

                // Act / Assert
                Assert.Throws<InvalidOperationException>(
                    () => Get<PackageDeprecationService>().GetDeprecationByPackage(package));
            }
        }
    }
}

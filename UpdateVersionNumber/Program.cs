using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using NuGet;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using UpdateVersionNumber.Util;

namespace UpdateVersionNumber
{
    class Program
    {
        static void Main(string[] args)
        {
            var hash = CreateMd5ForFolder(Path.Combine(args[0]));
            var account = CloudStorageAccount.Parse(args[1]);
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference("nuget");
            table.CreateIfNotExistsAsync().Wait();

            var latest = GetLatestVersion(table);
            var latestVersion =
                latest != null ? new Version(latest.Major + "." + latest.Minor + "." + latest.Patch) : null;

            Console.WriteLine($"Hash: {hash} (latest: {(latest?.Hash ?? "none")})");

            var projectFile = Path.Combine(args[0], "AzurePipelineAsCode.NET.csproj");
            var project = XDocument.Parse(File.ReadAllText(projectFile));
            var projectVersion = new Version(project.Root.Elements("PropertyGroup").First().Element("Version").Value);
            var nextVersion = latest?.Hash == hash ? latestVersion : NextVersion(latestVersion, projectVersion);



            if (nextVersion > latestVersion)
            {
                var next = new NugetPackageEntity(nextVersion.Major.ToString(), nextVersion.Minor.ToString(), nextVersion.Build.ToString(), hash);
                table.ExecuteAsync(TableOperation.Insert(next)).Wait();
            }

            project.Root.Elements("PropertyGroup").First().Element("Version").SetValue(nextVersion.Major + "." + nextVersion.Minor + "." + nextVersion.Build);
            File.WriteAllText(projectFile, project.ToString());
            PackNuget(nextVersion, args[0]);

        }

        private static void PackNuget(Version nextVersion, string path)
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            var sourceRepository = new SourceRepository(packageSource, providers);
            var packageMetadataResource = sourceRepository.GetResource<PackageMetadataResource>();
            var version = packageMetadataResource.GetMetadataAsync("AzurePipelineAsCode.NET", true, true, new NullLogger(), CancellationToken.None).Result
                .OfType<PackageSearchMetadata>()
                .Max(m => m.Version.Version);

            if (nextVersion <= version)
            {
                Console.WriteLine($"Skipping nuget pack, because the target version {nextVersion} is not greater than the latest published version {version}");
                Execute.Command("dotnet", $"build {path} -c Release");
                return;
            }

            Console.WriteLine($"Packing nuget version {nextVersion}");
            Execute.Command("dotnet", $"pack {Path.Combine(path, "AzurePipelineAsCode.NET.csproj")} -c Release");
        }

        private static Version NextVersion(Version latest, Version projectVersion)
        {
            Version nextVersion;
            if (latest == null)
            {
                nextVersion = projectVersion;
            }
            else
            {
                nextVersion = projectVersion > latest
                    ? projectVersion
                    : new Version(latest.Major, latest.Minor, latest.Build + 1);
            }

            return nextVersion;
        }

        private static NugetPackageEntity GetLatestVersion(CloudTable table)
        {
            try
            {
                var segment = table.ExecuteQuerySegmentedAsync(new TableQuery<NugetPackageEntity>(), null).Result;
                return segment.Results.FirstOrDefault();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static string CreateMd5ForFolder(string path)
        {
            var binObj = Path.Combine("bin", "obj");

            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                                 .OrderBy(p => p)
                .Where(f => !f.Contains(binObj)).ToList();

            var md5 = MD5.Create();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                // hash path
                var relativePath = file.Substring(path.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower().Replace(Path.DirectorySeparatorChar, '_'));
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                var contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                {
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                }
                else
                {
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }
    }
}

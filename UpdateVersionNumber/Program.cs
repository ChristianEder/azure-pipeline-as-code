using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace UpdateVersionNumber
{
    class Program
    {
        static void Main(string[] args)
        {
            var hash = CreateMd5ForFolder(Path.Combine(args[0], "Tasks"));
            var account = CloudStorageAccount.Parse(args[1]);
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference("nuget");
            table.CreateIfNotExistsAsync().Wait();

            var latest = GetLatestVersion(table);
            Console.WriteLine(latest);
            if(latest?.Hash != hash)
            {
                var projectFile = Path.Combine(args[0], "AzurePipelineAsCode.NET.csproj");
                var project = XDocument.Parse(File.ReadAllText(projectFile));
                var projectVersion = new Version(project.Root.Elements("PropertyGroup").First().Element("Version").Value);
                Version nextVersion = NextVersion(latest, projectVersion);

                var next = new NugetPackageEntity(nextVersion.Major.ToString(), nextVersion.Minor.ToString(), nextVersion.Build.ToString(), hash);


                project.Root.Elements("PropertyGroup").First().Element("Version").SetValue(next.Major + "." + next.Minor + "." + next.Patch);
                File.WriteAllText(projectFile, project.ToString());

                table.ExecuteAsync(TableOperation.Insert(next)).Wait();
            }
        }

        private static Version NextVersion(NugetPackageEntity latest, Version projectVersion)
        {
            Version nextVersion;
            if (latest == null)
            {
                nextVersion = projectVersion;
            }
            else
            {
                var latestVersion = new Version(latest.Major + "." + latest.Minor + "." + latest.Patch);
                nextVersion = projectVersion > latestVersion 
                    ? projectVersion 
                    : new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build + 1);
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
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

            var md5 = MD5.Create();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                // hash path
                var relativePath = file.Substring(path.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
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

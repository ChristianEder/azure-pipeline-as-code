using Microsoft.WindowsAzure.Storage.Table;

namespace UpdateVersionNumber
{
    public class NugetPackageEntity : TableEntity
    {
        public NugetPackageEntity()
        {
            PartitionKey = "AzurePipelineAsCode.NET";
        }

        public NugetPackageEntity(string major, string minor, string patch, string hash) : this()
        {
            RowKey = (999 - int.Parse(major)) + "_" + (999 - int.Parse(minor)) + "_" + (999 - int.Parse(patch));
            Major = major;
            Minor = minor;
            Patch = patch;
            Hash = hash;
        }

        public string Major { get; set; }
        public string Minor { get; set; }
        public string Patch { get; set; }
        public string Hash { get; set; }
    }
}

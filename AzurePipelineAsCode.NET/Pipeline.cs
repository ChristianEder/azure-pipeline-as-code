using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzurePipelineAsCode.NET
{
    public class Pipeline
    {
        public string Name { get; set; }
        public bool Clean { get; set; } = true;

        public List<BuildJob> BuildJobs { get; } = new List<BuildJob>();

        public List<Repository> Repositories { get; } = new List<Repository>();

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(Name))
            {
                builder.AppendLine("name: " + Name);
            }

            builder.AppendLine("resources:");
            builder.AppendLine("- repo: self");
            builder.AppendLine("- clean: " + Clean);

            if (Repositories.Any())
            {
                builder.AppendLine("  repositories:");

                foreach (var repo in Repositories)
                {
                    builder.AppendLine("  - repository: " + repo.Identifier);
                    builder.AppendLine("    type: " + repo.Type);
                    builder.AppendLine("    name: " + repo.Name);
                }
            }

            foreach (var job in BuildJobs)
            {
                builder.AppendLine(job.ToString());
            }


            return builder.ToString();
        }
    }

    public class Repository
    {
        public string Identifier { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }
}

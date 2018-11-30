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

        public Pool Pool { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(Name))
            {
                builder.AppendLine("name: " + Name);
            }

            if (Pool != null)
            {
                builder.AppendLine(Pool.ToString());
            }

            if (Clean != true)
            {
                builder.AppendLine("resources:");
                builder.AppendLine("- repo: self");
                builder.AppendLine("- clean: " + Clean);
                builder.AppendLine();
            }

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

            if (BuildJobs.Count == 1 && string.IsNullOrEmpty(BuildJobs.Single().DisplayName) &&
                string.IsNullOrEmpty(BuildJobs.Single().Name))
            {
                builder.AppendLine("steps: ");
                foreach (var step in BuildJobs.Single().Steps)
                {
                    builder.AppendLine(step.ToString());
                }
            }
            else
            {
                foreach (var job in BuildJobs)
                {
                    builder.AppendLine(job.ToString());
                }
            }

            var pipeline = builder.ToString();
            return pipeline.TrimEnd('\r', '\n') + Environment.NewLine;
        }
    }
}

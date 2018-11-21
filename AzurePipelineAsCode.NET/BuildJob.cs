﻿using AzurePipelineAsCode.NET.Tasks;
using System.Collections.Generic;
using System.Text;

namespace AzurePipelineAsCode.NET
{
    public class BuildJob
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public List<IBuildTask> Steps { get; } = new List<IBuildTask>();

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine("- job: " + Name);
            builder.AppendLine("  displayName: " + DisplayName);
            builder.AppendLine("  steps: ");

            foreach (var step in Steps)
            {
                builder.AppendLine(step.ToString());
            }

            return builder.ToString();
        }
    }
}
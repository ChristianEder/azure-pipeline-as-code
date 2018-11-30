using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzurePipelineAsCode.NET
{
    public class Pool
    {
        public string Name { get; set; }
        public string VmImage { get; set; }
        public List<string> Demands { get; } = new List<string>();

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine("pool:");
            if (!string.IsNullOrEmpty(Name))
            {
                builder.AppendLine("  name: " + Name);
            }
            if (!string.IsNullOrEmpty(VmImage))
            {
                builder.AppendLine("  vmImage: '" + VmImage + "'");
            }

            if (Demands.Any())
            {
                builder.AppendLine("  demands:");
                foreach (var demand in Demands)
                {
                    builder.AppendLine("  - " + demand);
                }
            }


            return builder.ToString();
        }
    }
}
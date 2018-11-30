using System.Text;

namespace AzurePipelineAsCode.NET.Tasks
{
    public class ScriptTask : IBuildTask, IReleaseTask
    {
        public string Script { get; set; }
        public string WorkingDirectory { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine("- script: " + Script);
            if (!string.IsNullOrEmpty(DisplayName))
            {
                builder.AppendLine("  displayName: '" + DisplayName + "'");
            }
            if (!string.IsNullOrEmpty(WorkingDirectory))
            {
                builder.AppendLine("  workingDirectory: " + WorkingDirectory);
            }

            return builder.ToString();
        }
    }
}
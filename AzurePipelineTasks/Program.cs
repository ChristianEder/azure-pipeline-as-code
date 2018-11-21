using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AzurePipelineTasks.Util;
using Newtonsoft.Json.Linq;

namespace AzurePipelineTasks
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetFolder = Path.Combine(args[0], "Tasks");
            Clean(targetFolder);

            var currentWorkingDir = Directory.GetCurrentDirectory();
            try
            {
                CloneAndEnterAzurePipelineTasksRepository();
                RenderTasks(targetFolder);
            }

            finally
            {
                Directory.SetCurrentDirectory(currentWorkingDir);
            }
        }

        private static void RenderTasks(string targetFolder)
        {
            var tasksDefinitionFiles = Directory.GetFiles("Tasks", "task.json", SearchOption.AllDirectories);
            var taskDefinitions = tasksDefinitionFiles.Select(f => JObject.Parse(File.ReadAllText(f))).ToArray();

            Directory.SetCurrentDirectory("..");

            CreateTaskInterfaces(targetFolder, taskDefinitions);

            var allTaskInterfaces = GetTaskInterfaces(taskDefinitions);

            foreach (var taskDefinition in taskDefinitions)
            {
                RenderTask(targetFolder, allTaskInterfaces, taskDefinition);
            }
        }

        private static void RenderTask(string targetFolder, string[] allTaskInterfaces, JObject taskDefinition)
        {
            var className = taskDefinition["name"].ToString() + "V" + taskDefinition["version"]["Major"].ToString();
            var interfaces = taskDefinition.ContainsKey("visibility") ? (taskDefinition["visibility"] as JArray).Select(v => v.ToString()).ToArray() : allTaskInterfaces;
            var taskName = taskDefinition["name"].ToString() + "@" + taskDefinition["version"]["Major"].ToString();
            var inputs = (taskDefinition["inputs"] as JArray ?? new JArray()).OfType<JObject>().ToArray();

            var inputTypes = new Dictionary<string, string>();

            File.WriteAllText(Path.Combine(targetFolder, $"{className}.cs"), $@"namespace AzurePipelineAsCode.NET.Tasks
{{
    /// <summary>
    /// {taskDefinition["friendlyName"]}
    /// {taskDefinition["description"].ToString().Replace(Environment.NewLine, Environment.NewLine + "    ///")}
    /// </summary>
    public class {className} : {string.Join(", ", interfaces.Select(i => "I" + i + "Task"))}
    {{
{string.Join(Environment.NewLine, inputs.Select(i => RenderTaskInput(i, inputTypes)).Where(i => !string.IsNullOrEmpty(i)))}

        public string DisplayName {{ get; set; }} = ""{taskName}"";
        public override string ToString()
        {{
            var builder = new System.Text.StringBuilder();

            builder.AppendLine(""  - task: {taskName}"");
            builder.AppendLine(""    displayName: "" + DisplayName);

            var wroteInputs = false;

{string.Join(Environment.NewLine, inputs.Where(i => inputTypes.ContainsKey(i["name"].ToString())).Select(i => $@"            if({ToUpper(i["name"].ToString())} != {(HasDefaultValue(i) ? ToUpper(i["name"].ToString()) + "DefaultValue" : $"default({inputTypes[i["name"].ToString()]})")})
            {{
                if(!wroteInputs)
                {{
                    builder.AppendLine(""    inputs: "");
                    wroteInputs = true;
                }}
                builder.AppendLine(""      {i["name"].ToString()}: "" + {ToUpper(i["name"].ToString())}.ToString());
            }}
"))}
        
            return builder.ToString();
        }}
    }}
}}
");
        }

        private static bool HasDefaultValue(JObject input)
        {
            return !string.IsNullOrEmpty(input.ContainsKey("defaultValue") ? input["defaultValue"].ToString() : string.Empty);
        }

        private static string RenderTaskInput(JObject input, Dictionary<string, string> types)
        {
            var name = input["name"].ToString();
            name = ToUpper(name);

            var defaultValue = input.ContainsKey("defaultValue") ? input["defaultValue"].ToString() : string.Empty;

            string enumType = string.Empty;
            string type;
            switch (input["type"].ToString().ToLowerInvariant())
            {
                case "string":
                case "multiline":
                case "filepath":
                    type = "string";
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? string.Empty : "@\"" + defaultValue.Replace("\"", "\"\"") + "\"";
                    break;
                case "boolean":
                    type = "bool";
                    defaultValue = defaultValue.ToLowerInvariant();
                    break;
                case "radio":
                case "picklist":
                    if (!input.ContainsKey("options"))
                    {
                        type = "string";
                        defaultValue = string.IsNullOrEmpty(defaultValue) ? string.Empty : "@\"" + defaultValue.Replace("\"", "\"\"") + "\"";
                        break;
                    }
                    type = $"{name}Options";
                    var options = (input["options"] as JObject).Children().OfType<JProperty>().ToArray();
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        var defaultOption = options.FirstOrDefault(p => p.Value.ToString() == defaultValue) ?? options.FirstOrDefault(p => p.Name.ToString() == defaultValue);
                        defaultValue = defaultOption.Name;
                        defaultValue = $"{type}.{EnumValueName(defaultValue)}";
                    }
                    enumType = EnumType(name, options);
                    break;
                case "int":
                    type = "int";
                    break;
                case "securefile":
                case "connectedservice:vsmobilecenter":
                case "connectedservice:azurerm":
                case "connectedservice:azure:certificate,usernamepassword":
                case "connectedservice:azure":
                case "connectedservice:externaltfs":
                case "connectedservice:dockerregistry":
                case "connectedservice:chef":
                case "connectedservice:ssh":
                case "connectedservice:generic":
                case "connectedservice:dockerhost":
                case "connectedservice:externalnugetfeed":
                case "connectedservice:kubernetes":
                case "connectedservice:jenkins":
                case "identities":
                case "connectedservice:externalnpmregistry":
                case "connectedservice:externalpythondownloadfeed":
                case "connectedservice:azureservicebus":
                case "querycontrol":
                case "connectedservice:servicefabric":
                case "connectedservice:externalpythonuploadfeed":
                    // Pull requests welcome :-)
                    return null;
                default:
                    throw new InvalidOperationException();
            }

            types.Add(input["name"].ToString(), type);

            defaultValue = !string.IsNullOrEmpty(defaultValue) ? (" = " + defaultValue + ";") : string.Empty;

            var defaultValueProperty = string.Empty;
            if (!string.IsNullOrEmpty(defaultValue))
            {
                defaultValueProperty = $"        public static {type} {name}DefaultValue {{ get; }}{defaultValue}{Environment.NewLine}";
                defaultValue = $" = {name}DefaultValue;";
            }


            return enumType + defaultValueProperty + $"        public {type} {name} {{ get; set; }}{defaultValue}";
        }

        private static string EnumType(string name, JProperty[] options)
        {
            return $@"        public class {name}Options 
        {{ 
            public string Key {{ get; }}
            public string Value {{ get; }}

            private {name}Options(string key, string value)
            {{
                Key = key;
                Value = value;
            }}

            public override string ToString()
            {{
                return Key;
            }}
            
{string.Join(Environment.NewLine, options.Where(p => !string.IsNullOrEmpty(p.Name)).Select(o => $"            public static readonly {name}Options {EnumValueName(o.Name)} = new {name}Options(\"{o.Name}\", \"{o.Value.ToString()}\");"))} 
        }}
";
        }

        private static string EnumValueName(string name)
        {
            var value = ToUpper(name
                .Replace(".", "_")
                .Replace(" ", "_")
                .Replace(":", "_")
                .Replace("-", "_")
                .Replace("/", "_")
                .Replace("|", "_").ToString());

            if (new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }.Contains(value[0]))
            {
                value = "_" + value;
            }

            return value;
        }

        private static string ToUpper(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
        }

        private static void Clean(string targetFolder)
        {
            if (Directory.Exists(targetFolder))
            {
                foreach (var file in Directory.GetFiles(targetFolder))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(targetFolder);
            }
        }

        private static void CloneAndEnterAzurePipelineTasksRepository()
        {
            if (Directory.Exists("azure-pipelines-tasks"))
            {
                Directory.SetCurrentDirectory("azure-pipelines-tasks");
                Execute.Command("git", "pull");
            }
            else
            {
                Execute.Command("git", "clone https://github.com/Microsoft/azure-pipelines-tasks.git");
                Directory.SetCurrentDirectory("azure-pipelines-tasks");
            }
        }
        private static void CreateTaskInterfaces(string targetFolder, JObject[] taskDefinitions)
        {
            var taskInterfaces = GetTaskInterfaces(taskDefinitions);

            foreach (var taskInterface in taskInterfaces)
            {
                File.WriteAllText(Path.Combine(targetFolder, $"I{taskInterface}Task.cs"), $@"namespace AzurePipelineAsCode.NET.Tasks
{{
    public interface I{taskInterface}Task
    {{
    }}
}}
");
            }
        }

        private static string[] GetTaskInterfaces(JObject[] taskDefinitions)
        {
            return taskDefinitions.Where(t => t.ContainsKey("visibility"))
                            .SelectMany(t => (t["visibility"] as JArray).Select(v => v.ToString())).Distinct().ToArray();
        }
    }
}

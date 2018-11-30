using System;
using System.IO;
using AzurePipelineAsCode.NET;
using AzurePipelineAsCode.NET.Tasks;

namespace Pipeline
{
    class Program
    {
        static void Main(string[] args)
        {
            var pipeline = new AzurePipelineAsCode.NET.Pipeline
            {
                Pool = new Pool
                {
                    VmImage = "Ubuntu 16.04"
                },
                BuildJobs =
                {
                    new BuildJob
                    {
                        Steps =
                        {
                            new ScriptTask
                            {
                                Script = "dotnet build",
                                DisplayName = "Build code generator",
                                WorkingDirectory = "AzurePipelineTasks"
                            },
                            new ScriptTask
                            {
                                Script = "dotnet run -- ../AzurePipelineAsCode.NET",
                                DisplayName = "Generate code",
                                WorkingDirectory = "AzurePipelineTasks"
                            },
                            new ScriptTask
                            {
                                Script = "dotnet build",
                                DisplayName = "Build generated code",
                                WorkingDirectory = "AzurePipelineAsCode.NET"
                            },
                            new ScriptTask
                            {
                                Script = "dotnet run -- \"../AzurePipelineAsCode.NET\" \"$(nugetVersionStorage)\"",
                                DisplayName = "Create nuget with updated version number, if required",
                                WorkingDirectory = "UpdateVersionNumber"
                            },
                            new PublishBuildArtifactsV1
                            {
                                DisplayName = "Publish nuget as artifact",
                                PathtoPublish = "AzurePipelineAsCode.NET/bin/Release",
                                ArtifactName = "AzurePipelineAsCode.NET"
                            }
                        }
                    }
                }
            };

            File.WriteAllText(args[0], pipeline.ToString());
        }
    }
}

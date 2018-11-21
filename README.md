# AzurePipelineAsCode.NET

- The AzurePipelineTasks project implements a command line tool that generates C# code that allows modelling Azure DevOps pipelines and generate the corresponding YAML pipeline definitions from that model. It uses the [azure-pipelines-tasks](https://github.com/Microsoft/azure-pipelines-tasks) as input for the code generation (specifically the task.json definition files in that repo)
- The AzurePipelineAsCode.NET project is the target for the code generation, and is used to package up the generated code into a nuget
- An automated build will run the code generation on a regular basis and publish updated nuget packages automatically, if changes occured


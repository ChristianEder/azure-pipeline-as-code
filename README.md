# AzurePipelineAsCode.NET

[![Build Status](https://christianederzuehlke.visualstudio.com/aprg%20Structurizr%20Infrastructure%20as%20Code/_apis/build/status/ChristianEder.azure-pipeline-as-code)](https://christianederzuehlke.visualstudio.com/aprg%20Structurizr%20Infrastructure%20as%20Code/_build/latest?definitionId=7)

[![AzurePipelineAsCode.NET](https://img.shields.io/nuget/v/AzurePipelineAsCode.NET.png "Latest nuget package for AzurePipelineAsCode.NET")](https://www.nuget.org/packages/AzurePipelineAsCode.NET/)

- The AzurePipelineTasks project implements a command line tool that generates C# code that allows modelling Azure DevOps pipelines and generate the corresponding YAML pipeline definitions from that model. It uses the [azure-pipelines-tasks](https://github.com/Microsoft/azure-pipelines-tasks) as input for the code generation (specifically the task.json definition files in that repo)
- The AzurePipelineAsCode.NET project is the target for the code generation, and is used to package up the generated code into a nuget
- An automated build will run the code generation on a regular basis and publish updated nuget packages automatically, if changes occured


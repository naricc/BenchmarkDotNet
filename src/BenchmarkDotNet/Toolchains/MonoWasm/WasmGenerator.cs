﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;



namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmGenerator : CsProjGenerator

    {
        private string RuntimePackPath;
        private string WasmAppBuilderAssembly;
        private string MainJS;

        public WasmGenerator(string targetFrameworkMoniker,
                             string cliPath,
                             string runtimePackPath,
                             string wasmAppBuilderAssembly,
                             string mainJS
                             )
            : base(targetFrameworkMoniker,
                   cliPath,
                   null,
                   null)
        {
            RuntimePackPath = runtimePackPath;
            WasmAppBuilderAssembly = wasmAppBuilderAssembly;
            MainJS = mainJS;

        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);
            using (var file = new StreamReader(File.OpenRead(projectFile.FullName)))
            {
                var (customProperties, sdkName) = GetSettingsThatNeedsToBeCopied(file, projectFile);

                string content = new StringBuilder(ResourceHelper.LoadTemplate("WasmCsProj.txt"))
                   .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                   .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                   .Replace("$CSPROJPATH$", projectFile.FullName)
                   .Replace("$TFM$", TargetFrameworkMoniker)
                   .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                   .Replace("$RUNTIMESETTINGS$", GetRuntimeSettings(benchmark.Job.Environment.Gc, buildPartition.Resolver))
                   .Replace("$COPIEDSETTINGS$", customProperties)
                   .Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration)
                   .Replace("$SDKNAME$", sdkName)
                   .Replace("$RUNTIMEPACKDIR$", RuntimePackPath)
                   .Replace("$WASMAPPBUILDERASSEMBLY$", WasmAppBuilderAssembly)
                   .Replace("$MAINJS$", MainJS)
                   .ToString();

                File.WriteAllText(artifactsPaths.ProjectFilePath, content);
            }

        }

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
    => Path.Combine(buildArtifactsDirectoryPath, "bin/net5.0/browser-wasm/publish/output");

        private static string GetPackagesDirectoryPath(bool useTempFolderForRestore, string packagesRestorePath)
    => packagesRestorePath ?? (useTempFolderForRestore ? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) : null);
    }
}
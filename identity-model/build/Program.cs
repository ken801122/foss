﻿using System;
using System.IO;
using System.Threading.Tasks;

using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    internal static class Program
    {
        private const string packOutput = "./artifacts";
        private const string envVarMissing = " environment variable is missing. Aborting.";

        private static class Targets
        {
            public const string RestoreTools = "restore-tools";
            public const string CleanBuildOutput = "clean-build-output";
            public const string CleanPackOutput = "clean-pack-output";
            public const string Build = "build";
            public const string Test = "test";
            public const string Pack = "pack";
            public const string SignPackage = "sign-package";
            public const string TrimmableAnalysis = "trimmable-analysis";
        }

        static async Task Main(string[] args)
        {
            Target(Targets.RestoreTools, () =>
            {
                Run("dotnet", "tool restore");
            });

            Target(Targets.CleanBuildOutput, () =>
            {
                Run("dotnet", "clean -c Release -v m --nologo");
            });

            Target(Targets.Build, DependsOn(Targets.CleanBuildOutput), () =>
            {
                Run("dotnet", "build -c Release --nologo");
            });

            Target(Targets.Test, DependsOn(Targets.Build), () =>
            {
                Run("dotnet",
                    $"test test/IdentityModel.Tests -c Release --nologo "
                    + $"--blame-hang "
                    + $"--blame-hang-timeout=120sec "
                    + $"--logger \"console;verbosity=normal\" --logger \"trx;LogFileName=Test.trx\"");
            });

            Target(Targets.CleanPackOutput, () =>
            {
                if (Directory.Exists(packOutput))
                {
                    Directory.Delete(packOutput, true);
                }
            });

            Target(Targets.Pack, DependsOn(Targets.Build, Targets.CleanPackOutput), () =>
            {
                Run("dotnet", $"pack src/IdentityModel.csproj -c Release -o {Directory.CreateDirectory(packOutput).FullName} --no-build --nologo");
            });

            Target(Targets.SignPackage, DependsOn(Targets.Pack, Targets.RestoreTools), () =>
            {
                SignNuGet();
            });

            Target(Targets.TrimmableAnalysis, () =>
            {
                Run("dotnet", "publish test/TrimmableAnalysis -c Release -r win-x64");
            });

            Target("default", DependsOn(Targets.Test, Targets.Pack, Targets.TrimmableAnalysis));

            Target("sign", DependsOn(Targets.RestoreTools), SignNuGet);

            await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.ExitCodeException || ex.Message.EndsWith(envVarMissing));
        }

        private static void SignNuGet()
        {
            var signClientSecret = Environment.GetEnvironmentVariable("SignClientSecret");

            if (string.IsNullOrWhiteSpace(signClientSecret))
            {
                throw new Exception($"SignClientSecret{envVarMissing}");
            }

            foreach (var file in Directory.GetFiles(packOutput, "*.nupkg", SearchOption.AllDirectories))
            {
                Console.WriteLine($"  Signing {file}");

                Run("dotnet",
                        "NuGetKeyVaultSignTool " +
                        $"sign {file} " +
                        "--file-digest sha256 " +
                        "--timestamp-rfc3161 http://timestamp.digicert.com " +
                        "--azure-key-vault-url https://duendecodesigning.vault.azure.net/ " +
                        "--azure-key-vault-client-id 18e3de68-2556-4345-8076-a46fad79e474 " +
                        "--azure-key-vault-tenant-id ed3089f0-5401-4758-90eb-066124e2d907 " +
                        $"--azure-key-vault-client-secret {signClientSecret} " +
                        "--azure-key-vault-certificate CodeSigning"
                        ,noEcho: true);
            }
        }
    }
}
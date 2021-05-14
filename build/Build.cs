using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions("ci", GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] {"main"},
    OnPullRequestBranches = new[] {"main"},
    AutoGenerate = true,
    InvokedTargets = new[] {nameof(Ci)},
    CacheKeyFiles = new string[0])]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;

    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    [Solution] readonly Solution Solution;

    AbsolutePath OutputDirectory => RootDirectory / "output";
    
    public static int Main() => Execute<Build>(x => x.Package);

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetClean();
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target Package => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject("NukeNugetSampleLibrary")
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDirectory)
                .SetVersion(GitVersion.NuGetVersionV2)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target Ci => _ => _
        .DependsOn(Package);
}
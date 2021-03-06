#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=GitVersion.CommandLine"
#addin "Cake.Incubator"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var toolpath = Argument("toolpath", @"");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./Artifacts") + Directory(configuration);
GitVersion gitVersion = null; 

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("GitVersion").Does(() => {
    gitVersion = GitVersion(new GitVersionSettings {
        UpdateAssemblyInfo = true
	});
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./LiquidProjections.PollingEventStore.sln", new NuGetRestoreSettings 
	{ 
		NoCache = true,
		Verbosity = NuGetVerbosity.Detailed,
		ToolPath = "./build/nuget.exe"
	});
});

Task("Build")
    .IsDependentOn("GitVersion")
    .Does(() =>
{
  // Use MSBuild
  MSBuild("./LiquidProjections.PollingEventStore.sln", settings => {
	settings.ToolPath = String.IsNullOrEmpty(toolpath) ? settings.ToolPath : toolpath;
	settings.ToolVersion = MSBuildToolVersion.VS2017;
	settings.PlatformTarget = PlatformTarget.MSIL;
	settings.SetConfiguration(configuration);
  });
});

Task("Run-Unit-Tests")
    .Does(() =>
{
    XUnit2("./Tests/LiquidProjections.PollingEventStore.Specs/**/bin/" + configuration + "/*.Specs.dll", new XUnit2Settings {
        });
});

Task("Pack")
    .IsDependentOn("GitVersion")
    .Does(() => 
    {
      NuGetPack("./src/.nuspec", new NuGetPackSettings {
        OutputDirectory = "./Artifacts",
        Version = gitVersion.NuGetVersionV2,
		Properties = new Dictionary<string, string> {
			{ "nugetversion", gitVersion.NuGetVersionV2 }
		}
      });        
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("Restore-NuGet-Packages")
	.IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Pack");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
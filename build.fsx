#r @"packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open Fake.Git
open Fake.MSBuildHelper
open Fake.Testing.XUnit2
open Fake.PaketTemplate
open Fake.DotNetCli

let buildDir = "./build/"
let sourceDir = "./source/"

let haliteProjectReferences = "./source/Halite/Halite.csproj"
let examplesProjectReferences = "./source/Halite.Examples/Halite.Examples.csproj" 

let projectName = "Halite"
let description = "Library for representing HAL objects and links."
let version = environVarOrDefault "version" "0.0.0"
let assemblyGuid = "670c2953-95c3-493c-a39c-987105130378"
let commitHash = Information.getCurrentSHA1(".")
let templateFilePath = "./Halite.paket.template"
let toolPathPaket = ".paket/paket.exe"

Target "Clean" (fun _ ->
  CleanDirs [buildDir]
)

let buildReleaseProperties = 
  [ "Configuration", "Release"
    "DocumentationFile", "Halite.xml" ]

Target "AddAssemblyVersion" (fun _ -> 
    let assemblyInfos = !!(@"../**/AssemblyInfo.cs") 

    ReplaceAssemblyInfoVersionsBulk assemblyInfos (fun f -> 
        { f with 
            AssemblyFileVersion = version
            AssemblyVersion = version })  
)

Target "Build" (fun _ -> 
    DotNetCli.Build (fun p -> 
        { p with
            Output = "../../" + buildDir
            Configuration = "Release"
            Project = haliteProjectReferences }) 
)

Target "BuildExamples" (fun _ ->
    DotNetCli.Build (fun p ->
    { p with 
        Output = "../../" + buildDir
        Configuration = "Release"
        Project = examplesProjectReferences })
)

Target "CreateNugetPackage" (fun _ -> 
    let description = "Implementation of the HAL specification."
    DotNetCli.Pack (fun c -> 
        { c with
            Configuration = "Release"
            Project = haliteProjectReferences
            AdditionalArgs = [ 
                                "/p:PackageVersion=" + version
                                "/p:Version=" + version]
            OutputPath = "../../" + buildDir
        }
    )
)

Target "CreatePaketTemplate" (fun _ ->
  PaketTemplate (fun p ->
    let targetLib = "lib/netstandard2.0"
    {
        p with
          TemplateFilePath = Some templateFilePath
          TemplateType = File
          Description = ["Implementation of the HAL specification."]
          Id = Some projectName
          ProjectUrl = Some "https://github.com/nrkno/halite"
          Version = Some version
          Authors = ["NRK"]
          Files = [ Include (buildDir @@ "Halite.dll", targetLib)
                    Include (buildDir @@ "Halite.pdb", targetLib)
                    Include (buildDir @@ "Halite.xml", targetLib)
                   ]
          Dependencies = 
            [ "JetBrains.Annotations", GreaterOrEqual (Version "11.1.0") ]
    } )
)

Target "CreatePackage" (fun _ ->
    Paket.Pack (fun p ->
      {
          p with
              Version = version
              ReleaseNotes = "fake release"
              OutputPath = buildDir
              TemplateFile = templateFilePath
              BuildConfig = "Release"
              ToolPath = toolPathPaket })
)

Target "PushPackage" (fun _ ->
  Paket.Push (fun p -> 
      {
          p with
            ApiKey = environVarOrDefault "nugetKey" ""
            PublishUrl = "https://www.nuget.org/api/v2/package"
            ToolPath = toolPathPaket
            WorkingDir = "./build/"
      })
)

"Clean"
==> "AddAssemblyVersion"
==> "Build"
==> "BuildExamples"
==> "CreatePaketTemplate"
==> "CreatePackage"
==> "PushPackage"

RunTargetOrDefault "CreatePackage"

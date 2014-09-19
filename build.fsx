// -----------------------------------------------------------------------------
// FAKE build script
// -----------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "FakeLib.dll"

#if MONO
#else
#load "packages/SourceLink.Fake/tools/SourceLink.fsx"
#endif

open System
open System.IO
open Fake
open Fake.AssemblyInfoFile
open Fake.Git

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let (!!) includes = (!! includes).SetBaseDirectory __SOURCE_DIRECTORY__

// -----------------------------------------------------------------------------
// Information about the project used on NuGet and in the AssemblyInfo files
// -----------------------------------------------------------------------------

let project = "BsonProvider"
let authors = ["Max Hirschhorn"]
let summary = "Type provider for BSON."
let description = "Type provider for BSON."
let tags = "F# fsharp data typeprovider bson mongodb mongo"

let gitHome = "https://github.com/visemet"
let gitName = "FSharp.Data.Bson"
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/visemet"

// Read release notes and version info from RELEASE_NOTES.md
let release =
    File.ReadLines "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let isAppVeyorBuild = environVar "APPVEYOR" <> null
let nugetVersion =
    if isAppVeyorBuild then
        let now = DateTime.UtcNow.ToString "yyMMddHHmm"
        sprintf "%s-a%s" release.NugetVersion now
    else release.NugetVersion

Target "AppVeyorBuildVersion" <| fun _ ->
    let args = sprintf "UpdateBuild -Version \"%s\"" nugetVersion
    Shell.Exec("appveyor", args) |> ignore

// -----------------------------------------------------------------------------
// Generate the AssemblyInfo files with the correct version number and
// up-to-date information about the project

Target "AssemblyInfo" <| fun () ->
    for file in !! "src/AssemblyInfo*.fs" do
        let replace (oldValue:string) newValue (str:string) =
            str.Replace(oldValue, newValue)
        let title =
            Path.GetFileNameWithoutExtension file
            |> replace "AssemblyInfo" "BsonProvider"
        let versionSuffix = ".0"
        let version = release.AssemblyVersion + versionSuffix
        CreateFSharpAssemblyInfo file
           [ Attribute.Title title
             Attribute.Product project
             Attribute.Description summary
             Attribute.Version version
             Attribute.FileVersion version ]

// -----------------------------------------------------------------------------
// Restore any NuGet packages

Target "RestorePackages" RestorePackages

// -----------------------------------------------------------------------------
// Clean up any build artifacts from the library and test projects, as well as
// any generated documentation

Target "Clean" <| fun () ->
    CleanDirs ["bin"; "src/bin"; "src/obj"]

Target "CleanTests" <| fun () ->
    CleanDirs [ "tests/BsonProvider.DesignTime.Tests/bin"
                "tests/BsonProvider.DesignTime.Tests/obj" ]

Target "CleanDocs" <| fun () ->
    CleanDirs ["docs/output"]

// -----------------------------------------------------------------------------
// Build the library and test projects

Target "Build" <| fun () ->
    !! "BsonProvider.sln"
    |> MSBuildRelease "" "Rebuild"
    |> ignore

Target "BuildTests" <| fun () ->
    !! "BsonProvider.Tests.sln"
    |> MSBuildRelease "" "Rebuild"
    |> ignore

// -----------------------------------------------------------------------------
// Run the unit tests using a sequential NUnit test runner

Target "RunTests" <| ignore

let runTestTask name =
    let taskName = sprintf "RunTest_%s" name
    Target taskName <| fun () ->
        !! (sprintf "tests/*/bin/Release/%s.dll" name)
        |> NUnit (fun p ->
            { p with
                DisableShadowCopy = true
                TimeOut = TimeSpan.FromMinutes 20.
                Framework = "4.0"
                Domain = MultipleDomainModel
                OutputFile = "TestResults.xml" })
    taskName ==> "RunTests" |> ignore

["BsonProvider.DesignTime.Tests"]
|> List.iter runTestTask

// -----------------------------------------------------------------------------
// Source link the PDB files

#if MONO
Target "SourceLink" <| id
#else
open SourceLink

Target "SourceLink" <| fun () ->
    let baseUrl = sprintf "%s/%s/{0}/%%var2%%" gitRaw gitName
    use repo = new GitRepo(__SOURCE_DIRECTORY__)
    for file in !! "src/*.fsproj" do
        let proj = VsProj.LoadRelease file
        logfn "source linking %s" proj.OutputFilePdb
        let files = proj.Compiles -- "**/AssemblyInfo*.fs"
        repo.VerifyChecksums files
        proj.VerifyPdbChecksums files
        proj.CreateSrcSrv baseUrl repo.Revision (repo.Paths files)
        Pdbstr.exec proj.OutputFilePdb proj.OutputFilePdbSrcSrv
    CopyFiles "bin" (!! "src/bin/Release/FSharp.Data.Bson.*")
#endif

// -----------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" <| fun () ->
    // Format the release notes
    let releaseNotes = release.Notes |> String.concat "\n"
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = nugetVersion
            ReleaseNotes = releaseNotes
            Tags = tags
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        "nuget/BsonProvider.nuspec"

// -----------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" <| fun () ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] []
    |> ignore

// -----------------------------------------------------------------------------
// Release the generated documentation or compiled binaries

let publishFiles what branch fromFolder toFolder =
    let tempFolder = "temp/" + branch
    CleanDir tempFolder

    let gitUrl = gitHome + "/" + gitName + ".git"
    Repository.cloneSingleBranch "" gitUrl branch tempFolder

    fullclean tempFolder
    CopyRecursive fromFolder (tempFolder + "/" + toFolder) true
    |> tracefn "%A"

    StageAll tempFolder
    Commit tempFolder <| sprintf "Update %s for version %s."
                                 what release.NugetVersion
    Branches.push tempFolder

Target "ReleaseDocs" <| fun () ->
    publishFiles "generated documentation" "gh-pages" "docs/output" ""

Target "ReleaseBinaries" <| fun () ->
    publishFiles "binaries" "release" "bin" "bin"

Target "Release" DoNothing

"CleanDocs" ==> "GenerateDocs" ==> "ReleaseDocs"

"ReleaseDocs" ==> "Release"
"ReleaseBinaries" ==> "Release"
"NuGet" ==> "Release"

// -----------------------------------------------------------------------------
// Help

Target "Help" <| fun () ->
    printfn ""
    printfn "  Please specify the target by calling 'build <target>'"
    printfn ""
    printfn "  Targets for building:"
    printfn "    1) Build"
    printfn "    2) BuildTests"
    printfn "    3) RunTests"
    printfn "    4) All (calls 1, 2, and 3)"
    printfn ""
    printfn "  Targets for releasing:"
    printfn "    5) GenerateDocs"
    printfn "    6) ReleaseDocs (calls 5)"
    printfn "    7) ReleaseBinaries"
    printfn "    8) NuGet (creates package only - does not publish)"
    printfn "    9) Release (calls 5, 6, 7, and 8)"
    printfn ""
    printfn "  Other targets:"
#if MONO
#else
    printfn "    10) SourceLink (requires autocrlf=input)"
#endif
    printfn ""

Target "All" DoNothing

"Clean" ==> "RestorePackages"  ==> "AssemblyInfo" ==> "Build"
"CleanTests" ==> "BuildTests"

"Build" ==> "All"
"BuildTests" ==> "All"
"RunTests" ==> "All"

RunTargetOrDefault "Help"

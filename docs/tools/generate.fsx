// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [ "FSharp.Data.Bson.dll"; "FSharp.Data.Bson.Runtime.dll" ]
// Web site location for the generated documentation
let repo = "https://github.com/visemet/FSharp.Data.Bson"
let website = "/FSharp.Data.Bson"

// Specify more information about your project
let info =
  [ "project-name", "Bson Provider"
    "project-author", "Max Hirschhorn"
    "project-summary", "A type provider for BSON."
    "project-github", "http://github.com/visemet/FSharp.Data.Bson"
    "project-nuget", "https://nuget.org/packages/FSharp.Data.Bson" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#I "../../packages/FAKE/tools"
#load "../../packages/FSharp.Formatting/FSharp.Formatting.fsx"
#r "NuGet.Core.dll"
#r "FakeLib.dll"

open System.IO
open Fake
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "../../bin"
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files       = __SOURCE_DIRECTORY__ @@ "../files"
let data       = __SOURCE_DIRECTORY__ @@ "../content/data"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/FSharp.Formatting"
let docTemplate = formatting @@ "templates/docpage.cshtml"

// Where to look for *.cshtml templates (in this order)
let layoutRoots =
  [ templates
    formatting @@ "templates"
    formatting @@ "templates/reference" ]

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  ensureDirectory (output @@ "data")
  CopyRecursive data (output @@ "data") true |> Log "Copying data files: "
  CopyRecursive files output true |> Log "Copying files: "

  ensureDirectory (output @@ "content")
  CopyRecursive (formatting @@ "styles") (output @@ "content") true
    |> Log "Copying styles and scripts: "

let references =
  if isMono then
    // Workaround compiler errors in Razor-ViewEngine
    let d = RazorEngine.Compilation.ReferenceResolver.UseCurrentAssembliesReferenceResolver()
    let loadedList = d.GetReferences () |> Seq.map (fun r -> r.GetFile()) |> Seq.cache
    // We replace the list and add required items manually as mcs doesn't like duplicates...
    let getItem name = loadedList |> Seq.find (fun l -> l.Contains name)
    Some [ (getItem "FSharp.Core").Replace("4.3.0.0", "4.3.1.0")
           Path.GetFullPath "../../packages/FSharp.Compiler.Service/lib/net40/FSharp.Compiler.Service.dll"
           Path.GetFullPath "../../packages/FSharp.Formatting/lib/net40/System.Web.Razor.dll"
           Path.GetFullPath "../../packages/FSharp.Formatting/lib/net40/RazorEngine.dll"
           Path.GetFullPath "../../packages/FSharp.Formatting/lib/net40/FSharp.Literate.dll"
           Path.GetFullPath "../../packages/FSharp.Formatting/lib/net40/FSharp.CodeFormat.dll"
           Path.GetFullPath "../../packages/FSharp.Formatting/lib/net40/FSharp.MetadataFormat.dll" ]
  else None

// Build API reference from XML comments
let buildReference () =
  CleanDir (output @@ "reference")
  MetadataFormat.Generate
    ( referenceBinaries |> List.map ((@@) bin),
      output @@ "reference",
      layoutRoots,
      parameters = ("root", root) :: info,
      sourceRepo = repo @@ "tree/master",
      sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
      publicOnly = true, libDirs = [bin],
      ?assemblyReferences = references )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output @@ sub, replacements = ("root", root) :: info,
        layoutRoots = layoutRoots,
        ?assemblyReferences = references,
        generateAnchors = true )

// Generate
copyFiles()
buildReference()
buildDocumentation()

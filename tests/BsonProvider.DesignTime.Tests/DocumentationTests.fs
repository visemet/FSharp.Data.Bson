(* Copyright (c) 2013-2014 Tomas Petricek and Gustavo Guerra
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *)

#if INTERACTIVE
#I "../../packages/FSharp.Compiler.Service.0.0.44/lib/net40"
#I "../../packages/FSharp.Formatting.2.4.8/lib/net40"
#I "../../packages/Microsoft.AspNet.Razor.2.0.30506.0/lib/net40"
#I "../../packages/NUnit.2.6.3/lib"
#I "../../packages/RazorEngine.3.3.0/lib/net40"
#r "FSharp.Compiler.Service.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.Literate.dll"
#r "FSharp.MetadataFormat.dll"
#r "nunit.framework.dll"
#r "RazorEngine.dll"
#r "System.Web.Razor.dll"
#else
module BsonProvider.DesignTime.Tests.DocumentationTests
#endif

open System.IO
open FSharp.Literate
open FSharp.CodeFormat
open NUnit.Framework

// Initialization of the test - lookup the documentation files,
// create temp folder for the output and load the F# compiler DLL

let (@@) a b = Path.Combine(a, b)

let sources = __SOURCE_DIRECTORY__ @@ "../../docs/content"

let output = Path.GetTempPath() @@ "BsonProvider.Docs"

if Directory.Exists(output) then Directory.Delete(output, true)
Directory.CreateDirectory(output) |> ignore

/// Process a specified file in the documentation folder and return
/// the total number of unexpected errors found (print them to the output too)
let processFile file =
    printfn "Processing '%s'" file

    let dir = Path.GetDirectoryName(Path.Combine(output, file))
    if not (Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore

    let literateDoc = Literate.ParseScriptFile(Path.Combine(sources, file))
    literateDoc.Errors
    |> Seq.choose (fun (SourceError(startl, endl, kind, msg)) ->
        if msg <> "Multiple references to 'mscorlib.dll' are not permitted" then
            Some <| sprintf "%A %s (%s)" (startl, endl) msg file
        else None)
    |> String.concat "\n"

// ------------------------------------------------------------------------------------
// Core API documentation

let docFiles =
    seq {
        for sub in ["library"] do
            for file in Directory.EnumerateFiles(Path.Combine(sources, sub), "*.fsx") do
                yield sub + "/" + Path.GetFileName(file)
    }

#if INTERACTIVE
for file in docFiles do
    printfn "%s" (processFile file)
#else

[<Test>]
[<TestCaseSource "docFiles">]
let ``Documentation generated correctly`` file =
    let errors = processFile file
    if errors <> "" then
        Assert.Fail("Found errors when processing file '" + file + "':\n" + errors)

#endif

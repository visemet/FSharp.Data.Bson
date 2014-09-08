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
#load "SetupTesting.fsx"
SetupTesting.generateSetupScript __SOURCE_DIRECTORY__ "BsonProvider.DesignTime"
#load "__setup__BsonProvider.DesignTime__.fsx"
#else
module internal Test
#endif

open System
open System.IO
open System.Net
open BsonProvider.ProviderImplementation

let (++) a b = Path.Combine(a, b)
let resolutionFolder = ""
let outputFolder = __SOURCE_DIRECTORY__ ++ ".." ++ "tests" ++ "BsonProvider.DesignTime.Tests" ++ "expected"
let assemblyName = "FSharp.Data.Bson.dll"

type Platform = Net40 | Portable47

let dump signatureOnly ignoreOutput platform saveToFileSystem (inst:TypeProviderInstantiation) =
    let runtimeAssembly =
        match platform with
        | Net40 -> __SOURCE_DIRECTORY__ ++ ".." ++ "bin" ++ assemblyName
        | Portable47 -> __SOURCE_DIRECTORY__ ++ ".." ++ "bin" ++ "portable47" ++ assemblyName
    inst.Dump resolutionFolder (if saveToFileSystem then outputFolder else "") runtimeAssembly signatureOnly ignoreOutput
    |> Console.WriteLine

let dumpAll inst =
    dump false false Net40 false inst
    dump false false Portable47 false inst

Bson { Sample = "optionals.bson"
       SampleIsList = false
       RootName = ""
       Culture = ""
       Encoding = ""
       ResolutionFolder = ""
       EmbeddedResource = "" }
|> dumpAll

let testCases =
    __SOURCE_DIRECTORY__ ++ ".." ++ "tests" ++ "BsonProvider.DesignTime.Tests" ++ "SignatureTestCases.config"
    |> File.ReadAllLines
    |> Array.map TypeProviderInstantiation.Parse

for testCase in testCases do
    dump false false Net40 true testCase

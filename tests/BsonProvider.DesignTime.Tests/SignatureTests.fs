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
#r "../../packages/NUnit.2.6.3/lib/nunit.framework.dll"
#r "../../bin/FSharp.Data.DesignTime.dll"
#r "../../bin/BsonProvider.DesignTime.dll"
#load "../Common/FsUnit.fs"
#else
module BsonProvider.DesignTime.Tests.SignatureTests
#endif

open System.IO
open FsUnit
open NUnit.Framework
open BsonProvider.ProviderImplementation

let (++) a b = Path.Combine(a, b)

let sourceDirectory = __SOURCE_DIRECTORY__

let testCases =
    sourceDirectory ++ "SignatureTestCases.config"
    |> File.ReadAllLines
    |> Array.map TypeProviderInstantiation.Parse

let expectedDirectory = sourceDirectory ++ "expected"

let resolutionFolder = sourceDirectory ++ "TestData"
let assemblyName = "FSharp.Data.Bson.Runtime.dll"
let runtimeAssembly = sourceDirectory ++ ".." ++ ".." ++ "bin" ++ assemblyName

let generateAllExpected() =
    if not <| Directory.Exists expectedDirectory then
        Directory.CreateDirectory expectedDirectory |> ignore
    for testCase in testCases do
        testCase.Dump resolutionFolder expectedDirectory runtimeAssembly (*signatureOnly*)false (*ignoreOutput*)false
        |> ignore

let normalize (str:string) =
  str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("@\"<RESOLUTION_FOLDER>\"", "\"<RESOLUTION_FOLDER>\"")

[<Test>]
[<TestCaseSource "testCases">]
let ``Validate signature didn't change`` (testCase:TypeProviderInstantiation) =
    let expected = testCase.ExpectedPath expectedDirectory |> File.ReadAllText |> normalize
    let output = testCase.Dump resolutionFolder "" runtimeAssembly (*signatureOnly*)false (*ignoreOutput*)false |> normalize
    if output <> expected then
        printfn "Obtained Signature:\n%s" output
    output |> should equal expected

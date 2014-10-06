(* Copyright (c) 2013-2014 Tomas Petricek and Gustavo Guerra
 * Copyright (c) 2014 Max Hirschhorn
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

namespace BsonProvider.ProviderImplementation

open System
open System.IO
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open BsonProvider.Runtime

type BsonProviderArgs =
    { Path : string
      InferLimit : int
      RootName : string
      ResolutionFolder : string
      EmbeddedResource : string }

type TypeProviderInstantiation =
    | Bson of BsonProviderArgs

    member x.GenerateType resolutionFolder runtimeAssembly =
        let f, args =
            match x with
            | Bson x ->
                (fun cfg -> new BsonProvider(cfg) :> TypeProviderForNamespaces),
                [| box x.Path
                   box x.InferLimit
                   box x.RootName
                   box x.ResolutionFolder
                   box x.EmbeddedResource |]
        Debug.generate resolutionFolder runtimeAssembly f args

    override x.ToString() =
        match x with
        | Bson x ->
            [ "Bson"
              x.Path
              x.InferLimit.ToString()
              x.RootName ]
        |> String.concat ","

    member x.ExpectedPath outputFolder =
        Path.Combine(outputFolder, sprintf "%O.expected" x)

    member x.Dump resolutionFolder outputFolder runtimeAssembly signatureOnly ignoreOutput =
        let replace (oldValue:string) (newValue:string) (str:string) = str.Replace(oldValue, newValue)
        let output =
            x.GenerateType resolutionFolder runtimeAssembly
            |> Debug.prettyPrint signatureOnly ignoreOutput 10 100
            |> if String.IsNullOrEmpty resolutionFolder then id
               else replace resolutionFolder "<RESOLUTION_FOLDER>"
        if outputFolder <> "" then
            File.WriteAllText(x.ExpectedPath outputFolder, output)
        output

    static member Parse (line:string) =
        let args = line.Split [| ',' |]
        match args.[0] with
        | "Bson" ->
            Bson { Path = args.[1]
                   InferLimit = int args.[2]
                   RootName = args.[3]
                   ResolutionFolder = ""
                   EmbeddedResource = "" }
        | _ -> failwithf "Unknown: %s" args.[0]

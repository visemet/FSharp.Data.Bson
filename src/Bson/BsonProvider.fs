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
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Quotations
open MongoDB.Bson
open MongoDB.Bson.Serialization
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open ProviderImplementation.ProviderHelpers
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open BsonProvider
open BsonProvider.Runtime
open BsonProvider.Runtime.IO
open BsonProvider.ProviderImplementation
open BsonProvider.ProviderImplementation.Members

// ----------------------------------------------------------------------------------------------

#nowarn "10001"

[<TypeProvider>]
type public BsonProvider(cfg:TypeProviderConfig) as this =
    inherit DisposableTypeProviderForNamespaces()

    do DependencyResolver.init()

    // Generate namespace and type 'BsonProvider.BsonProvider'
    let asm = Assembly.ReflectionOnlyLoadFrom cfg.RuntimeAssembly
    let ns = "BsonProvider"
    let bsonProvTy = ProvidedTypeDefinition(asm, ns, "BsonProvider", Some typeof<obj>)

    let buildTypes (typeName:string) (args:obj[]) =

        // Generate the required type
        let tpType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<IBsonTop>)

        let path = args.[0] :?> string
        let inferLimit = args.[1] :?> int
        let resolutionFolder = args.[2] :?> string

        let getSpecFromSamples samples =

            let inferedType =
                [ for sampleBson in samples -> BsonInference.inferType "" sampleBson ]
                |> Seq.fold (StructuralInference.subtypeInfered (*allowEmptyValues*)false) StructuralTypes.Top

            let ctx = BsonGenerationContext.Create(tpType)
            let result = BsonTypeBuilder.generateRecordType ctx inferedType

            { GeneratedType = tpType
              RepresentationType = result.ConvertedType
              CreateFromStream = fun stream ->
                result.GetConverter ctx <@@ BsonTop.ParseList(%stream) @@> }

        let getSamplesFromPath path =

            let resolver =
                { ResolutionType = DesignTime
                  DefaultResolutionFolder = cfg.ResolutionFolder
                  ResolutionFolder = resolutionFolder }

            let path = path |> getPath resolver

            let limit =
                if inferLimit <= 0 then None
                else Some inferLimit

            let maybeTruncate =
                let flip f x y = f y x
                flip <| Option.fold (flip Seq.truncate)

            File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)
            |> BsonTop.ParseSeq
            |> maybeTruncate limit

        let spec = getSamplesFromPath path |> getSpecFromSamples

        createAllMembers cfg spec path resolutionFolder
        |> tpType.AddMembers

        spec.GeneratedType

    // Add static parameter that specifies the API we want to get (compile-time)
    let parameters =
        [ ProvidedStaticParameter("Path", typeof<string>)
          ProvidedStaticParameter("InferLimit", typeof<int>, parameterDefaultValue = 100)
          ProvidedStaticParameter("ResolutionFolder", typeof<string>, parameterDefaultValue = "") ]

    let helpText =
         """<summary>
                Typed representation of a BSON document.
            </summary>
            <param name='Path'>
                Location of a BSON sample file.
            </param>
            <param name='InferLimit'>
                Number of documents to use for inference. Defaults to 100.
                If this is set as zero, then all documents are used.
            </param>
            <param name='ResolutionFolder'>
                Directory used for resolving relative file references
                (at design time and in hosted execution).
            </param>"""

    do bsonProvTy.AddXmlDoc helpText
    do bsonProvTy.DefineStaticParameters(parameters, buildTypes)

    // Register the main type with F# compiler
    do this.AddNamespace(ns, [ bsonProvTy ])

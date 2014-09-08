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
open Microsoft.FSharp.Core.CompilerServices
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
open BsonProvider.ProviderImplementation.Members

// ----------------------------------------------------------------------------------------------

#nowarn "10001"

[<TypeProvider>]
type public BsonProvider(cfg:TypeProviderConfig) as this =
    inherit DisposableTypeProviderForNamespaces()

    // Generate namespace and type 'BsonProvider.BsonProvider'
    let asm, version, replacer = AssemblyResolver.init cfg
    let ns = "BsonProvider"
    let bsonProvTy = ProvidedTypeDefinition(asm, ns, "BsonProvider", Some typeof<BsonValue>)

    let buildTypes (typeName:string) (args:obj[]) =

        // Generate the required type
        let tpType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>)

        let path = args.[0] :?> string
        let inferLimit = args.[1] :?> int
        let rootName = args.[2] :?> string
        let resolutionFolder = args.[3] :?> string
        let resource = args.[4] :?> string

        let getSpecFromSamples samples =

            let inferedType =
                [ for sampleBson in samples -> BsonInference.inferType "" sampleBson ]
                |> Seq.fold (StructuralInference.subtypeInfered (*allowEmptyValues*)false) StructuralTypes.Top

            let nameOverride =
                if String.IsNullOrWhiteSpace rootName then "Root"
                else NameUtils.singularize rootName

            let ctx = BsonGenerationContext.Create(tpType, replacer)
            let result = BsonTypeBuilder.generateBsonType ctx (*canPassAllConversionCallingTypes*)false
                    (*optionalityHandledByParent*)false nameOverride inferedType

            { GeneratedType = tpType
              RepresentationType = result.ConvertedType
              CreateFromStream = fun stream ->
                    result.GetConverter ctx <@@ BsonTop.CreateList(%stream) @@> }

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
            |> BsonTop.Parse
            |> maybeTruncate limit

        let spec = getSamplesFromPath path |> getSpecFromSamples

        createAllMembers cfg spec path resolutionFolder resource
        |> tpType.AddMembers

        spec.GeneratedType

    // Add static parameter that specifies the API we want to get (compile-time)
    let parameters =
        [ ProvidedStaticParameter("Path", typeof<string>)
          ProvidedStaticParameter("InferLimit", typeof<int>, parameterDefaultValue = 100)
          ProvidedStaticParameter("RootName", typeof<string>, parameterDefaultValue = "Root")
          ProvidedStaticParameter("ResolutionFolder", typeof<string>, parameterDefaultValue = "")
          ProvidedStaticParameter("EmbeddedResource", typeof<string>, parameterDefaultValue = "") ]

    let helpText =
        """<summary>Typed representation of a BSON document.</summary>
           <param name='Path'>Location of a BSON sample file.</param>
           <param name='InferLimit'>Number of documents to use for inference. Defaults to `100`.
                If this is set as zero then all documents are used.</param>
           <param name='RootName'>Name used for the root type. Defaults to `Root`.</param>
           <param name='ResolutionFolder'>Directory used for resolving relative file references
                (at design time and in hosted execution).</param>
           <param name='EmbeddedResource'>When specified, the type provider first attempts to load
                the file from the specified resource (e.g. `MyCompany.MyAssembly, resource_name.bson`).
                This is useful for when exposing types generated by the type provider.</param>"""

    do bsonProvTy.AddXmlDoc helpText
    do bsonProvTy.DefineStaticParameters(parameters, buildTypes)

    // Register the main type with F# compiler
    do this.AddNamespace(ns, [ bsonProvTy ])

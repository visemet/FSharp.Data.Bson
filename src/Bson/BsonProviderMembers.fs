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
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open BsonProvider.Runtime
open BsonProvider.Runtime.IO

#nowarn "10001"

[<AutoOpen>]
module internal Helpers =

    // Carries part of the information needed to generate the type
    type TypeProviderSpec =
        { // the generated type
          GeneratedType : ProvidedTypeDefinition
          // the representation type (may or may not be the same type as what is returned by the constructor)
          RepresentationType : Type
          // the constructor from a stream to the representation
          CreateFromStream : Expr<Stream> -> Expr }

    let invalidChars = [ for c in "\"|<>{}[]," -> c ] @ [ for i in 0..31 -> char i ] |> set

    let tryGetUri str =
        if String.IsNullOrWhiteSpace str || Seq.exists invalidChars.Contains str then None
        else
            match Uri.TryCreate(str, UriKind.RelativeOrAbsolute) with
            | false, _ -> None
            | true, uri -> Some uri

    let getPath (resolver:UriResolver) path =
        match tryGetUri path with
        | None -> failwith "path does not represent a location"
        | Some uri ->
            match resolver.TryResolveToPath uri with
            | None -> failwith "path could not be resolved as a file"
            | Some path -> path

module internal Members =

    /// Generates the static GetSamples() method
    let private createGetSamplesMember (spec:TypeProviderSpec) path (resolver:UriResolver) =

        let resolver = { resolver with ResolutionType = DesignTime }
        let path = path |> getPath resolver

        let returnType = spec.RepresentationType.MakeArrayType()
        let m = ProvidedMethod("GetSamples", [], returnType, IsStaticMethod = true)

        m.InvokeCode <- fun _ ->
            <@ File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read) :> Stream @>
            |> spec.CreateFromStream

        m.AddXmlDoc "Returns the entire set of sample BSON documents"
        m

    /// Generates the static ReadAll() method
    let private createReadAllMember (spec:TypeProviderSpec) =

        let args = [ ProvidedParameter("stream", typeof<Stream>) ]
        let returnType = spec.RepresentationType.MakeArrayType()
        let m = ProvidedMethod("ReadAll", args, returnType, IsStaticMethod = true)

        m.InvokeCode <- fun (Singleton stream) ->
            stream |> Expr.Cast |> spec.CreateFromStream

        m.AddXmlDoc "Reads BSON from the specified stream"
        m

    let createAllMembers (cfg:TypeProviderConfig) (spec:TypeProviderSpec)
                         path resolutionFolder resource =

        let resolver =
            { ResolutionType = Runtime
              DefaultResolutionFolder = cfg.ResolutionFolder
              ResolutionFolder = resolutionFolder }

        [ createGetSamplesMember spec path resolver :> MemberInfo
          createReadAllMember spec :> MemberInfo ]

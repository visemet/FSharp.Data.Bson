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

namespace BsonProvider.Runtime

open System
open System.IO

/// Helpers called from the generated code for working with URIs
module IO =

    let private (++) a b = Path.Combine(a,b)

    let private isWeb (uri:Uri) = uri.IsAbsoluteUri && not uri.IsUnc
                                                    && uri.Scheme <> Uri.UriSchemeFile

    type UriResolutionType =
       | DesignTime
       | Runtime
       | RuntimeInFsi

    type UriResolver = {
        ResolutionType : UriResolutionType
        DefaultResolutionFolder : string
        ResolutionFolder : string
    } with

        /// Resolve the absolute location of a file or web resource
        /// according to the same rules as the standard F# type providers
        member x.Resolve (uri:Uri) =
            if uri.IsAbsoluteUri then uri, isWeb uri
            else
                let root =
                    match x.ResolutionType with
                    | DesignTime ->
                        if String.IsNullOrEmpty x.ResolutionFolder
                        then x.DefaultResolutionFolder
                        else x.ResolutionFolder

                    | RuntimeInFsi -> x.DefaultResolutionFolder
                    | Runtime -> AppDomain.CurrentDomain.BaseDirectory

                Uri(root ++ uri.OriginalString, UriKind.Absolute), false

        /// Optionally resolve the absolute path of a file
        member x.TryResolveToPath (uri:Uri) =
            match x.Resolve uri with
            | _, true -> None
            | uri, false -> Some (Uri.UnescapeDataString uri.AbsolutePath)

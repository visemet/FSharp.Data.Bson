(* Copyright (c) 2012 BlueMountain Capital Management, LLC
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *
 * Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *)

namespace BsonProvider.ProviderImplementation

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices

module DependencyResolver =

    let mutable private initialized = false

    /// Attempts to find the assembly specified by its name among the
    /// assemblies loaded in the current application domain
    let resolveReferencedAssembly asmName =
        let loadedAsm =
            AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.filter (fun asm -> asm.GetName().Name <> "mscorlib")
            |> Seq.tryFind (fun asm ->
                AssemblyName.ReferenceMatchesDefinition(asmName, asm.GetName()))
        defaultArg loadedAsm null

    let init() =
        let initOnce() =
            AppDomain.CurrentDomain.add_AssemblyResolve(fun _ args ->
                resolveReferencedAssembly <| AssemblyName args.Name)

        if not initialized then
            initialized <- true
            initOnce()

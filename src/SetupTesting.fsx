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

#r "System.Xml.Linq"

open System
open System.IO
open System.Xml.Linq

let generateSetupScript dir proj =

    let getElemName name = XName.Get(name, "http://schemas.microsoft.com/developer/msbuild/2003")

    let getElemValue name (parent:XElement) =
        let elem = parent.Element(getElemName name)
        if elem = null || String.IsNullOrEmpty elem.Value then None else Some(elem.Value)

    let getAttrValue name (elem:XElement) =
        let attr = elem.Attribute(XName.Get name)
        if attr = null || String.IsNullOrEmpty attr.Value then None else Some(attr.Value)

    let (|??) (option1: 'a Option) option2 =
        if option1.IsSome then option1 else option2

    let fsProjFile = Path.Combine(dir, proj + ".fsproj")
    let fsProjXml = XDocument.Load fsProjFile

    let refs =
        fsProjXml.Document.Descendants(getElemName "Reference")
        |> Seq.choose (fun elem -> getElemValue "HintPath" elem |?? getAttrValue "Include" elem)
        |> Seq.map (fun ref -> ref.Replace(@"\", @"\\").Split(',').[0])
        |> Seq.filter (fun ref -> ref <> "mscorlib" && ref <> "FSharp.Core")
        |> Seq.map (fun ref -> "#r \"" + ref + "\"")
        |> Seq.toList

    let fsFiles =
        fsProjXml.Document.Descendants(getElemName "Compile")
        |> Seq.choose (fun elem -> getAttrValue "Include" elem)
        |> Seq.filter (Path.GetExtension >> ((<>) ".fsi"))
        |> Seq.map (fun path -> "#load \"" + path + "\"")
        |> Seq.toList

    let tempFile = Path.Combine(dir, "__setup__" + proj + "__.fsx")
    File.WriteAllLines(tempFile, refs @ fsFiles)

namespace BsonProvider.Runtime

open System
open System.IO

/// Helper functions called from the generated code for working with files
module internal IO =

    let private (++) a b = Path.Combine(a,b)

    let isWeb (uri:Uri) = uri.IsAbsoluteUri && not uri.IsUnc && uri.Scheme <> Uri.UriSchemeFile

    type UriResolutionType =
        | DesignTime
        | Runtime
        | RuntimeInFSI

    type UriResolver =

        { ResolutionType : UriResolutionType
          DefaultResolutionFolder : string
          ResolutionFolder : string }

        /// Resolve the absolute location of a file (or web URL) according to the rules
        /// used by standard F# type providers as described here:
        /// https://github.com/fsharp/fsharpx/issues/195#issuecomment-12141785
        ///
        ///  * if it is web resource, just return it
        ///  * if it is full path, just return it
        ///  * otherwise.
        ///
        ///    At design-time:
        ///      * if the user specified resolution folder, use that
        ///      * otherwise use the default resolution folder
        ///    At run-time:
        ///      * if the user specified resolution folder, use that
        ///      * if it is running in F# interactive (config.IsHostedExecution)
        ///        use the default resolution folder
        ///      * otherwise, use 'CurrentDomain.BaseDirectory'
        /// returns an absolute uri * isWeb flag
        member x.Resolve(uri:Uri) =
            if uri.IsAbsoluteUri then uri, isWeb uri
            else
                let root =
                    match x.ResolutionType with
                    | DesignTime ->
                        if String.IsNullOrEmpty x.ResolutionFolder
                        then x.DefaultResolutionFolder
                        else x.ResolutionFolder

                    | RuntimeInFSI -> x.DefaultResolutionFolder
                    | Runtime -> AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/')

                Uri(root ++ uri.OriginalString, UriKind.Absolute), false

        member x.TryResolveToPath(uri:Uri) =
            match x.Resolve uri with
            | _, true -> None
            | uri, false -> Some uri.AbsolutePath

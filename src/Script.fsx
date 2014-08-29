#I "../packages/mongocsharpdriver.1.9.2/lib/net35"
#r "../packages/mongocsharpdriver.1.9.2/lib/net35/MongoDB.Bson.dll"
#I "../packages/FSharp.Data.2.0.12/lib/net40"
#r "../packages/FSharp.Data.2.0.12/lib/net40/FSharp.Data.dll"
#r "bin/Debug/FSharp.Data.Bson.dll"

open System
open System.IO
open System.Text
open MongoDB.Bson
open BsonProvider
open BsonProvider.Runtime

type Messages = BsonProvider<"C:/Users/10gen/Dropbox/messages.bson">
let samples = Messages.GetSamples()

let sample = samples.[0]
printf "%A\n" <| sample.Headers.XFrom

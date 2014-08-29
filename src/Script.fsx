#I "../packages/mongocsharpdriver.1.9.2/lib/net35"
#r "../packages/mongocsharpdriver.1.9.2/lib/net35/MongoDB.Bson.dll"
#I "../packages/FSharp.Data.2.0.9/lib/net40"
#r "../packages/FSharp.Data.2.0.9/lib/net40/FSharp.Data.dll"
#r "bin/Debug/FSharp.Data.Bson.dll"

open System
open System.IO
open System.Text
open MongoDB.Bson
open BsonProvider
open BsonProvider.Runtime

type Zips = BsonProvider<"/Users/maxh/Dropbox/zips.bson">
let samples = Zips.GetSamples()

printf "%A\n" samples.[0].Loc

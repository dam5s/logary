namespace Logary.Metric

open System

type HistogramConf =
  { conf: BasicConf
    buckets: float[]
  }

  interface MetricBuilder<IHistogram> with
    member x.conf = x.conf
    member x.build _ labels = new Histogram(x, labels) :> IHistogram

and Histogram(conf, labels) =
  do
    let containsLe = conf.conf.labelNames |> Array.contains "le"
    if containsLe then failwith "Histogram cannot have a label named 'le'"

  let sumCounter = new DoubleAdder()
  let originSortedBucket = conf.buckets |> Array.sort
  let sortedBuckets =
    if Double.IsPositiveInfinity(Array.last originSortedBucket) then originSortedBucket
    else Array.append originSortedBucket [| Double.PositiveInfinity |]

  let sortedBucketCounters = sortedBuckets |> Seq.map(fun upperBound -> (upperBound, new DoubleAdder())) |> Map.ofSeq

  interface IHistogram with
    member x.observe value =
      (x :> IHistogram).observe(value, 1.)

    member x.observe (value, count) =
      if not <| Double.IsNaN(value) then
        let firstUpperBound = Array.find (fun upperBound -> value <= upperBound ) sortedBuckets
        sortedBucketCounters.[firstUpperBound].Add count
        sumCounter.Add value

    member x.explore () =
      let confInfo = conf.conf
      let basicInfo = { name= confInfo.name; description = confInfo.description }
      let sumInfo =  sumCounter.Sum()
      let bucketsInfo = sortedBucketCounters |> Map.map (fun _ counter -> counter.Sum())
      let metricInfo = { labels = labels; sumInfo = sumInfo; bucketsInfo = bucketsInfo }
      (basicInfo, MetricInfo.Histogram metricInfo)

module HistogramConf =

  /// defaultBuckets are the default Histogram buckets. The default buckets are
  /// tailored to broadly measure the response time (in seconds) of a network
  /// service. Most likely, however, you will be required to define buckets
  /// customized to your use case.
  let defaultBuckets = [| 0.005; 0.01; 0.025; 0.05; 0.075; 0.1; 0.25; 0.5; 0.75; 1.; 2.5; 5.0; 7.5; 10. |]

  let buckets buckets conf =
    { conf with buckets = buckets }

  let linearBuckets start width count conf =
    if count < 1 then invalidArg "count" "count must be positive"
    let buckets = seq {
      for i in 0 .. count-1 do
        yield start + float i * width
      }
    { conf with buckets = buckets |> Array.ofSeq }


  let exponentialBuckets start factor count conf =
    if count < 1 then invalidArg "count" "count must be positive"
    if start <= 0. then invalidArg "start" "start must be positive"
    if factor <= 1. then invalidArg "factor" "factor must be > 1"

    let buckets = seq {
      for i in 0 .. count-1 do
        yield start * Math.Pow(factor, float i)
      }
    { conf with buckets = buckets |> Array.ofSeq }

open HistogramConf

type HistogramConf with
  static member create(name, description) =
    let basic = BasicConf.create(name, description)
    { conf = basic; buckets = defaultBuckets }

  static member create basic =
    { conf = basic; buckets = defaultBuckets }
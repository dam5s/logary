namespace Logary.Metric
open System.Collections.Concurrent


type BasicInfo =
  {
    name: string
    description: string
  }

type GaugeInfo =
  {
    labels: Map<string,string>
    gaugeValue: float
  }

type HistogramInfo =
  {
    labels: Map<string,string>
    bucketsInfo: Map<float,float>
    sumInfo: float
  }

type MetricInfo =
  | Gauge of GaugeInfo
  | Histogram of HistogramInfo

type IMetric =
  abstract explore: unit -> BasicInfo * MetricInfo

type IGauge =
  inherit IMetric

  abstract inc: float -> unit
  abstract dec: float -> unit
  abstract set: float -> unit

type IHistogram =
  inherit IMetric

  abstract observe: value:float -> unit
  abstract observe: value:float * count:float -> unit


type BasicConf =
  {
    name: string
    description: string
    labelNames: string []
    avoidHighCardinality: option<int>
  }

module BasicConf =

  /// https://prometheus.io/docs/practices/instrumentation/#do-not-overuse-labels
  let defaultHighCardinalityLimit = 150

  let create name description =
    { name =  name; description = description; labelNames = [||]; avoidHighCardinality = Some defaultHighCardinalityLimit }

  let labelNames labelNames conf =
    { conf with labelNames = labelNames }

/// used for exporting data
type MetricExporter =
  abstract basicConf: BasicConf
  abstract export: unit -> BasicInfo * seq<MetricInfo>

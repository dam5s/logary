/// Conventional metrics.
///
/// https://prometheus.io/docs/practices/naming/
module Logary.Metric.Conventions

let http_server_request_duration_seconds =
  HistogramConf.create("http_server_request_duration_seconds", "The duration of received HTTP requests in seconds")

let http_client_request_duration_seconds =
  HistogramConf.create("http_client_request_duration_seconds", "The duration of sent HTTP requests in seconds")
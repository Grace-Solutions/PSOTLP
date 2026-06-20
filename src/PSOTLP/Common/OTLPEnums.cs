using System;

namespace PSOTLP.Common
{
    public enum OTLPTransport
    {
        Http = 0,
        Grpc = 1
    }

    public enum OTLPEncoding
    {
        Json = 0,
        Protobuf = 1,
        NDJson = 2
    }

    public enum OTLPCompression
    {
        None = 0,
        Gzip = 1
    }

    public enum OTLPAuthenticationMode
    {
        None = 0,
        CustomHeader = 1
    }

    public enum OTLPSignalType
    {
        Logs = 0,
        Traces = 1,
        Metrics = 2
    }

    public enum OTLPSeverity
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    public enum OTLPSessionDropPolicy
    {
        DropOldest = 0,
        DropNewest = 1,
        Block = 2
    }

    public enum OTLPSpanKind
    {
        Internal = 0,
        Server = 1,
        Client = 2,
        Producer = 3,
        Consumer = 4
    }

    public enum OTLPStatusCode
    {
        Unset = 0,
        Ok = 1,
        Error = 2
    }

    public enum OTLPMetricType
    {
        Gauge = 0,
        Sum = 1
    }

    public enum OTLPAggregationTemporality
    {
        Unspecified = 0,
        Delta = 1,
        Cumulative = 2
    }
}

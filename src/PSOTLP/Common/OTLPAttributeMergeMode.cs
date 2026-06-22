namespace PSOTLP.Common
{
    /// <summary>
    /// Controls how user-supplied attribute dictionaries combine with the defaults already
    /// resolved for a record (connection-level <c>ResourceAttributes</c>/<c>LogAttributes</c>,
    /// module identity, and host inventory).
    /// </summary>
    public enum OTLPAttributeMergeMode
    {
        /// <summary>
        /// Default. Caller-supplied keys overlay the defaults; caller wins on collision and
        /// keys that the caller did not specify are preserved from the defaults.
        /// </summary>
        Merge = 0,

        /// <summary>
        /// Caller-supplied dictionary becomes the entire user layer. Connection-level
        /// defaults (<c>connection.ResourceAttributes</c>, <c>connection.LogAttributes</c>) are
        /// dropped for this record. Host inventory and module identity remain because they
        /// are required to identify the workload that emitted the telemetry.
        /// </summary>
        Replace = 1,

        /// <summary>
        /// Defaults win on collision. Caller-supplied keys are only added when no default
        /// already supplies that key.
        /// </summary>
        Skip = 2
    }
}

namespace MultiDownloader
{
    /// <summary>
    /// Application settings.
    /// </summary>
    class Settings
    {
        /// <summary>
        /// Location downloaded files will be saved to.
        /// </summary>
        public string DownloadLocation { get; set; }
        /// <summary>
        /// Maximum number of concurrent connections.
        /// </summary>
        public int DefaultConnectionLimit { get; set; }
        /// <summary>
        /// Splits file into number of segments to download concurrently (HTTP/HTTPS only).
        /// </summary>
        public int SegmentsPerFile { get; set; }
        /// <summary>
        /// Seconds to connection timeout.
        /// </summary>
        public int TimeoutSeconds { get; set; }
        /// <summary>
        /// Number of timeout retries.
        /// </summary>
        public int TimeoutRetries { get; set; }
        /// <summary>
        /// Linearly decrease the rate of retries.
        /// </summary>
        public int LinearBackoffInterval { get; set; }
        /// <summary>
        /// List of files' URIs to download.
        /// </summary>
        public string[] URIs { get; set; }
    }
}

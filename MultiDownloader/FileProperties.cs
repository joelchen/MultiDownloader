using System.IO;

namespace MultiDownloader
{
    /// <summary>
    /// Properties of a file.
    /// </summary>
    class FileProperties
    {
        /// <summary>
        /// Name of file.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Size of file.
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// AcceptRanges is in HTTP response.
        /// </summary>
        public bool RangeAccepted { get; set; }
        /// <summary>
        /// Stream of file content.
        /// </summary>
        public Stream ContentStream { get; set; }
    }
}

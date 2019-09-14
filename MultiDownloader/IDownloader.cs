using System;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// Interface for all downloaders.
    /// </summary>
    interface IDownloader
    {
        /// <summary>
        /// Download files asynchronously.
        /// </summary>
        /// <param name="uris">List of URIs to download.</param>
        Task DownloadFilesAsync(string[] uris);
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// Files downloader of multiple protocols.
    /// </summary>
    public class FilesDownloader
    {
        private static readonly IList<IDownloader> Downloaders;
        private static readonly FTPDownloader FtpDownloader = new FTPDownloader();
        private static readonly HTTPDownloader HttpDownloader = new HTTPDownloader();

        /// <summary>
        /// Initialize with downloaders of multiple protocols.
        /// </summary>
        static FilesDownloader()
        {
            try
            {
                ServicePointManager.DefaultConnectionLimit = Singleton.Settings.DefaultConnectionLimit;
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                Console.WriteLine($"{aoore.GetType().FullName}: {aoore.Message}");
                return;
            }

            Downloaders = new List<IDownloader>
            {
                FtpDownloader,
                HttpDownloader
            };
        }

        /// <summary>
        /// Get files asynchronously.
        /// </summary>
        /// <returns>
        /// Boolean of whether downloads are successful in asynchronous task.
        /// </returns>
        /// <param name="uris">Array of URIs</param>
        public async Task<bool> GetFilesAsync(string[] uris)
        {
            if (uris == null || uris.Length == 0)
            {
                return false;
            }
            else
            {
                int exceptionsCount = 0;

                foreach (var downloader in Downloaders)
                {
                    try
                    {
                        await downloader.DownloadFilesAsync(uris);
                    }
                    catch (Exception e)
                    {
                        exceptionsCount++;
                        Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
                    }
                }

                if (exceptionsCount > 0) return false;
            }

            return true;
        }
    }
}

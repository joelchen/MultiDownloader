using System;
using System.Linq;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// HTTP Downloader.
    /// </summary>
    public class HTTPDownloader: IDownloader
    {
        /// <summary>
        /// Download HTTP files asynchronously.
        /// </summary>
        /// <param name="uris">List of URIs to download.</param>
        public async Task DownloadFilesAsync(string[] uris)
        {
            try
            {
                uris = uris.Where(s => new Uri(s).Scheme.Equals(Uri.UriSchemeHttp) || new Uri(s).Scheme.Equals(Uri.UriSchemeHttps)).ToArray();

                await Task.WhenAll(uris.Select(async uri =>
                {
                    using (HTTPDownload download = new HTTPDownload())
                    {
                        if (await download.LoadFileAsync(uri))
                        {
                            await download.FetchFileAsync(p =>
                            {
                                Console.WriteLine($"{uri}: {string.Format("{0:N2}% downloaded", p)}");
                            });
                        }
                    }
                }));
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}

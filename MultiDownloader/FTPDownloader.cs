using FluentFTP;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// FTP Downloader.
    /// </summary>
    public class FTPDownloader : IDownloader
    {
        /// <summary>
        /// Download FTP files asynchronously.
        /// </summary>
        /// <param name="uris">List of files' URIs to download.</param>
        public async Task DownloadFilesAsync(string[] uris)
        {
            try
            {
                uris = uris.Where(s => new Uri(s).Scheme.Equals(Uri.UriSchemeFtp)).ToArray();

                await Task.WhenAll(uris.Select(async uri =>
                {
                    Uri u = new Uri(uri);

                    using (var client = new FtpClient(u.Host))
                    {
                        client.ReadTimeout = Singleton.Settings.TimeoutSeconds * 1000;
                        client.RetryAttempts = Singleton.Settings.TimeoutRetries;
                        string[] s = u.UserInfo.Split(':');

                        if (s.Length == 2)
                        {
                            Console.WriteLine($"{uri}: Authenticating...");
                            client.Credentials = new NetworkCredential(s[0], s[1]);
                        }

                        await client.ConnectAsync();

                        if (await client.FileExistsAsync(u.LocalPath))
                        {
                            Progress<FtpProgress> progress = new Progress<FtpProgress>(x =>
                            {
                                if (x.Progress >= 0) Console.WriteLine($"{uri}: {string.Format("{0:N2}% downloaded", x.Progress)}");
                            });

                            await client.DownloadFileAsync(Singleton.Settings.DownloadLocation + "/" + FtpExtensions.GetFtpFileName(uri), u.LocalPath, FtpLocalExists.Append, FtpVerify.None, progress);
                        }
                        else
                        {
                            Console.WriteLine($"{uri}: File does not exist");
                        }

                        await client.DisconnectAsync();
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

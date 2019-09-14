using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// Utility to download file from HTTP.
    /// </summary>
    public class HTTPDownload : IDisposable
    {
        /// <summary>
        /// HTTP message handler.
        /// </summary>
        private static readonly HttpClientHandler ClientHandler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        /// <summary>
        /// HTTP timeout handler.
        /// </summary>
        private static readonly TimeoutHandler TimeoutHandler = new TimeoutHandler()
        {
            DefaultTimeout = TimeSpan.FromSeconds(100),
            InnerHandler = ClientHandler
        };
        /// <summary>
        /// HTTP client for HTTP requests and responses.
        /// </summary>
        private static readonly HttpClient HttpClient = new HttpClient(TimeoutHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        /// <summary>
        /// List of file segments.
        /// </summary>
        private readonly List<FileSegment> _fileSegments = new List<FileSegment>();
        /// <summary>
        /// List of fetch tasks.
        /// </summary>
        private readonly List<Task> _fetchTasks = new List<Task>();
        /// <summary>
        /// Path of file.
        /// </summary>
        private string _filePath;

        /// <summary>
        /// Authenticate HTTP request with basic access authentication.
        /// </summary>
        /// <param name="request">HTTP request message.</param>
        /// <param name="credential">Username and password.</param>
        private static void BasicAuthentication(HttpRequestMessage request, string[] credential)
        {
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format($"{credential[0]}:{credential[1]}"))));
        }

        /// <summary>
        /// Gets segment positions for all segments.
        /// </summary>
        /// <returns>
        /// Enumerator of all segments.
        /// </returns>
        /// <param name="contentLength">Length of content.</param>
        /// <param name="segments">Number of segments.</param>
        private static IEnumerable<(long start, long end)> SegmentPosition(long contentLength, int segments)
        {
            long partSize = (long)Math.Ceiling(contentLength / (double)segments);

            for (var i = 0; i < segments; i++)
                yield return (i * partSize + Math.Min(1, i), Math.Min((i + 1) * partSize, contentLength));
        }

        /// <summary>
        /// Get properties of file asynchronously.
        /// </summary>
        /// <returns>
        /// FileProperties in asynchronous task.
        /// </returns>
        /// <param name="uri">URI of file.</param>
        private static async Task<FileProperties> GetFilePropertiesAsync(string uri)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    request.SetTimeout(TimeSpan.FromSeconds(Singleton.Settings.TimeoutSeconds));
                    request.Headers.Range = new RangeHeaderValue(from: 0, to: 0);
                    request.Headers.Add("User-Agent", Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
                    string[] s = new Uri(uri).UserInfo.Split(':');

                    if (s != null && s.Length == 2)
                    {
                        Console.WriteLine($"{uri}: Authenticating with Basic authentication...");
                        BasicAuthentication(request, s);
                    }

                    using (var response = await HttpClient.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        FileProperties properties = new FileProperties();

                        if (response.Content.Headers.ContentDisposition == null)
                        {
                            if (response.Content.Headers.TryGetValues("Content-Disposition", out IEnumerable<string> contentDisposition))
                            {
                                response.Content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse(contentDisposition.ToArray()[0].TrimEnd(';').Replace("\"", ""));
                            }
                        }

                        properties.Name = response.Content.Headers?.ContentDisposition?.FileName ?? response.RequestMessage.RequestUri.Segments.LastOrDefault();

                        if (response.Content.Headers.TryGetValues(@"Content-Range", out IEnumerable<string> range))
                        {
                            properties.Size = long.Parse(System.Text.RegularExpressions.Regex.Match(range.Single(), @"(?<=^bytes\s[0-9]+\-[0-9]+/)[0-9]+$").Value);
                        }
                        else
                        {
                            properties.Size = 0;
                        }

                        properties.RangeAccepted = response.Headers.AcceptRanges.Contains("bytes");
                        properties.ContentStream = await response.Content.ReadAsStreamAsync();

                        return properties;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Prepare file for download asynchronously.
        /// </summary>
        /// <returns>
        /// Boolean of whether preparation is successful in asynchronous task.
        /// </returns>
        /// <param name="uri">URI of file.</param>
        public async Task<bool> LoadFileAsync(string uri)
        {
            try
            {
                int retry = Singleton.Settings.TimeoutRetries;
                FileProperties properties = null;

                while (retry >= 0 && properties == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds((Singleton.Settings.TimeoutRetries - retry) * Singleton.Settings.LinearBackoffInterval));

                    try
                    {
                        properties = await GetFilePropertiesAsync(uri);
                    }
                    catch (TimeoutException te)
                    {
                        retry--;
                        Console.WriteLine($"{uri}: {te.GetType().FullName}: {te.Message}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{uri}: {e.GetType().FullName}: {e.Message}");
                        return false;
                    }
                }

                _filePath = $"{Singleton.Settings.DownloadLocation}/{properties.Name}";

                if (properties == null)
                {
                    return false;
                }
                else if (properties.Name == "/")
                {
                    Console.WriteLine($"{uri}: URI is not a file");
                    return false;
                }
                else if (properties.Size == 0)
                {
                    return true;
                }

                // Uncomment to debug properties
                //var dump = ObjectDumper.Dump(properties);
                //Console.WriteLine(dump);
                if (!properties.RangeAccepted) Console.WriteLine($"{uri}: Segmented downloading not supported, continuing with normal download...");
                int count = 0;

                foreach (var (start, end) in SegmentPosition(properties.Size, properties.RangeAccepted ? Singleton.Settings.SegmentsPerFile : 1))
                {
                    retry = Singleton.Settings.TimeoutRetries;
                    count++;
                    bool isSuccessStatusCode = false;

                    while (retry >= 0 && !isSuccessStatusCode)
                    {
                        await Task.Delay(TimeSpan.FromSeconds((Singleton.Settings.TimeoutRetries - retry) * Singleton.Settings.LinearBackoffInterval));
                        Console.WriteLine($"{uri}: Building segment {count}...");

                        try
                        {
                            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                            {
                                string[] s = new Uri(uri).UserInfo.Split(':');

                                if (s != null && s.Length == 2)
                                {
                                    Console.WriteLine($"{uri}: Authenticating with Basic authentication...");
                                    BasicAuthentication(request, s);
                                }

                                request.SetTimeout(TimeSpan.FromSeconds(Singleton.Settings.TimeoutSeconds));
                                request.Headers.Range = new RangeHeaderValue(start, end);
                                request.Headers.Add("User-Agent", Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
                                var responseMessage = await HttpClient.SendAsync(request);
                                isSuccessStatusCode = responseMessage.IsSuccessStatusCode;
                                responseMessage.EnsureSuccessStatusCode();
                                _fileSegments.Add(new FileSegment
                                {
                                    ID = count,
                                    Start = start,
                                    End = end,
                                    BytesRead = 0,
                                    Content = await responseMessage.Content.ReadAsStreamAsync()
                                });
                            }
                        }
                        catch (TimeoutException te)
                        {
                            retry--;
                            Console.WriteLine($"{uri}: {te.GetType().FullName}: {te.Message}");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{uri}: {e.GetType().FullName}: {e.Message}");
                        }
                    }
                }

                // Uncomment to debug fileSegments
                //dump = ObjectDumper.Dump(fileSegments);
                //Console.WriteLine(dump);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{uri}: {e.GetType().FullName}: {e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Fetch all file segments asynchronously and joins them together.
        /// </summary>
        /// <returns>
        /// Asynchronous task.
        /// </returns>
        /// <param name="report">Report of fetch progress.</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        public async Task FetchFileAsync(Action<float> report, CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                float[] percentage = new float[_fileSegments.Count];

                foreach (var segment in _fileSegments)
                {
                    _fetchTasks.Add(segment.FetchSegmentAsync((e) => {
                        percentage[segment.ID - 1] = e;
                        report.Invoke(percentage.Average());
                    }, cancellationToken));
                }

                await Task.WhenAll(_fetchTasks);
                _fetchTasks.Clear();

                using (Stream fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    foreach (var segment in _fileSegments.OrderBy(x => x.ID))
                    {
                        fileStream.Seek(segment.Start, SeekOrigin.Begin);

                        using (Stream stream = new FileStream(segment.TempFilePath, FileMode.Open, FileAccess.Read))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{_filePath}: {e.GetType().FullName}: {e.Message}");
            }

            foreach (var segment in _fileSegments)
            {
                File.Delete(segment.TempFilePath);
            }

            _fileSegments.Clear();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            _fetchTasks.Clear();
            _fileSegments.Clear();
            GC.SuppressFinalize(this);
        }
    }
}

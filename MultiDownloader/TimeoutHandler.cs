using ExtensionMethods;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// HTTP timeout handler.
    /// </summary>
    class TimeoutHandler : DelegatingHandler
    {
        /// <summary>
        /// Default timeout of HTTP request.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(100);

        /// <summary>
        /// Sends HTTP request.
        /// </summary>
        /// <returns>
        /// HttpResponseMessage in asynchronous task.
        /// </returns>
        /// <param name="request">HTTP request message.</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (var cts = GetCancellationTokenSource(request, cancellationToken))
            {
                try
                {
                    return await base.SendAsync(request, cts?.Token ?? cancellationToken);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// Gets signal of HTTP request cancellation.
        /// </summary>
        /// <returns>
        /// CancellationTokenSource.
        /// </returns>
        /// <param name="request">HTTP request message.</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        private CancellationTokenSource GetCancellationTokenSource(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var timeout = request.GetTimeout() ?? DefaultTimeout;
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return null;
            }
            else
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                return cts;
            }
        }
    }
}

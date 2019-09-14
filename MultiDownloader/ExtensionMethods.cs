using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    /// <summary>
    /// Extensions of HTTP requests.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Property key for timeout.
        /// </summary>
        private static string TimeoutPropertyKey = "RequestTimeout";

        /// <summary>
        /// Sets timeout for HTTP request.
        /// </summary>
        /// <param name="request">HTTP request message (this).</param>
        /// <param name="timeout">Timeout for HTTP request.</param>
        public static void SetTimeout(this HttpRequestMessage request, TimeSpan? timeout)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Properties[TimeoutPropertyKey] = timeout;
        }

        /// <summary>
        /// Gets timeout for HTTP request.
        /// </summary>
        /// <returns>
        /// Timeout of HTTP request in TimeSpan.
        /// </returns>
        /// <param name="request">HTTP request message (this).</param>
        public static TimeSpan? GetTimeout(this HttpRequestMessage request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Properties.TryGetValue(TimeoutPropertyKey, out var value) && value is TimeSpan timeout) return timeout;
            return null;
        }
    }

    /// <summary>
    /// Extensions of Stream.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads asynchronously from stream to buffer.
        /// </summary>
        /// <returns>
        /// Asynchronous Task&lt;int&gt; or int of total number of bytes read into the buffer.
        /// </returns>
        /// <param name="stream">Stream (this).</param>
        /// <param name="buffer">Buffer of Memory&lt;byte&gt;</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask<int>(stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken));
            }
            else
            {
                byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

                async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
                {
                    try
                    {
                        int result = await readTask.ConfigureAwait(false);
                        new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                        return result;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(localBuffer);
                    }
                }
            }
        }

        /// <summary>
        /// Writes asynchronously to stream from buffer.
        /// </summary>
        /// <returns>
        /// Asynchronous task.
        /// </returns>
        /// <param name="stream">Stream (this).</param>
        /// <param name="buffer">Buffer of Memory&lt;byte&gt;</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask(stream.WriteAsync(array.Array, array.Offset, array.Count, cancellationToken));
            }
            else
            {
                byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.Span.CopyTo(sharedBuffer);
                return new ValueTask(FinishWriteAsync(stream.WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer));
            }
        }

        /// <summary>
        /// Awaits write task to finish asynchronously.
        /// </summary>
        /// <returns>
        /// Asynchronous task.
        /// </returns>
        /// <param name="writeTask">Stream (this).</param>
        /// <param name="localBuffer">Buffer to return ArrayPool.</param>
        private static async Task FinishWriteAsync(Task writeTask, byte[] localBuffer)
        {
            try
            {
                await writeTask.ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(localBuffer);
            }
        }
    }
}

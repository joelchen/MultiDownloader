using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDownloader
{
    /// <summary>
    /// Segment of a file.
    /// </summary>
    class FileSegment
    {
        /// <summary>
        /// ID of file segment.
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Segment start position.
        /// </summary>
        public long Start { get; set; }
        /// <summary>
        /// Segment end position.
        /// </summary>
        public long End { get; set; }
        /// <summary>
        /// Number of bytes read.
        /// </summary>
        public long BytesRead { get; set; }
        /// <summary>
        /// Path of temporary file.
        /// </summary>
        public string TempFilePath { get; set; } = Path.GetTempFileName();
        /// <summary>
        /// Stream of file content.
        /// </summary>
        public Stream Content { get; set; }

        /// <summary>
        /// Fetch file segment asynchronously.
        /// </summary>
        /// <returns>
        /// Asynchronous task.
        /// </returns>
        /// <param name="progress">Progress of fetch.</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        public async Task FetchSegmentAsync(Action<float> progress, CancellationToken cancellationToken = default)
        {
            var pipe = new Pipe();
            Task writing = FillPipeAsync(pipe.Writer, progress, cancellationToken);
            Task reading = ReadPipeAsync(pipe.Reader);
            await Task.WhenAll(writing, reading);
        }

        /// <summary>
        /// Read from content stream asynchronously and fill pipe asynchronously.
        /// </summary>
        /// <returns>
        /// Asynchronous task.
        /// </returns>
        /// <param name="writer">Pipe writer.</param>
        /// <param name="progress">Progress of fetch.</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        private async Task FillPipeAsync(PipeWriter writer, Action<float> progress, CancellationToken cancellationToken = default)
        {
            int bytesRead;

            while ((bytesRead = await Content.ReadAsync(writer.GetMemory(), cancellationToken)) > 0)
            {
                BytesRead += bytesRead;
                progress.Invoke((float)BytesRead / Content.Length * 100);
                writer.Advance(bytesRead);
                var flushResult = await writer.FlushAsync(cancellationToken);
                if (flushResult.IsCanceled) break;
                if (flushResult.IsCompleted) break;
            }

            writer.Complete();
        }

        /// <summary>
        /// Read from pipe asynchronously and write to file asynchronously.
        /// </summary>
        /// <returns>
        /// Asynchronous task.
        /// </returns>
        /// <param name="writer">Pipe reader.</param>
        /// <param name="cancellationToken">Token for cancellation.</param>
        private async Task ReadPipeAsync(PipeReader reader, CancellationToken cancellationToken = default)
        {
            using (Stream fileStream = new FileStream(TempFilePath, FileMode.Append, FileAccess.Write))
            {
                fileStream.Seek(fileStream.Length, SeekOrigin.Begin);

                while (true)
                {
                    var ReadResult = await reader.ReadAsync(cancellationToken);
                    foreach (var buffer in ReadResult.Buffer)
                    {
                        await fileStream.WriteAsync(buffer, cancellationToken);
                    }

                    reader.AdvanceTo(ReadResult.Buffer.End);

                    if (ReadResult.IsCompleted || ReadResult.IsCanceled)
                    {
                        break;
                    }
                }
            }

            reader.Complete();
        }
    }
}

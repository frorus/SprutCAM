using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SprutCAM
{
    /// <summary>
    /// Class for downloading file using HttpClient and updating download progress
    /// </summary>
    internal class HttpClientDownloader : IDisposable
    {
        private readonly string _downloadUrl;
        private readonly string _destinationFilePath;
        private readonly CancellationToken? _cancellationToken;

        private HttpClient _httpClient;

        public delegate void ProgressChangedHandler(long? totalFileSize,
                                                    long totalBytesDownloaded,
                                                    double? progressPercentage);

        public event ProgressChangedHandler ProgressChanged;

        public HttpClientDownloader(string downloadUrl,
                                    string destinationFilePath,
                                    CancellationToken? cancellationToken = null)
        {
            _downloadUrl = downloadUrl;
            _destinationFilePath = destinationFilePath;
            _cancellationToken = cancellationToken;
        }

        public async Task StartDownload()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromDays(1) };

            using (var response = await _httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                await DownloadFileFromHttpResponseMessage(response);
            }
        }

        private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                await ProcessContentStream(totalBytes, contentStream);
            }
        }

        private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
        {
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            try
            {
                using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    do
                    {
                        int bytesRead;
                        if (_cancellationToken.HasValue)
                        {
                            bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken.Value);
                        }
                        else
                        {
                            bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        }

                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            continue;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                        totalBytesRead += bytesRead;
                        readCount += 1;

                        if (readCount % 10 == 0)
                            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                    }
                    while (isMoreToRead);

                }

                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
            }
            catch (Exception)
            {
                File.Delete(_destinationFilePath);
            }
        }

        private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
        {
            if (ProgressChanged == null)
                return;

            double? progressPercentage = null;
            if (totalDownloadSize.HasValue)
                progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);

            ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
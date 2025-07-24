using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PackViewApp.Services
{
    public class HikvisionApiService
    {
        private readonly HttpClient _httpClient;

        public HikvisionApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SearchAsync(string startTime, string endTime)
        {
            string xmlBody = @$"<?xml version=""1.0"" encoding=""utf-8""?>
                <CMSearchDescription>
                    <searchID>tesgdsgsdgdgfd</searchID>
                    <trackList><trackID>101</trackID></trackList>
                    <timeSpanList>
                        <timeSpan>
                            <startTime>{startTime}</startTime>
                            <endTime>{endTime}</endTime>
                        </timeSpan>
                    </timeSpanList>
                    <maxResults>100</maxResults>
                    <searchResultPosition>0</searchResultPosition>
                    <metadataList>
                        <metadataDescriptor>//recordType.meta.std-cgi.com</metadataDescriptor>
                    </metadataList>
                </CMSearchDescription>";

            var content = new StringContent(xmlBody, Encoding.UTF8, "application/xml");

            var response = await _httpClient.PostAsync("/ISAPI/ContentMgmt/search", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<string>> DownloadAsync(List<string> playbackUris, Action<string> statusCallback)
        {
            var tempPaths = new List<string>();
            int total = playbackUris.Count;
            var semaphore = new SemaphoreSlim(3); // Limit to 3 concurrent downloads
            var downloadTasks = new List<Task<string>>();

            for (int i = 0; i < total; i++)
            {
                string rawUri = playbackUris[i];
                int index = i + 1;

                if (string.IsNullOrWhiteSpace(rawUri))
                    continue;

                downloadTasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await DownloadSingleAsync(rawUri, statusCallback, index, total);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            var results = await Task.WhenAll(downloadTasks);
            tempPaths.AddRange(results.Where(p => !string.IsNullOrWhiteSpace(p)));

            statusCallback($"Zakończono pobieranie {tempPaths.Count}/{total}");

            return tempPaths;
        }

        private async Task<string> DownloadSingleAsync(string rawUri, Action<string> statusCallback, int index, int total)
        {
            string escapedUri = System.Security.SecurityElement.Escape(rawUri);
            string xmlBody = @$"<?xml version='1.0'?><downloadRequest><playbackURI>{escapedUri}</playbackURI></downloadRequest>";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_httpClient.BaseAddress!, "/ISAPI/ContentMgmt/download"),
                Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml")
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? contentLength = response.Content.Headers.ContentLength;
            int totalMB = contentLength.HasValue ? (int)(contentLength.Value / (1024 * 1024)) : -1;

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"hikvision_{Guid.NewGuid():N}.mp4");

            await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4 * 1024 * 1024, useAsync: true);
            await using var responseStream = await response.Content.ReadAsStreamAsync();

            byte[] buffer = new byte[4 * 1024 * 1024];
            long totalRead = 0;
            int bytesRead;
            int lastReportedMB = 0;

            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                int currentMB = (int)(totalRead / (1024 * 1024));
                if (currentMB > lastReportedMB)
                {
                    lastReportedMB = currentMB;

                    if (totalMB > 0)
                        statusCallback($"Pobieranie pliku {index}/{total}: {currentMB} MB / {totalMB} MB pobrane");
                    else
                        statusCallback($"Pobieranie pliku {index}/{total}: {currentMB} MB pobrane");
                }
            }

            statusCallback($"Pobieranie pliku {index}/{total}: zakończono ({lastReportedMB} MB)");

            return tempFilePath;
        }
    }
}
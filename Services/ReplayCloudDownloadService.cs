using System.Net;
using System.Net.Http.Headers;
using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public sealed class ReplayCloudDownloadService : IReplayCloudDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, ReplayCloudAvailability> _probeCache = new(StringComparer.OrdinalIgnoreCase);

    public ReplayCloudDownloadService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (_httpClient.Timeout == TimeSpan.FromSeconds(100))
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }
    }

    public Task<ReplayCloudAvailability> ProbeAsync(ReplayEntry entry, CancellationToken cancellationToken = default)
        => ProbeCoreAsync(entry, cancellationToken);

    public Task<ReplayCloudAvailability> ProbeAsync(DeletedReplaySnapshot snapshot, CancellationToken cancellationToken = default)
        => ProbeCoreAsync(snapshot.ToReplayEntry(), cancellationToken);

    public async Task<ReplayCloudDownloadResult> DownloadAsync(
        ReplayEntry entry,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (ReplayCloudRetention.ShouldSkipProbe(entry))
        {
            return ReplayCloudDownloadResult.TooOld;
        }

        if (!ReplayDownloadUrlBuilder.TryGetRecordUrl(entry, out var url) || url is null)
        {
            return ReplayCloudDownloadResult.NotApplicable;
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrEmpty(directory))
        {
            return ReplayCloudDownloadResult.Failed;
        }

        Directory.CreateDirectory(directory);

        var tempPath = destinationPath + ".download.tmp";
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                SetCacheFromEntry(entry, ReplayCloudAvailability.NotFound);
                return ReplayCloudDownloadResult.NotFound;
            }

            if (!response.IsSuccessStatusCode)
            {
                return ReplayCloudDownloadResult.Failed;
            }

            await using var network = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await network.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }

            var info = new FileInfo(tempPath);
            if (info.Length <= 0)
            {
                TryDeleteFile(tempPath);
                return ReplayCloudDownloadResult.Failed;
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(tempPath, destinationPath);

            if (ReplayDownloadUrlBuilder.TryGetServerRecordFileName(entry, out var serverFileName) && serverFileName is not null)
            {
                _probeCache[serverFileName] = ReplayCloudAvailability.Available;
            }

            return ReplayCloudDownloadResult.Success;
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(tempPath);
            throw;
        }
        catch
        {
            TryDeleteFile(tempPath);
            return ReplayCloudDownloadResult.Failed;
        }
    }

    public void InvalidateCacheForEntry(ReplayEntry entry)
    {
        if (ReplayDownloadUrlBuilder.TryGetServerRecordFileName(entry, out var serverFileName) && serverFileName is not null)
        {
            _probeCache.Remove(serverFileName);
        }
    }

    public void InvalidateCacheForSnapshot(DeletedReplaySnapshot snapshot)
        => InvalidateCacheForEntry(snapshot.ToReplayEntry());

    private async Task<ReplayCloudAvailability> ProbeCoreAsync(ReplayEntry entry, CancellationToken cancellationToken)
    {
        if (ReplayCloudRetention.ShouldSkipProbe(entry))
        {
            return ReplayCloudAvailability.TooOld;
        }

        if (!ReplayDownloadUrlBuilder.TryGetServerRecordFileName(entry, out var serverFileName)
            || serverFileName is null
            || !ReplayDownloadUrlBuilder.TryGetRecordUrl(entry, out var url)
            || url is null)
        {
            return ReplayCloudAvailability.NotApplicable;
        }

        if (_probeCache.TryGetValue(serverFileName, out var cached))
        {
            return cached;
        }

        var result = await ProbeUrlAsync(url, cancellationToken).ConfigureAwait(false);
        _probeCache[serverFileName] = result;
        return result;
    }

    private async Task<ReplayCloudAvailability> ProbeUrlAsync(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            using var headResponse = await _httpClient.SendAsync(headRequest, cancellationToken).ConfigureAwait(false);

            if (headResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return ReplayCloudAvailability.NotFound;
            }

            if (headResponse.IsSuccessStatusCode)
            {
                return ReplayCloudAvailability.Available;
            }

            if (headResponse.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                return await ProbeWithRangeGetAsync(url, cancellationToken).ConfigureAwait(false);
            }

            return ReplayCloudAvailability.Unavailable;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ReplayCloudAvailability.ProbeFailed;
        }
    }

    private async Task<ReplayCloudAvailability> ProbeWithRangeGetAsync(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, 0);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ReplayCloudAvailability.NotFound;
            }

            return response.IsSuccessStatusCode
                ? ReplayCloudAvailability.Available
                : ReplayCloudAvailability.Unavailable;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return ReplayCloudAvailability.ProbeFailed;
        }
    }

    private void SetCacheFromEntry(ReplayEntry entry, ReplayCloudAvailability availability)
    {
        if (ReplayDownloadUrlBuilder.TryGetServerRecordFileName(entry, out var serverFileName) && serverFileName is not null)
        {
            _probeCache[serverFileName] = availability;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}

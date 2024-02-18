using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using Celestite.I18N;
using Celestite.Network.Models;
using Celestite.Utils;
using Celestite.ViewModels.Pages;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using ZeroLog;

namespace Celestite.Network.Downloader
{
    public class KashimaDownloaderV1(DownloadStatistic statistic) : IKashimaDownloader
    {
        private bool _isInit;

        private const int MaxTasks = 16;

        public DownloadStatistic DownloadStatistic { get; } = statistic;

        private SemaphoreSlim _semaphore = null!;
        private CancellationTokenSource? _cts = null!;

        private string _installPath = null!;
        private HttpClient _httpClient = null!;
        private SocketsHttpHandler _socketsHttpHandler = null!;
        private bool _keepHttpHandler;

        private readonly Log _logger = LogManager.GetLogger("Kashima");

        private long _downloadedBytesAfterLastCheckpoint;
        private long _lastCheckpointTime = Environment.TickCount64;
        private long _downloadedBytesTotal;
        private long _totalSize;

        public event EventHandler<EventArgs>? OnBulkDownloadFinishedEvent;
        public event EventHandler<UnhandledExceptionEventArgs>? OnBulkDownloadErrorEvent;

        public long GetSpeedPerSec()
        {
            var currentTime = Environment.TickCount64;
            var result = Interlocked.Exchange(ref _downloadedBytesAfterLastCheckpoint, 0) / ((double)(currentTime - Interlocked.Exchange(ref _lastCheckpointTime, currentTime)) / 1000);
            return Convert.ToInt64(result);
        }

        public long GetDownloadedBytes() => _downloadedBytesTotal;

        public bool IsCancelled() => _cts!.IsCancellationRequested;

        private readonly ConcurrentQueue<Func<UniTask>> _downloadTasks = [];

        public long GetTotalSize() => _totalSize;

        public void PrepareInit(CancellationToken token = default) => _cts ??= CancellationTokenSource.CreateLinkedTokenSource(token);

        public void InitDefault(string installPath, Func<SocketsHttpHandler> handlerFactory, bool keepHttpHandler = false, CancellationToken token = default)
        {
            PrepareInit(token);
            _installPath = installPath;

            _socketsHttpHandler = handlerFactory();
            _socketsHttpHandler.UseCookies = true;
            _httpClient = new HttpClient(_socketsHttpHandler)
            {
                MaxResponseContentBufferSize = BufferSize
            };
            _keepHttpHandler = keepHttpHandler;
        }
        private static async UniTask<FileListData?> GetFileList(string fileListUrl, int page,
            CancellationTokenSource cts)
        {
            var gameFileList =
                await DmmGamePlayerApiHelper.GetFileList(fileListUrl, page, cts.Token);
            if (!gameFileList.Failed) return gameFileList.Value;
            await cts.CancelAsync();
            return null;
        }
        public async UniTask<DownloadFileInfoStruct?> GetDiffGameFiles(ClInfo clInfo, string installDir, CancellationToken cancellationToken)
        {
            var totalPages = await DmmGamePlayerApiHelper.GetTotalPages(clInfo.FileListUrl, cancellationToken);
            if (totalPages.Failed) return null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var results = new List<FileListData>();
            for (var i = 0; i < totalPages.Value.TotalPages; i++)
            {
                var fileList = await GetFileList(clInfo.FileListUrl, i + 1, cts);
                if (fileList == null || cts.IsCancellationRequested)
                {
                    NotificationHelper.Warn(Localization.GetDownloadInfoFailed);
                    return null;
                }
                results.Add(fileList);
            }

            var list = results.SelectMany(x => x!.FileList);
            return new DownloadFileInfoStruct(results[0]!.Domain,
            [
                .. list.AsParallel().Where(x =>
                {
                    if (x.Size <= 0) return false;
                    var localPath = ZString.Concat(installDir, x.LocalPath);
                    var fileInfo = new FileInfo(localPath);
                    if (!fileInfo.Exists) return true;
                    using var stream = fileInfo.OpenRead();
                    var hash = Convert.ToHexString(MD5.HashData(stream));
                    return !hash.Equals(x.Hash, StringComparison.OrdinalIgnoreCase);
                }),
            ]);
        }

        public void InitDownloader(string domain, AmazonCookie cookies, CancellationToken token = default)
        {
            PrepareInit(token);
            _semaphore = new SemaphoreSlim(MaxTasks, MaxTasks);

            var cookiesContainer = new CookieContainer();
            var domainUri = new Uri(domain);
            cookiesContainer.Add(new Cookie("CloudFront-Key-Pair-Id", cookies.Key, "/", domainUri.Host));
            cookiesContainer.Add(new Cookie("CloudFront-Signature", cookies.Signature, "/", domainUri.Host));
            cookiesContainer.Add(new Cookie("CloudFront-Policy", cookies.Policy, "/", domainUri.Host));
            _socketsHttpHandler.CookieContainer = cookiesContainer;
            _socketsHttpHandler.UseCookies = true;
            _httpClient.BaseAddress = domainUri;

            _isInit = true;
        }

        private void CheckReady()
        {
            if (!_isInit || _cts == null)
                throw new InvalidOperationException("kashima is not ready");
        }

        private const long ChunkSize = 4 * 1024 * 1024;
        private const int BufferSize = 32 * 1024; // 32KB

        public async UniTask<bool> DownloadModules(IEnumerable<ModuleFile> moduleFiles)
        {
            foreach (var module in moduleFiles)
            {
                try
                {
                    await DownloadSingleModuleFile(module);
                    var fileName = Path.Combine(_installPath, module.DirName, module.ExecFileName);
                    var startInfo = new ProcessStartInfo(fileName)
                    {
                        UseShellExecute = true
                    };
                    using var process = Process.Start(startInfo);
                    if (process == null)
                        return false;
                    await process.WaitForExitAsync();
                }
                catch (Exception e)
                {
                    LogManager.GetLogger("DownloadManager").Error(e.Message);
                }
            }

            return true;
        }

        public async UniTask DownloadBulkDmmFiles(IEnumerable<DownloadFileInfo> downloadFileInfos)
        {
            CheckReady();
            foreach (var info in downloadFileInfos)
            {
                _downloadTasks.Enqueue(() => DownloadSingleDmmFile(info));
                _totalSize += info.Size;
            }

            var tasks = new UniTask[MaxTasks];
            for (var i = 0; i < MaxTasks; i++)
            {
                tasks[i] = UniTask.Create(async () =>
                {
                    while (!_cts!.IsCancellationRequested && !_downloadTasks.IsEmpty)
                    {
                        if (!_downloadTasks.TryDequeue(out var task)) continue;
                        await task();
                    }
                    _downloadTasks.Clear();
                });
            }

            try
            {
                await UniTask.WhenAll(tasks);
                OnBulkDownloadFinishedEvent?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                OnBulkDownloadErrorEvent?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
                throw;
            }
        }

        public async UniTask DownloadSingleModuleFile(ModuleFile moduleFile)
        {
            var localPath = Path.Combine(_installPath, moduleFile.DirName, moduleFile.ExecFileName);
            var fileInfo = new FileInfo(localPath);
            if (fileInfo.Exists)
                fileInfo.Delete();

            var parentDirectory = Path.GetDirectoryName(localPath)!;
            if (!Directory.Exists(parentDirectory))
                Directory.CreateDirectory(parentDirectory);

            using var httpClient = new HttpClient();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                ZString.Concat(moduleFile.Domain, moduleFile.ModuleFileUrl));
            requestMessage.Headers.TryAddWithoutValidation("Cookie", moduleFile.Sign);

            using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, _cts!.Token);
            response.EnsureSuccessStatusCode();
            var contentLength = response.Content.Headers.ContentLength!.Value;
            Interlocked.Add(ref _totalSize, contentLength);

            await using var stream = await response.Content.ReadAsStreamAsync(_cts.Token);
            using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
            int lastRead;
            var offset = 0;
            using var fileHandle = File.OpenHandle(localPath, FileMode.CreateNew, FileAccess.Write, FileShare.Write,
                FileOptions.Asynchronous, preallocationSize: contentLength);
            while (!_cts.IsCancellationRequested &&
                   (lastRead = await stream.ReadAsync(buffer.Memory[..BufferSize], _cts.Token)) > 0)
            {
                await RandomAccess.WriteAsync(fileHandle!, buffer.Memory[..lastRead], offset, _cts.Token);
                Interlocked.Add(ref _downloadedBytesAfterLastCheckpoint, lastRead);
                Interlocked.Add(ref _downloadedBytesTotal, lastRead);
                offset += lastRead;
            }
        }

        public async UniTask DownloadSingleDmmFile(DownloadFileInfo downloadFileInfo)
        {
            CheckReady();
            if (downloadFileInfo.Size <= 0)
                return;
            var localPath = ZString.Concat(_installPath, downloadFileInfo.LocalPath);
            var fileInfo = new FileInfo(localPath);
            if (fileInfo.Exists && fileInfo.Length != 0)
            {
                if (downloadFileInfo.ForceDeleteFlg)
                {
                    fileInfo.Delete();
                    return;
                }
                /*
                if (downloadFileInfo.ProtectedFlg)
                {
                    // TODO: DMM要求protected不允许被更新
                    return;
                }*/
            }
            // 计算chunk
            var chunkSizeCalc = Math.Min(ChunkSize, downloadFileInfo.Size);
            var tasks = new UniTask[chunkSizeCalc != 0 ? (int)Math.Ceiling((double)downloadFileInfo.Size / chunkSizeCalc) : 0];
            var parentDirectory = Path.GetDirectoryName(localPath)!;
            if (!Directory.Exists(parentDirectory))
                Directory.CreateDirectory(parentDirectory);

            // TODO: 合理共享fileHandle
            for (var i = 0; i < tasks.Length; i++)
            {
                if (downloadFileInfo.Size > ChunkSize)
                {
                    var chunkStart = chunkSizeCalc * i;
                    var chunkEnd = Math.Min(chunkSizeCalc * (i + 1), downloadFileInfo.Size) - 1;
                    tasks[i] = DownloadSingleDmmFileChunk(new DownloadFileChunk(downloadFileInfo, i + 1, localPath, chunkStart,
                        chunkEnd, Math.Min(chunkSizeCalc, chunkEnd - chunkStart + 1)));
                }
                else
                    tasks[i] = DownloadSingleDmmFileChunk(new DownloadFileChunk(downloadFileInfo, i + 1, localPath, -1,
                        -1, -1));
            }
            File.Delete(localPath);

            await UniTask.WhenAll(tasks).ContinueWith(async () =>
            {
                if (downloadFileInfo.Size <= 0) return;
                if (!File.Exists(localPath)) return;
                await using var stream = fileInfo.OpenRead();
                var hash = Convert.ToHexString(await MD5.HashDataAsync(stream));
                if (downloadFileInfo.Hash.Equals(hash,
                        StringComparison.OrdinalIgnoreCase)) return;
                throw new KashimaException(downloadFileInfo,
                    ZString.Format(Localization.KashimaErrorMd5NotCorrect, downloadFileInfo.LocalPath, downloadFileInfo.Hash, hash.ToLower())
                    );
            });
        }

        private async UniTask DownloadSingleDmmFileChunk(DownloadFileChunk downloadFileChunk)
        {
            CheckReady();

            if (_cts!.IsCancellationRequested)
                return;

            await _semaphore.WaitAsync(_cts.Token);
            try
            {
                using var request = new HttpRequestMessage();
                request.RequestUri =
                    new Uri(ZString.Concat(_httpClient.BaseAddress, downloadFileChunk.DownloadFileInfo.Path));
                request.Method = HttpMethod.Get;
                if (downloadFileChunk.Start != -1 && downloadFileChunk.End != -1 && downloadFileChunk.ChunkSize != -1)
                    request.Headers.Range = new RangeHeaderValue(downloadFileChunk.Start, downloadFileChunk.End);
                using var response =
                    await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
                response.EnsureSuccessStatusCode();

                var modifiedChunkSize = downloadFileChunk.ChunkSize;
                if (response.Content.Headers.ContentLength != null && downloadFileChunk.ChunkSize == -1)
                    modifiedChunkSize = response.Content.Headers.ContentLength.Value;

                if (modifiedChunkSize == -1)
                    throw new KashimaException(downloadFileChunk.DownloadFileInfo,
                        ZString.Format("Internal Error -1 {0}", downloadFileChunk.DownloadFileInfo.LocalPath));

                await using var stream = await response.Content.ReadAsStreamAsync();

                using var buffer = MemoryPool<byte>.Shared.Rent(BufferSize);
                var offset = downloadFileChunk.ChunkSize == -1 ? 0 : downloadFileChunk.Start;
                var lastRead = 0;
                var readSize = 0;
                using var fileHandle = File.OpenHandle(downloadFileChunk.LocalPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write,
                    FileOptions.Asynchronous);
                while (!_cts.IsCancellationRequested &&
                       (lastRead = await stream.ReadAsync(buffer.Memory[..BufferSize], _cts.Token)) > 0)
                {
                    await RandomAccess.WriteAsync(fileHandle!, buffer.Memory[..lastRead], offset, _cts.Token);
                    Interlocked.Add(ref _downloadedBytesAfterLastCheckpoint, lastRead);
                    Interlocked.Add(ref _downloadedBytesTotal, lastRead);
                    offset += lastRead;
                    readSize += lastRead;
                }

                if (readSize != modifiedChunkSize)
                    throw new KashimaException(downloadFileChunk.DownloadFileInfo,
                        ZString.Format(Localization.KashimaErrorChunkSize, downloadFileChunk.DownloadFileInfo.LocalPath, downloadFileChunk.ChunkOffset, modifiedChunkSize, readSize));
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not KashimaException)
            {
                _cts.Cancel(true);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_isInit)
            {
                _cts!.Dispose();
                if (!_keepHttpHandler)
                    _httpClient.Dispose();
                _semaphore.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}

using System;
using System.Collections.Generic;
using Celestite.Network.Models;
using Celestite.ViewModels.Pages;
using Cysharp.Threading.Tasks;

namespace Celestite.Network.Downloader
{
    public record DownloadFileChunk(DownloadFileInfo DownloadFileInfo, int ChunkOffset, string LocalPath, long Start, long End, long ChunkSize);
    public interface IKashimaDownloader : IDisposable
    {
        UniTask DownloadBulkDmmFiles(IEnumerable<DownloadFileInfo> downloadFileInfos);
        UniTask DownloadSingleDmmFile(DownloadFileInfo downloadFileInfos);
        UniTask<bool> DownloadModules(IEnumerable<ModuleFile> moduleFiles);
        long GetTotalSize();
        long GetDownloadedBytes();
        long GetSpeedPerSec();

        DownloadStatistic DownloadStatistic { get; }
    }

    public class KashimaException : Exception
    {
        public DownloadFileInfo FileInfo;
        public KashimaException(DownloadFileInfo fileInfo, string message) : base(message)
        {
            FileInfo = fileInfo;
        }
    }
}

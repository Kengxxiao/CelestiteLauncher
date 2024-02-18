using System;
using System.IO;

namespace Celestite.Network.Caching
{
    public partial class HttpCaching
    {
        public string ETag { get; set; } = string.Empty;
        public long ContentLength { get; set; }
        public Stream Data { get; set; } = null!;
        public long LastUpdateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

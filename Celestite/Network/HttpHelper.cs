using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Celestite.I18N;
using Celestite.Network.Caching;
using Celestite.Network.CelestiteBypassCore;
using Celestite.Network.DynamicProxy;
using Celestite.Utils;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using TurnerSoftware.DinoDNS;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.Protocol.ResourceRecords;
using ZeroLog;

namespace Celestite.Network
{
    public static class HttpHelper
    {
        internal static readonly CookieContainer GlobalCookieContainer = new();
        private static readonly X509Chain CaCertChain = GenerateCaCertBasedX509Chain();
        private static readonly HttpClient MainHttpClient = GenerateClient();

        private static IBrowsingContext? _htmlParserContext;
        public static IBrowsingContext HtmlParserContext => LazyInitializer.EnsureInitialized(ref _htmlParserContext, () => BrowsingContext.New(Configuration.Default.WithDefaultLoader()));

        private static readonly ConcurrentBag<ulong> CachedCertificatesHash = [];

        public static string LoginSecureId { get; set; } = string.Empty;
        public static string LoginSessionId { get; set; } = string.Empty;

        private static readonly ConcurrentDictionary<string, ARecordWithExpiredTime> DnsTimeToLiveCache = new();
        private static readonly DnsClient DnsClient = new([ // 注意 DinoDNS 已经被修改过以在连接前进行并发测速
            new NameServer(new IPAddress(84215263), ConnectionType.Udp), // 223.5.5.5
            new NameServer(new IPAddress(488447351), ConnectionType.Udp), // 119.29.29.29
            new NameServer(new IPAddress(1920103026), ConnectionType.Udp), // 114.114.114.114
            new NameServer(new IPAddress(16890036), ConnectionType.Udp), // 180.184.1.1
            NameServers.Cloudflare.IPv4.GetPrimary(ConnectionType.Udp),
            NameServers.Google.IPv4.GetPrimary(ConnectionType.Udp)
        ], DnsMessageOptions.Default);

        static HttpHelper()
        {
            HttpHelperImplementation.HttpClientImpl2(DnsTimeToLiveCache);
        }

        private static readonly Log Logger = LogManager.GetLogger("HTTP");

        public static void ClearCookies()
        {
            foreach (var c in (ICollection<Cookie>)GlobalCookieContainer.GetAllCookies())
                c.Expired = true;
        }
        public static void PushCookieToContainer(CookieCollection cookie)
        {
            GlobalCookieContainer.Add(cookie);
        }

        public static CookieCollection GetAllCookies()
        {
            return GlobalCookieContainer.GetAllCookies();
        }

        public static string EncryptHimariProxyHeader(string data)
        {
            using var hmrCertStream = AssetLoader.Open(new Uri("avares://Celestite/Assets/Cert/himari_proxy.pem"));
            using var sr = new StreamReader(hmrCertStream);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(sr.ReadToEnd());

            return CyBase64.EncodeToBase64UrlString(rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.Pkcs1));
        }


        private static X509Certificate2[] GenerateCertificates()
        {
            using var caCertDataStream =
                AssetLoader.Open(new Uri("avares://Celestite/Assets/Cert/cacert.pem"));
            using var brotli = new BrotliStream(caCertDataStream, CompressionMode.Decompress);
            using var binaryReader = new BinaryReader(brotli);
            var set = new X509Certificate2[145];

            for (var i = 0; i < 145; i++)
            {
                var hash = binaryReader.ReadUInt64();
                var nextBytesLength = binaryReader.ReadInt32();
                var cert = binaryReader.ReadBytes(nextBytesLength);
                if (hash != XxHash3.HashToUInt64(cert))
                    continue;
                set[i] = new X509Certificate2(cert);
            }

            return set;
        }


        private static X509Chain GenerateCaCertBasedX509Chain()
        {
            var x509Chain = new X509Chain();
            var x509Sets = GenerateCertificates();
            x509Chain.ChainPolicy.ExtraStore.AddRange(new X509Certificate2Collection(x509Sets));
            x509Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return x509Chain;
        }

        public struct ARecordWithExpiredTime
        {
            public IPAddress IpAddress;
            public long ExpiredTime;
            public bool NoExpireForever;
        }
        private static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context,
            CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            if (OperatingSystem.IsWindows())
                socket.SetRawSocketOption(6, 15, [0x1]); // TCP FAST OPEN
            if (IPAddress.TryParse(context.DnsEndPoint.Host, out _))
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            var httpHost = context.DnsEndPoint.Host;
            var isExpired = false;
            if (!DnsTimeToLiveCache.TryGetValue(httpHost, out var dns) || (isExpired = !dns.NoExpireForever && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > dns.ExpiredTime))
            {
                if (isExpired)
                {
                    DnsTimeToLiveCache.Remove(httpHost, out _);
                    // Logger.Info(ZString.Format("CeDNS Expired <{0}> <{1}> <TTL:{2}>", httpHost, dns.Record.ToIPAddress().ToString(), dns.Record.TimeToLive));
                }
                try
                {
                    var dnsMessage = await DnsClient
                        .QueryAsync(httpHost, DnsQueryType.A, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    var aRecords = dnsMessage.Answers.WithARecords();
                    var enumerable = aRecords as ARecord[] ?? aRecords.ToArray();
                    if (enumerable.Length == 0)
                        throw new Exception("dns failed");
                    var record = enumerable.First();
                    var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    dns = new ARecordWithExpiredTime
                    {
                        ExpiredTime = Math.Max(1800, record.TimeToLive) + ts,
                        IpAddress = record.ToIPAddress(),
                        NoExpireForever = false
                    };
                }
                catch (Exception)
                {
                    var record = await Dns.GetHostEntryAsync(httpHost, cancellationToken);
                    if (record.AddressList.Length == 0)
                        throw new HttpRequestException("dns failed");
                    var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    dns = new ARecordWithExpiredTime
                    {
                        ExpiredTime = 1800 + ts,
                        IpAddress = record.AddressList.First(),
                        NoExpireForever = false
                    };
                }
                if (DnsTimeToLiveCache.TryAdd(httpHost, dns))
                {
                    // Logger.Info(ZString.Format("CeDNS <{0}> <{1}> <TTL:{2}>", httpHost, record.ToIPAddress().ToString(), record.TimeToLive));
                }
            }
            await socket.ConnectAsync(new IPEndPoint(dns.IpAddress, context.DnsEndPoint.Port), cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }

        // 根证书校验
        private static bool ServerCertificateCustomValidation_Core(X509Certificate2 certificate, X509Chain
            chain, SslPolicyErrors sslErrors)
        {
            if (sslErrors != SslPolicyErrors.None && sslErrors != SslPolicyErrors.RemoteCertificateNameMismatch)
                return false;
            var certificateCrc = XxHash3.HashToUInt64(certificate.RawDataMemory.Span);
            if (CachedCertificatesHash.Contains(certificateCrc))
                return true;
            if (!CaCertChain.Build(certificate) || CaCertChain.SafeHandle!.DangerousGetHandle() == IntPtr.Zero)
                return false;
            if (chain.ChainElements.Count != CaCertChain.ChainElements.Count)
                return false;
            var selfSignedCert = CaCertChain.ChainElements.FirstOrDefault(x =>
                x.Certificate.IssuerName.Name == x.Certificate.SubjectName.Name);
            if (selfSignedCert == null)
                return false;
            var result = CaCertChain.ChainPolicy.ExtraStore.Any(
                x => x.Thumbprint == selfSignedCert.Certificate.Thumbprint);
            if (result)
                CachedCertificatesHash.Add(certificateCrc);
            return result;
        }

        private static bool ServerCertificateCustomValidation_SocketsHttpHandler(object obj,
            X509Certificate? certificate1, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            if (certificate1 == null || chain == null)
                return false;
            var certificate = new X509Certificate2(certificate1);
            if (obj is not SslStream sslStream)
                return ServerCertificateCustomValidation_Core(certificate, chain, sslErrors);
            return certificate.MatchesHostname(sslStream.TargetHostName) && ServerCertificateCustomValidation_Core(certificate, chain, sslErrors);
        }

        // .NET 不公开 CertificateCallbackMapper 无法对 SocketsHttpHandler 使用
        private static bool ServerCertificateCustomValidation(HttpRequestMessage requestMessage,
            X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            if (certificate == null || chain == null)
                return false;
            return certificate.MatchesHostname(requestMessage.RequestUri!.Host) && ServerCertificateCustomValidation_Core(certificate, chain, sslErrors);
        }

        private static HttpClient GenerateClient()
        {
            var sslPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>(x => x.InnerException is AuthenticationException)
                .FallbackAsync(new HttpResponseMessage(), onFallbackAsync: (exception, context) =>
                {
                    Logger.Error("TLS Man-in-the-Middle attack detected");
                    throw new AuthenticationException(Localization.MITMDetected);
                });
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>(x => x.InnerException is not AuthenticationException)
                .OrTransientHttpStatusCode()
                .Or<TimeoutRejectedException>()
                .RetryAsync(3);
            var policyHttpMessageHandler = new PolicyHttpMessageHandler(Policy.WrapAsync(sslPolicy, retryPolicy,
                Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5))));

            var policy = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust };
            policy.CustomTrustStore.AddRange(GenerateCertificates());
            HttpClient.DefaultProxy = DynamicProxyImpl.GetProxy();
            var socksHandler = new SocketsHttpHandler()
            {
                UseCookies = true,
                CookieContainer = GlobalCookieContainer,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                ConnectCallback = ConnectCallback,
                SslOptions = new SslClientAuthenticationOptions
                {
                    // RemoteCertificateValidationCallback = ServerCertificateCustomValidation_SocketsHttpHandler,
#if !DEBUG
                    CertificateChainPolicy = policy
#endif
                },
                Proxy = HttpClient.DefaultProxy
            };
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAddressChanged;
            policyHttpMessageHandler.InnerHandler = socksHandler;
            var client = new HttpClient(policyHttpMessageHandler);
            client.HttpClientImpl();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                SystemInfoUtils.RequestHeaderUserAgentName);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Client-App", "DMMGamePlayer5");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Client-version",
                SystemInfoUtils.AppConfigProtocolVersion);
            return client;
        }

        private static void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
        {
            return;
        }

        public const string ApiBase = "https://apidgp-gameplayer.games.dmm.com";

        public static async UniTask<NetworkOperationResult<TResp>> DgpGetJsonAsync<TResp>(string path,
            JsonTypeInfo<TResp> jsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
            => await GetJsonAsync(ApiBase, path, jsonTypeInfo, cancellationToken);
        public static async UniTask<NetworkOperationResult<TResp>> GetJsonAsync<TResp>(string baseUrl, string path, JsonTypeInfo<TResp> jsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);
                var httpResponse = await MainHttpClient.GetFromJsonAsync(url, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
                return httpResponse == null ? NetworkOperationResult.Fail<TResp>(new NoNullAllowedException()) : NetworkOperationResult.Ok(httpResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<TResp>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<string>> DgpGetStringAsync(string path,
            CancellationToken cancellationToken = default) => await GetStringAsync(ApiBase, path, cancellationToken);
        public static async UniTask<NetworkOperationResult<string>> GetStringAsync(string baseUrl, string path, CancellationToken cancellationToken = default, bool forceMobileUserAgent = false)
        {
            try
            {
                var url = !string.IsNullOrEmpty(baseUrl) ? ZString.Concat(baseUrl, path) : path;
                using var request = new HttpRequestMessage();
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(url);
                request.Headers.TryAddWithoutValidation("User-Agent",
                    forceMobileUserAgent ? MobileUa : SystemInfoUtils.RequestHeaderUserAgentName);
                using var httpResponse = await MainHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
                return NetworkOperationResult.Ok(await httpResponse.Content.ReadAsStringAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<string>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult> PostRawStringAsJsonAsync(string url, string jsonData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new StringContent(jsonData, new MediaTypeHeaderValue("application/json"));
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;
                await MainHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return NetworkOperationResult.Ok();
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<Tuple<string, string>>> AdvancedGetStringAsync(string baseUrl, string path,
            CancellationToken cancellationToken = default)
        {
            var originalUrl = new Uri(!string.IsNullOrEmpty(baseUrl) ? ZString.Concat(baseUrl, path) : path);
            try
            {
                using var httpResponse = await MainHttpClient.GetAsync(originalUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (httpResponse.IsSuccessStatusCode)
                    return NetworkOperationResult.Ok(new Tuple<string, string>(await httpResponse.Content.ReadAsStringAsync(cancellationToken), originalUrl.Scheme));
                if (httpResponse.StatusCode != HttpStatusCode.Found)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    throw new InvalidOperationException(); // 这段代码不可能执行
                }
                if (httpResponse.Headers.Location!.Scheme == originalUrl.Scheme)
                    return NetworkOperationResult.Ok(new Tuple<string, string>(await httpResponse.Content.ReadAsStringAsync(cancellationToken), originalUrl.Scheme)); // 这段代码也不太可能执行
                var newUrl = ZString.Concat(httpResponse.Headers.Location.Scheme, "://", originalUrl.Host,
                    originalUrl.AbsolutePath);
                return await AdvancedGetStringAsync(newUrl, string.Empty, cancellationToken);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<Tuple<string, string>>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<Stream>> DgpGetStreamAsync(string path,
            CancellationToken cancellationToken = default) => await GetStreamAsync(ApiBase, path, cancellationToken);
        public static async UniTask<NetworkOperationResult<Stream>> GetStreamAsync(string baseUrl, string path, CancellationToken cancellationToken = default)
            => await GetStreamAsync(ZString.Concat(baseUrl, path), cancellationToken);
        public static async UniTask<NetworkOperationResult<Stream>> GetStreamAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var httpResponse = await MainHttpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
                return NetworkOperationResult.Ok(httpResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<Stream>(ex);
            }
        }

        private const string MobileUa =
            "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1 Edg/120.0.0.0";
        public static async UniTask<NetworkOperationResult<HttpResponseMessage>> GetAsync(string url, bool forceMobileUserAgent = false, bool addSessionId = false, CancellationToken cancellationToken = default)
            => await GetAsync(new Uri(url), forceMobileUserAgent, addSessionId, cancellationToken);
        public static async UniTask<NetworkOperationResult<HttpResponseMessage>> GetAsync(Uri url, bool forceMobileUserAgent = false, bool addSessionId = false, CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage();
                request.Method = HttpMethod.Get;
                request.RequestUri = url;
                request.Headers.TryAddWithoutValidation("User-Agent",
                    forceMobileUserAgent ? MobileUa : SystemInfoUtils.RequestHeaderUserAgentName);

                if (addSessionId && !string.IsNullOrEmpty(LoginSecureId) && !string.IsNullOrEmpty(LoginSessionId))
                {
                    request.Headers.TryAddWithoutValidation("X-Dmm-Secure-Id", EncryptHimariProxyHeader(LoginSecureId));
                    request.Headers.TryAddWithoutValidation("X-Dmm-Session-Id", EncryptHimariProxyHeader(LoginSessionId));
                }

                using var httpResponse = await MainHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                return NetworkOperationResult.Ok(httpResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<HttpResponseMessage>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<Bitmap>> GetByteArrayWithCacheFromWebAsync(string url,
            CancellationToken cancellationToken = default)
        {
            HttpCaching? cacheData = null;
            byte[]? bytes = null;
            try
            {
                var cacheUri = new Uri(url);
                if ((cacheData = LoadHttpCachingFromDisk(cacheUri.AbsolutePath, cancellationToken)) != null &&
                    Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - cacheData!.LastUpdateTime) <= 3600000)
                {
                    return NetworkOperationResult.Ok(new Bitmap(cacheData.Data));
                }

                // cacheData.Value 是stream这里还没释放
                using var message = new HttpRequestMessage();
                if (cacheData != null)
                {
                    message.Headers.Add("If-None-Match", cacheData.ETag);
                    // message.Headers.Add("If-Modified-Since", cacheData.LastModified);
                }

                message.RequestUri = cacheUri;
                message.Method = HttpMethod.Get;
                using var httpResponse = await MainHttpClient.SendAsync(message,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (httpResponse.StatusCode == HttpStatusCode.NotModified && cacheData != null)
                {
                    await ModifyCacheTime(cacheData.Data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        cancellationToken);
                    return NetworkOperationResult.Ok(new Bitmap(cacheData.Data));
                }

                if (cacheData != null)
                {
                    cacheData.Data.SetLength(0);
                    await cacheData.Data.DisposeAsync();
                    cacheData = null;
                }

                if (httpResponse.Content.Headers.ContentLength == null)
                    return NetworkOperationResult.Fail<Bitmap>(new NullReferenceException("ContentLength"));
                await using var newData = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
                bytes = ArrayPool<byte>.Shared.Rent((int)httpResponse.Content.Headers.ContentLength.Value);
                _ = await newData.ReadAtLeastAsync(bytes, (int)httpResponse.Content.Headers.ContentLength.Value, cancellationToken: cancellationToken);
                var memoryStream = new MemoryStream(bytes, 0, (int)httpResponse.Content.Headers.ContentLength.Value);
                if (httpResponse.Headers.ETag != null)
                {
                    await CreateCacheToDisk(cacheUri.AbsolutePath, new HttpCaching
                    {
                        ContentLength = bytes.Length,
                        Data = memoryStream,
                        ETag = httpResponse.Headers.ETag.Tag,
                        LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }, cancellationToken);
                }
                else
                {
                    await CreateCacheToDisk(cacheUri.AbsolutePath, new HttpCaching
                    {
                        ContentLength = bytes.Length,
                        Data = memoryStream,
                        ETag = string.Empty,
                        LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }, cancellationToken);
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                return NetworkOperationResult.Ok(new Bitmap(memoryStream));
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<Bitmap>(ex);
            }
            finally
            {
                if (bytes != null) ArrayPool<byte>.Shared.Return(bytes);
                if (cacheData is
                    {
                        Data.CanRead: true
                    })
                    await cacheData.Data.DisposeAsync();
            }
        }

        private static async UniTask ModifyCacheTime(Stream stream, long lastUpdateTime, CancellationToken cancellationToken = default)
        {
            stream.Seek(-sizeof(long) * 2, SeekOrigin.End);
            using var reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true);
            var etag = reader.ReadInt64();
            stream.Seek(-etag - sizeof(long) * 2, SeekOrigin.Current);
            await using var writer = new BinaryWriter(stream, Encoding.Default, leaveOpen: true);
            writer.Write(lastUpdateTime);
            await stream.FlushAsync(cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);
        }

        private static async UniTask CreateCacheToDisk(string tag, HttpCaching cache, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(FileUtils.HttpCacheFolder))
                return;
            var cachePath = Path.Combine(FileUtils.HttpCacheFolder,
                Convert.ToHexString(XxHash3.Hash(Encoding.UTF8.GetBytes(tag))));

            await using var stream = File.OpenWrite(cachePath);
            stream.SetLength(0);
            await using var writer = new BinaryWriter(stream);
            await cache.Data.CopyToAsync(stream, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            var compressedLength = stream.Position - 1024;
            writer.Write(cache.LastUpdateTime);
            var enc = Encoding.UTF8.GetBytes(cache.ETag);
            writer.Write(enc);
            writer.Write(enc.LongLength);
            writer.Write(compressedLength | (2L << 32));
        }

        private static HttpCaching? LoadHttpCachingFromDisk(string tag, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(FileUtils.HttpCacheFolder))
                return null;
            var cachePath = Path.Combine(FileUtils.HttpCacheFolder,
                Convert.ToHexString(XxHash3.Hash(Encoding.UTF8.GetBytes(tag))));
            if (!File.Exists(cachePath))
                return null;

            var stream = File.Open(cachePath, FileMode.Open, FileAccess.ReadWrite);
            if (stream.Length < 1024)
                return null;
            using var reader = new BinaryReader(stream, Encoding.Default, leaveOpen: true);

            stream.Seek(0x100, SeekOrigin.Begin);
            if (reader.ReadUInt64() == 0)
                return null; // 粗略判断v1缓存

            stream.Seek(-sizeof(long), SeekOrigin.End);
            var compressedLength = reader.ReadInt64();
            var headerVersion = compressedLength >> 32 & int.MaxValue;
            if (headerVersion <= 1)
                return null;
            compressedLength &= int.MaxValue;
            stream.Seek(-sizeof(long) * 2, SeekOrigin.Current);
            var eTagLength = reader.ReadInt64();
            stream.Seek(-eTagLength - sizeof(long), SeekOrigin.Current);
            var etag = Encoding.UTF8.GetString(reader.ReadBytes((int)eTagLength));
            stream.Seek(-sizeof(long) - eTagLength, SeekOrigin.Current);
            var lastUpdateTime = reader.ReadInt64();

            stream.Seek(0, SeekOrigin.Begin);
            return new HttpCaching()
            {
                ContentLength = compressedLength,
                Data = stream,
                ETag = etag,
                LastUpdateTime = lastUpdateTime
            };
        }

        public static async UniTask<NetworkOperationResult<byte[]>> GetByteArrayAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var httpResponse = await MainHttpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                return NetworkOperationResult.Ok(httpResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<byte[]>(ex);
            }
        }


        public static async UniTask<NetworkOperationResult<TResp>> DgpPostJson2Async<TReq, TResp>(string path, TReq request, JsonTypeInfo<TReq> requestJsonTypeInfo, JsonTypeInfo<TResp> responseJsonTypeInfo, bool forceMobileUserAgent = false, bool addSessionId = false, CancellationToken cancellationToken = default) where TResp : class
            => await PostJsonAsync(ApiBase, path, request, requestJsonTypeInfo, responseJsonTypeInfo, forceMobileUserAgent, addSessionId, cancellationToken);
        public static async UniTask<NetworkOperationResult<TResp>> DgpPostJsonAsync<TReq, TResp>(string path, TReq request, JsonTypeInfo<TReq> requestJsonTypeInfo, JsonTypeInfo<TResp> responseJsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
            => await PostJsonAsync(ApiBase, path, request, requestJsonTypeInfo, responseJsonTypeInfo, false, false, cancellationToken);

        public static async UniTask<NetworkOperationResult> PostJsonAsync<TReq>(string url,
            TReq request, JsonTypeInfo<TReq> requestJsonTypeInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpResponse = await MainHttpClient.PostAsJsonAsync(url, request, requestJsonTypeInfo, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
                return NetworkOperationResult.Ok();
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<TResp>> PostJsonAsync<TReq, TResp>(string baseUrl, string path, TReq request, JsonTypeInfo<TReq> requestJsonTypeInfo, JsonTypeInfo<TResp> responseJsonTypeInfo, bool forceMobileUserAgent = false, bool addSessionId = false, CancellationToken cancellationToken = default) where TResp : class
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                using var content = JsonContent.Create(request, requestJsonTypeInfo);
                requestMessage.Headers.TryAddWithoutValidation("User-Agent",
                    forceMobileUserAgent ? MobileUa : SystemInfoUtils.RequestHeaderUserAgentName);
                requestMessage.Content = content;

                if (addSessionId && !string.IsNullOrEmpty(LoginSecureId) && !string.IsNullOrEmpty(LoginSessionId))
                {
                    requestMessage.Headers.TryAddWithoutValidation("X-Dmm-Secure-Id", EncryptHimariProxyHeader(LoginSecureId));
                    requestMessage.Headers.TryAddWithoutValidation("X-Dmm-Session-Id", EncryptHimariProxyHeader(LoginSessionId));
                }

                using var httpResponse = await MainHttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
                var jsonResponse = await httpResponse.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
                return jsonResponse == null ? NetworkOperationResult.Fail<TResp>(new NoNullAllowedException()) : NetworkOperationResult.Ok(jsonResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<TResp>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<TResp>> DgpPostAsync<TResp>(string path, JsonTypeInfo<TResp> responseJsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
            => await PostAsync(ApiBase, path, responseJsonTypeInfo, cancellationToken);
        public static async UniTask<NetworkOperationResult<TResp>> PostAsync<TResp>(string baseUrl, string path, JsonTypeInfo<TResp> responseJsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
        {
            try
            {
                using var httpContent = new StringContent(string.Empty);
                var url = ZString.Concat(baseUrl, path);
                using var httpResponse = await MainHttpClient.PostAsync(url, httpContent, cancellationToken).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
                var jsonResponse = await httpResponse.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
                return jsonResponse == null ? NetworkOperationResult.Fail<TResp>(new NoNullAllowedException()) : NetworkOperationResult.Ok(jsonResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<TResp>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<TResp>> PostJsonWithAuthorizationAsync<TReq, TResp>(string baseUrl, string path, TReq request, JsonTypeInfo<TReq> requestJsonTypeInfo, JsonTypeInfo<TResp> responseJsonTypeInfo, AuthenticationHeaderValue authentication, CancellationToken cancellationToken = default) where TResp : class
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);
                MainHttpClient.DefaultRequestHeaders.Authorization = authentication;
                using var httpContent = JsonContent.Create(request, requestJsonTypeInfo);
                using var httpResponse = await MainHttpClient.PostAsync(url, httpContent, cancellationToken).ConfigureAwait(false);
                MainHttpClient.DefaultRequestHeaders.Authorization = null;
                httpResponse.EnsureSuccessStatusCode();
                var jsonResponse =
                    await httpResponse.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
                return jsonResponse == null ? NetworkOperationResult.Fail<TResp>(new NoNullAllowedException()) : NetworkOperationResult.Ok(jsonResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<TResp>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<TResp>> PostWithAuthorizationAsync<TResp>(string baseUrl, string path, JsonTypeInfo<TResp> responseJsonTypeInfo, AuthenticationHeaderValue authentication, CancellationToken cancellationToken = default) where TResp : class
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);
                MainHttpClient.DefaultRequestHeaders.Authorization = authentication;
                using var httpContent = new StringContent(string.Empty);
                using var httpResponse = await MainHttpClient.PostAsync(url, httpContent, cancellationToken).ConfigureAwait(false);
                MainHttpClient.DefaultRequestHeaders.Authorization = null;
                httpResponse.EnsureSuccessStatusCode();
                var jsonResponse = await httpResponse.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
                return jsonResponse == null ? NetworkOperationResult.Fail<TResp>(new NoNullAllowedException()) : NetworkOperationResult.Ok(jsonResponse);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<TResp>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<HttpResponseMessage>> GetResponseHeaderAsync(string fullUrl)
        {
            try
            {
                return NetworkOperationResult.Ok(await MainHttpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<HttpResponseMessage>(ex);
            }
        }
        public static async UniTask<NetworkOperationResult<HttpResponseMessage>> GetResponseHeaderAsync(Uri fullUrl)
        {
            try
            {
                return NetworkOperationResult.Ok(await MainHttpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<HttpResponseMessage>(ex);
            }
        }

        private static string AddQueryString(
            string uri,
            IEnumerable<KeyValuePair<string, string>> queryString)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentNullException.ThrowIfNull(queryString);

            var anchorIndex = uri.IndexOf('#');
            var uriToBeAppended = uri.AsSpan();
            var anchorText = ReadOnlySpan<char>.Empty;
            // If there is an anchor, then the query string must be inserted before its first occurrence.
            if (anchorIndex != -1)
            {
                anchorText = uriToBeAppended[anchorIndex..];
                uriToBeAppended = uriToBeAppended[..anchorIndex];
            }

            var queryIndex = uriToBeAppended.IndexOf('?');
            var hasQuery = queryIndex != -1;

            using var sb = ZString.CreateStringBuilder();
            sb.Append(uriToBeAppended);
            foreach (var parameter in queryString)
            {
                if (string.IsNullOrEmpty(parameter.Value))
                {
                    continue;
                }

                sb.Append(hasQuery ? '&' : '?');
                sb.Append(UrlEncoder.Default.Encode(parameter.Key));
                sb.Append('=');
                sb.Append(UrlEncoder.Default.Encode(parameter.Value));
                hasQuery = true;
            }

            sb.Append(anchorText);
            return sb.ToString();
        }

        public static async UniTask<NetworkOperationResult<TResp>> DgpGetJsonWithFormQueryAsync<TResp>(string path, IEnumerable<KeyValuePair<string, string>> queryData, JsonTypeInfo<TResp> responseJsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
            => await GetJsonWithFormQueryAsync(ApiBase, path, queryData, responseJsonTypeInfo, cancellationToken);
        public static async UniTask<NetworkOperationResult<TResp>> GetJsonWithFormQueryAsync<TResp>(string baseUrl,
            string path, IEnumerable<KeyValuePair<string, string>> queryData, JsonTypeInfo<TResp> responseJsonTypeInfo, CancellationToken cancellationToken = default) where TResp : class
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);
                var response = await MainHttpClient.GetFromJsonAsync(new Uri(AddQueryString(url, queryData)), responseJsonTypeInfo, cancellationToken).ConfigureAwait(false);
                return response != null ? NetworkOperationResult.Ok(response) : throw new NullReferenceException("formQueryResponse");
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<TResp>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<HttpResponseMessage>> PostFormDataAsync(string baseUrl, string path, IEnumerable<KeyValuePair<string, string>> postData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                using var content = new FormUrlEncodedContent(postData);
                request.Content = content;
                using var response = await MainHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                return NetworkOperationResult.Ok(response);
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<HttpResponseMessage>(ex);
            }
        }

        public static async UniTask<NetworkOperationResult<string>> PostFormDataThenReceiveStringAsync(string baseUrl, string path, IEnumerable<KeyValuePair<string, string>> postData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var url = ZString.Concat(baseUrl, path);
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                using var content = new FormUrlEncodedContent(postData);
                request.Content = content;
                using var response = await MainHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Found)
                    return NetworkOperationResult.Ok(await response.Content.ReadAsStringAsync(cancellationToken));
                throw new HttpRequestException($"Error StatusCode {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<string>(ex);
            }
        }

        public static async IAsyncEnumerable<TResp>
            PostAndGetAsyncEnumerableJson<TReq, TResp>(string baseUrl, string path, TReq request,
                JsonTypeInfo<TReq> requestJsonTypeInfo, JsonTypeInfo<TResp> responseJsonTypeInfo,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var url = ZString.Concat(baseUrl, path);
            using var content = JsonContent.Create(request, requestJsonTypeInfo);
            using var message = new HttpRequestMessage(HttpMethod.Post, url);
            message.Content = content;
            using var response = await MainHttpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            await foreach (var value in JsonSerializer.DeserializeAsyncEnumerable(
                               stream, responseJsonTypeInfo, cancellationToken).ConfigureAwait(false))
                yield return value!;
        }

        public static async UniTask<NetworkOperationResult<HttpResponseMessage>> SendRawAsync(HttpRequestMessage requestMessage, IEnumerable<HttpStatusCode> acceptedCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await MainHttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode || acceptedCode.Contains(response.StatusCode))
                    return NetworkOperationResult.Ok(response);
                throw new HttpRequestException($"Error StatusCode {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return NetworkOperationResult.Fail<HttpResponseMessage>(ex);
            }
        }
    }
}

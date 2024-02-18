using System.Net;

namespace TurnerSoftware.DinoDNS.Connection.Listeners;

public delegate ValueTask<int> OnDnsQueryCallback(ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken);

public interface IDnsQueryListener
{
	Task ListenAsync(IPEndPoint endPoint, OnDnsQueryCallback callback, DnsMessageOptions options, CancellationToken cancellationToken);
}

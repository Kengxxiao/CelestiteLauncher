using System.Net;

namespace TurnerSoftware.DinoDNS.Connection;

public interface IDnsResolver
{
	ValueTask<int> SendMessageAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken);
}

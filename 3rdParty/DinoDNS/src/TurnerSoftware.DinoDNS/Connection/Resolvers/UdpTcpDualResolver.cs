using System.Net;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Connection;

public sealed class UdpTcpDualResolver : IDnsResolver
{
	public static readonly UdpTcpDualResolver Instance = new();

	public async ValueTask<int> SendMessageAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		var messageLength = await UdpResolver.Instance.SendMessageAsync(endPoint, requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);

		new DnsProtocolReader(responseBuffer).ReadHeader(out var header);
		if (header.Flags.Truncation == Truncation.Yes)
		{
			messageLength = await TcpResolver.Instance.SendMessageAsync(endPoint, requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);
		}

		return messageLength;
	}
}

using System.Net;
using System.Net.Sockets;

namespace TurnerSoftware.DinoDNS.Connection;

public sealed class UdpResolver : IDnsResolver
{
	public static readonly UdpResolver Instance = new();

	private Socket? _basedSocket;

	private Socket GetSocket(IPEndPoint endPoint)
	{
		if (_basedSocket is { Connected: true })
			return _basedSocket;
		_basedSocket?.Dispose();
		_basedSocket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
		_basedSocket.Connect(endPoint);
		return _basedSocket;
	}

	public async ValueTask<int> SendMessageAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		var socket = GetSocket(endPoint);

		await socket.SendAsync(requestBuffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
		var messageLength = await socket.ReceiveAsync(responseBuffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);

		if (SocketMessageOrderer.CheckMessageId(requestBuffer, responseBuffer) == MessageIdResult.Mixed)
		{
			messageLength = SocketMessageOrderer.Exchange(
				socket,
				requestBuffer,
				responseBuffer,
				messageLength,
				cancellationToken
			);
		}

		return messageLength;
	}
}

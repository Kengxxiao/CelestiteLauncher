using System.Net;
using System.Net.Sockets;

namespace TurnerSoftware.DinoDNS.Connection.Listeners;

public sealed class UdpQueryListener : IDnsQueryListener
{
	public static readonly UdpQueryListener Instance = new();

	public async Task ListenAsync(IPEndPoint endPoint, OnDnsQueryCallback callback, DnsMessageOptions options, CancellationToken cancellationToken)
	{
		using var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
		socket.Bind(endPoint);

		while (!cancellationToken.IsCancellationRequested)
		{
			var transitData = TransitData.Rent(options);
			var socketReceived = await socket.ReceiveFromAsync(transitData.RequestBuffer, SocketFlags.None, endPoint, cancellationToken).ConfigureAwait(false);
			_ = HandleRequestAsync(socket, transitData, socketReceived, callback, cancellationToken).ConfigureAwait(false);
		}

		static async Task HandleRequestAsync(Socket socket, TransitData transitData, SocketReceiveFromResult socketReceived, OnDnsQueryCallback callback, CancellationToken cancellationToken)
		{
			try
			{
				var (requestBuffer, responseBuffer) = transitData;
				var bytesWritten = await callback(requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);
				await socket.SendToAsync(transitData.ResponseBuffer[..bytesWritten], SocketFlags.None, socketReceived.RemoteEndPoint, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				TransitData.Return(transitData);
			}
		}
	}
}
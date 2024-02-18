using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TurnerSoftware.DinoDNS.Connection;

public class TcpResolver : IDnsResolver
{
	public static readonly TcpResolver Instance = new();

	private readonly ConcurrentDictionary<IPEndPoint, Socket> Sockets = new();
	private readonly object NewSocketLock = new();

	private Socket GetSocket(IPEndPoint endPoint)
	{
		if (Sockets.TryGetValue(endPoint, out var socket))
		{
			if (socket.Connected)
			{
				return socket;
			}

			//TODO: Investigate whether we can just re-connect to existing sockets that are closed
			SocketMessageOrderer.ClearSocket(socket);
			OnSocketEnd(socket);
			socket.Dispose();
		}

		//We can't rely on GetOrAdd-type methods on ConcurrentDictionary as the factory can be called multiple times.
		//Instead, we rely on TryGetValue for the hot path (existing socket) otherwise use a typical lock.
		lock (NewSocketLock)
		{
			if (!Sockets.TryGetValue(endPoint, out socket))
			{
				socket = CreateSocket(endPoint);

				Sockets.TryAdd(endPoint, socket);
			}

			return socket;
		}
	}

	protected virtual Socket CreateSocket(IPEndPoint endPoint) => new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
	{
		SendBufferSize = 2048,
		ReceiveBufferSize = 2048,
		SendTimeout = 10,
		ReceiveTimeout = 10
	};

	protected virtual ValueTask OnConnectAsync(Socket socket, IPEndPoint endPoint, CancellationToken cancellationToken) => ValueTask.CompletedTask;

	protected virtual void OnSocketEnd(Socket socket) { }

	public async ValueTask<int> SendMessageAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		var socket = GetSocket(endPoint);
		if (!socket.Connected)
		{
			await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
			await OnConnectAsync(socket, endPoint, cancellationToken).ConfigureAwait(false);
		}

		var messageLength = await PerformQueryAsync(socket, requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);

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

	protected virtual async ValueTask<int> PerformQueryAsync(Socket socket, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		//TCP connections require sending a 2-byte length value before the message.
		//Use our destination buffer as a temporary buffer to get and send the length.
		BinaryPrimitives.WriteUInt16BigEndian(responseBuffer.Span, (ushort)requestBuffer.Length);
		await socket.SendAsync(responseBuffer[..2], SocketFlags.None, cancellationToken).ConfigureAwait(false);
		//Send our main message from our source buffer	
		await socket.SendAsync(requestBuffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);

		//Read the corresponding 2-byte length in the response to know how long the message is
		await socket.ReceiveAsync(responseBuffer[..2], SocketFlags.None, cancellationToken).ConfigureAwait(false);
		var messageLength = BinaryPrimitives.ReadUInt16BigEndian(responseBuffer.Span);
		//Read the response based on the determined message length
		await socket.ReceiveAsync(responseBuffer[..messageLength], SocketFlags.None, cancellationToken).ConfigureAwait(false);
		return messageLength;
	}
}

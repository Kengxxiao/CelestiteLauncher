using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace TurnerSoftware.DinoDNS.Connection.Listeners;

public class TcpQueryListener : IDnsQueryListener
{
	public static readonly TcpQueryListener Instance = new();

	public async Task ListenAsync(IPEndPoint endPoint, OnDnsQueryCallback callback, DnsMessageOptions options, CancellationToken cancellationToken)
	{
		using var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		socket.Bind(endPoint);
		socket.Listen();

		while (!cancellationToken.IsCancellationRequested)
		{
			var newSocket = await socket.AcceptAsync(cancellationToken).ConfigureAwait(false);
			_ = HandleSocketAsync(newSocket, callback, options, cancellationToken).ConfigureAwait(false);
		}
	}

	private async Task HandleSocketAsync(Socket socket, OnDnsQueryCallback callback, DnsMessageOptions options, CancellationToken cancellationToken)
	{
		try
		{
			await OnConnectAsync(socket, cancellationToken).ConfigureAwait(false);
			using var writerLock = new SemaphoreSlim(1);
			while (true)
			{
				var transitData = TransitData.Rent(options);
				var hasReadData = false;
				try
				{
					var bytesRead = await ReadRequestAsync(socket, transitData.RequestBuffer, cancellationToken).ConfigureAwait(false);
					if (bytesRead == 0)
					{
						socket.Shutdown(SocketShutdown.Both);
						socket.Dispose();
						return;
					}

					hasReadData = true;
					_ = HandleRequestAsync(socket, callback, transitData, writerLock, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					if (!hasReadData)
					{
						//Returning transit data only when data hasn't been read.
						//Once data has been read, the responsibility for returning the data belongs in the request handling.
						TransitData.Return(transitData);
					}
				}
			}
		}
		catch (Exception ex)
		{
			//TODO: Logger
			Console.WriteLine($"Socket:{ex.Message}");
		}
		finally
		{
			OnSocketEnd(socket);
		}
	}

	private async Task HandleRequestAsync(Socket socket, OnDnsQueryCallback callback, TransitData transitData, SemaphoreSlim writerLock, CancellationToken cancellationToken)
	{
		try
		{
			var (requestBuffer, responseBuffer) = transitData;
			var bytesWritten = await callback(requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);
			await writerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			await WriteResponseAsync(socket, responseBuffer[..bytesWritten], cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			TransitData.Return(transitData);
			writerLock.Release();
		}
	}

	protected virtual ValueTask OnConnectAsync(Socket socket, CancellationToken cancellationToken) => ValueTask.CompletedTask;
	protected virtual void OnSocketEnd(Socket socket) { }

	protected virtual async ValueTask<int> ReadRequestAsync(Socket socket, Memory<byte> requestBuffer, CancellationToken cancellationToken)
	{
		//Read the corresponding 2-byte length in the request to know how long the message is
		await socket.ReceiveAsync(requestBuffer[..2], SocketFlags.None, cancellationToken).ConfigureAwait(false);
		var messageLength = BinaryPrimitives.ReadUInt16BigEndian(requestBuffer.Span);
		//Read the request based on the determined message length
		await socket.ReceiveAsync(requestBuffer[..messageLength], SocketFlags.None, cancellationToken).ConfigureAwait(false);
		return messageLength;
	}

	protected virtual async ValueTask WriteResponseAsync(Socket socket, ReadOnlyMemory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		var tempBuffer = ArrayPool<byte>.Shared.Rent(2);
		try
		{
			//TCP connections require sending a 2-byte length value before the message.
			BinaryPrimitives.WriteUInt16BigEndian(tempBuffer.AsSpan(), (ushort)responseBuffer.Length);
			await socket.SendAsync(tempBuffer.AsMemory(0, 2), SocketFlags.None, cancellationToken).ConfigureAwait(false);
			//Send the response message.
			await socket.SendAsync(responseBuffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(tempBuffer);
		}
	}
}

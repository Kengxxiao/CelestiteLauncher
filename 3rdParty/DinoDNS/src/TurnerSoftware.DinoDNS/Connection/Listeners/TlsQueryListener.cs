using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace TurnerSoftware.DinoDNS.Connection.Listeners;

public sealed class TlsQueryListener : TcpQueryListener
{
	public readonly SslProtocols EnabledSslProtocols;

	private readonly ConcurrentDictionary<IntPtr, SslStream> StreamLookup = new();
	private readonly SslServerAuthenticationOptions Options;

	public TlsQueryListener(SslServerAuthenticationOptions options)
	{
		Options = options;
	}

	protected override async ValueTask OnConnectAsync(Socket socket, CancellationToken cancellationToken)
	{
		var stream = new SslStream(new NetworkStream(socket), true);
		await stream.AuthenticateAsServerAsync(Options, cancellationToken).ConfigureAwait(false);
		StreamLookup.TryAdd(socket.Handle, stream);
	}

	protected override void OnSocketEnd(Socket socket) => StreamLookup.TryRemove(socket.Handle, out _);

	protected override async ValueTask<int> ReadRequestAsync(Socket socket, Memory<byte> requestBuffer, CancellationToken cancellationToken)
	{
		var stream = StreamLookup.GetValueOrDefault(socket.Handle)!;
		//Read the corresponding 2-byte length in the request to know how long the message is
		await stream.ReadAsync(requestBuffer[..2], cancellationToken).ConfigureAwait(false);
		var messageLength = BinaryPrimitives.ReadUInt16BigEndian(requestBuffer.Span);
		//Read the request based on the determined message length
		await stream.ReadAsync(requestBuffer[..messageLength], cancellationToken).ConfigureAwait(false);
		return messageLength;
	}

	protected override async ValueTask WriteResponseAsync(Socket socket, ReadOnlyMemory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		var tempBuffer = ArrayPool<byte>.Shared.Rent(2);
		try
		{
			var stream = StreamLookup.GetValueOrDefault(socket.Handle)!;
			//TCP connections require sending a 2-byte length value before the message.
			BinaryPrimitives.WriteUInt16BigEndian(tempBuffer.AsSpan(), (ushort)responseBuffer.Length);
			await stream.WriteAsync(tempBuffer.AsMemory(0, 2), cancellationToken).ConfigureAwait(false);
			//Send the response message.
			await stream.WriteAsync(responseBuffer, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(tempBuffer);
		}
	}
}
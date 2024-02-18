using System.Net;
using System.Net.Quic;
using System.Net.Security;

namespace TurnerSoftware.DinoDNS.Connection.Resolvers
{
	public class DoQResolver : IDnsResolver
	{
		public static DoQResolver Instance = new();

		public async ValueTask<int> SendMessageAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer,
			CancellationToken cancellationToken)
		{
			if (!QuicConnection.IsSupported)
				throw new NotImplementedException();
			QuicConnection? connection = null;
			try
			{
				var doqConnectionOptions = new QuicClientConnectionOptions()
				{
					RemoteEndPoint = endPoint,
					DefaultStreamErrorCode = 0xa,
					DefaultCloseErrorCode = 0xb,
					ClientAuthenticationOptions = new SslClientAuthenticationOptions()
					{
						ApplicationProtocols = [new SslApplicationProtocol("doq")],
						RemoteCertificateValidationCallback = (_, _, _, _) => true
					}
				};
				connection = await QuicConnection.ConnectAsync(doqConnectionOptions, cancellationToken).ConfigureAwait(false);
				var outgoingStream =
					await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken).ConfigureAwait(false);
				await outgoingStream.WriteAsync(requestBuffer, cancellationToken).ConfigureAwait(false);
				return await outgoingStream.ReadAsync(responseBuffer, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				return -1;
			}
			finally
			{
				if (connection != null)
					await connection.DisposeAsync().ConfigureAwait(false);
			}
		}
	}
}

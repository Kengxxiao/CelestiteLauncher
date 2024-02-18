using System.Net;
using TurnerSoftware.DinoDNS.Connection.Listeners;

namespace TurnerSoftware.DinoDNS;

/// <summary>
/// Represents a DNS server endpoint running at a specific <paramref name="EndPoint"/> using the provided <paramref name="QueryListener"/>.
/// </summary>
/// <param name="EndPoint">The IP address and port combination that the listener should listen on.</param>
/// <param name="QueryListener">The query listener implementation to run (eg. UDP, TCP, TLS, HTTPS).</param>
public readonly record struct ServerEndPoint(IPEndPoint EndPoint, IDnsQueryListener QueryListener)
{
	/// <summary>
	/// Create a <see cref="ServerEndPoint"/> for running on <see cref="IPAddress.Any"/> for the provided <paramref name="connectionType"/>.
	/// The port number chosen is determined by the <paramref name="connectionType"/>.
	/// </summary>
	/// <param name="connectionType">The connection type to use. Only UDP and TCP can be specified as DoH and DoT required additional configuration.</param>
	public ServerEndPoint(ConnectionType connectionType)
		: this(IPAddress.Any, connectionType) { }

	/// <summary>
	/// Create a <see cref="ServerEndPoint"/> for running on <paramref name="address"/> for the provided <paramref name="connectionType"/>.
	/// The port number chosen is determined by the <paramref name="connectionType"/>.
	/// </summary>
	/// <param name="address">The IP address to listen for connections on.</param>
	/// <param name="connectionType">The connection type to use. Only UDP and TCP can be specified as DoH and DoT required additional configuration.</param>
	public ServerEndPoint(IPAddress address, ConnectionType connectionType)
		: this(address, NameServers.GetDefaultPort(connectionType), connectionType) { }

	/// <summary>
	/// Create a <see cref="ServerEndPoint"/> for listening on <paramref name="address"/> at <paramref name="port"/> for the provided <paramref name="connectionType"/>.
	/// </summary>
	/// <param name="address">The IP address to listen for connections on.</param>
	/// <param name="port">The port number to listen for connections on.</param>
	/// <param name="connectionType">The connection type to use. Only UDP and TCP can be specified as DoH and DoT required additional configuration.</param>
	public ServerEndPoint(IPAddress address, int port, ConnectionType connectionType)
		: this(new IPEndPoint(address, port), connectionType) { }

	/// <summary>
	/// Create a <see cref="ServerEndPoint"/> for listening on <paramref name="endPoint"/> for the provided <paramref name="connectionType"/>.
	/// </summary>
	/// <param name="endPoint">The IP endpoint to listen for connections on.</param>
	/// <param name="connectionType">The connection type to use. Only UDP and TCP can be specified as DoH and DoT required additional configuration.</param>
	public ServerEndPoint(IPEndPoint endPoint, ConnectionType connectionType)
		: this(endPoint, GetDefaultConnectionServer(connectionType)) { }

	public static implicit operator ServerEndPoint(ConnectionType connectionType) => new(connectionType);

	/// <summary>
	/// Gets the default <see cref="IDnsQueryListener"/> for the provided <paramref name="connectionType"/>.
	/// </summary>
	/// <param name="connectionType">The connection type to use. Only UDP and TCP can be specified as DoH and DoT required additional configuration.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException"></exception>
	public static IDnsQueryListener GetDefaultConnectionServer(ConnectionType connectionType) => connectionType switch
	{
		ConnectionType.Udp => UdpQueryListener.Instance,
		ConnectionType.Tcp => TcpQueryListener.Instance,
		ConnectionType.DoH => throw new ArgumentException("DNS-over-HTTPS needs to be configured - pass a configured instance of HttpQueryListener into the ServerEndPoint constructor.", nameof(connectionType)),
		ConnectionType.DoT => throw new ArgumentException("DNS-over-TLS needs to be configured - pass a configured instance of TlsQueryListener into the ServerEndPoint constructor.", nameof(connectionType)),
		_ => throw new ArgumentException("Unknown connection type", nameof(connectionType))
	};
}
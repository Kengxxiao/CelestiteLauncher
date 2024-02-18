using System.Net;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Connection.Resolvers;

namespace TurnerSoftware.DinoDNS;

public readonly record struct NameServer(IPEndPoint EndPoint, IDnsResolver Connection)
{
	public NameServer(IPAddress address, ConnectionType connectionType)
		: this(address, NameServers.GetDefaultPort(connectionType), connectionType) { }

	public NameServer(IPAddress address, int port, ConnectionType connectionType)
		: this(new IPEndPoint(address, port), connectionType) { }

	public NameServer(IPEndPoint endPoint, ConnectionType connectionType)
		: this(endPoint, GetDefaultConnectionClient(connectionType)) { }

	public static implicit operator NameServer(IPAddress address) => new(address, ConnectionType.Udp);

	public static IDnsResolver GetDefaultConnectionClient(ConnectionType connectionType) => connectionType switch
	{
		ConnectionType.Udp => UdpResolver.Instance,
		ConnectionType.Tcp => TcpResolver.Instance,
		ConnectionType.UdpWithTcpFallback => UdpTcpDualResolver.Instance,
		ConnectionType.DoT => TlsResolver.Instance,
		ConnectionType.DoQ => DoQResolver.Instance,
		_ => throw new NotImplementedException()
	};
}
namespace TurnerSoftware.DinoDNS;

public enum ConnectionType
{
	/// <summary>
	/// A UDP-only connection.
	/// </summary>
	Udp,
	/// <summary>
	/// A TCP-only connection.
	/// </summary>
	Tcp,
	/// <summary>
	/// A UDP connection that falls back to TCP when the message is truncated.
	/// </summary>
	UdpWithTcpFallback,
	/// <summary>
	/// DNS over HTTPS.
	/// </summary>
	DoH,
	/// <summary>
	/// DNS over TLS.
	/// </summary>
	DoT,
	/// <summary>
	/// DNS over QUIC.
	/// </summary>
	DoQ
}
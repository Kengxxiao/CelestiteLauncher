using System.Net;

namespace TurnerSoftware.DinoDNS;

public static class NameServers
{
	/// <summary>
	/// Default DNS over UDP/TCP Port
	/// </summary>
	public const int DefaultPort = 53;
	/// <summary>
	/// Default DNS over HTTPS Port
	/// </summary>
	public const int DefaultDoHPort = 443;
	/// <summary>
	/// Default DNS over TLS Port
	/// </summary>
	public const int DefaultDoTPort = 853;

	public static int GetDefaultPort(ConnectionType connectionType) => connectionType switch
	{
		ConnectionType.DoH => DefaultDoHPort,
		ConnectionType.DoT => DefaultDoTPort,
		ConnectionType.DoQ => DefaultDoTPort,
		_ => DefaultPort
	};

	public static class Cloudflare
	{
		public static class IPv4
		{
			private static readonly IPAddress PrimaryAddress = IPAddress.Parse("1.1.1.1");
			private static readonly IPAddress SecondaryAddress = IPAddress.Parse("1.0.0.1");

			public static NameServer GetPrimary(ConnectionType connectionType)
				=> new(PrimaryAddress, connectionType);

			public static NameServer GetSecondary(ConnectionType connectionType)
				=> new(SecondaryAddress, connectionType);
		}

		public static class IPv6
		{
			private static readonly IPAddress PrimaryAddress = IPAddress.Parse("2606:4700:4700::1111");
			private static readonly IPAddress SecondaryAddress = IPAddress.Parse("2606:4700:4700::1001");

			public static NameServer GetPrimary(ConnectionType connectionType)
				=> new(PrimaryAddress, connectionType);

			public static NameServer GetSecondary(ConnectionType connectionType)
				=> new(SecondaryAddress, connectionType);
		}
	}

	public static class Google
	{
		public static class IPv4
		{
			private static readonly IPAddress PrimaryAddress = IPAddress.Parse("8.8.8.8");
			private static readonly IPAddress SecondaryAddress = IPAddress.Parse("8.8.4.4");

			public static NameServer GetPrimary(ConnectionType connectionType)
				=> new(PrimaryAddress, connectionType);

			public static NameServer GetSecondary(ConnectionType connectionType)
				=> new(SecondaryAddress, connectionType);
		}

		public static class IPv6
		{
			private static readonly IPAddress PrimaryAddress = IPAddress.Parse("2001:4860:4860::8888");
			private static readonly IPAddress SecondaryAddress = IPAddress.Parse("2001:4860:4860::8844");

			public static NameServer GetPrimary(ConnectionType connectionType)
				=> new(PrimaryAddress, connectionType);

			public static NameServer GetSecondary(ConnectionType connectionType)
				=> new(SecondaryAddress, connectionType);
		}
	}
}
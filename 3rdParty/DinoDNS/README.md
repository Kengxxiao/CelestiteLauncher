<div align="center">

![Icon](images/icon.png)
# Dino DNS
A fast and efficient DNS server and client

![Build](https://img.shields.io/github/workflow/status/TurnerSoftware/DinoDNS/Build)
[![NuGet](https://img.shields.io/nuget/v/TurnerSoftware.DinoDNS.svg)](https://www.nuget.org/packages/TurnerSoftware.DinoDNS/)
</div>

## Overview

Dino DNS provides fast and flexible DNS client and server implementations for:

- DNS-over-UDP
- DNS-over-TCP
- [DNS-over-TLS](https://en.wikipedia.org/wiki/DNS-over-TLS)
- [DNS-over-HTTPS](https://en.wikipedia.org/wiki/DNS-over-HTTPS)

## 🤝 Licensing and Support

Dino DNS is licensed under the MIT license. It is free to use in personal and commercial projects.

There are [support plans](https://turnersoftware.com.au/support-plans) available that cover all active [Turner Software OSS projects](https://github.com/TurnerSoftware).
Support plans provide private email support, expert usage advice for our projects, priority bug fixes and more.
These support plans help fund our OSS commitments to provide better software for everyone.

## 🥇 Performance

These performance comparisons show the performance overhead of the DNS library itself and associated allocations.
They do not represent the network overhead to a remote DNS servers.

The server implementation that each benchmark is performing against is Dino DNS.

### DNS-over-UDP

This is your typical DNS query.
While fast and efficient, it is limited by the lack of transport-layer encryption, reliable delivery and message length.

|            Method |      Mean |     Error |    StdDev |     Op/s | Ratio | RatioSD |   Gen 0 |  Gen 1 | Allocated |
|------------------ |----------:|----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|
|           DinoDNS |  90.28 us |  1.066 us |  0.945 us | 11,077.1 |  1.00 |    0.00 |  0.4883 |      - |   1,704 B |
|       Kapetan_DNS | 325.99 us | 10.447 us | 30.803 us |  3,067.6 |  3.58 |    0.19 | 23.4375 | 0.9766 |  73,996 B |
| MichaCo_DnsClient | 257.72 us |  5.141 us | 10.384 us |  3,880.1 |  2.84 |    0.11 | 22.4609 |      - |  71,640 B |

### DNS-over-TCP

With TCP DNS queries, there is a small overhead from negotiating the connection but otherwise is very fast.
It addresses the reliable delivery and message length limitations that occur with UDP queries.

A good DNS client implementation will pool TCP sockets to avoid needing to negotiate the connection per request.

|            Method |      Mean |    Error |   StdDev |     Op/s | Ratio | RatioSD |  Gen 0 | Allocated |
|------------------ |----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|
|           DinoDNS |  94.99 us | 1.018 us | 0.902 us | 10,527.1 |  1.00 |    0.00 | 0.4883 |   1,892 B |
| MichaCo_DnsClient | 112.52 us | 2.246 us | 3.562 us |  8,887.1 |  1.21 |    0.05 | 1.4648 |   5,064 B |

<small>
⚠ Note: While Kapetan's DNS client does support TCP, it can't be benchmarked due to port exhaustion issues it has.
</small>

### DNS-over-TLS

With DNS-over-TLS, you get the benefits of DNS-over-TCP with transport-layer encryption between the client and the server.

|  Method |     Mean |   Error |  StdDev |    Op/s | Ratio |  Gen 0 | Allocated |
|-------- |---------:|--------:|--------:|--------:|------:|-------:|----------:|
| DinoDNS | 126.5 us | 2.09 us | 1.95 us | 7,908.1 |  1.00 | 0.4883 |   2,274 B |

👋 Know of a .NET DNS-over-TLS client? Raise a PR to add it as a comparison!

### DNS-over-HTTPS

An alternative to DNS-over-TLS is DNS-over-HTTPS, providing the same core functionality through a different method.
This can disguise DNS traffic when performed over port 443 (the default port for HTTPS).

|  Method |     Mean |   Error |  StdDev |    Op/s | Ratio |  Gen 0 | Allocated |
|-------- |---------:|--------:|--------:|--------:|------:|-------:|----------:|
| DinoDNS | 207.2 us | 3.77 us | 3.52 us | 4,827.1 |  1.00 | 1.4648 |   5,625 B |

👋 Know of a .NET DNS-over-HTTPS client? Raise a PR to add it as a comparison!

## ⭐ Getting Started

### Perform a DNS query

This is a basic query against a DNS server, retrieving "A" records to further process.

```csharp
var client = new DnsClient(new NameServer[]
{
	new NameServer(IPAddress.Parse("192.168.0.1"), ConnectionType.Udp)
	NameServers.Cloudflare.IPv4.GetPrimary(ConnectionType.DoH),
}, DnsMessageOptions.Default);

var dnsMessage = await client.QueryAsync("example.org", DnsQueryType.A);
var aRecords = dnsMessage.Answers.WithARecords();
```

### Implement a basic DNS server

This is a basic forwarding DNS server where you can, for example, have use a UDP server endpoint but forward over TLS to another name server.

```csharp
public class DnsForwardingServer : DnsServerBase
{
	private readonly DnsClient Client;

	public DnsForwardingServer(
		NameServer[] nameServers,
		ServerEndPoint[] endPoints,
		DnsMessageOptions options
	) : base(endPoints, options)
	{
		Client = new DnsClient(nameServers, options);
	}

	protected override async ValueTask<int> OnReceiveAsync(ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		return await Client.SendAsync(requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);
	}
}

var server = new DnsForwardingServer(
	new[] { NameServers.Cloudflare.IPv4.GetPrimary(ConnectionType.DoT) },
	new[] { new ServerEndpoint(ConnectionType.Udp) },
	DnsMessageOptions.Default
);
server.Start();
```
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Connection.Listeners;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

namespace TurnerSoftware.DinoDNS.Tests.Connection;

[TestClass]
public class UdpTcpDualResolverTests
{
	public static IAsyncDisposable RunTestServer(
		Func<DnsMessage, DnsMessage> udpResponse,
		Func<DnsMessage, DnsMessage> tcpResponse
	)
	{
		return new Disposable(
			new DnsTestServer().Run(new UdpQueryListener(), udpResponse),
			new DnsTestServer().Run(new TcpQueryListener(), tcpResponse)
		);
	}

	private struct Disposable : IAsyncDisposable
	{
		private readonly IAsyncDisposable UdpServer;
		private readonly IAsyncDisposable TcpServer;

		public Disposable(IAsyncDisposable udpServer, IAsyncDisposable tcpServer)
		{
			UdpServer = udpServer;
			TcpServer = tcpServer;
		}

		public async ValueTask DisposeAsync()
		{
			await UdpServer.DisposeAsync();
			await TcpServer.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task UdpSuccess()
	{
		await using var _ = RunTestServer(
			request => DnsTestServer.ExampleData.Response,
			request => DnsTestServer.ExampleData.Response
				.WithAnswers(new ResourceRecord[]
				{
					new()
					{
						DomainName = new LabelSequence("test.www.example.org"),
						Type = DnsType.A,
						Class = DnsClass.IN,
						TimeToLive = 1800,
						ResourceDataLength = 4,
						Data = new byte[] { 192, 168, 0, 2 }
					}
				})
		);

		var client = new DnsClient(new NameServer[]
		{
			new(DnsTestServer.DefaultEndPoint, new UdpTcpDualResolver())
		}, DnsMessageOptions.Default);

		var response = await client.SendAsync(DnsTestServer.ExampleData.Request);

		Assert.AreEqual(DnsTestServer.ExampleData.Response, response);
	}

	[TestMethod]
	public async Task FallbackToTcp()
	{
		await using var _ = RunTestServer(
			request => DnsMessage.CreateResponse(request, ResponseCode.NOERROR, truncation: Truncation.Yes)
				.WithAnswers(new ResourceRecord[]
				{
					new()
					{
						DomainName = new LabelSequence("test.www.example.org"),
						Type = DnsType.A,
						Class = DnsClass.IN,
						TimeToLive = 1800,
						ResourceDataLength = 4,
						Data = new byte[] { 192, 168, 0, 2 }
					}
				}),
			request => DnsTestServer.ExampleData.Response
		);

		var client = new DnsClient(new NameServer[]
		{
			new(DnsTestServer.DefaultEndPoint, new UdpTcpDualResolver())
		}, DnsMessageOptions.Default);

		var response = await client.SendAsync(DnsTestServer.ExampleData.Request);

		Assert.AreEqual(DnsTestServer.ExampleData.Response, response);
	}
}

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Connection.Listeners;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

namespace TurnerSoftware.DinoDNS.Tests.Connection;

[TestClass]
public class TlsConnectionTests
{
	public static IAsyncDisposable RunTestServer(Func<DnsMessage, DnsMessage> getResponse)
	{
		return DnsTestServer.Instance.Run(new TlsQueryListener(new System.Net.Security.SslServerAuthenticationOptions
		{
			ServerCertificate = DnsTestServer.CreateTemporaryCertificate()
		}), getResponse);
	}

	[TestMethod]
	public async Task BasicTls()
	{
		await using var _ = RunTestServer(request => DnsTestServer.ExampleData.Response);

		var client = new DnsClient(new NameServer[]
		{
			new(DnsTestServer.DefaultEndPoint, new TlsResolver(TlsResolver.AuthOptions.DoNotValidate)) 
		}, DnsMessageOptions.Default);

		var response = await client.SendAsync(DnsTestServer.ExampleData.Request);
		Assert.AreEqual(DnsTestServer.ExampleData.Response, response);
	}
}

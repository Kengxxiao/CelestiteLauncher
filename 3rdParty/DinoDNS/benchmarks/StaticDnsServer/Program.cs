using TurnerSoftware.DinoDNS.Connection.Listeners;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

try
{
	var cancellationTokenSource = new CancellationTokenSource();
	_ = Task.Run(() => {
		Console.Read();
		cancellationTokenSource.Cancel();
	});

	IDnsQueryListener? server = null;
	var protocol = args.Length > 0 ? args[0] : "udp";
	switch (protocol)
	{
		case "tcp":
			Console.WriteLine("TCP server started!");
			server = TcpQueryListener.Instance;
			break;
		case "tls":
			Console.WriteLine("TLS server started!");
			server = new TlsQueryListener(new System.Net.Security.SslServerAuthenticationOptions
			{
				ServerCertificate = DnsTestServer.CreateTemporaryCertificate()
			});
			break;
		case "https":
			Console.WriteLine("HTTPS server started!");
			server = new HttpsQueryListener(DnsTestServer.CreateTemporaryCertificate());
			break;
		case "udp":
		default:
			Console.WriteLine("UDP server started!");
			server = UdpQueryListener.Instance;
			break;
	}

	await DnsTestServer.Instance.StartAsync(
		server, 
		request => DnsMessage.CreateResponse(request, ResponseCode.NOERROR) with
		{
			Answers = DnsTestServer.ExampleData.Response.Answers
		},
		cancellationToken: cancellationTokenSource.Token
	);
}
catch (OperationCanceledException)
{
	Console.WriteLine("Benchmark server has ended.");
}
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TurnerSoftware.DinoDNS.Connection.Listeners;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.TestServer;

public class DnsTestServer
{
	public const int PortNumber = 5001;
	public readonly static IPEndPoint DefaultEndPoint = new(IPAddress.Loopback, PortNumber);

	public static readonly DnsTestServer Instance = new();

	private CancellationTokenSource CancellationTokenSource = new();

	private readonly List<Task> StartedTasks = new();

	public IAsyncDisposable Run(IDnsQueryListener server, Func<DnsMessage, DnsMessage> getResponse)
	{
		StartedTasks.Add(StartAsync(server, getResponse));
		return new Disposable(this);
	}
	public async Task StartAsync(IDnsQueryListener server, Func<DnsMessage, DnsMessage> getResponse, IPEndPoint? serverEndPoint = null, CancellationToken cancellationToken = default)
	{
		await StopAsync();
		CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		try
		{
			var endPoint = serverEndPoint ?? DefaultEndPoint;
			await server.ListenAsync(DefaultEndPoint, (requestBuffer, responseBuffer, token) =>
			{
				new DnsProtocolReader(requestBuffer).ReadMessage(out var message);
				var responseMessage = getResponse(message);
				var writer = new DnsProtocolWriter(responseBuffer).AppendMessage(responseMessage);
				return ValueTask.FromResult(writer.BytesWritten);
			}, DnsMessageOptions.Default, CancellationTokenSource.Token);
		}
		catch (OperationCanceledException) 
		{ 
		}
	}

	public async ValueTask StopAsync()
	{
		CancellationTokenSource.Cancel();
		await Task.WhenAll(StartedTasks);
		StartedTasks.Clear();
	}

	public static X509Certificate2 CreateTemporaryCertificate()
	{
		var ecdsa = ECDsa.Create(ECCurve.CreateFromValue("1.2.840.10045.3.1.7"));
		var certRequest = new CertificateRequest("CN=127.0.0.1", ecdsa, HashAlgorithmName.SHA256);
		var beforeDate = DateTime.Now.AddHours(-1);
		var afterDate = beforeDate.AddHours(2);
		var generatedCert = certRequest.CreateSelfSigned(beforeDate, afterDate);
		return new X509Certificate2(generatedCert.Export(X509ContentType.Pfx));
	}

	public sealed class ExampleData
	{
		public static readonly DnsMessage Request;
		public static readonly DnsMessage Response;

		static ExampleData()
		{
			Request = DnsMessage.CreateQuery(44124)
				.WithQuestions(new Question[]
				{
					new()
					{
						Query = new LabelSequence("test.www.example.org"),
						Type = DnsQueryType.A,
						Class = DnsClass.IN
					}
				});

			Response = DnsMessage.CreateResponse(Request, ResponseCode.NOERROR)
				.WithAnswers(new ResourceRecord[]
				{
					new()
					{
						DomainName = new LabelSequence("test.www.example.org"),
						Type = DnsType.A,
						Class = DnsClass.IN,
						TimeToLive = 1800,
						ResourceDataLength = 4,
						Data = new byte[] { 192, 168, 0, 1 }
					}
				});
		}
	}

	private struct Disposable : IAsyncDisposable
	{
		private readonly DnsTestServer Server;

		public Disposable(DnsTestServer server)
		{
			Server = server;
		}

		public async ValueTask DisposeAsync()
		{
			await Server.StopAsync();
		}
	}
}

using TurnerSoftware.DinoDNS;
using TurnerSoftware.DinoDNS.Protocol;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

Console.WriteLine(@"QDCOUNT: Question record count
ANCOUNT: Answer record count
NSCOUNT: Authority record count
ARCOUNT: Additional record count
=====");

// Support updates: https://datatracker.ietf.org/doc/html/rfc2136
// Core RFC for DNS: http://www.networksorcery.com/enp/rfc/rfc1035.txt
// https://datatracker.ietf.org/doc/html/rfc1035

await RunClientAsync();

static async ValueTask RunClientAsync()
{
	var random = new Random();
	var dnsClient = new DnsClient(new NameServer[]
	{
		NameServers.Cloudflare.IPv4.GetPrimary(ConnectionType.DoT)
		//new(IPAddress.Parse("192.168.0.11"), ConnectionType.Tcp)
	}, DnsMessageOptions.Default);

	var stopwatch = new Stopwatch();
	while (true)
	{
		Console.Write("Query: ");
		var query = Console.ReadLine();
		if (query is null)
		{
			break;
		}

		QueryType:
		Console.Write("Type: ");
		var queryType = Console.ReadLine();
		if (queryType is null)
		{
			continue;
		}
		
		if (!Enum.TryParse(typeof(DnsQueryType), queryType, out var dnsQueryType))
		{
			Console.WriteLine("Invalid query type! (eg. A, CNAME, TXT)");
			goto QueryType;
		}

		var message = DnsMessage.CreateQuery((ushort)random.Next(ushort.MaxValue))
			.WithQuestions(new Question[]
			{
				new Question(new LabelSequence(query), (DnsQueryType)dnsQueryType!, DnsClass.IN)
			});

		stopwatch.Restart();
		var result = await dnsClient.SendAsync(message);
		stopwatch.Stop();
		PrintMessage(result);

		Console.WriteLine("Time (ms): {0}", stopwatch.ElapsedMilliseconds);
		Console.WriteLine();
		Console.WriteLine();
	}
}

static async ValueTask RunServerAsync()
{
	using var localClient = new UdpClient(53);
	using var remoteServer = new UdpClient();
	remoteServer.Connect("192.168.0.10", 53);
	while (true)
	{
		var result = await localClient.ReceiveAsync();
		new DnsProtocolReader(result.Buffer.AsMemory())
			.ReadMessage(out var requestMessage);
		PrintMessage(requestMessage);

		await remoteServer.SendAsync(result.Buffer);
		var remoteResult = await remoteServer.ReceiveAsync();

		new DnsProtocolReader(result.Buffer.AsMemory())
			.ReadMessage(out var responseMessage);
		PrintMessage(responseMessage);

		await localClient.SendAsync(remoteResult.Buffer, result.RemoteEndPoint);
	}
}

static void PrintMessage(DnsMessage message)
{
	Console.WriteLine(message.Header.ToString());

	foreach (var question in message.Questions)
	{
		Console.WriteLine(question.ToString());
	}

	foreach (var resourceRecord in message.Answers)
	{
		Console.WriteLine(resourceRecord.ToString());
	}

	foreach (var resourceRecord in message.Authorities)
	{
		Console.WriteLine(resourceRecord.ToString());
	}

	foreach (var resourceRecord in message.AdditionalRecords)
	{
		Console.WriteLine(resourceRecord.ToString());
	}
}
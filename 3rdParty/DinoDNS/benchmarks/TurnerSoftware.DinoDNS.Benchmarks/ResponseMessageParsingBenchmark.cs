using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class ResponseMessageParsingBenchmark
{
	private byte[]? Message;

	[GlobalSetup]
	public void Setup()
	{
		var requestMessage = DnsMessage.CreateQuery(44124)
			.WithQuestions(new Question[]
			{
				new()
				{
					Query = new LabelSequence("test.www.example.org"),
					Type = DnsQueryType.A,
					Class = DnsClass.IN
				}
			});

		var responseMessage = DnsMessage.CreateResponse(requestMessage, ResponseCode.NOERROR)
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

		var buffer = new byte[1024];
		var messageBytes = new DnsProtocolWriter(buffer.AsMemory())
			.AppendMessage(responseMessage)
			.GetWrittenBytes()
			.ToArray();

		Message = messageBytes;
	}

	[Benchmark(Baseline = true)]
	public DnsMessage DinoDNS()
	{
		new DnsProtocolReader(Message.AsMemory())
			.ReadMessage(out var message);
		return message;
	}

	[Benchmark]
	public DNS.Protocol.Response Kapetan_DNS()
	{
		return DNS.Protocol.Response.FromArray(Message);
	}
}

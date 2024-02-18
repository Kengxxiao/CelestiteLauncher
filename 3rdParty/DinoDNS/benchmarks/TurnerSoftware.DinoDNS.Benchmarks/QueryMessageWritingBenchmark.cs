using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class QueryMessageWritingBenchmark
{
	private readonly byte[] Buffer = new byte[1024];

	private DnsMessage DinoDNS_Message;
	private DNS.Protocol.Request? DNS_Request;

	[GlobalSetup]
	public void Setup()
	{
		DinoDNS_Message = DnsMessage.CreateQuery(44124)
			.WithQuestions(new Question[]
			{
				new()
				{
					Query = new LabelSequence("test.www.example.org"),
					Type = DnsQueryType.A,
					Class = DnsClass.IN
				}
			});

		DNS_Request = new DNS.Protocol.Request()
		{
			Id = 44124,
			OperationCode = DNS.Protocol.OperationCode.Query
		};
		DNS_Request.Questions.Add(new DNS.Protocol.Question(
			new DNS.Protocol.Domain("test.www.example.org")
		));
	}

	[Benchmark(Baseline = true)]
	public void DinoDNS()
	{
		new DnsProtocolWriter(Buffer.AsMemory())
			.AppendMessage(DinoDNS_Message);
	}

	[Benchmark]
	public void Kapetan_DNS()
	{
		DNS_Request!.ToArray();
	}
}

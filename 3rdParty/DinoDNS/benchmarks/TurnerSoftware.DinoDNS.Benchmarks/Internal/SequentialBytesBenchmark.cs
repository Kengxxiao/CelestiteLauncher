using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.Internal;

[Config(typeof(IntrinsicBenchmarkConfig))]
public class SequentialBytesBenchmark
{
	private LabelSequence LabelSequence;

	[GlobalSetup]
	public void Setup()
	{
		var buffer = new byte[1024];
		var messageBytes = new DnsProtocolWriter(buffer.AsMemory())
			.AppendLabelSequence(new LabelSequence("test.www.example.org"))
			.GetWrittenBytes()
			.ToArray();

		new DnsProtocolReader(messageBytes.AsMemory())
			 .ReadLabelSequence(out LabelSequence);
	}

	[Benchmark]
	public int GetSequentialByteLength()
	{
		return LabelSequence.GetSequentialByteLength();
	}
}

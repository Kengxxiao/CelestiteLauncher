using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.Internal;

[Config(typeof(IntrinsicBenchmarkConfig))]
public class SequenceBytesBenchmark
{
	private LabelSequence LabelSequence;

	[GlobalSetup]
	public void Setup()
	{
		var buffer = new byte[1024];
		var writtenBytes = new DnsProtocolWriter(buffer.AsMemory())
				.AppendLabel("www").AppendLabel("example").AppendLabel("org").AppendLabelSequenceEnd()
				.AppendLabel("test").AppendLabel("site").AppendPointer(4).AppendByte(0)
				.GetWrittenBytes();

		new DnsProtocolReader(new(writtenBytes, 17))
			 .ReadLabelSequence(out LabelSequence);
	}

	[Benchmark]
	public int GetSequenceByteLength()
	{
		return LabelSequence.GetSequenceByteLength();
	}
}

using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.Internal;

[Config(typeof(DefaultBenchmarkConfig))]
public class DnsRawValueBenchmark
{
	private readonly DnsRawValue ByteValue = new(new byte[] { 
		116,
		101,
		115,
		116,
		46,
		119,
		119,
		119,
		46,
		101,
		120,
		97,
		109,
		112,
		108,
		101,
		46,
		111,
		114,
		103,
	}.AsMemory());

	private readonly DnsRawValue CharValue = new("test.www.example.org".AsMemory());

	[Benchmark]
	public bool Equality_Mixed() => ByteValue.Equals(CharValue);

	[Benchmark]
	public bool Equality_Byte() => ByteValue.Equals(ByteValue);

	[Benchmark]
	public bool Equality_Char() => CharValue.Equals(CharValue);
}

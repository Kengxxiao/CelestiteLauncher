using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.Internal;

[Config(typeof(IntrinsicBenchmarkConfig))]
public class HeaderReadWriteBenchmark
{
	private byte[] HeaderReadBytes;

	private Header HeaderStruct;
	private byte[] HeaderWriteBytes;

	[GlobalSetup]
	public void Setup()
	{
		HeaderWriteBytes = new byte[Header.Length];
		HeaderStruct = new Header()
		{
			Identification = 44124,
			Flags = new()
			{
				QueryOrResponse = QueryOrResponse.Response,
				Opcode = Opcode.Query,
				RecursionDesired = RecursionDesired.Yes
			}
		};

		var buffer = new byte[Header.Length];
		HeaderReadBytes = new DnsProtocolWriter(buffer.AsMemory())
			.AppendHeader(HeaderStruct)
			.GetWrittenBytes()
			.ToArray();
	}

	[Benchmark]
	public Header ReadHeader()
	{
		new DnsProtocolReader(HeaderReadBytes.AsMemory())
			.ReadHeader(out var header);
		return header;
	}

	[Benchmark]
	public void WriteHeader()
	{
		new DnsProtocolWriter(HeaderWriteBytes.AsMemory())
			.AppendHeader(HeaderStruct);
	}
}

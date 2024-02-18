using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

namespace TurnerSoftware.DinoDNS.Benchmarks.Internal;

[Config(typeof(DefaultBenchmarkConfig))]
public class MessageReadWriteBenchmark
{
	private DnsMessage Message;
	private DnsMessage ByteMessage;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	private byte[] ReadBuffer;
	private byte[] WriteBuffer;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	[GlobalSetup]
	public void Setup()
	{
		Message = DnsTestServer.ExampleData.Response;

		ReadBuffer = new DnsProtocolWriter(new byte[512].AsMemory())
			.AppendMessage(Message)
			.GetWrittenBytes()
			.ToArray();

		new DnsProtocolReader(ReadBuffer.AsMemory())
			.ReadMessage(out ByteMessage);

		WriteBuffer = new byte[512];
	}

	[Benchmark]
	public DnsMessage ReadMessage()
	{
		new DnsProtocolReader(ReadBuffer.AsMemory())
			.ReadMessage(out var message);
		return message;
	}

	[Benchmark]
	public void WriteMessage()
	{
		new DnsProtocolWriter(WriteBuffer.AsMemory())
			.AppendMessage(in Message);
	}

	[Benchmark]
	public void WriteByteMessage()
	{
		new DnsProtocolWriter(WriteBuffer.AsMemory())
			.AppendMessage(in ByteMessage);
	}
}

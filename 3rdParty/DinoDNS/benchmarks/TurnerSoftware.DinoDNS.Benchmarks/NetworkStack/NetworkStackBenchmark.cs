using System.Net;
using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

namespace TurnerSoftware.DinoDNS.Benchmarks.NetworkStack;

[Config(typeof(DefaultBenchmarkConfig))]
public abstract class NetworkStackBenchmark
{
	protected byte[]? RawMessage;

	protected DnsClient? DinoDNS_DnsClient;
	protected DnsMessage DinoDNS_Message;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	protected IPEndPoint ServerEndPoint;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public virtual void Setup()
	{
		var requestMessage = DnsTestServer.ExampleData.Request;
		DinoDNS_Message = requestMessage;

		var buffer = new byte[1024];
		var messageBytes = new DnsProtocolWriter(buffer.AsMemory())
			.AppendMessage(requestMessage)
			.GetWrittenBytes()
			.ToArray();

		RawMessage = messageBytes;

		ServerEndPoint = DnsTestServer.DefaultEndPoint;
	}
}

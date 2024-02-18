using System.Net;
using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Connection.Resolvers;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

namespace TurnerSoftware.DinoDNS.Benchmarks;

[Config(typeof(DefaultBenchmarkConfig))]
public class HostsFileResolverBenchmark
{
	private HostsFileResolver? Client { get; set; }

	private readonly IPEndPoint Endpoint = IPEndPoint.Parse("127.0.0.1:53");
	private ReadOnlyMemory<byte> RequestMessage;
	private Memory<byte> ResponseMessage;

	[GlobalSetup]
	public void Setup()
	{
		var hostsFile = new DnsHostsFile();
		Client = new HostsFileResolver(hostsFile);
		hostsFile.Add("test.www.example.org", new byte[] { 192, 168, 0, 1 });

		var data = new byte[1024].AsMemory();
		var bytesWritten = DnsTestServer.ExampleData.Request.WriteTo(data);
		RequestMessage = data[..bytesWritten];

		ResponseMessage = new byte[1024];
	}

	[Benchmark]
	public async Task DinoDNS()
	{
		await Client!.SendMessageAsync(Endpoint, RequestMessage, ResponseMessage, default);
	}
}

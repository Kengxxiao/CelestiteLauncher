using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.NetworkStack;

public class HttpsStackBenchmark : NetworkStackBenchmark
{
	[GlobalSetup]
	public override void Setup()
	{
		base.Setup();

		DinoDNS_DnsClient = new DnsClient(new NameServer[] { new(ServerEndPoint, new HttpsResolver(HttpConnectionClientOptions.Insecure)) }, DnsMessageOptions.Default);

		ExternalTestServer.StartHttps();
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		ExternalTestServer.Stop();
	}


	[Benchmark(Baseline = true)]
	public async Task<DnsMessage> DinoDNS()
	{
		return await DinoDNS_DnsClient!.SendAsync(DinoDNS_Message);
	}
}

using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.NetworkStack;

public class TlsStackBenchmark : NetworkStackBenchmark
{
	[GlobalSetup]
	public override void Setup()
	{
		base.Setup();

		DinoDNS_DnsClient = new DnsClient(new NameServer[] { new(ServerEndPoint, new TlsResolver(TlsResolver.AuthOptions.DoNotValidate)) }, DnsMessageOptions.Default);

		ExternalTestServer.StartTls();
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

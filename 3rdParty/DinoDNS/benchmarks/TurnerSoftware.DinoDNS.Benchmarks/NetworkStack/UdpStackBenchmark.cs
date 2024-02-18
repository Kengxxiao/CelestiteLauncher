using BenchmarkDotNet.Attributes;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.NetworkStack;

public class UdpStackBenchmark : NetworkStackBenchmark
{
	private DNS.Client.DnsClient? Kapetan_DNS_DnsClient;
	private DNS.Client.ClientRequest? Kapetan_DNS_ClientRequest;

	private global::DnsClient.LookupClient? MichaCo_DnsClient_LookupClient;
	private global::DnsClient.DnsQuestion? MichaCo_DnsClient_DnsQuestion;

	[GlobalSetup]
	public override void Setup()
	{
		base.Setup();

		DinoDNS_DnsClient = new DnsClient(new NameServer[] { new(ServerEndPoint, ConnectionType.Udp) }, DnsMessageOptions.Default);
		Kapetan_DNS_DnsClient = new DNS.Client.DnsClient(new DNS.Client.RequestResolver.UdpRequestResolver(ServerEndPoint));
		MichaCo_DnsClient_LookupClient = new global::DnsClient.LookupClient(new global::DnsClient.LookupClientOptions(ServerEndPoint)
		{
			UseCache = false
		});

		ExternalTestServer.StartUdp();

		Kapetan_DNS_ClientRequest = Kapetan_DNS_DnsClient.FromArray(RawMessage);
		MichaCo_DnsClient_DnsQuestion = new global::DnsClient.DnsQuestion("test.www.example.org", global::DnsClient.QueryType.A);
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

	[Benchmark]
	public async Task<DNS.Protocol.IResponse> Kapetan_DNS()
	{
		return await Kapetan_DNS_ClientRequest!.Resolve();
	}

	[Benchmark]
	public async Task<global::DnsClient.IDnsQueryResponse> MichaCo_DnsClient()
	{
		return await MichaCo_DnsClient_LookupClient!.QueryAsync(MichaCo_DnsClient_DnsQuestion);
	}
}

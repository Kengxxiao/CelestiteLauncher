using BenchmarkDotNet.Attributes;
using System.Net;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Benchmarks.NetworkStack;

public class TcpStackBenchmark : NetworkStackBenchmark
{
	private DNS.Client.DnsClient? Kapetan_DNS_DnsClient;
	private DNS.Client.ClientRequest? Kapetan_DNS_ClientRequest;

	private global::DnsClient.LookupClient? MichaCo_DnsClient_LookupClient;
	private global::DnsClient.DnsQuestion? MichaCo_DnsClient_DnsQuestion;

	[GlobalSetup]
	public override void Setup()
	{
		base.Setup();

		DinoDNS_DnsClient = new DnsClient(new NameServer[] { new(ServerEndPoint, ConnectionType.Tcp) }, DnsMessageOptions.Default);
		Kapetan_DNS_DnsClient = new DNS.Client.DnsClient(new DNS.Client.RequestResolver.TcpRequestResolver(ServerEndPoint));
		MichaCo_DnsClient_LookupClient = new global::DnsClient.LookupClient(new global::DnsClient.LookupClientOptions(ServerEndPoint)
		{
			UseCache = false,
			UseTcpOnly = true
		});

		ExternalTestServer.StartTcp();

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

	///// <summary>
	///// Due to how this creates new sockets per request leading to port exhaustion, this can't be benchmarked with the others.
	///// </summary>
	//[Benchmark]
	//public async Task<DNS.Protocol.IResponse> Kapetan_DNS()
	//{
	//	return await Kapetan_DNS_ClientRequest!.Resolve();
	//}

	[Benchmark]
	public async Task<global::DnsClient.IDnsQueryResponse> MichaCo_DnsClient()
	{
		return await MichaCo_DnsClient_LookupClient!.QueryAsync(MichaCo_DnsClient_DnsQuestion);
	}
}

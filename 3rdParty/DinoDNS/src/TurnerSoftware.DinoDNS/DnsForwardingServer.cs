namespace TurnerSoftware.DinoDNS;

public class DnsForwardingServer : DnsServerBase
{
	private readonly DnsClient Client;

	public DnsForwardingServer(
		NameServer[] nameServers,
		ServerEndPoint[] endPoints,
		DnsMessageOptions options
	) : base(endPoints, options)
	{
		Client = new DnsClient(nameServers, options);
	}

	protected override async ValueTask<int> OnReceiveAsync(ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken)
	{
		return await Client.SendAsync(requestBuffer, responseBuffer, cancellationToken).ConfigureAwait(false);
	}
}
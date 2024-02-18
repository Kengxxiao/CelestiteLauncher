namespace TurnerSoftware.DinoDNS;

public abstract class DnsServerBase
{
	private readonly ServerEndPoint[] EndPoints;
	public readonly DnsMessageOptions Options;

	private CancellationTokenSource? TokenSource;

	public DnsServerBase(ServerEndPoint[] endPoints, DnsMessageOptions options)
	{
		if (endPoints is null || endPoints.Length == 0)
		{
			throw new ArgumentException("Invalid number of server endpoints configured.");
		}

		if (!options.Validate(out var errorMessage))
		{
			throw new ArgumentException($"Invalid DNS options. {errorMessage}");
		}

		EndPoints = endPoints;
		Options = options;
	}

	public void Start(CancellationToken cancellationToken = default)
	{
		if (TokenSource is not null)
		{
			throw new InvalidOperationException("DNS server has already started");
		}

		TokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		foreach (var server in EndPoints)
		{
			_ = server.QueryListener.ListenAsync(server.EndPoint, OnReceiveAsync, Options, TokenSource.Token);
		}
	}

	public void Stop()
	{
		TokenSource?.Cancel();
	}

	protected abstract ValueTask<int> OnReceiveAsync(ReadOnlyMemory<byte> requestBuffer, Memory<byte> responseBuffer, CancellationToken cancellationToken);
}
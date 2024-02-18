using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS;

public static class DnsClientExtensions
{
	private static ushort GetRandomIdentifier() => (ushort)Random.Shared.Next(ushort.MaxValue);

	public static async ValueTask<DnsMessage> QueryAsync(this DnsClient client, string query, DnsQueryType type, DnsClass dnsClass = DnsClass.IN, CancellationToken cancellationToken = default)
		=> await client.QueryAsync(new Question(query, type, dnsClass), cancellationToken).ConfigureAwait(false);

	public static async ValueTask<DnsMessage> QueryAsync(this DnsClient client, Question question, CancellationToken cancellationToken = default) => await client.SendAsync(
		DnsMessage.CreateQuery(GetRandomIdentifier()).WithQuestions(new[] { question }),
		cancellationToken
	).ConfigureAwait(false);
}

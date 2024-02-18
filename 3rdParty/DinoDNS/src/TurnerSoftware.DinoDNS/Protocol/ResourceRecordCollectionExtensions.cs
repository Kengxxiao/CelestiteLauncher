using TurnerSoftware.DinoDNS.Protocol.ResourceRecords;

namespace TurnerSoftware.DinoDNS.Protocol;

public static class ResourceRecordCollectionExtensions
{
	private static IEnumerable<T> SelectWhere<T>(ResourceRecordCollection resourceRecords, DnsType type, Func<ResourceRecord, T> map)
	{
		foreach (var record in resourceRecords)
		{
			if (record.Type == type)
			{
				yield return map(record);
			}
		}
	}

	public static IEnumerable<ARecord> WithARecords(this ResourceRecordCollection resourceRecords)
		=> SelectWhere(resourceRecords, DnsType.A, r => new ARecord(r));
}

using System.Net;

namespace TurnerSoftware.DinoDNS.Protocol.ResourceRecords;

public readonly record struct ARecord(
	LabelSequence DomainName,
	DnsClass Class,
	uint TimeToLive,
	DnsRawValue Value
)
{
	public const int DataLength = 4;

	public ARecord(ResourceRecord record) : this(
		record.DomainName,
		record.Class,
		record.TimeToLive,
		record.Data
	)
	{ }

	public IPAddress ToIPAddress()
	{
		Span<byte> ipAddressBytes = stackalloc byte[4];
		if (Value.TryWriteBytes(ipAddressBytes, out var bytesWritten))
		{
			return new IPAddress(ipAddressBytes[..bytesWritten]);
		}
		throw new FormatException("Invalid data for IP address");
	}

	public ResourceRecord AsResourceRecord()
	{
		return new ResourceRecord(
			DomainName,
			DnsType.A,
			Class,
			TimeToLive,
			DataLength,
			Value.ByteValue
		);
	}

	public static implicit operator ResourceRecord(in ARecord record)
	{
		return new ResourceRecord(
			record.DomainName,
			DnsType.A,
			record.Class,
			record.TimeToLive,
			DataLength,
			record.Value.ByteValue
		);
	}
}

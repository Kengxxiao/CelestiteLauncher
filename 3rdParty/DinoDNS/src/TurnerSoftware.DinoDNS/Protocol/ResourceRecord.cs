namespace TurnerSoftware.DinoDNS.Protocol;

/// <summary>
/// 
/// </summary>
/// <param name="DomainName"></param>
/// <param name="Type"></param>
/// <param name="Class"></param>
/// <param name="TimeToLive">Number of seconds the resource can be cached for.</param>
/// <param name="ResourceDataLength"></param>
/// <param name="Data"></param>
public readonly record struct ResourceRecord(
	in LabelSequence DomainName,
	DnsType Type,
	DnsClass Class,
	uint TimeToLive,
	ushort ResourceDataLength,
	ReadOnlyMemory<byte> Data
) : IEquatable<ResourceRecord>
{
	public bool Equals(ResourceRecord other) =>
		DomainName.Equals(other.DomainName) &&
		Type == other.Type &&
		Class == other.Class &&
		TimeToLive == other.TimeToLive &&
		ResourceDataLength == other.ResourceDataLength &&
		Data.Span.SequenceEqual(other.Data.Span);

	public override int GetHashCode() => HashCode.Combine(DomainName, Type, Class, TimeToLive, ResourceDataLength, Data);

	public override string ToString() => $"DomainName:{DomainName},Type:{Type},CLASS:{Class},TTL:{TimeToLive},Length:{ResourceDataLength}";
}

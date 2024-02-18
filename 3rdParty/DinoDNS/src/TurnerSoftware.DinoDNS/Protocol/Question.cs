namespace TurnerSoftware.DinoDNS.Protocol;

public readonly record struct Question(LabelSequence Query, DnsQueryType Type, DnsClass Class)
{
	public override string ToString() => $"QNAME:{Query},QTYPE:{Type},QCLASS:{Class}";
}
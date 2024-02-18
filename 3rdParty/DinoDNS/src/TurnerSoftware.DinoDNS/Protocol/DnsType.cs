namespace TurnerSoftware.DinoDNS.Protocol;

public enum DnsType
{
	A = 1,
	NS = 2,
	CNAME = 5,
	SOA = 6,
	WKS = 11,
	PTR = 12,
	HINFO = 13,
	MINFO = 14,
	MX = 15,
	TXT = 16,
	AAAA = 28
}


public enum DnsQueryType
{
	A = 1,
	NS = 2,
	CNAME = 5,
	SOA = 6,
	WKS = 11,
	PTR = 12,
	HINFO = 13,
	MINFO = 14,
	MX = 15,
	TXT = 16,
	AAAA = 28,

	AXFR = 252,
	ANY = 255
}

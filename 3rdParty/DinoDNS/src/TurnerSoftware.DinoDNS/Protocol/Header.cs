using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace TurnerSoftware.DinoDNS.Protocol;

/// <summary>
/// Represents the first 12-bytes of a DNS message
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Header(
	ushort Identification,
	HeaderFlags Flags,
	ushort QuestionRecordCount,
	ushort AnswerRecordCount,
	ushort AuthorityRecordCount,
	ushort AdditionalRecordCount
)
{
	public const int Length = 12;

	/// <summary>
	/// Endian shuffle allows faster big-to-little endian conversion for reading/writing.
	/// We only want to shuffle the first 12-bytes in 2-byte blocks.
	/// </summary>
	internal static readonly Vector128<byte> EndianShuffle = Vector128.Create((byte)1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, /* <- Header End */ 12, 13, 14, 15);

	public static Header CreateResponseHeader(
		Header requestHeader,
		ResponseCode responseCode,
		RecursionAvailable recursionAvailable = RecursionAvailable.No,
		Truncation truncation = Truncation.No,
		AuthoritativeAnswer authoritativeAnswer = AuthoritativeAnswer.No,
		ushort answerRecordCount = 0,
		ushort authorityRecordCount = 0,
		ushort additionalRecordCount = 0
	) => requestHeader with
	{
		Flags = requestHeader.Flags with
		{
			QueryOrResponse = QueryOrResponse.Response,
			RecursionAvailable = recursionAvailable,
			AuthoritativeAnswer = authoritativeAnswer,
			Truncation = truncation,
			ResponseCode = responseCode
		},
		AnswerRecordCount = answerRecordCount,
		AuthorityRecordCount = authorityRecordCount,
		AdditionalRecordCount = additionalRecordCount
	};

	public override string ToString()
	{
		var flags = Flags.ToString();
		return $"ID:{Identification:00000},{flags},QDCOUNT:{QuestionRecordCount},ANCOUNT:{AnswerRecordCount},NSCOUNT:{AuthorityRecordCount},ARCOUNT:{AdditionalRecordCount}";
	}
}

/// <summary>
/// Represents the 2-byte header flags of a DNS message
/// </summary>
/// <param name="Value">The raw value representing the combined header flags.</param>
public readonly record struct HeaderFlags(ushort Value)
{
	public QueryOrResponse QueryOrResponse
	{
		get => (QueryOrResponse)(Value & 0b10000000_00000000);
		init => Value = (ushort)(Value & ~0b10000000_00000000 | (int)value);
	}
	public Opcode Opcode
	{
		get => (Opcode)(Value & 0b01111000_00000000);
		init => Value = (ushort)(Value & ~0b01111000_00000000 | (int)value);
	}
	public AuthoritativeAnswer AuthoritativeAnswer
	{
		get => (AuthoritativeAnswer)(Value & 0b00000100_00000000);
		init => Value = (ushort)(Value & ~0b00000100_00000000 | (int)value);
	}
	public Truncation Truncation
	{
		get => (Truncation)(Value & 0b00000010_00000000);
		init => Value = (ushort)(Value & ~0b00000010_00000000 | (int)value);
	}
	public RecursionDesired RecursionDesired
	{
		get => (RecursionDesired)(Value & 0b00000001_00000000);
		init => Value = (ushort)(Value & ~0b00000001_00000000 | (int)value);
	}
	public RecursionAvailable RecursionAvailable
	{
		get => (RecursionAvailable)(Value & 0b00000000_10000000);
		init => Value = (ushort)(Value & ~0b00000000_10000000 | (int)value);
	}
	public ushort Z
	{
		get => (ushort)((Value & 0b00000000_01110000) >> 4);
		init => Value = (ushort)(Value & ~0b00000000_01110000 | value << 4);
	}
	public ResponseCode ResponseCode
	{
		get => (ResponseCode)(Value & 0b00000000_00001111);
		init => Value = (ushort)(Value & ~0b00000000_00001111 | (int)value);
	}

	public override string ToString() => $"[{Convert.ToString(Value, 2).PadLeft(16, '0')}/{QueryOrResponse},{Opcode},{AuthoritativeAnswer},{Truncation},{RecursionDesired},{RecursionAvailable},{Z},{ResponseCode}]";
}

/// <summary>
/// Indicates if the message is a query or a response
/// </summary>
public enum QueryOrResponse : ushort
{
	Query = 0,
	Response = 1 << 15
}

public enum Opcode : ushort
{
	/// <summary>
	/// Query
	/// (RFC 1035)
	/// </summary>
	Query = 0,
	/// <summary>
	/// Inverse Query (aka. Reverse DNS Lookup)
	/// (RFC 1035)
	/// </summary>
	IQuery = 1 << 11,
	/// <summary>
	/// Server Status
	/// (RFC 1035)
	/// </summary>
	Status = 2 << 11,
	/// <summary>
	/// Specifies whether to delete, add or update RRs from a specified DNS zone.
	/// (RFC 2136)
	/// </summary>
	Update = 5 << 11
}

/// <summary>
/// Indicates if the name server is authoritative for the domain in the question section.
/// </summary>
public enum AuthoritativeAnswer : ushort
{
	No = 0,
	Yes = 1 << 10
}

/// <summary>
/// Indicates if the response was truncated.
/// </summary>
public enum Truncation : ushort
{
	No = 0,
	Yes = 1 << 9
}

/// <summary>
/// This flag tells the name server to handle the query itself, called a recursive query.
/// If the bit is not set, and the requested name server doesn't have an authoritative answer, the requested name server returns a list of other name servers to contact for the answer.
/// This is called an iterative query.
/// </summary>
public enum RecursionDesired : ushort
{
	No = 0,
	Yes = 1 << 8
}

/// <summary>
/// Indicates if the server supports recursion.
/// </summary>
public enum RecursionAvailable : ushort
{
	No = 0,
	Yes = 1 << 7
}

public enum ResponseCode : ushort
{
	/// <summary>
	/// No Error
	/// (RFC 1035)
	/// </summary>
	NOERROR = 0,
	/// <summary>
	/// Format Error:
	/// The name server was unable to interpret the query.
	/// (RFC 1035)
	/// </summary>
	FORMERR = 1,
	/// <summary>
	/// Server Failure:
	/// The name server was unable to process this query due to a problem with the name server.
	/// (RFC 1035)
	/// </summary>
	SERVFAIL = 2,
	/// <summary>
	/// Non-Existent Domain:
	/// Meaningful only for responses from an authoritative name server, 
	/// this signifies that the domain name referenced in the query does not exist.
	/// (RFC 1035)
	/// </summary>
	NXDOMAIN = 3,
	/// <summary>
	/// Not Implemented:
	/// The name server does not support the requested kind of query.
	/// (RFC 1035)
	/// </summary>
	NOTIMP = 4,
	/// <summary>
	/// Query Refused:
	/// The name server refuses to perform the specified operation for policy reasons.
	/// For example, a name server may not wish to provide the information to the particular requester,
	/// or a name server may not wish to perform a particular operation (eg. zone transfer) for a particular data.
	/// (RFC 1035)
	/// </summary>
	REFUSED = 5,
	/// <summary>
	/// Existing Domain:
	/// An existing domain is found when it shouldn't.
	/// (RFC 2136)
	/// </summary>
	YXDOMAIN = 6,
	/// <summary>
	/// Existing Resource Record Set:
	/// An existing resource record set is found when it shouldn't.
	/// (RFC 2136)
	/// </summary>
	YXRRSET = 7,
	/// <summary>
	/// Non-Existent Resource Record Set:
	/// A resource record set doesn't exist when it should.
	/// (RFC 2136)
	/// </summary>
	NXRRSET = 8,
	/// <summary>
	/// Not Authoriative:
	/// The server is not authoritative for the zone named in the Zone Section.
	/// (RFC 2136)
	/// </summary>
	NOTAUTH = 9,
	/// <summary>
	/// Not Zone:
	/// A name used in the Prerequisite or Update Section is not within the zone donated by the Zone Section.
	/// (RFC 2136)
	/// </summary>
	NOTZONE = 10
}
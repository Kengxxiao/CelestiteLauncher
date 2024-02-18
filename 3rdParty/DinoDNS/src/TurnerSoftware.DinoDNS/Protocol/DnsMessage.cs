namespace TurnerSoftware.DinoDNS.Protocol;

public readonly record struct DnsMessage(
	Header Header,
	QuestionCollection Questions,
	ResourceRecordCollection Answers,
	ResourceRecordCollection Authorities,
	ResourceRecordCollection AdditionalRecords
)
{
	public static DnsMessage CreateQuery(
		ushort identification,
		Opcode opcode = Opcode.Query,
		RecursionDesired recursionDesired = RecursionDesired.Yes
	) => new()
	{
		Header = new()
		{
			Identification = identification,
			Flags = new()
			{
				QueryOrResponse = QueryOrResponse.Query,
				Opcode = opcode,
				RecursionDesired = recursionDesired
			}
		}
	};

	public static DnsMessage CreateResponse(
		in DnsMessage request,
		ResponseCode responseCode,
		RecursionAvailable recursionAvailable = RecursionAvailable.No,
		Truncation truncation = Truncation.No,
		AuthoritativeAnswer authoritativeAnswer = AuthoritativeAnswer.No
	) => request with
	{
		Header = Header.CreateResponseHeader(
			request.Header,
			responseCode,
			recursionAvailable,
			truncation,
			authoritativeAnswer
		)
	};
}

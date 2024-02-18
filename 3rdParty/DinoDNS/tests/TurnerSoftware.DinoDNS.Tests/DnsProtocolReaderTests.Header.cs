using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Tests.Protocol;

[TestClass]
public class DnsProtocolReaderTests_Header
{
	public static string GetTestName(MethodInfo _, object[] data) => data[^1].ToString()!;
	public static IEnumerable<object[]> ReadHeaderData()
	{
		var buffer = new byte[12];
		var writer = new DnsProtocolWriter(buffer.AsMemory());

		var query = new Header()
		{
			Identification = 1,
			Flags = new()
			{
				Opcode = Opcode.Query,
				QueryOrResponse = QueryOrResponse.Query,
				RecursionDesired = RecursionDesired.Yes
			}
		};
		yield return new object[]
		{
			writer.AppendHeader(query).GetWrittenBytes().ToArray(),
			query,
			"Query"
		};

		var response = new Header()
		{
			Identification = 2,
			Flags = new()
			{
				Opcode = Opcode.Query,
				QueryOrResponse = QueryOrResponse.Response,
				RecursionAvailable = RecursionAvailable.Yes,
				ResponseCode = ResponseCode.NOERROR
			},
			QuestionRecordCount = 2,
			AnswerRecordCount = 4,
			AuthorityRecordCount = 6,
			AdditionalRecordCount = 8
		};
		yield return new object[]
		{
			writer.AppendHeader(response).GetWrittenBytes().ToArray(),
			response,
			"Response"
		};
	}

	[DynamicData(nameof(ReadHeaderData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetTestName))]
	[DataTestMethod]
	public void ReadHeader(byte[] data, Header expected, string testName)
	{
		new DnsProtocolReader(data.AsMemory())
			.ReadHeader(out var header);
		Assert.AreEqual(expected, header);
	}
}

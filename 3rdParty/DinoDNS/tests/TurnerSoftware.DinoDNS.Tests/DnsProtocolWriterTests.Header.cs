using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Tests.Protocol;

[TestClass]
public class DnsProtocolWriterTests_Header
{
	public static string GetTestName(MethodInfo _, object[] data) => data[^1].ToString()!;
	public static IEnumerable<object[]> AppendHeaderData()
	{
		yield return new object[]
		{
			new Header()
			{
				Identification = 1,
				Flags = new()
				{
					Opcode = Opcode.Query,
					QueryOrResponse = QueryOrResponse.Query,
					RecursionDesired = RecursionDesired.Yes
				}
			},
			new byte[]
			{
				0, 1,
				0b00000001, 0b0000000,
				0, 0,
				0, 0,
				0, 0,
				0, 0
			},
			"Query"
		};

		yield return new object[]
		{
			new Header()
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
			},
			new byte[]
			{
				0, 2,
				0b10000000, 0b10000000,
				0, 2,
				0, 4,
				0, 6,
				0, 8
			},
			"Response"
		};
	}

	[DynamicData(nameof(AppendHeaderData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetTestName))]
	[DataTestMethod]
	public void AppendHeader(Header data, byte[] expected, string testName)
	{
		var buffer = new byte[Header.Length];
		var result = new DnsProtocolWriter(buffer.AsMemory())
			.AppendHeader(data)
			.GetWrittenBytes()
			.ToArray();

		CollectionAssert.AreEqual(expected, result);
	}
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using TurnerSoftware.DinoDNS.Internal;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Tests.Protocol;

[TestClass]
public class LabelSequenceLengthTests
{
	public static string LabelSequenceDataNames(MethodInfo _, object[] data) => data[^1].ToString()!;
	public static IEnumerable<object[]> LabelSequenceData()
	{
		var buffer = ArrayPool<byte>.Shared.Rent(512);
		var writer = new DnsProtocolWriter(buffer.AsMemory());

		var singleLabelData = writer.AppendLabel("localhost").EndLabelSequence();
		var multipleLabelData = writer.AppendLabel("example").AppendLabel("org").EndLabelSequence();
		var multipleLabelWithPointersData = writer
				.AppendLabel("www").AppendLabel("example").AppendLabel("org").AppendLabelSequenceEnd()
				.AppendLabel("test").AppendLabel("site").AppendPointer(4).EndLabelSequence();

		ArrayPool<byte>.Shared.Return(buffer);

		yield return new object[]
		{
			singleLabelData,
			0,
			11,
			11,
			"Single Label"
		};
		yield return new object[]
		{
			multipleLabelData,
			0,
			13,
			13,
			"Multiple Label"
		};
		yield return new object[]
		{
			multipleLabelWithPointersData,
			17,
			12,
			23,
			"Multiple Label with Pointers"
		};
	}

	[DynamicData(nameof(LabelSequenceData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(LabelSequenceDataNames))]
	[DataTestMethod]
	public void SequentialBytes(byte[] data, int offset, int sequentialExpectedBytes, int sequenceExpectedBytes, string testName)
	{
		var bytes = new SeekableReadOnlyMemory<byte>(data.AsMemory()).Seek(offset);
		var sequence = new LabelSequence(bytes);

		var result = sequence.GetSequentialByteLength();

		Assert.AreEqual(sequentialExpectedBytes, result);
	}

	[DynamicData(nameof(LabelSequenceData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(LabelSequenceDataNames))]
	[DataTestMethod]
	public void SequenceBytes(byte[] data, int offset, int sequentialExpectedBytes, int sequenceExpectedBytes, string testName)
	{
		var bytes = new SeekableReadOnlyMemory<byte>(data.AsMemory()).Seek(offset);
		var sequence = new LabelSequence(bytes);

		var result = sequence.GetSequenceByteLength();

		Assert.AreEqual(sequenceExpectedBytes, result);
	}
}

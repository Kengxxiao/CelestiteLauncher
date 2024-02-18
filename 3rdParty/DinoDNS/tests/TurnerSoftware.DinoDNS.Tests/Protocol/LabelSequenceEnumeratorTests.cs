using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using TurnerSoftware.DinoDNS.Internal;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Tests.Protocol;

[TestClass]
public class LabelSequenceEnumeratorTests
{
	public static string ReadLabelsDataNames(MethodInfo _, object[] data) => data[^1].ToString()!;
	public static IEnumerable<object[]> ReadLabelsData()
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
			"localhost",
			0,
			"Single Label"
		};
		yield return new object[] 
		{
			multipleLabelData,
			"example.org",
			0,
			"Multiple Label"
		};
		yield return new object[]
		{
			multipleLabelWithPointersData,
			"test.site.example.org",
			17,
			"Multiple Label with Pointers"
		};
	}

	[DynamicData(nameof(ReadLabelsData), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(ReadLabelsDataNames))]
	[DataTestMethod]
	public void ReadLabels(byte[] data, string expectedDomain, int offset, string testName)
	{
		var bytes = new SeekableReadOnlyMemory<byte>(data.AsMemory()).Seek(offset);
		var enumerator = new LabelSequence(bytes).GetEnumerator();

		var expectedLabels = expectedDomain.Split('.');
		for (var i = 0; i < expectedLabels.Length; i++)
		{
			var expected = expectedLabels[i];
			enumerator.MoveNext();
			var result = enumerator.Current.ToString();
			Assert.AreEqual(expected, result);
		}

		Assert.IsFalse(enumerator.MoveNext(), "Reader has more labels than expected");
	}
}

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurnerSoftware.DinoDNS.Protocol;

namespace TurnerSoftware.DinoDNS.Tests.Protocol;

[TestClass]
public class DnsRawValueTests
{
	[TestMethod]
	public void TryWriteBytes_FromBytes()
	{
		var input = new byte[] { 97, 98, 99, 100 };
		var expected = input;

		var dnsRawValue = new DnsRawValue(input);
		var actual = new byte[expected.Length];
		Assert.IsTrue(dnsRawValue.TryWriteBytes(actual, out var bytesWritten));
		Assert.AreEqual(expected.Length, bytesWritten);
		CollectionAssert.AreEqual(expected, actual);
	}
	[TestMethod]
	public void TryWriteBytes_FromChars()
	{
		var input = "abcd";
		var expected = new byte[] { 97, 98, 99, 100 };

		var dnsRawValue = new DnsRawValue(input.AsMemory());
		var actual = new byte[expected.Length];
		Assert.IsTrue(dnsRawValue.TryWriteBytes(actual, out var bytesWritten));
		Assert.AreEqual(expected.Length, bytesWritten);
		CollectionAssert.AreEqual(expected.ToArray(), actual);
	}
	[TestMethod]
	public void TryWriteBytes_NotEnoughSpace()
	{
		var input = "abcd";

		var dnsRawValue = new DnsRawValue(input.AsMemory());
		var actual = new byte[input.Length - 1];
		Assert.IsFalse(dnsRawValue.TryWriteBytes(actual, out var bytesWritten));
		Assert.AreEqual(0, bytesWritten);
	}

	[DataTestMethod]
	[DataRow("abcd", new byte[] { 97, 98, 99, 100 }, true, DisplayName = "Equal")]
	[DataRow("", new byte[] { }, true, DisplayName = "Equal (Empty)")]
	[DataRow("ABCD", new byte[] { 97, 98, 99, 100 }, false, DisplayName = "Not Equal")]
	public void Equals_CharsToBytes(string valueA, byte[] valueB, bool expected)
	{
		var dnsRawValueA = new DnsRawValue(valueA.AsMemory());
		var dnsRawValueB = new DnsRawValue(valueB.AsMemory());
		Assert.AreEqual(expected, dnsRawValueA.Equals(dnsRawValueB));
	}
	[DataTestMethod]
	[DataRow("abcd", "abcd", true, DisplayName = "Equal")]
	[DataRow("", "", true, DisplayName = "Equal (Empty)")]
	[DataRow("ABCD", "abcd", false, DisplayName = "Not Equal")]
	public void Equals_CharsToChars(string valueA, string valueB, bool expected)
	{
		var dnsRawValueA = new DnsRawValue(valueA.AsMemory());
		var dnsRawValueB = new DnsRawValue(valueB.AsMemory());
		Assert.AreEqual(expected, dnsRawValueA.Equals(dnsRawValueB));
	}
	[DataTestMethod]
	[DataRow(new byte[] { 97, 98, 99, 100 }, new byte[] { 97, 98, 99, 100 }, true, DisplayName = "Equal")]
	[DataRow(new byte[] { }, new byte[] { }, true, DisplayName = "Equal (Empty)")]
	[DataRow(new byte[] { 192, 168, 0, 1 }, new byte[] { 97, 98, 99, 100 }, false, DisplayName = "Not Equal")]
	public void Equals_BytesToBytes(byte[] valueA, byte[] valueB, bool expected)
	{
		var dnsRawValueA = new DnsRawValue(valueA.AsMemory());
		var dnsRawValueB = new DnsRawValue(valueB.AsMemory());
		Assert.AreEqual(expected, dnsRawValueA.Equals(dnsRawValueB));
	}
	[DataTestMethod]
	[DataRow(new byte[] { 97, 98, 99, 100 }, "abcd", true, DisplayName = "Equal")]
	[DataRow(new byte[] { }, "", true, DisplayName = "Equal (Empty)")]
	[DataRow(new byte[] { 97, 98, 99, 100 }, "ABCD", false, DisplayName = "Not Equal")]
	public void Equals_BytesToChars(byte[] valueA, string valueB, bool expected)
	{
		var dnsRawValueA = new DnsRawValue(valueA.AsMemory());
		var dnsRawValueB = new DnsRawValue(valueB.AsMemory());
		Assert.AreEqual(expected, dnsRawValueA.Equals(dnsRawValueB));
	}

	[TestMethod]
	public void ToBytes_FromBytes()
	{
		var input = new byte[] { 192, 168, 0, 1 };
		var expected = input;

		var dnsRawValue = new DnsRawValue(input);
		CollectionAssert.AreEqual(expected, dnsRawValue.ToBytes().ToArray());
	}
	[TestMethod]
	public void ToBytes_FromChars()
	{
		var input = "abcd";
		var expected = new byte[] { 97, 98, 99, 100 };

		var dnsRawValue = new DnsRawValue(input.AsMemory());
		CollectionAssert.AreEqual(expected, dnsRawValue.ToBytes().ToArray());
	}
	[TestMethod]
	public void ToBytes_Empty()
	{
		var input = Array.Empty<byte>();
		var expected = input;

		var dnsRawValue = new DnsRawValue(input);
		CollectionAssert.AreEqual(expected, dnsRawValue.ToBytes().ToArray());
	}

	[TestMethod]
	public void ToString_FromBytes()
	{
		var input = new byte[] { 97, 98, 99, 100 };
		var expected = "abcd";

		var dnsRawValue = new DnsRawValue(input);
		Assert.AreEqual(expected, dnsRawValue.ToString());
	}
	[TestMethod]
	public void ToString_FromChars()
	{
		var input = "abcd";
		var expected = input;

		var dnsRawValue = new DnsRawValue(input.AsMemory());
		Assert.AreEqual(expected, dnsRawValue.ToString());
	}
	[TestMethod]
	public void ToString_Empty()
	{
		var input = string.Empty;
		var expected = input;

		var dnsRawValue = new DnsRawValue(input.AsMemory());
		Assert.AreEqual(expected, dnsRawValue.ToString());
	}
}

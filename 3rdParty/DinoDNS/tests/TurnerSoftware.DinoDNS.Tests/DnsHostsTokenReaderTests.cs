using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurnerSoftware.DinoDNS.Tests;

[TestClass]
public class DnsHostsTokenReaderTests
{
	private static void CheckToken(ref DnsHostsTokenReader reader, string expected, HostsTokenType tokenType)
	{
		Assert.IsTrue(reader.NextToken(out var token), "No token");
		Assert.AreEqual(tokenType, token.TokenType, $"Token-mismatch for {expected}");
		Assert.AreEqual(expected, token.Value.ToString());
	}

	[DataTestMethod]
	[DataRow("# This is a comment", HostsTokenType.Comment, DisplayName = "Comment")]
	[DataRow("\r", HostsTokenType.NewLine, DisplayName = "Carriage Return")]
	[DataRow("\n", HostsTokenType.NewLine, DisplayName = "New Line")]
	[DataRow("\r\n", HostsTokenType.NewLine, DisplayName = "Carriage Return + New Line")]
	[DataRow(" ", HostsTokenType.Whitespace, DisplayName = "Space")]
	[DataRow("\t", HostsTokenType.Whitespace, DisplayName = "Tab")]
	[DataRow("127.0.0.1", HostsTokenType.Identifier, DisplayName = "Identifier (Address)")]
	[DataRow("example.org", HostsTokenType.Identifier, DisplayName = "Identifier (Host)")]
	public void ReadSingleToken(string hostsFile, HostsTokenType tokenType)
	{
		var reader = new DnsHostsTokenReader(hostsFile);
		CheckToken(ref reader, hostsFile, tokenType);
	}

	[TestMethod]
	public void ReadHostsFile()
	{
		var hostsFile = @"# This is an example hosts file
127.0.0.1 localhost
192.168.0.1	gateway router

1.1.1.1 cloudflare # This is cloudflare";

		var reader = new DnsHostsTokenReader(hostsFile);
		CheckToken(ref reader, "# This is an example hosts file", HostsTokenType.Comment);
		CheckToken(ref reader, Environment.NewLine, HostsTokenType.NewLine);
		CheckToken(ref reader, "127.0.0.1", HostsTokenType.Identifier);
		CheckToken(ref reader, " ", HostsTokenType.Whitespace);
		CheckToken(ref reader, "localhost", HostsTokenType.Identifier);
		CheckToken(ref reader, Environment.NewLine, HostsTokenType.NewLine);
		CheckToken(ref reader, "192.168.0.1", HostsTokenType.Identifier);
		CheckToken(ref reader, "\t", HostsTokenType.Whitespace);
		CheckToken(ref reader, "gateway", HostsTokenType.Identifier);
		CheckToken(ref reader, " ", HostsTokenType.Whitespace);
		CheckToken(ref reader, "router", HostsTokenType.Identifier);
		CheckToken(ref reader, Environment.NewLine, HostsTokenType.NewLine);
		CheckToken(ref reader, Environment.NewLine, HostsTokenType.NewLine);
		CheckToken(ref reader, "1.1.1.1", HostsTokenType.Identifier);
		CheckToken(ref reader, " ", HostsTokenType.Whitespace);
		CheckToken(ref reader, "cloudflare", HostsTokenType.Identifier);
		CheckToken(ref reader, " ", HostsTokenType.Whitespace);
		CheckToken(ref reader, "# This is cloudflare", HostsTokenType.Comment);
		Assert.IsFalse(reader.NextToken(out var token), $"Found token {token.Value}");
	}
}

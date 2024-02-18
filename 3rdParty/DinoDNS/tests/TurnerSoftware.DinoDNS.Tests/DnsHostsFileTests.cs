using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TurnerSoftware.DinoDNS.Tests;

[TestClass]
public class DnsHostsFileTests
{
	[DataTestMethod]
	[DataRow("localhost", "127.0.0.1", "localhost", DisplayName = "Simple")]
	[DataRow("LOCALHOST", "127.0.0.1", "localhost", DisplayName = "Uppercase host add")]
	[DataRow("localhost", "127.0.0.1", "LOCALHOST", DisplayName = "Uppercase host get")]
	public void TryGetAddressTest(string expectedHost, string expectedAddress, string testHost)
	{
		var hostsFile = new DnsHostsFile();
		hostsFile.Add(expectedHost, expectedAddress);
		Assert.IsTrue(hostsFile.TryGetAddress(testHost, out var actualAddress), $"No host found {testHost}");
		Assert.AreEqual(expectedAddress, new IPAddress(actualAddress.Span).ToString());
	}

	[TestMethod]
	public void FromStringTest()
	{
		var hostsFileString = @"# This is an example hosts file
127.0.0.1 localhost
192.168.0.1	gateway router.local

1.1.1.1 cloudflare # This is cloudflare";

		var hostsFile = DnsHostsFile.FromString(hostsFileString);
		Assert.IsTrue(hostsFile.TryGetAddress("localhost", out var address1), "No host found (localhost)");
		Assert.AreEqual("127.0.0.1", new IPAddress(address1.Span).ToString());
		Assert.IsTrue(hostsFile.TryGetAddress("router.local", out var address2), "No host found (router.local)");
		Assert.AreEqual("192.168.0.1", new IPAddress(address2.Span).ToString());
		Assert.IsTrue(hostsFile.TryGetAddress("cloudflare", out var address3), "No host found (cloudflare)");
		Assert.AreEqual("1.1.1.1", new IPAddress(address3.Span).ToString());
	}
}

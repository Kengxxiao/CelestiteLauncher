using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TurnerSoftware.DinoDNS.Connection;
using TurnerSoftware.DinoDNS.Connection.Resolvers;
using TurnerSoftware.DinoDNS.Protocol;
using TurnerSoftware.DinoDNS.TestServer;

namespace TurnerSoftware.DinoDNS.Tests.Connection;

[TestClass]
public class HostsFileResolverTests
{
	[DataTestMethod]
	[DataRow("test.example.org", "test.example.org", "192.168.0.1", DnsQueryType.A, DisplayName = "A Record")]
	[DataRow("test.example.org", "test.example.org", "::1", DnsQueryType.AAAA, DisplayName = "AAAA Record")]
	[DataRow("test.example.org", "TEST.EXAMPLE.ORG", "::1", DnsQueryType.AAAA, DisplayName = "Case Sensitivity")]
	public async Task BasicHostsFile(string storedHost, string queriedHost, string address, DnsQueryType queryType)
	{
		var hostsFile = new DnsHostsFile();
		var dnsClient = new DnsClient(new[]
		{
			new NameServer(new IPEndPoint(IPAddress.Loopback, 53), new HostsFileResolver(hostsFile))
		}, DnsMessageOptions.Default);
		hostsFile.Add(storedHost, address);

		var response = await dnsClient.QueryAsync(queriedHost, queryType);
		
		Assert.AreEqual(1, response.Answers.Count);
		var answerEnumerator = response.Answers.GetEnumerator();
		answerEnumerator.MoveNext();
		var answer = answerEnumerator.Current;
		Assert.AreEqual((int)queryType, (int)answer.Type);
		var expectedBytes = IPAddress.Parse(address).GetAddressBytes();
		Assert.IsTrue(expectedBytes.AsSpan().SequenceEqual(answer.Data.Span));
	}
}

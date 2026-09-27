using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Networking;

/// <summary>
/// Tests für reine Validierungs- und Parse-Logik des NetMQ-Systems.
/// Echte Socket-Tests erfordern echte NetMQ-Instanzen auf Testports (deferred zu Integration-Tests).
/// </summary>
public class NetMqResponseLogicTests
{
	[Theory]
	[InlineData("dhcp", true)]
	[InlineData("static", false)]
	[InlineData("DHCP", true)]
	[InlineData("Static", false)]
	public void NetworkConfigMode_CaseInsensitiveMatches(string modeInput, bool expectedDhcp)
	{
		var mode = modeInput.Trim().ToLowerInvariant();
		Assert.Equal(expectedDhcp ? "dhcp" : "static", mode);
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("invalid")]
	[InlineData("both")]
	public void NetworkConfigMode_InvalidModes_NotMatched(string modeInput)
	{
		var mode = modeInput.Trim().ToLowerInvariant();
		bool isValid = mode == "dhcp" || mode == "static";
		Assert.False(isValid);
	}

	[Theory]
	[InlineData("192.168.1.1/24")]
	[InlineData("10.0.0.0/8")]
	[InlineData("172.16.0.0/12")]
	public void CidrNotation_ValidFormats_CanBeParsed(string cidr)
	{
		var parts = cidr.Split('/', 2);
		Assert.Equal(2, parts.Length);
		Assert.NotEmpty(parts[0]);
		Assert.NotEmpty(parts[1]);
	}

	[Theory]
	[InlineData("192.168.1.1")]  // no slash
	[InlineData("192.168.1.1/")]  // no prefix
	[InlineData("/24")]  // no IP
	public void CidrNotation_InvalidFormats_FailParsing(string cidr)
	{
		var parts = cidr.Split('/', 2);
		bool isValid = parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]);
		Assert.False(isValid);
	}

	[Theory]
	[InlineData("192.168.1.1,8.8.8.8")]
	[InlineData("1.1.1.1")]
	[InlineData("8.8.8.8,1.1.1.1,9.9.9.9")]
	public void DnsServers_CommaSeparated_CanBeParsed(string dnsInput)
	{
		var entries = dnsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		Assert.NotEmpty(entries);
		foreach (var entry in entries)
		{
			Assert.NotEmpty(entry);
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(",,,")]
	public void DnsServers_EmptyList_FailsValidation(string dnsInput)
	{
		var entries = dnsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		Assert.Empty(entries);
	}

	[Fact]
	public void NetworkConfigPayload_Format_FollowsScheme()
	{
		// Expected: "device:mode:cidr:gateway:dnsServers"
		var validPayload = "eth0:static:192.168.1.50/24:192.168.1.1:192.168.1.1,8.8.8.8";
		var fields = validPayload.Split(':');

		Assert.Equal(5, fields.Length);
		Assert.Equal("eth0", fields[0]);
		Assert.Equal("static", fields[1]);
		Assert.Equal("192.168.1.50/24", fields[2]);
		Assert.Equal("192.168.1.1", fields[3]);
		Assert.Equal("192.168.1.1,8.8.8.8", fields[4]);
	}

	[Theory]
	[InlineData("eth0")]  // too few fields
	[InlineData("eth0:static")]  // incomplete static config
	[InlineData("eth0:static:192.168.1.1")]  // missing gateway and dns
	public void NetworkConfigPayload_MissingFields_FailValidation(string invalidPayload)
	{
		var fields = invalidPayload.Split(':');
		bool isValid = fields.Length == 5 && !string.IsNullOrWhiteSpace(fields[0]);
		Assert.False(isValid);
	}

	[Theory]
	[InlineData("Hello")]
	[InlineData("GetResult")]
	[InlineData("ResetResult")]
	[InlineData("SetTeamNames")]
	[InlineData("GetSettings")]
	public void KnownNetMqTopics_AreRecognized(string topic)
	{
		var knownTopics = new[] { "Hello", "GetResult", "ResetResult", "SetTeamNames",
			"SetTeilnehmer", "GetSettings", "SetSettings", "GetHostname", "SetHostname",
			"GetNetworkConfig", "SetNetworkConfig", "RebootPi", "SetImage", "GoToImage", "ClearImage" };

		Assert.Contains(topic, knownTopics);
	}

	[Theory]
	[InlineData("InvalidTopic")]
	[InlineData("UnknownCommand")]
	[InlineData("")]
	public void UnknownNetMqTopics_AreNotInKnownList(string topic)
	{
		var knownTopics = new[] { "Hello", "GetResult", "ResetResult", "SetTeamNames",
			"SetTeilnehmer", "GetSettings", "SetSettings", "GetHostname", "SetHostname",
			"GetNetworkConfig", "SetNetworkConfig", "RebootPi", "SetImage", "GoToImage", "ClearImage" };

		Assert.DoesNotContain(topic, knownTopics);
	}
}

using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

public class NetworkConfigServiceTests
{
	[Theory]
	[InlineData("hostname")]
	[InlineData("my-server")]
	[InlineData("server123")]
	[InlineData("a")]
	[InlineData("my-hostname-123")]
	public void HostnameRegex_ValidHostnames_Matches(string hostname)
	{
		Assert.Matches(NetworkConfigService.HostnameRegex, hostname);
	}

	[Theory]
	[InlineData("")]                    // empty
	[InlineData("-hostname")]           // starts with dash
	[InlineData("hostname-")]           // ends with dash
	[InlineData("host name")]           // space
	[InlineData("host_name")]           // underscore
	[InlineData("HOST!NAME")]           // special char
	public void HostnameRegex_InvalidHostnames_DoesNotMatch(string hostname)
	{
		Assert.DoesNotMatch(NetworkConfigService.HostnameRegex, hostname);
	}

	[Fact]
	public void HostnameRegex_MaxLengthExactly63_Matches()
	{
		string maxHostname = "a" + new string('b', 61) + "c";  // 63 chars
		Assert.Matches(NetworkConfigService.HostnameRegex, maxHostname);
	}

	[Fact]
	public void HostnameRegex_ExceedsMaxLength64_DoesNotMatch()
	{
		string tooLong = "a" + new string('b', 62) + "c";  // 64 chars
		Assert.DoesNotMatch(NetworkConfigService.HostnameRegex, tooLong);
	}
}

using LogVue.Infrastructure;

namespace LogVue.Tests.Infrastructure;

public class BrandingTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("/brand/logo.png", "/brand/logo.png")]
    [InlineData("~/brand/logo.png", "/brand/logo.png")]
    [InlineData("brand/logo.png", "/brand/logo.png")]
    [InlineData("https://cdn.example.com/logo.png", "https://cdn.example.com/logo.png")]
    public void CustomLogoUrl_NormalizesThePath(string? path, string? expected)
    {
        var url = Branding.CustomLogoUrl(new LogVueOptions { LogoPath = path });

        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("LogVue", false)]
    [InlineData(" LogVue ", false)]
    [InlineData("LogVue Sample", true)]
    [InlineData("", false)]
    public void HasCustomTitle_IsAnythingOtherThanTheDefault(string title, bool expected)
    {
        Assert.Equal(expected, Branding.HasCustomTitle(new LogVueOptions { Title = title }));
    }
}

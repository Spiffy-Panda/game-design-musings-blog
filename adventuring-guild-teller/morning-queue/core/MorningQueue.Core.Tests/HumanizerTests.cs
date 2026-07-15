using Xunit;

namespace MorningQueue.Core.Tests;

public class HumanizerTests
{
    [Theory]
    [InlineData("item_check", "Item Check")]
    [InlineData("cistern-wisp-swarm", "Cistern Wisp Swarm")]
    [InlineData("copper->bronze", "Copper → Bronze")]
    public void Humanize_TitleCasesSlugs(string raw, string expected)
        => Assert.Equal(expected, new Humanizer().Humanize(raw));

    [Fact]
    public void Humanize_OverridesFromLocaleWin()
    {
        var h = Humanizer.FromLocaleJson(TestData.LocaleEn);
        // en.json overrides map: hulbr-odd-eye -> "Hulbr Odd-Eye" (hyphen kept by override).
        Assert.Equal("Hulbr Odd-Eye", h.Humanize("hulbr-odd-eye"));
        // A slug with no override still title-cases.
        Assert.Equal("Odd Eyes", h.Humanize("odd-eyes"));
    }
}

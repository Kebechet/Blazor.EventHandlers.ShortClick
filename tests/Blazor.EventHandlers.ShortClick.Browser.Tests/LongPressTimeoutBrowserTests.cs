namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

/// <summary>
/// data-shortclick-long-press-ms sets how long a press may be held and still be a short click, for shortclick and click
/// mode alike: an accessibility setting such as Android's "Touch &amp; hold delay" gives slow taps more time.
/// </summary>
public class LongPressTimeoutBrowserTests(BrowserFixture browserFixture) : IClassFixture<BrowserFixture>
{
    /// <summary>Well inside the default 400ms limit.</summary>
    private static readonly TimeSpan _quickHold = TimeSpan.FromMilliseconds(250);

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Hold_OneSecondUnderALongerPageLimit_RaisesShortClickAndClicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), longPressMilliseconds: "1500");

        // Act
        await page.Hold(ClickModePage.ButtonId, ClickModePage.OneSecondHold);

        // Assert
        var shortClickCount = await page.GetShortClickCount(ClickModePage.ButtonId);
        shortClickCount.ShouldBe(1);
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Hold_OneSecondUnderALongerPageLimitWithClickModeOff_RaisesShortClick(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), clickModeValue: null, longPressMilliseconds: "1500");

        // Act
        await page.Hold(ClickModePage.ButtonId, ClickModePage.OneSecondHold);

        // Assert
        var shortClickCount = await page.GetShortClickCount(ClickModePage.ButtonId);
        shortClickCount.ShouldBe(1);
    }

    /// <summary>Without the attribute the limit stays at Android's 400ms: a quicker hold is a tap, a longer one is not.</summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Hold_WithoutALimitOnThePage_KeepsTheDefault400Milliseconds(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.Hold(ClickModePage.ButtonId, _quickHold);
        await page.Hold(ClickModePage.ButtonId, ClickModePage.LongPressDuration);

        // Assert
        var shortClickCount = await page.GetShortClickCount(ClickModePage.ButtonId);
        shortClickCount.ShouldBe(1);
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>The nearest element carrying the attribute decides, as it does for the mode.</summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium, ClickModePage.PatientButtonId, 1)]
    [InlineData(BrowserFixture.Chromium, ClickModePage.ButtonId, 0)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.PatientButtonId, 1)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.ButtonId, 0)]
    public async Task Hold_OneSecondWhereAContainerSetsALongerLimit_FollowsTheContainerOverThePage(string browserName, string elementId, int expectedCount)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), longPressMilliseconds: "400");

        // Act
        await page.Hold(elementId, ClickModePage.OneSecondHold);

        // Assert
        var shortClickCount = await page.GetShortClickCount(elementId);
        shortClickCount.ShouldBe(expectedCount);
        var clickCount = await page.GetClickCount(elementId);
        clickCount.ShouldBe(expectedCount);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium, "abc")]
    [InlineData(BrowserFixture.Chromium, "0")]
    [InlineData(BrowserFixture.Chromium, "-1000")]
    [InlineData(BrowserFixture.Chromium, "")]
    [InlineData(BrowserFixture.WebKit, "abc")]
    [InlineData(BrowserFixture.WebKit, "0")]
    [InlineData(BrowserFixture.WebKit, "-1000")]
    [InlineData(BrowserFixture.WebKit, "")]
    public async Task Hold_UnderAnInvalidPageLimit_FallsBackTo400Milliseconds(string browserName, string longPressMilliseconds)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), longPressMilliseconds: longPressMilliseconds);

        // Act
        await page.Hold(ClickModePage.ButtonId, _quickHold);
        await page.Hold(ClickModePage.ButtonId, ClickModePage.LongPressDuration);

        // Assert
        var shortClickCount = await page.GetShortClickCount(ClickModePage.ButtonId);
        shortClickCount.ShouldBe(1);
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }
}

namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

/// <summary>
/// Click mode lets a pointer's click reach the page only when its press was a short click, so a plain click handler
/// behaves like a shortclick one.
/// </summary>
public class ClickModeBrowserTests(BrowserFixture browserFixture) : IClassFixture<BrowserFixture>
{
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_WithClickModeOff_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), isClickModeOn: false);

        // Act
        await page.LongPress(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>
    /// Chromium only: headless WebKit reports screenX and screenY as 0 for every mouse event, and the drag is measured
    /// on those.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    public async Task Drag_WithClickModeOff_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), isClickModeOn: false);

        // Act
        await page.Drag(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_WithClickModeOn_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.Tap(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task TouchTap_WithClickModeOn_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), hasTouch: true);

        // Act
        await page.TouchTap(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_WithClickModeOn_StillRaisesShortClick(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.Tap(ClickModePage.ButtonId);

        // Assert
        var shortClickCount = await page.GetShortClickCount(ClickModePage.ButtonId);
        shortClickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_WithClickModeOn_DoesNotClick(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.LongPress(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(0);
    }

    /// <summary>
    /// Chromium only: headless WebKit reports screenX and screenY as 0 for every mouse event, and the drag is measured
    /// on those.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    public async Task Drag_WithClickModeOn_DoesNotClick(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.Drag(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(0);
    }

    /// <summary>The verdict of a long press must not stick to the presses after it.</summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_AfterALongPressWithClickModeOn_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));
        await page.LongPress(ClickModePage.ButtonId);

        // Act
        await page.Tap(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>A keyboard's click carries no press at all, so click mode never judges it.</summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium, "Enter")]
    [InlineData(BrowserFixture.Chromium, "Space")]
    [InlineData(BrowserFixture.WebKit, "Enter")]
    [InlineData(BrowserFixture.WebKit, "Space")]
    public async Task KeyPress_OnAFocusedButtonWithClickModeOn_Clicks(string browserName, string key)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.PressKey(ClickModePage.ButtonId, key);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task ClickFromScript_WithClickModeOn_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.ClickFromScript(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_OnALinkWithClickModeOn_Navigates(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.Tap(ClickModePage.LinkId);

        // Assert
        var locationHash = await page.GetLocationHash();
        locationHash.ShouldBe(ClickModePage.LinkTargetHash);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_OnALinkWithClickModeOn_DoesNotNavigate(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.LongPress(ClickModePage.LinkId);

        // Assert
        var locationHash = await page.GetLocationHash();
        locationHash.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_OnACheckboxWithClickModeOn_TogglesIt(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.Tap(ClickModePage.CheckboxId);

        // Assert
        var isChecked = await page.IsChecked(ClickModePage.CheckboxId);
        isChecked.ShouldBeTrue();
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_OnACheckboxWithClickModeOn_DoesNotToggleIt(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.LongPress(ClickModePage.CheckboxId);

        // Assert
        var isChecked = await page.IsChecked(ClickModePage.CheckboxId);
        isChecked.ShouldBeFalse();
    }

    /// <summary>
    /// A long press in a text field places the caret or selects text. Its click belongs to the editing, so click mode
    /// leaves it alone.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_InATextInputWithClickModeOn_KeepsItsClickAndTyping(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));
        await page.LongPress(ClickModePage.TextInputId);

        // Act
        await page.TypeText("abc");

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.TextInputId);
        clickCount.ShouldBe(1);
        var inputValue = await page.GetInputValue(ClickModePage.TextInputId);
        inputValue.ShouldBe("abc");
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_InASubtreeThatTurnsClickModeOff_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.LongPress(ClickModePage.TurnedOffButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.TurnedOffButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>
    /// The click is the last event of a tap, so a handler that runs on it can replace the button without a later click
    /// landing on the replacement, the ghost click a shortclick handler is exposed to.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task TouchTap_WhoseClickHandlerReplacesTheButton_DoesNotClickTheReplacement(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), hasTouch: true);

        // Act
        await page.TouchTap(ClickModePage.ReplacedOnClickButtonId);

        // Assert
        var replacedButtonClickCount = await page.GetClickCount(ClickModePage.ReplacedOnClickButtonId);
        replacedButtonClickCount.ShouldBe(1);
        var replacementClickCount = await page.GetClickCount(ClickModePage.ReplacementButtonId);
        replacementClickCount.ShouldBe(0);
    }
}

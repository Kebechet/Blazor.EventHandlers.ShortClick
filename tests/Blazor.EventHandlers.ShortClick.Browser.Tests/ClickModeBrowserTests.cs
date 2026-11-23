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
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), clickModeValue: null);

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
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), clickModeValue: null);

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
    /// HTML matches enumerated attribute values without regard to case, and so does click mode.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_WithClickModeValueInUpperCase_DoesNotClick(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), clickModeValue: "CLICK");

        // Act
        await page.LongPress(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(0);
    }

    /// <summary>
    /// Chromium only: Playwright's touchscreen can only tap, so the long press goes through the DevTools protocol.
    /// </summary>
    [Theory]
    [InlineData(ClickModePage.ClickModeValue, 0)]
    [InlineData(null, 1)]
    public async Task TouchLongPress_InChromium_ClicksOnlyWithClickModeOff(string? clickModeValue, int expectedClickCount)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(BrowserFixture.Chromium), clickModeValue, hasTouch: true);

        // Act
        await page.TouchLongPress(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(expectedClickCount);
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

    /// <summary>
    /// A component's own script may stop its pointerdown from propagating, so the press is never measured. Its click must
    /// then keep the browser's behaviour, not be judged by the long press just before it.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_RightAfterALongPress_OnAnElementThatStopsItsPointerDown_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));
        await page.LongPress(ClickModePage.ButtonId);

        // Act
        await page.Tap(ClickModePage.PointerDownStoppingButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.PointerDownStoppingButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>
    /// A keyboard's click carries no press, so click mode never judges it - not even right after a long press, whose
    /// verdict would otherwise still apply.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium, "Enter")]
    [InlineData(BrowserFixture.Chromium, "Space")]
    [InlineData(BrowserFixture.WebKit, "Enter")]
    [InlineData(BrowserFixture.WebKit, "Space")]
    public async Task KeyPress_RightAfterALongPressWithClickModeOn_Clicks(string browserName, string key)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));
        await page.LongPress(ClickModePage.ButtonId);

        // Act
        await page.PressKey(ClickModePage.ButtonId, key);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task ClickFromScript_RightAfterALongPressWithClickModeOn_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));
        await page.LongPress(ClickModePage.ButtonId);

        // Act
        await page.ClickFromScript(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>A script's click is not a press even when its detail says it was, so it passes as untrusted.</summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task DispatchedClick_RightAfterALongPressWithClickModeOn_Clicks(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));
        await page.LongPress(ClickModePage.ButtonId);

        // Act
        await page.DispatchClick(ClickModePage.ButtonId);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(1);
    }

    /// <summary>
    /// A real click without a pointerdown of its own (an assistive technology's activation) is judged by the long press
    /// that ended just before it, so it is dropped. Chromium only, see the page's chorded press.
    /// </summary>
    [Fact]
    public async Task ClickWithoutAPointerDown_RightAfterALongPress_DoesNotClick()
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(BrowserFixture.Chromium));

        // Act
        await page.ClickWithoutAPointerDownAfterALongPress(ClickModePage.ButtonId, TimeSpan.Zero);

        // Assert
        var clickCount = await page.GetClickCount(ClickModePage.ButtonId);
        clickCount.ShouldBe(0);
    }

    /// <summary>
    /// Once 350ms have passed since a long press ended, a click without a pointerdown of its own is no longer part of
    /// it and keeps the browser's behaviour. Chromium only, see the page's chorded press.
    /// </summary>
    [Fact]
    public async Task ClickWithoutAPointerDown_WellAfterALongPress_Clicks()
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(BrowserFixture.Chromium));

        // Act
        await page.ClickWithoutAPointerDownAfterALongPress(ClickModePage.ButtonId, ClickModePage.PastThePressWindow);

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
    /// A long press in a text-entry control places the caret, selects text or opens the list. Its click belongs to that
    /// editing, so click mode leaves it alone.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium, ClickModePage.TextInputId)]
    [InlineData(BrowserFixture.Chromium, ClickModePage.TextareaId)]
    [InlineData(BrowserFixture.Chromium, ClickModePage.SelectId)]
    [InlineData(BrowserFixture.Chromium, ClickModePage.EditableId)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.TextInputId)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.TextareaId)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.SelectId)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.EditableId)]
    public async Task LongPress_InATextEntryControlWithClickModeOn_KeepsItsClick(string browserName, string elementId)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.LongPress(elementId);

        // Assert
        var clickCount = await page.GetClickCount(elementId);
        clickCount.ShouldBe(1);
    }

    /// <summary>
    /// At the window a click inside a shadow root targets the shadow host, so the exemption has to look at where the
    /// click really landed: the text input inside.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_InATextInputInsideAShadowRootWithClickModeOn_KeepsItsClick(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.LongPress(ClickModePage.ShadowFieldId);

        // Assert
        var shadowInputClickCount = await page.GetShadowInputClickCount();
        shadowInputClickCount.ShouldBe(1);
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

    /// <summary>The mode can be turned on for one container instead of the whole page.</summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium, ClickModePage.ScopedButtonId, 0)]
    [InlineData(BrowserFixture.Chromium, ClickModePage.ButtonId, 1)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.ScopedButtonId, 0)]
    [InlineData(BrowserFixture.WebKit, ClickModePage.ButtonId, 1)]
    public async Task LongPress_WithClickModeOnOneContainerOnly_DropsOnlyTheClicksInsideIt(string browserName, string elementId, int expectedClickCount)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), clickModeValue: null);

        // Act
        await page.LongPress(elementId);

        // Assert
        var clickCount = await page.GetClickCount(elementId);
        clickCount.ShouldBe(expectedClickCount);
    }

    /// <summary>
    /// A shortclick handler still re-renders before the tap's compatibility click, and with click mode on the ghost-click
    /// guard must still keep that click off the element that replaced the pressed one.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task TouchTap_WhoseShortClickHandlerReplacesTheButton_DoesNotClickTheReplacement(string browserName)
    {
        // Arrange
        await using var page = await ClickModePage.Open(browserFixture.GetBrowser(browserName), hasTouch: true);

        // Act
        await page.TouchTap(ClickModePage.ReplacedOnShortClickButtonId);

        // Assert
        var isReplacementOnPage = await page.IsOnPage(ClickModePage.ReplacementButtonId);
        isReplacementOnPage.ShouldBeTrue();
        var replacementClickCount = await page.GetClickCount(ClickModePage.ReplacementButtonId);
        replacementClickCount.ShouldBe(0);
    }
}

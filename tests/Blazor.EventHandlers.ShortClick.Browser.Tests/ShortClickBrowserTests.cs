namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

public class ShortClickBrowserTests(BrowserFixture browserFixture) : IClassFixture<BrowserFixture>
{
    /// <summary>Well past the 400ms long-press limit, as a render or a GC pause in a WebAssembly app can be.</summary>
    private static readonly TimeSpan _busyMainThreadDuration = TimeSpan.FromMilliseconds(700);

    /// <summary>Lets the busy loop start before the finger lifts, so the pointerup waits behind it.</summary>
    private static readonly TimeSpan _busyLoopStartDelay = TimeSpan.FromMilliseconds(50);

    private const int FirstPointerId = 1;
    private const int SecondPointerId = 2;

    /// <summary>Where the second finger rests: on the button, but far more than 8 dips from the first one.</summary>
    private const int SecondPointerOffsetPixels = 80;

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_OnTheButton_RaisesShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));

        // Act
        await page.PressButton();
        await page.Release();

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(1);
    }

    /// <summary>
    /// The press used to be timed by <c>new Date()</c> inside each handler, so a quick tap whose pointerup waited behind
    /// a busy main thread measured as a long press and raised nothing. The events' own timestamps say when the finger
    /// went down and up, however late the handlers run.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task Tap_ReleasedWhileTheMainThreadIsBusy_RaisesShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.PressButton();

        // Act
        await page.BlockMainThread(_busyMainThreadDuration);
        await Task.Delay(_busyLoopStartDelay);
        await page.Release();

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task LongPress_OnTheButton_RaisesNoShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.PressButton();

        // Act
        await Task.Delay(ShortClickPage.LongPressDuration);
        await page.Release();

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(0);
    }

    /// <summary>
    /// Chromium only: headless WebKit reports screenX and screenY as 0 for every mouse event, and the drag is measured
    /// on those.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    public async Task Drag_AcrossTheButton_RaisesNoShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.PressButton();

        // Act
        await page.MoveBy(ShortClickPage.DragDistancePixels);
        await page.Release();

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(0);
    }

    /// <summary>
    /// One press used to be shared by every pointer, so a second finger going down overwrote where the first one
    /// started, and the first finger's tap measured as a drag from the second finger's spot.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task TapWithOneFinger_WhileAnotherFingerRestsElsewhere_RaisesShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.DispatchPointerEvent("pointerdown", FirstPointerId);
        await page.DispatchPointerEvent("pointerdown", SecondPointerId, SecondPointerOffsetPixels);

        // Act
        await page.DispatchPointerEvent("pointerup", FirstPointerId);

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(1);
    }

    /// <summary>
    /// A system gesture that starts over the page (a home swipe from Android's gesture bar) reaches it as pointerdown,
    /// then pointercancel once the system claims the gesture - never a pointerup.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task PressThenCancel_OnTheButton_RaisesNoShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.DispatchPointerEvent("pointerdown", FirstPointerId);

        // Act
        await page.DispatchPointerEvent("pointercancel", FirstPointerId);

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(0);
    }
}

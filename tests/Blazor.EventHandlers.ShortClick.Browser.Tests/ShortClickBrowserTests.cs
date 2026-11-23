namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

public class ShortClickBrowserTests(BrowserFixture browserFixture) : IClassFixture<BrowserFixture>
{
    /// <summary>Well past the 400ms long-press limit, as a render or a GC pause in a WebAssembly app can be.</summary>
    private static readonly TimeSpan _busyMainThreadDuration = TimeSpan.FromMilliseconds(700);

    /// <summary>Lets the busy loop start before the finger lifts, so the pointerup waits behind it.</summary>
    private static readonly TimeSpan _busyLoopStartDelay = TimeSpan.FromMilliseconds(50);

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
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.FirstFinger);
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.SecondFinger);

        // Act
        await page.DispatchPointerEvent("pointerup", SyntheticPointer.FirstFinger);

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(1);
    }

    /// <summary>
    /// Fingers that tap together make one tap. Once every pointer kept its own press, each finger's pointerup passed
    /// the checks and raised a shortclick of its own.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task TapWithTwoFingersTogether_OnTheButton_RaisesOneShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.FirstFinger);
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.SecondFinger);

        // Act
        await page.DispatchPointerEvent("pointerup", SyntheticPointer.FirstFinger);
        await page.DispatchPointerEvent("pointerup", SyntheticPointer.SecondFinger);

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(1);
    }

    /// <summary>
    /// A mouse or a pen is always the primary pointer, so its pointerdown starts a new gesture of its own kind. It must
    /// not end a finger's press that is still in progress.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task TapWithAFinger_WhileTheMouseIsPressedElsewhere_RaisesShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.FirstFinger);
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.Mouse);

        // Act
        await page.DispatchPointerEvent("pointerup", SyntheticPointer.FirstFinger);

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(1);
    }

    /// <summary>
    /// A cancelled press is forgotten. Browsers send no pointerup after a pointercancel, so this stages the case a
    /// remembered press would get wrong: a pointerup that reuses the cancelled pointer's id without a new pointerdown.
    /// </summary>
    [Theory]
    [InlineData(BrowserFixture.Chromium)]
    [InlineData(BrowserFixture.WebKit)]
    public async Task PointerUp_AfterItsPressWasCancelled_RaisesNoShortClick(string browserName)
    {
        // Arrange
        await using var page = await ShortClickPage.Open(browserFixture.GetBrowser(browserName));
        await page.DispatchPointerEvent("pointerdown", SyntheticPointer.FirstFinger);
        await page.DispatchPointerEvent("pointercancel", SyntheticPointer.FirstFinger);

        // Act
        await page.DispatchPointerEvent("pointerup", SyntheticPointer.FirstFinger);

        // Assert
        var shortClickCount = await page.GetShortClickCount();
        shortClickCount.ShouldBe(0);
    }
}

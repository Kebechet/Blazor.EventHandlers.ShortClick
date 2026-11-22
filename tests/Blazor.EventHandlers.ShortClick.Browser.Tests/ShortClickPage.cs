using Microsoft.Playwright;

namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

/// <summary>
/// A page with one button and the shipping ShortClick.js, which counts the shortclick events the button receives.
/// </summary>
internal sealed class ShortClickPage : IAsyncDisposable
{
    private const string Origin = "http://shortclick.test";
    private const string ScriptFileName = "ShortClick.js";
    private const string ButtonSelector = "#target";

    /// <summary>Far enough from the button's centre to count as a drag, not a tap.</summary>
    public const int DragDistancePixels = 40;

    /// <summary>Longer than the 400ms past which a press is a long press, not a tap.</summary>
    public static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(600);

    private static readonly string _pageHtml = $"""
        <!doctype html>
        <html>
        <head><meta charset="utf-8"></head>
        <body style="margin:0">
        <button id="target" style="position:absolute;left:100px;top:100px;width:200px;height:100px">Tap me</button>
        <script>
            window.shortClickCount = 0;
            document.getElementById('target').addEventListener('shortclick', () => window.shortClickCount++);
        </script>
        <script src="/{ScriptFileName}"></script>
        </body>
        </html>
        """;

    private static string _scriptPath => Path.Combine(AppContext.BaseDirectory, "BrowserAssets", ScriptFileName);

    private readonly IBrowserContext _browserContext;
    private readonly IPage _page;

    private ShortClickPage(IBrowserContext browserContext, IPage page)
    {
        _browserContext = browserContext;
        _page = page;
    }

    public static async Task<ShortClickPage> Open(IBrowser browser)
    {
        var browserContext = await browser.NewContextAsync();
        var page = await browserContext.NewPageAsync();
        await page.RouteAsync($"{Origin}/**", async route =>
        {
            var isScriptRequest = route.Request.Url.EndsWith(ScriptFileName);
            await route.FulfillAsync(new RouteFulfillOptions
            {
                ContentType = isScriptRequest
                    ? "text/javascript"
                    : "text/html",
                Body = isScriptRequest
                    ? await File.ReadAllTextAsync(_scriptPath)
                    : _pageHtml,
            });
        });
        await page.GotoAsync($"{Origin}/");

        return new ShortClickPage(browserContext, page);
    }

    public async Task<int> GetShortClickCount()
    {
        return await _page.EvaluateAsync<int>("() => window.shortClickCount");
    }

    /// <summary>
    /// The button's centre in CSS pixels. Doubles, not the bounding box's floats: Playwright serializes a float
    /// argument as an object, which a script then reads as NaN.
    /// </summary>
    private async Task<(double X, double Y)> GetButtonCentre()
    {
        var box = await _page.Locator(ButtonSelector).BoundingBoxAsync();
        box.ShouldNotBeNull();

        return ((double)box.X + (box.Width / 2), (double)box.Y + (box.Height / 2));
    }

    /// <summary>Presses the mouse on the button's centre through the browser's real input pipeline.</summary>
    public async Task PressButton()
    {
        var (x, y) = await GetButtonCentre();
        await _page.Mouse.MoveAsync((float)x, (float)y);
        await _page.Mouse.DownAsync();
    }

    public async Task MoveBy(int distancePixels)
    {
        var (x, y) = await GetButtonCentre();
        await _page.Mouse.MoveAsync((float)(x + distancePixels), (float)y);
    }

    public async Task Release()
    {
        await _page.Mouse.UpAsync();
    }

    /// <summary>
    /// Keeps the page's main thread busy for <paramref name="duration"/>, starting right after this returns, the way a
    /// render or a garbage collection in a WebAssembly app does. Input that arrives meanwhile waits in the queue.
    /// </summary>
    public async Task BlockMainThread(TimeSpan duration)
    {
        await _page.EvaluateAsync(
            "durationMilliseconds => setTimeout(() => { const end = performance.now() + durationMilliseconds; while (performance.now() < end) { } }, 0)",
            duration.TotalMilliseconds);
    }

    /// <summary>Dispatches a synthetic pointer event from <paramref name="pointer"/> on the button.</summary>
    public async Task DispatchPointerEvent(string eventType, SyntheticPointer pointer)
    {
        var (x, y) = await GetButtonCentre();
        await _page.EvaluateAsync(
            """
            ([eventType, pointerId, pointerType, isPrimary, x, y]) => {
                const target = document.getElementById('target');
                target.dispatchEvent(new PointerEvent(eventType, {
                    bubbles: true, cancelable: true, composed: true, pointerId, pointerType, isPrimary,
                    clientX: x, clientY: y, screenX: x, screenY: y, button: 0, buttons: eventType === 'pointerdown' ? 1 : 0 }));
            }
            """,
            new object[] { eventType, pointer.PointerId, pointer.PointerType, pointer.IsPrimary, x + pointer.OffsetPixels, y });
    }

    public async ValueTask DisposeAsync()
    {
        await _browserContext.CloseAsync();
    }
}

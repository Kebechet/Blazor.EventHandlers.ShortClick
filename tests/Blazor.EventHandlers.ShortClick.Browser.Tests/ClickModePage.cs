using Microsoft.Playwright;

namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

/// <summary>
/// A page with the shipping ShortClick.js and one element of every kind click mode treats differently. Like Blazor, a
/// single listener on the document counts the clicks and shortclicks each element receives.
/// </summary>
internal sealed class ClickModePage : IAsyncDisposable
{
    private const string Origin = "http://clickmode.test";
    private const string ScriptFileName = "ShortClick.js";

    public const string ButtonId = "button";
    public const string LinkId = "link";
    public const string CheckboxId = "checkbox";
    public const string TextInputId = "text-input";
    public const string TurnedOffButtonId = "turned-off-button";
    public const string ReplacedOnClickButtonId = "replaced-on-click";
    public const string ReplacementButtonId = "replacement";

    /// <summary>Where the link points: the page navigates there in place, so the test can read it from the hash.</summary>
    public const string LinkTargetHash = "#navigated";

    /// <summary>Far enough from the press to count as a drag, not a tap.</summary>
    public const int DragDistancePixels = 40;

    /// <summary>Longer than the 400ms past which a press is a long press, not a tap.</summary>
    public static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(600);

    private static string _scriptPath => Path.Combine(AppContext.BaseDirectory, "BrowserAssets", ScriptFileName);

    private readonly IBrowserContext _browserContext;
    private readonly IPage _page;

    private ClickModePage(IBrowserContext browserContext, IPage page)
    {
        _browserContext = browserContext;
        _page = page;
    }

    /// <param name="browser">The browser to open the page in.</param>
    /// <param name="isClickModeOn">Whether the page's &lt;html&gt; turns click mode on.</param>
    /// <param name="hasTouch">Whether the page gets a touchscreen, for <see cref="TouchTap"/>.</param>
    public static async Task<ClickModePage> Open(IBrowser browser, bool isClickModeOn = true, bool hasTouch = false)
    {
        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions { HasTouch = hasTouch });
        var page = await browserContext.NewPageAsync();
        var pageHtml = GetPageHtml(isClickModeOn);
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
                    : pageHtml,
            });
        });
        await page.GotoAsync($"{Origin}/");

        return new ClickModePage(browserContext, page);
    }

    private static string GetPageHtml(bool isClickModeOn)
    {
        var clickModeAttribute = isClickModeOn
            ? """data-shortclick-mode="click" """
            : string.Empty;

        return $$"""
            <!doctype html>
            <html {{clickModeAttribute}}>
            <head><meta charset="utf-8"></head>
            <body style="margin:0">
            <button id="{{ButtonId}}" style="position:absolute;left:20px;top:20px;width:200px;height:60px">Button</button>
            <a id="{{LinkId}}" href="{{LinkTargetHash}}" style="position:absolute;left:20px;top:100px;width:200px;height:40px;display:block">Link</a>
            <input id="{{CheckboxId}}" type="checkbox" style="position:absolute;left:20px;top:160px;width:40px;height:40px;margin:0">
            <input id="{{TextInputId}}" type="text" style="position:absolute;left:20px;top:220px;width:200px;height:40px">
            <div data-shortclick-mode="off">
                <button id="{{TurnedOffButtonId}}" style="position:absolute;left:20px;top:280px;width:200px;height:60px">Turned off</button>
            </div>
            <button id="{{ReplacedOnClickButtonId}}" style="position:absolute;left:20px;top:360px;width:200px;height:60px">Replaced on click</button>
            <script>
                window.clickCountsById = {};
                window.shortClickCountsById = {};
                document.addEventListener('click', e => {
                    clickCountsById[e.target.id] = (clickCountsById[e.target.id] ?? 0) + 1;

                    if (e.target.id === '{{ReplacedOnClickButtonId}}') {
                        const replacement = document.createElement('button');
                        replacement.id = '{{ReplacementButtonId}}';
                        replacement.style.cssText = e.target.style.cssText;
                        replacement.textContent = 'Replacement';
                        e.target.replaceWith(replacement);
                    }
                });
                document.addEventListener('shortclick', e => {
                    shortClickCountsById[e.target.id] = (shortClickCountsById[e.target.id] ?? 0) + 1;
                });
            </script>
            <script src="/{{ScriptFileName}}"></script>
            </body>
            </html>
            """;
    }

    public async Task<int> GetClickCount(string elementId)
    {
        return await _page.EvaluateAsync<int>("elementId => window.clickCountsById[elementId] ?? 0", elementId);
    }

    public async Task<int> GetShortClickCount(string elementId)
    {
        return await _page.EvaluateAsync<int>("elementId => window.shortClickCountsById[elementId] ?? 0", elementId);
    }

    public async Task<string> GetLocationHash()
    {
        return await _page.EvaluateAsync<string>("() => location.hash");
    }

    public async Task<bool> IsChecked(string elementId)
    {
        return await _page.Locator($"#{elementId}").IsCheckedAsync();
    }

    public async Task<string> GetInputValue(string elementId)
    {
        return await _page.Locator($"#{elementId}").InputValueAsync();
    }

    private async Task<(float X, float Y)> GetCentre(string elementId)
    {
        var box = await _page.Locator($"#{elementId}").BoundingBoxAsync();
        box.ShouldNotBeNull();

        return (box.X + (box.Width / 2), box.Y + (box.Height / 2));
    }

    /// <summary>A quick mouse click on the element's centre, through the browser's real input pipeline.</summary>
    public async Task Tap(string elementId)
    {
        await PressAndRelease(elementId, TimeSpan.Zero, movePixels: 0);
    }

    public async Task LongPress(string elementId)
    {
        await PressAndRelease(elementId, LongPressDuration, movePixels: 0);
    }

    public async Task Drag(string elementId)
    {
        await PressAndRelease(elementId, TimeSpan.Zero, DragDistancePixels);
    }

    private async Task PressAndRelease(string elementId, TimeSpan holdDuration, int movePixels)
    {
        var (x, y) = await GetCentre(elementId);
        await _page.Mouse.MoveAsync(x, y);
        await _page.Mouse.DownAsync();
        if (holdDuration > TimeSpan.Zero)
        {
            await Task.Delay(holdDuration);
        }

        if (movePixels != 0)
        {
            await _page.Mouse.MoveAsync(x + movePixels, y);
        }

        await _page.Mouse.UpAsync();
    }

    /// <summary>A finger tap on the element's centre. Needs a page opened with a touchscreen.</summary>
    public async Task TouchTap(string elementId)
    {
        var (x, y) = await GetCentre(elementId);
        await _page.Touchscreen.TapAsync(x, y);
    }

    /// <summary>Focuses the element and presses <paramref name="key"/> on it, the way a keyboard user activates it.</summary>
    public async Task PressKey(string elementId, string key)
    {
        await _page.Locator($"#{elementId}").FocusAsync();
        await _page.Keyboard.PressAsync(key);
    }

    /// <summary>Calls the element's click(), as a script does.</summary>
    public async Task ClickFromScript(string elementId)
    {
        await _page.EvaluateAsync("elementId => document.getElementById(elementId).click()", elementId);
    }

    public async Task TypeText(string text)
    {
        await _page.Keyboard.TypeAsync(text);
    }

    public async ValueTask DisposeAsync()
    {
        await _browserContext.CloseAsync();
    }
}

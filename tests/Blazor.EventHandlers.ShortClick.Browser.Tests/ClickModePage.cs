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

    public const string ClickModeValue = "click";

    public const string ButtonId = "button";
    public const string LinkId = "link";
    public const string CheckboxId = "checkbox";
    public const string TextInputId = "text-input";
    public const string TextareaId = "textarea";
    public const string SelectId = "select";
    public const string EditableId = "editable";
    public const string TurnedOffButtonId = "turned-off-button";
    public const string ScopedButtonId = "scoped-button";
    public const string PointerDownStoppingButtonId = "pointerdown-stopping-button";
    public const string ShadowFieldId = "shadow-field";
    public const string ReplacedOnShortClickButtonId = "replaced-on-shortclick";
    public const string ReplacementButtonId = "replacement";

    /// <summary>Where the link points: the page navigates there in place, so the test can read it from the hash.</summary>
    public const string LinkTargetHash = "#navigated";

    /// <summary>Far enough from the press to count as a drag, not a tap.</summary>
    public const int DragDistancePixels = 40;

    /// <summary>Longer than the 400ms past which a press is a long press, not a tap.</summary>
    public static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(600);

    /// <summary>Well past the 350ms after its pointerup in which a click still counts as part of a press.</summary>
    public static readonly TimeSpan PastThePressWindow = TimeSpan.FromMilliseconds(700);

    private static string _scriptPath => Path.Combine(AppContext.BaseDirectory, "BrowserAssets", ScriptFileName);

    private readonly IBrowserContext _browserContext;
    private readonly IPage _page;

    private ClickModePage(IBrowserContext browserContext, IPage page)
    {
        _browserContext = browserContext;
        _page = page;
    }

    /// <param name="browser">The browser to open the page in.</param>
    /// <param name="clickModeValue">The <c>data-shortclick-mode</c> value on &lt;html&gt;, or null for none.</param>
    /// <param name="hasTouch">Whether the page gets a touchscreen, for <see cref="TouchTap"/>.</param>
    /// <param name="isScriptLoaded">False leaves ShortClick.js out, to prove a test needs it.</param>
    public static async Task<ClickModePage> Open(
        IBrowser browser,
        string? clickModeValue = ClickModeValue,
        bool hasTouch = false,
        bool isScriptLoaded = true)
    {
        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions { HasTouch = hasTouch });
        var page = await browserContext.NewPageAsync();
        var pageHtml = GetPageHtml(clickModeValue, isScriptLoaded);
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

    private static string GetPageHtml(string? clickModeValue, bool isScriptLoaded)
    {
        var clickModeAttribute = clickModeValue is null
            ? string.Empty
            : $"""data-shortclick-mode="{clickModeValue}" """;
        var scriptTag = isScriptLoaded
            ? $"""<script src="/{ScriptFileName}"></script>"""
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
            <button id="{{ReplacedOnShortClickButtonId}}" style="position:absolute;left:20px;top:360px;width:200px;height:60px">Replaced on shortclick</button>
            <button id="{{PointerDownStoppingButtonId}}" style="position:absolute;left:300px;top:20px;width:200px;height:60px">Stops pointerdown</button>
            <textarea id="{{TextareaId}}" style="position:absolute;left:300px;top:100px;width:200px;height:60px"></textarea>
            <select id="{{SelectId}}" style="position:absolute;left:300px;top:180px;width:200px;height:40px"><option>One</option><option>Two</option></select>
            <div id="{{EditableId}}" contenteditable="true" style="position:absolute;left:300px;top:240px;width:200px;height:40px;border:1px solid">Editable</div>
            <shadow-field id="{{ShadowFieldId}}" style="position:absolute;left:300px;top:300px;width:200px;height:40px;display:block"></shadow-field>
            <div data-shortclick-mode="click">
                <button id="{{ScopedButtonId}}" style="position:absolute;left:300px;top:360px;width:200px;height:60px">Click mode here only</button>
            </div>
            <script>
                window.clickCountsById = {};
                window.shortClickCountsById = {};
                window.shadowInputClickCount = 0;
                document.addEventListener('click', e => {
                    clickCountsById[e.target.id] = (clickCountsById[e.target.id] ?? 0) + 1;
                });
                document.addEventListener('shortclick', e => {
                    shortClickCountsById[e.target.id] = (shortClickCountsById[e.target.id] ?? 0) + 1;

                    // Re-renders on pointerup, before the tap's compatibility click, as a @onshortclick handler does.
                    if (e.target.id === '{{ReplacedOnShortClickButtonId}}') {
                        const replacement = document.createElement('button');
                        replacement.id = '{{ReplacementButtonId}}';
                        replacement.style.cssText = e.target.style.cssText;
                        replacement.textContent = 'Replacement';
                        e.target.replaceWith(replacement);
                    }
                });

                // A component's own script that keeps its presses to itself, like a menu's outside-click guard.
                document.getElementById('{{PointerDownStoppingButtonId}}').addEventListener('pointerdown', e => e.stopPropagation());

                customElements.define('shadow-field', class extends HTMLElement {
                    constructor() {
                        super();
                        this.attachShadow({ mode: 'open' }).innerHTML = '<input type="text" style="width:190px;height:30px">';
                        this.shadowRoot.querySelector('input').addEventListener('click', () => window.shadowInputClickCount++);
                    }
                });
            </script>
            {{scriptTag}}
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

    /// <summary>The clicks the text input inside <see cref="ShadowFieldId"/>'s shadow root received, on its own listener.</summary>
    public async Task<int> GetShadowInputClickCount()
    {
        return await _page.EvaluateAsync<int>("() => window.shadowInputClickCount");
    }

    public async Task<bool> IsOnPage(string elementId)
    {
        return await _page.Locator($"#{elementId}").CountAsync() == 1;
    }

    public async Task<string> GetLocationHash()
    {
        return await _page.EvaluateAsync<string>("() => location.hash");
    }

    public async Task<bool> IsChecked(string elementId)
    {
        return await _page.Locator($"#{elementId}").IsCheckedAsync();
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

    /// <summary>
    /// A finger held on the element's centre for <see cref="LongPressDuration"/>. Chromium only: Playwright's touchscreen
    /// can only tap, so the touch points go through the DevTools protocol.
    /// </summary>
    public async Task TouchLongPress(string elementId)
    {
        var (x, y) = await GetCentre(elementId);
        var cdpSession = await _browserContext.NewCDPSessionAsync(_page);
        var touchPoint = new Dictionary<string, object> { ["x"] = x, ["y"] = y, ["id"] = 0 };

        await cdpSession.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchStart",
            ["touchPoints"] = new[] { touchPoint },
        });
        await Task.Delay(LongPressDuration);
        await cdpSession.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchEnd",
            ["touchPoints"] = Array.Empty<object>(),
        });
    }

    /// <summary>Focuses the element and presses <paramref name="key"/> on it, the way a keyboard user activates it.</summary>
    public async Task PressKey(string elementId, string key)
    {
        await _page.Locator($"#{elementId}").FocusAsync();
        await _page.Keyboard.PressAsync(key);
    }

    /// <summary>Calls the element's click(), as a script does: a click with a detail of 0.</summary>
    public async Task ClickFromScript(string elementId)
    {
        await _page.EvaluateAsync("elementId => document.getElementById(elementId).click()", elementId);
    }

    /// <summary>Dispatches a click with a detail of 1 from a script: untrusted, but shaped like a pointer's click.</summary>
    public async Task DispatchClick(string elementId)
    {
        await _page.EvaluateAsync(
            "elementId => document.getElementById(elementId).dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, detail: 1 }))",
            elementId);
    }

    /// <summary>
    /// Ends a long press at <paramref name="elementId"/>, then makes a real click there that has no pointerdown of its own,
    /// <paramref name="delayBeforeTheClick"/> later. Such a click is rare (an assistive technology's activation), and the
    /// only way to stage one is a chorded mouse press: while another button is down, pressing the primary one raises
    /// pointermove instead of pointerdown, yet its release still clicks. Chromium only.
    /// </summary>
    /// <remarks>
    /// The secondary button goes down first, as its own pointerdown would otherwise clear the long press. The long press
    /// itself is made of synthetic pointer events: ShortClick.js measures them like real ones.
    /// </remarks>
    public async Task ClickWithoutAPointerDownAfterALongPress(string elementId, TimeSpan delayBeforeTheClick)
    {
        var (x, y) = await GetCentre(elementId);
        await _page.Mouse.MoveAsync(x, y);
        await _page.Mouse.DownAsync(new MouseDownOptions { Button = MouseButton.Right });

        await DispatchPointerEvent(elementId, "pointerdown", x, y);
        await Task.Delay(LongPressDuration);
        await DispatchPointerEvent(elementId, "pointerup", x, y);

        await Task.Delay(delayBeforeTheClick);
        await _page.Mouse.DownAsync();
        await _page.Mouse.UpAsync();
        await _page.Mouse.UpAsync(new MouseUpOptions { Button = MouseButton.Right });
    }

    /// <summary>Coordinates as doubles: Playwright serializes a float argument as an object, which a script reads as NaN.</summary>
    private async Task DispatchPointerEvent(string elementId, string eventType, double x, double y)
    {
        await _page.EvaluateAsync(
            """
            ([elementId, eventType, x, y]) => document.getElementById(elementId).dispatchEvent(new PointerEvent(eventType, {
                bubbles: true, cancelable: true, composed: true, pointerId: 1, pointerType: 'mouse', isPrimary: true,
                clientX: x, clientY: y, screenX: x, screenY: y, button: 0, buttons: eventType === 'pointerdown' ? 1 : 0 }))
            """,
            new object[] { elementId, eventType, x, y });
    }

    public async ValueTask DisposeAsync()
    {
        await _browserContext.CloseAsync();
    }
}

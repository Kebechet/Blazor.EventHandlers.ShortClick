[!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/kebechet)

# Blazor.EventHandlers.ShortClick
[![NuGet Version](https://img.shields.io/nuget/v/Kebechet.Blazor.EventHandlers.ShortClick)](https://www.nuget.org/packages/Kebechet.Blazor.EventHandlers.ShortClick/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Kebechet.Blazor.EventHandlers.ShortClick)](https://www.nuget.org/packages/Kebechet.Blazor.EventHandlers.ShortClick/)
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/samuel_sidor.svg?style=social&label=Follow%20samuel_sidor)](https://x.com/samuel_sidor)

This package contains `@onshortclick` event handler.

**Motivation:** In order to unify development experience among multiple platforms I have decided to create `onshortclick` event for cases when you would like to use [@onlongpress](https://github.com/Kebechet/Blazor.EventHandlers.LongPress) event handler. Because for example on Windows browser `onclick` event is triggered even after holding `mousedown` for more than 500ms. On the other side mobile devices behave differently. And `onclick` is not triggered after more than 400-500ms .
- usage:
```
<button @onshortclick="OnShortClick">Click me</button>
```
or the combination with `@onlongpress`:
```
<button @onlongpress="OnLongPress" @onshortclick="OnShortClick">Click or longpress me</button>
```

## Ghost clicks (behaviour change in 3.0.0)

`shortclick` is dispatched on `pointerup`, which is **before** the browser dispatches the compatibility `click` for the same gesture. A handler is therefore free to re-render the DOM in between, and Blazor applies its render batch synchronously.

On touch, that compatibility click is hit-tested against the DOM **as it stands when it is dispatched**. So if your handler removed the element that was pressed, the click lands on whatever now occupies that point. When the replacement is a `<button type="submit">` or an `<a href>`, it activates through the browser's native default action - no handler involved, nothing you can intercept from C#.

A concrete case: an "Edit" button that swaps itself for "Cancel" + "Save". Tapping the right half of "Edit" enables edit mode **and** submits the form, because "Save" lands under the finger before the click is dispatched.

(This never shows up with a mouse: `click` is fired on the nearest common ancestor of `mousedown` and `mouseup`, so a removed element simply yields no click. It only reproduces on touch - use device emulation, e.g. iPhone SE.)

Since 3.0.0 the library swallows that click, but **only when it did not land on the element that was pressed** (or an ancestor/descendant of it). That covers both ghost shapes: the pressed element was removed and the click lands on its replacement, or the pressed element is still in the DOM but no longer hit-testable (moved, covered, `pointer-events: none`) and the click falls through to whatever is behind it. A click that lands on the pressed element is genuine and keeps reaching `submit` / `href` / your handler as before. Keyboard and programmatic clicks (`detail === 0`) are never swallowed. The guard is armed only for the ~350ms following a shortclick and disarms itself if no click arrives, so it can never swallow an unrelated later tap.

**This is a breaking change** if you were (knowingly or not) depending on that trailing click firing.

Note that `shortclick` never fires for keyboard activation - Enter/Space produce a `click` with no pointer events. If an element must be operable by keyboard, keep a plain `@onclick` on it.

## What counts as a short click

A press is a short click when the same pointer goes down and up within 400ms and moves at most 8 dips in between (Android's long-press timeout and touch slop).

- The 400ms are measured between the two events' own `timeStamp`s, not by when the handlers run. On a busy main thread (a render or a garbage collection in a WebAssembly app) the handlers run late, and a quick tap would otherwise measure as a long press and be dropped.
- Every pointer is tracked on its own (by `pointerId`), so a second finger neither overwrites nor completes another finger's press.
- A press that ends in `pointercancel` never raises `shortclick`. That is what the browser sends when the system takes the gesture over, such as a home swipe from Android's gesture bar that starts over your element.

## Tests

`tests/Blazor.EventHandlers.ShortClick.Browser.Tests` drives the shipping `ShortClick.js` in Chromium and WebKit through Playwright:

```
dotnet test tests/Blazor.EventHandlers.ShortClick.Browser.Tests
```

The first run downloads the browser builds of the pinned Playwright version.

# TODO
- minification
- build and publish pipeline

# License
This repository is licensed with the [MIT](LICENSE.txt) license.
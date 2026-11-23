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

## Click mode (opt-in, since 3.1.0)

Click mode gives a plain `@onclick` the semantics of `@onshortclick`: a click that a pointer made reaches the page only when its press was a short click. You keep writing `@onclick`, and the handler runs on `click`, the last event of a tap.

Turn it on for the whole page with an attribute on `<html>` (or `<body>`):

```html
<html data-shortclick-mode="click">
```

- Blazor WebAssembly and .NET MAUI Blazor Hybrid: `wwwroot/index.html`.
- Blazor Web App (interactive server, WebAssembly or auto, and static SSR): the `<html>` element in `App.razor`.

`ShortClick.js` reads the attribute on every click, so the attribute can be changed at runtime. The value is matched in any case (`Click` works too), as HTML matches its own enumerated attribute values. The nearest element carrying the attribute decides, the clicked element included: put it on one container to turn the mode on there only, or give a subtree any other value to turn it back off, for example around a third-party component that relies on long-press clicks:

```html
<div data-shortclick-mode="off">...</div>
```

What it drops - the click and its default action:
- a press held for 400ms or longer (or the page's own limit, see [How long a press may be held](#how-long-a-press-may-be-held-since-310)): a long-pressed link does not navigate, a held checkbox does not toggle, a held submit button does not submit
- a press that moved further than a short click may (see `MOVE_THRESHOLD` in `ShortClick.js`): a drag or a text selection that ends on a card does not activate it

What it never touches:
- keyboard activation (Enter, Space) and `element.click()` (`detail === 0`), and clicks a script dispatches (`isTrusted === false`). Unlike `@onshortclick`, an `@onclick` element stays operable by keyboard.
- text-entry controls: `<input>` types that edit a value (text, number, date, range...), `<textarea>`, `<select>` and `contenteditable` content, also inside a web component's shadow root. A long press or a drag there places the caret or selects text, so their clicks are left alone.
- a click that follows no press the script measured, such as a press whose `pointerdown` or `pointerup` a component's own script stopped from propagating, or an assistive technology's activation. Only a press measured as not short loses its click, and only a click that arrives within 350ms of that press's `pointerup`.
- `@onshortclick`, which keeps working the same with or without the mode.

A handler that runs on `click` cannot be followed by a ghost click (see below): nothing of the tap comes after it.

It is off by default because it changes every click in the page, third-party components included, and on a desktop a user who holds the mouse button down for 400ms or longer no longer clicks at all.

The listener is registered on `window` in the capture phase when `ShortClick.js` loads, so it runs before Blazor's own `click` listener on `document`. A dropped click is stopped with `preventDefault()` and `stopImmediatePropagation()` and never reaches Blazor.

Where it lets clicks through that it should not (it always fails open, never the other way):
- `ShortClick.js` is added as an `async` script by the package's JS initializer, before Blazor starts, and may finish loading after Blazor's first render. A tap in between is not filtered.
- In a browser without Pointer Events (iOS 12 and older), the mouse events that follow a touch measure the press again as a short one, so a touch long press keeps its click.

⚠️ **A busy main thread can make a quick tap look like a long press.** This version times a press with `new Date()` when its handlers run, not with the events' own timestamps. When the `pointerup` waits behind a long WebAssembly render or a garbage collection, a 150ms tap during a 300ms render measures as a long press: with click mode on its `@onclick` is dropped, as its `@onshortclick` already is. https://github.com/Kebechet/Blazor.EventHandlers.ShortClick/pull/4 times presses by event timestamps and fixes both.

**Measured on devices** (2026-11, one test page injected into a .NET MAUI BlazorWebView):
- Android 16 WebView (Chrome 157, Samsung Galaxy A53): a press held past Android's own long-press timeout fires no `click` at all, only `contextmenu`, with or without the mode. A press of 450-480ms does fire `click`; click mode drops it. A swipe ends in `pointercancel` and no click.
- iOS 26.2 WKWebView (iPhone 11): a long press of 800ms **does** fire `click`, so without the mode a long-pressed link navigates and a held checkbox toggles; click mode drops it. A drag fires no click.
- iOS has historically dispatched `click` only to elements it considers clickable (links, form controls, elements with `cursor: pointer` or a click listener of their own), while Blazor listens on `document`. On iOS 26.2 a tap on a plain `<div>` whose only click listener was on `document` did receive `click`. Older iOS versions were not tested: on those, give a tappable non-interactive element `cursor: pointer` or use a `<button>`.

## How long a press may be held (since 3.1.0)

By default a press held for 400ms or longer is a long press, not a short click (Android's long-press timeout). People who tap slowly need longer: Android's "Touch & hold delay" accessibility setting goes up to 1.5s, and iOS has "Hold Duration". Raise the limit with an attribute, in milliseconds:

```html
<html data-shortclick-long-press-ms="1000">
```

- It applies to `@onshortclick` and to click mode alike. Both take their verdict from the same measurement, so they never disagree on what a short press is.
- The nearest element carrying the attribute decides, starting from the element the press ends on: `<html>` sets the page's limit, and a container can set its own.
- A value that is not a positive number (empty, `0`, negative, text) falls back to 400.
- It is read on every press, so the app can change it at runtime, for example from a setting in its own UI. The library does not read the operating system's setting.

## Ghost clicks (behaviour change in 3.0.0)

`shortclick` is dispatched on `pointerup`, which is **before** the browser dispatches the compatibility `click` for the same gesture. A handler is therefore free to re-render the DOM in between, and Blazor applies its render batch synchronously.

On touch, that compatibility click is hit-tested against the DOM **as it stands when it is dispatched**. So if your handler removed the element that was pressed, the click lands on whatever now occupies that point. When the replacement is a `<button type="submit">` or an `<a href>`, it activates through the browser's native default action - no handler involved, nothing you can intercept from C#.

A concrete case: an "Edit" button that swaps itself for "Cancel" + "Save". Tapping the right half of "Edit" enables edit mode **and** submits the form, because "Save" lands under the finger before the click is dispatched.

(This never shows up with a mouse: `click` is fired on the nearest common ancestor of `mousedown` and `mouseup`, so a removed element simply yields no click. It only reproduces on touch - use device emulation, e.g. iPhone SE.)

Since 3.0.0 the library swallows that click, but **only when it did not land on the element that was pressed** (or an ancestor/descendant of it). That covers both ghost shapes: the pressed element was removed and the click lands on its replacement, or the pressed element is still in the DOM but no longer hit-testable (moved, covered, `pointer-events: none`) and the click falls through to whatever is behind it. A click that lands on the pressed element is genuine and keeps reaching `submit` / `href` / your handler as before. Keyboard and programmatic clicks (`detail === 0`) are never swallowed. The guard is armed only for the ~350ms following a shortclick and disarms itself if no click arrives, so it can never swallow an unrelated later tap.

**This is a breaking change** if you were (knowingly or not) depending on that trailing click firing.

Note that `shortclick` never fires for keyboard activation - Enter/Space produce a `click` with no pointer events. If an element must be operable by keyboard, keep a plain `@onclick` on it.

# TODO
- minification
- build and publish pipeline
- tests

# License
This repository is licensed with the [MIT](LICENSE.txt) license.
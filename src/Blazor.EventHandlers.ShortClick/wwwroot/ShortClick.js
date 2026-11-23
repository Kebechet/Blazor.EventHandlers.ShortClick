/*!
 * short-click-event - v@version@
 * JavaScript short-click-event
 * https://github.com/Kebechet/Blazor.EventHandlers.ShortClick
 * @license MIT
 */

let startTime, endTime;
let startPos = null;

//is same as Android DEFAULT_LONG_PRESS_TIMEOUT
const LONG_PRESS_TIMEOUT = 400;

//is same as Android TOUCH_SLOP and the unit is in DIPs
const MOVE_THRESHOLD = 8;

// How long after a shortclick a compatibility click is still considered part of the same gesture.
// Past this the guard disarms, so it can never swallow an unrelated later click.
const COMPATIBILITY_CLICK_WINDOW = 350;

// The elements whose native default action a click would trigger. See getPressedElement.
const INTERACTIVE_SELECTOR = 'a[href], button, input, select, textarea, label, summary, [role="button"]';

// Only one ghost-click guard may be armed at a time. See guardAgainstGhostClick.
let disarmActiveGuard = null;

// Click mode is on for a click when the nearest element carrying this attribute, the target included, has it set to
// CLICK_MODE_VALUE. See filterClick.
const CLICK_MODE_ATTRIBUTE = 'data-shortclick-mode';
const CLICK_MODE_VALUE = 'click';

// The <input> types a press activates, as it does a button. Every other type edits a value. See isTextEntryControl.
const ACTIVATING_INPUT_TYPES = new Set(['button', 'submit', 'reset', 'image', 'checkbox', 'radio', 'file', 'color']);

// How the last press ended, for click mode: when its "up" event came and whether it raised a shortclick. Null while a
// press is in progress, so a click is never judged by an earlier press.
let lastPressEnd = null;

// Get the device pixel ratio
const DEVICE_PIXEL_RATIO = window.devicePixelRatio || 1;

// Define a function to handle the "down" event
function handleDown(e) {
    lastPressEnd = null;

    // Record the time at the start of the event
    startTime = new Date();
    startPos = { x: e.screenX, y: e.screenY };
}

// Define a function to handle the "up" event
function handleUp(e) {
    // If no corresponding down event was recorded, ignore this up event
    if (startPos === null || startTime === undefined) {
        return;
    }

    // Capture and reset startPos so stale state doesn't carry over to the next event
    const capturedStartPos = startPos;
    startPos = null;
    lastPressEnd = { timeStamp: e.timeStamp, isShortClick: false };

    // Record the time at the end of the event
    endTime = new Date();
    // Calculate the difference between start and end times
    let timeDiff = endTime - startTime; // in ms

    // If the time difference is less than 400 ms, trigger your custom event
    if (timeDiff >= LONG_PRESS_TIMEOUT) {
        return;
    }

    // Calculate the distance moved - euclidean distance
    let dx = e.screenX - capturedStartPos.x;
    let dy = e.screenY - capturedStartPos.y;
    let distanceMovedInPixels = Math.sqrt(dx * dx + dy * dy);
    let distanceMovedInDips = Math.round(distanceMovedInPixels / DEVICE_PIXEL_RATIO);

    if (distanceMovedInDips > MOVE_THRESHOLD) {
        return;
    }

    lastPressEnd.isShortClick = true;
    triggerEvent(e);
}

// Define the custom event you want to trigger
function triggerEvent(event) {
    // Create a new custom event 'onshortclick' with MouseEvent properties
    const shortclickEvent = new CustomEvent('shortclick', {
        bubbles: true,
        cancelable: true,
        detail: {
            screenX: event.screenX,
            screenY: event.screenY,
            clientX: event.clientX,
            clientY: event.clientY,
            offsetX: event.offsetX,
            offsetY: event.offsetY,
            pageX: event.pageX,
            pageY: event.pageY,
            movementX: event.movementX,
            movementY: event.movementY,
            button: event.button,
            buttons: event.buttons,
            ctrlKey: event.ctrlKey,
            shiftKey: event.shiftKey,
            altKey: event.altKey,
            metaKey: event.metaKey,
            type: event.type,
        }
    });

    // Resolve the anchor BEFORE dispatching: the handler runs synchronously and may detach the target,
    // and closest() cannot walk up out of a node that has already been removed from the tree.
    const pressedElement = getPressedElement(event.target);

    event.target.dispatchEvent(shortclickEvent);

    guardAgainstGhostClick(pressedElement);
}

/*
 * A pointer event targets the deepest node under the finger - typically a label <span> inside a <button>,
 * not the button. Anchor the guard on the element that would actually receive the click's native default
 * action instead: a handler is free to replace that inner <span> while the <button> itself survives, and
 * the click that follows is then genuine. Anchoring on the replaced <span> would see it disconnected and
 * swallow a real click.
 *
 * Falls back to the target itself when the press was not on an interactive element (a @onshortclick on a
 * plain <div> or <tr>, for instance).
 */
function getPressedElement(target) {
    if (typeof target.closest !== 'function') {
        return target;
    }

    return target.closest(INTERACTIVE_SELECTOR) ?? target;
}

/*
 * `shortclick` is dispatched on pointerup, which is BEFORE the browser dispatches the compatibility
 * `click` for the same gesture. A handler is therefore free to re-render the DOM in between - and Blazor
 * applies its render batch synchronously.
 *
 * On touch, the compatibility click is hit-tested against the DOM as it stands when it is dispatched, so
 * it lands on whatever now occupies that point - not on the element that was actually pressed. If the
 * replacement happens to be a <button type="submit"> or an <a href>, it activates through the browser's
 * native default action, with no handler involved at all. That is a "ghost click".
 *
 * (Mouse input does not show this: `click` is fired on the nearest common ancestor of mousedown and
 * mouseup, so an element that has been removed simply yields no click.)
 *
 * A ghost click takes two shapes, and both mean the same thing - the click did not land on what was
 * pressed:
 *
 *   1. The pressed element was REMOVED, and the click lands on whatever replaced it
 *      (an "Edit" button that swaps itself for "Cancel" + "Save").
 *   2. The pressed element is still in the DOM but is no longer hit-testable - it was moved, covered, or
 *      turned `pointer-events: none` (e.g. an overlay animating out). The click then hit-tests straight
 *      through to whatever sits behind it.
 *
 * So the test is not "was the element removed" but "did the click land on what was pressed". A click on
 * the pressed element (or on an ancestor/descendant of it, which is what the browser targets when you
 * press a child and release on its parent) is genuine, and must keep reaching submit / href / the
 * framework.
 *
 * `target.isConnected` cannot be checked straight after dispatchEvent - the handler may be async and its
 * render batch may not have been applied yet. The check has to happen when the click actually arrives.
 */
function guardAgainstGhostClick(pressedElement) {
    // At most one guard may be armed. Some touch browsers emit no compatibility click at all, so a guard
    // can outlive its own gesture. If the user taps again inside that window, the stale guard would fire
    // first on the NEW gesture's genuine click and - seeing its own long-gone pressed element - swallow
    // it. Retire the previous guard before arming this one.
    disarmActiveGuard?.();

    let disarmTimer;

    const disarm = () => {
        clearTimeout(disarmTimer);
        window.removeEventListener('click', swallow, true);
        disarmActiveGuard = null;
    };

    const swallow = (clickEvent) => {
        // Keyboard activation and element.click() carry detail === 0; a compatibility click for a real
        // tap never does. Those are not the click this guard exists for - let them pass, and stay armed:
        // a programmatic click can arrive BEFORE the compatibility click, and disarming here would let
        // the actual ghost through behind it.
        if (clickEvent.detail === 0) {
            return;
        }

        disarm();

        const clickTarget = clickEvent.target;
        const landedOnPressedElement =
            pressedElement === clickTarget ||
            pressedElement.contains(clickTarget) ||
            clickTarget.contains(pressedElement);

        if (pressedElement.isConnected && landedOnPressedElement) {
            return;
        }

        clickEvent.preventDefault();
        clickEvent.stopImmediatePropagation();
    };

    disarmTimer = setTimeout(disarm, COMPATIBILITY_CLICK_WINDOW);

    // Capture phase: this has to run before the framework's delegated listener and before the browser
    // performs the click's default action.
    window.addEventListener('click', swallow, true);

    disarmActiveGuard = disarm;
}

/*
 * Click mode (opt-in): a click that a pointer made reaches the page only when its press was a short click, so a plain
 * @onclick behaves like @onshortclick. A press that was held past LONG_PRESS_TIMEOUT or moved past MOVE_THRESHOLD loses
 * its click - and with it the click's default action, so a long-pressed link does not navigate and a held checkbox does
 * not toggle.
 *
 * The handler then runs on `click`, the LAST event of a tap, so unlike a shortclick handler it cannot re-render the DOM
 * before a compatibility click arrives.
 *
 * Blazor listens for `click` on the document, in the bubble phase; this listener is on the window, in the capture phase,
 * so it runs first and a dropped click never reaches the framework.
 */
function filterClick(clickEvent) {
    // Keyboard activation and element.click() carry detail === 0, and a dispatchEvent() from a script is not trusted.
    // Neither comes from a press.
    if (clickEvent.detail === 0 || !clickEvent.isTrusted) {
        return;
    }

    const clickTarget = clickEvent.target;
    if (!isClickModeOn(clickTarget) || isTextEntryControl(clickTarget)) {
        return;
    }

    // Only a press measured as NOT short loses its click. A click that follows no measured press (a pointerup stopped
    // before it reached the window, an assistive technology's activation) keeps the browser's behaviour.
    const isClickOfLastPress =
        lastPressEnd !== null &&
        clickEvent.timeStamp - lastPressEnd.timeStamp <= COMPATIBILITY_CLICK_WINDOW;

    if (!isClickOfLastPress || lastPressEnd.isShortClick) {
        return;
    }

    clickEvent.preventDefault();
    clickEvent.stopImmediatePropagation();
}

function isClickModeOn(clickTarget) {
    if (typeof clickTarget.closest !== 'function') {
        return false;
    }

    const modeElement = clickTarget.closest(`[${CLICK_MODE_ATTRIBUTE}]`);

    return modeElement?.getAttribute(CLICK_MODE_ATTRIBUTE) === CLICK_MODE_VALUE;
}

/*
 * A press in a text field, a <select> or editable content places the caret, selects text or opens the list. Its click
 * belongs to that editing, not to an activation, so a long press or a drag-select keeps it.
 */
function isTextEntryControl(clickTarget) {
    if (clickTarget.isContentEditable || clickTarget.closest('textarea, select') !== null) {
        return true;
    }

    return clickTarget.tagName === 'INPUT' && !ACTIVATING_INPUT_TYPES.has(clickTarget.type);
}

// Capture phase, registered once: see filterClick. It does nothing until a page turns click mode on.
window.addEventListener('click', filterClick, true);

// Attach the event listeners for various event types
// Check if Pointer Events are supported.
if (window.PointerEvent) {
    // If they are, use them
    window.addEventListener('pointerdown', handleDown);
    window.addEventListener('pointerup', handleUp);
} else {
    // If they aren't, fall back to mouse and touch events
    window.addEventListener('mousedown', handleDown);
    window.addEventListener('mouseup', handleUp);

    window.addEventListener('touchstart', handleDown);
    window.addEventListener('touchend', handleUp);
}
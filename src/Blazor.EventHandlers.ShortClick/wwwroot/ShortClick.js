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

// Get the device pixel ratio
const DEVICE_PIXEL_RATIO = window.devicePixelRatio || 1;

// Define a function to handle the "down" event
function handleDown(e) {
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

    event.target.dispatchEvent(shortclickEvent);

    guardAgainstGhostClick(event.target);
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
 * So: swallow the click that trails a shortclick, but ONLY when the pressed element is gone. A click on
 * an element that is still there is genuine, and must keep reaching submit / href / the framework.
 *
 * `target.isConnected` cannot be checked straight after dispatchEvent - the handler may be async and its
 * render batch may not have been applied yet. The check has to happen when the click actually arrives.
 */
function guardAgainstGhostClick(target) {
    let disarmTimer;

    const disarm = () => {
        clearTimeout(disarmTimer);
        window.removeEventListener('click', swallow, true);
    };

    const swallow = (clickEvent) => {
        disarm();

        if (target.isConnected) {
            return;
        }

        clickEvent.preventDefault();
        clickEvent.stopImmediatePropagation();
    };

    // Some touch browsers emit no compatibility click at all. Disarm on a timer so a listener can never
    // linger and swallow the user's next, unrelated tap.
    disarmTimer = setTimeout(disarm, COMPATIBILITY_CLICK_WINDOW);

    // Capture phase: this has to run before the framework's delegated listener and before the browser
    // performs the click's default action.
    window.addEventListener('click', swallow, true);
}

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
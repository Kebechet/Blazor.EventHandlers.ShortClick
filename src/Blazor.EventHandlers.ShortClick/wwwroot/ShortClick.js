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

//is same as Android TOUCH_SLOP
const MOVE_THRESHOLD = 8;

// Define a function to handle the "down" event
function handleDown(e) {
    // Record the time at the start of the event
    startTime = new Date();
    startPos = { x: e.screenX, y: e.screenY };
}

// Define a function to handle the "up" event
function handleUp(e) {
    // Record the time at the end of the event
    endTime = new Date();
    // Calculate the difference between start and end times
    let timeDiff = endTime - startTime; // in ms

    // If the time difference is less than 400 ms, trigger your custom event
    if (timeDiff >= LONG_PRESS_TIMEOUT) {
        return;
    }

    // Calculate the distance moved - euclidean distance
    let dx = e.screenX - startPos.x;
    let dy = e.screenY - startPos.y;
    let distanceMoved = Math.sqrt(dx * dx + dy * dy);
    if (distanceMoved >= MOVE_THRESHOLD) {
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
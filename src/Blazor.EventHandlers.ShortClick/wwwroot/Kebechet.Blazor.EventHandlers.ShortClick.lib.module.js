export function beforeWebStart(options, extensions) {
    beforeStart(options, extensions);
}

export function afterWebStarted(blazor) {
    afterStarted(blazor)
}

export function beforeStart(options, extensions) {
    console.log("Injecting ShortClick.js");

    var element = document.createElement('script');
    element.src = "_content/Kebechet.Blazor.EventHandlers.ShortClick/ShortClick.js";
    element.async = true;
    document.body.appendChild(element);
}

export function afterStarted(blazor) {
    console.log("Registering ShortClick.js");

    blazor.registerCustomEventType('shortclick', {
        createEventArgs: event => {
            return {
                screenX: event.detail.screenX,
                screenY: event.detail.screenY,
                clientX: event.detail.clientX,
                clientY: event.detail.clientY,
                offsetX: event.detail.offsetX,
                offsetY: event.detail.offsetY,
                pageX: event.detail.pageX,
                pageY: event.detail.pageY,
                movementX: event.detail.movementX,
                movementY: event.detail.movementY,
                button: event.detail.button,
                buttons: event.detail.buttons,
                ctrlKey: event.detail.ctrlKey,
                shiftKey: event.detail.shiftKey,
                altKey: event.detail.altKey,
                metaKey: event.detail.metaKey,
                type: event.detail.type,
            };
        }
    });
}

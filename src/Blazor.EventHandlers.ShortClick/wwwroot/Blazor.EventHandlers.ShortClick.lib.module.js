export function beforeStart(options, extensions) {
    console.log("Injecting ShortClick.js");

    var element = document.createElement('script');
    element.src = "_content/Blazor.EventHandlers.ShortClick/ShortClick.js";
    element.async = true;
    document.body.appendChild(element);
}

export function afterStarted(blazor) {
    console.log("Registering ShortClick.js");

    blazor.registerCustomEventType('shortclick', {
        createEventArgs: event => {
            return {
                clientX: event.detail.clientX,
                clientY: event.detail.clientY,
            };
        }
    });
}

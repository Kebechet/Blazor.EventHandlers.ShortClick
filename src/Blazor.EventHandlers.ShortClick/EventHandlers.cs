using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Blazor.EventHandlers.ShortClick;

[EventHandler("onshortclick", typeof(MouseEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}

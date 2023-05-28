# Blazor.EventHandlers.ShortClick


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

# TODO
- minification
- build and publish pipeline
- tests

# License
This repository is licensed with the [MIT](LICENSE.txt) license.
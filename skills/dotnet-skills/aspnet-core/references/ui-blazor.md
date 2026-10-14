# Blazor

Primary docs:
- https://learn.microsoft.com/aspnet/core/blazor/
- https://learn.microsoft.com/aspnet/core/blazor/fundamentals/
- https://learn.microsoft.com/aspnet/core/blazor/security/

## Choose Blazor Deliberately

Prefer Blazor when the UI itself should be built as reusable .NET components and the team wants a full-stack .NET model.

Current guidance centers on the Blazor Web App model, which can combine:

- static SSR for fast first render
- interactive server rendering
- interactive WebAssembly rendering
- per-component render mode choices

Use standalone Blazor WebAssembly only when the app is intentionally client-heavy or must run as static files without a server-rendered host.

## Render Mode Heuristics

- Start with static SSR when the page is mostly read-only and fast first paint matters
- Use interactive server rendering when you want rich interactivity without shipping the full .NET runtime to the browser
- Use interactive WebAssembly when offline capability, client-side execution, or browser-local compute is the point
- Mix render modes only when the split is clear and justified

## Static SSR 404 Handling (.NET 10 breaking change)

- In .NET 10, manually setting `Response.StatusCode = 404` in a component under Static SSR **suppresses the response body** regardless of where/when you set it (`OnInitialized`, `OnInitializedAsync`, render phase): the framework detects a final 404 without its own NotFound flow and returns a bare empty-body 404. Other status codes (e.g. 402, 500 set via the same pattern) still render normally — the suppression is specific to 404.
- The `<NotFound>` render fragment parameter on `Router` is `[Obsolete]` in .NET 10; the replacement is the `Router.NotFoundPage` parameter pointing to a routable NotFound page component.
- Correct patterns for Static SSR 404s:
  - Interactive flows and enhanced navigation: call `NavigationManager.NotFound()` from the component (which triggers the router's NotFoundPage).
  - Address-bar requests with no matching `@page` route (bare 404): handle via `UseStatusCodePagesWithReExecute` re-executing a dedicated 404 endpoint; do not try to set the status code manually inside a component.
- Rule of thumb: **never set `Response.StatusCode = 404` by hand in a component**. Non-404 status codes (401/402/403/…) set from `OnInitializedAsync` still work, but keep the timing rule: set them only after async data loading completes.

## Component Patterns

- Keep components focused and composable
- Move data access and business rules into injected services
- Pass data through parameters, not hidden global state
- Use forms and validation with Blazor's built-in editing and validation components
- Prefer shared Razor Class Libraries for reusable component sets

## Data And Interactivity

- Use DI in components with restraint; avoid turning components into service locators
- Treat JS interop as an edge mechanism for browser APIs or third-party libraries, not the primary application model
- Keep long-running work off the UI event path
- Be deliberate about prerendering, streaming rendering, and enhanced navigation when they improve perceived performance

## Security Notes

- Follow the general ASP.NET Core security guidance first, then load the Blazor-specific docs for details that supersede it
- Remember that client-side code and browser state are not trusted
- Keep secrets and privileged operations on the server
- Use authorization-aware UI only as a convenience layer; enforce rules on the server as well

## When Not To Use Blazor

- Do not force Blazor onto a mostly conventional server-rendered app that already fits Razor Pages or MVC well
- Do not choose WebAssembly by default for small interaction needs that SSR or interactive server rendering handles more simply

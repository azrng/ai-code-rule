---
name: bootstrap-blazor
version: 10.9.3
last_updated: 2026-08-20
description: Build, review, or refactor Blazor UI using the BootstrapBlazor component library (v10.9.2). Use when the project references the `BootstrapBlazor` NuGet package and the task involves building pages, CRUD tables (server-side paging/search/edit), forms (ValidateForm/EditorForm), feedback (Dialog/Toast/Swal/Modal/Drawer), or admin layouts (Layout/Menu/Tab). Covers real component tag names, service injection by concrete class, the `BootstrapBlazorRoot` host requirement, and the `OnQueryAsync -> QueryData<T>` server query pattern. Also use to audit code for hallucinated APIs (wrong param names, params borrowed from MudBlazor/Radzen/AntDesign Blazor).
---

# BootstrapBlazor (v10.9.2)

Build Blazor pages with the real BootstrapBlazor component library API — prefer its components over hand-rolled HTML/JS, and never guess an API that may belong to another Blazor UI library.

All API facts in this skill are extracted from the BootstrapBlazor **10.9.2** source tree (`D:/SourceCode/blazor/bootstrap/BootstrapBlazor`) and cross-checked against the official `BootstrapBlazor.Server` samples and a real RBAC project (`rbac-blazor`, using 10.6.0). Every param references its `源码文件:行号` in the reference files.

## When to use

- The project has `<PackageReference Include="BootstrapBlazor" ... />` (check the `.csproj` / `Directory.Packages.props`).
- You are adding a page, table, form, dialog, toast, or admin layout that should use BootstrapBlazor — not raw `<table>`/Bootstrap CSS/JS.
- You are reviewing/auditing Blazor markup for hallucinated or version-mismatched BootstrapBlazor APIs.

## Core principles

1. **Prefer BootstrapBlazor components.** If a `<Table>`, `<Dialog>`, `<Modal>`, or `<Toast>` capability exists, use it — do not reimplement with raw HTML, Bootstrap CSS classes, or JavaScript.
2. **Never guess APIs.** BootstrapBlazor is NOT MudBlazor / Radzen / Ant Design Blazor / Blazorise. Param names, service method names, and tag names differ. When unsure → read [references/anti-hallucination.md](references/anti-hallucination.md) or the source first.
3. **Match the 10.9.2 API exactly.** Common traps: `ShowAsync` does not exist (it's `Show`); table paging is `IsPagination` (not `Pageable`); multi-select is `IsMultipleSelect` (not `ShowRowCheckbox`); text inputs are `<BootstrapInput>` / `<BootstrapInputNumber>` (with prefix).
4. **Follow the project's conventions.** If the repo already uses a Modal-subcomponent + `@ref` pattern for edit dialogs (like rbac-blazor), match it instead of switching to `DialogService`.

## UI review mode

For a UI/interaction audit, first read [references/anti-hallucination.md](references/anti-hallucination.md) and then [references/ui-audit.md](references/ui-audit.md). Report only findings supported by markup, code, configuration, or a rendered page; distinguish an API defect from a product/design recommendation.

Use this mode to catch issues that compile cleanly but make an admin console unreliable: unvalidated persisted forms, re-entrant writes, missing loading/empty/error states, destructive actions without confirmation, inaccessible custom navigation, and broken responsive layouts. Do not demand a BootstrapBlazor component when the project deliberately uses a custom implementation that meets the same behavior and accessibility bar.

## Prerequisites (missing any of these breaks every page)

Before any BootstrapBlazor page works, the host must be set up. See [references/setup.md](references/setup.md).

- `builder.Services.AddBootstrapBlazor();` — registers ALL popup/tool services in one call (namespace `Microsoft.Extensions.DependencyInjection`).
- **`<BootstrapBlazorRoot>` in the interactive Layout** — place it in `MainLayout.razor` wrapping `@Body`, **not** in `App.razor` wrapping `<Routes>`. Popup components (Modal/Toast/Dialog/Swal/Drawer) portal their DOM to it via `<BootstrapBlazorRootContent>`, so it must share the page's interactive circuit. Wrong placement → popups never show and `Show()` throws **no exception** (silent JS failure; the hardest bug to find). Two related traps (can't add `@rendermode` to it; `ChildContent` not serializable) in [references/setup.md](references/setup.md) and [references/anti-hallucination.md](references/anti-hallucination.md).
- Static bundles in `App.razor`: `_content/BootstrapBlazor/css/bootstrap.blazor.bundle.min.css` + `_content/BootstrapBlazor/js/bootstrap.blazor.bundle.min.js`.
- `_Imports.razor` has `@using BootstrapBlazor.Components`.
- `UseBootstrapBlazor()` middleware comes from the **separate** `BootstrapBlazor.Middleware` package — the core library does not require it.

## Services — inject by concrete class, method is `Show`

There are no `IXxxService` interfaces. Inject the concrete class:

```razor
@inject DialogService DialogService
@inject ToastService ToastService
@inject SwalService SwalService
@inject MessageService MessageService
@inject DrawerService DrawerService
```

| Service | Core method | Shortcut extensions |
|---|---|---|
| `DialogService` | `Show(DialogOption)` | `Show<T>()`, `ShowEditDialog<T>()`, `ShowModal()`→`Task<DialogResult>` |
| `ToastService` | `Show(ToastOption)` | `Success/Error/Information/Warning(title,content)` (**no `Question`**) |
| `SwalService` | `Show(SwalOption)` | `ShowModal()`→`Task<bool>` (confirm) |
| `MessageService` | `Show(MessageOption)` | (none) |
| `DrawerService` | `Show(DrawerOption)` | (none) |
| `NotificationService` | `Dispatch(NotificationItem)` | browser-native notifications, not an in-page popup |
| `FullScreenService` | `Toggle(...)` | (no Open/Close) |

Full service list + options classes: [references/di-services.md](references/di-services.md).

## Code generation priority (highest first)

```text
BootstrapBlazor recommended pattern (from Samples / rbac-blazor)
        ↓
BootstrapBlazor component + its params
        ↓
Bootstrap utility / CSS
        ↓
Custom Razor component
        ↓
Custom HTML/CSS
        ↓
JavaScript   ← last resort; almost never needed
```

## Component selection (quick)

| Need | Use |
|---|---|
| Data table | `<Table TItem>` — `Items` (client) or `OnQueryAsync` (server) |
| Validated form | `<ValidateForm Model OnValidSubmit>` |
| Auto-generated form | `<EditorForm TModel>` inside a `<ValidateForm>` |
| Text input | `<BootstrapInput @bind-Value>` / `<BootstrapInputNumber>` / `<Textarea>` |
| Dropdown | `<Select @bind-Value Items>`; multi → `<MultiSelect>`; typed items → `<SelectGeneric>` |
| Checkbox group | `<CheckboxList @bind-Value Items>` |
| Toggle | `<Switch @bind-Value OnText OffText>` |
| Date | `<DateTimePicker @bind-Value>` |
| Success/error toast | `ToastService.Success(title, content)` |
| Delete confirm | `<TableCellPopConfirmButton>` inline (preferred) or `SwalService.ShowModal()` |
| Edit dialog | `<Modal><ModalDialog>` sub-component + `@ref` (preferred) or `DialogService.Show<T>()` |
| Drawer panel | `<Drawer @bind-IsOpen>` or `DrawerService.Show()` |
| Admin shell | `<Layout>` with `Header/Side/Main/Footer` + `Menus` + `UseTabSet` |

Tag-name gotcha: `<BootstrapInput>`, `<BootstrapInputNumber>`, `<BootstrapInputGroup>` carry the `Bootstrap` prefix; `<Select>`, `<Switch>`, `<Checkbox>`, `<CheckboxList>`, `<RadioList>`, `<DateTimePicker>`, `<Cascader>`, `<AutoComplete>`, `<AutoFill>`, `<ValidateForm>`, `<EditorForm>`, `<Textarea>` do **not**.

## CRUD page recipe

When asked to "create a user-management page", derive this structure (from rbac-blazor). Full code: [references/crud-recipes.md](references/crud-recipes.md) and [examples/](examples/).

```text
Page (Index.razor + .razor.cs code-behind)
  ├─ Toolbar: <Card><BodyTemplate> buttons </BodyTemplate></Card>
  ├─ <Table TItem OnQueryAsync IsPagination IsMultipleSelect ShowSearch>
  │     ├─ <TableColumns>...<TableColumn @bind-Field>...</TableColumns>
  │     ├─ <CustomerSearchTemplate> search fields </CustomerSearchTemplate>
  │     └─ <RowButtonTemplate> edit + <TableCellPopConfirmButton> delete </RowButtonTemplate>
  ├─ <Add @ref OnSaveCallback/>   ← Modal sub-component
  └─ <Edit @ref OnSaveCallback/>  ← Modal sub-component
```

- Server query: `OnQueryAsync(QueryPageOptions) → Task<QueryData<TItem>>` returning `{ Items, TotalCount, IsFiltered, IsSorted, IsSearch, IsAdvanceSearch }`. See [references/table.md](references/table.md).
- Feedback: `ToastService.Success("消息通知", "操作成功")` / `.Error(...)`.
- Refresh table: `await TableRef.QueryAsync();`.
- Code-behind: follow the repository's existing convention. For a new or growing component, `.razor` markup plus a `.razor.cs` partial class is a useful default; retain an existing compact `@code` component unless splitting improves readability or testability.

## Reference guide

Load only the reference that matches the task — do not load all by default.

- [references/setup.md](references/setup.md) — install, `AddBootstrapBlazor`, `BootstrapBlazorRoot`, static assets, `Program.cs` / `Startup.cs` template.
- [references/anti-hallucination.md](references/anti-hallucination.md) — **★ wrong-vs-right API table**. Read this first when writing or auditing any markup.
- [references/ui-audit.md](references/ui-audit.md) — review-only checklist for form/state/feedback, navigation accessibility, responsiveness, and BootstrapBlazor runtime wiring.
- [references/table.md](references/table.md) — `<Table TItem>` deep API: data source, columns, sort, search, paging, selection, edit/add/delete, toolbar, templates, server query.
- [references/forms.md](references/forms.md) — `ValidateForm` / `EditorForm` and every form control's real params.
- [references/feedback.md](references/feedback.md) — Dialog / Toast / Swal / Message / Modal / Drawer / Tooltip / Popover.
- [references/layout.md](references/layout.md) — `Layout` / `Menu` / `Tab` / `Breadcrumb` / `Pagination` / `Step` / `Card` / `Row`.
- [references/components.md](references/components.md) — other frequent components (Upload / TreeView / Validate / Badge / Tag / Avatar / Spinner / Skeleton).
- [references/di-services.md](references/di-services.md) — full service registry + options classes.
- [references/crud-recipes.md](references/crud-recipes.md) — the four standard recipes (list / form / layout / feedback) with skeletons.
- examples/: `BasicPage.razor`, `Login.razor`, `CrudTable.razor`, `FormModal.razor`, `AdminLayout.razor`.

## Prohibitions

- Do NOT invent components, params, `EventCallback`s, services, or extension methods. If not confirmed in source/reference, search the source tree (`D:/SourceCode/blazor/bootstrap/BootstrapBlazor/src/BootstrapBlazor/Components/`) first.
- Do NOT port APIs from MudBlazor / Radzen / Ant Design Blazor / DevExpress / Telerik / Blazorise.
- Do NOT set `Items` and `OnQueryAsync` together on a `<Table>` — they are mutually exclusive and throw.
- Do NOT bind `SortName`/`SortOrder` from markup (not `[Parameter]`); use column `DefaultSort`/`DefaultSortOrder`.
- Do NOT omit `<BootstrapBlazorRoot>` — every popup service call will throw.
- Do NOT write `ShowAsync`/`ShowModalAsync` — the method is `Show` (extensions like `ShowModal` exist, `ShowAsync` does not).

## Version & provenance

```text
BootstrapBlazor Version : 10.9.2   (source HEAD, multi-target net6–net10)
Skill Generated        : 2026-08-13
API Source             : D:/SourceCode/blazor/bootstrap/BootstrapBlazor (src/BootstrapBlazor/Components/*)
Cross-checked against  : BootstrapBlazor.Server official Samples + rbac-blazor (10.6.0)
Build-verified         : examples/ compiled CLEAN (0 warning, 0 error) against BootstrapBlazor 10.9.2
                         on .NET 10.0.400 Blazor Server. Covers the high-frequency API surface —
                         ValidateForm/BootstrapInput(+/Number)/Textarea/Select(+Options/SelectOption)/
                         CheckboxList/Switch/DateTimePicker, Table(OnQueryAsync, IsPagination, IsMultipleSelect,
                         TableColumn, RowButtonTemplate, TableCellButton/TableCellPopConfirmButton), Modal+
                         ModalDialog, Card/Row/Divider/Badge/Spinner/Progress, Layout/Menu/Logout/ErrorLogger,
                         ToastService.Success/Error, QueryData<T>/QueryPageOptions/SortOrder.
                         Reference-file params beyond the examples are sourced line-by-line from the 10.9.2
                         tree (each carries 文件:行号) but not all are build-exercised — verify before relying.
```

Reproduce the build check: `dotnet new blazor --interactivity Server`, `dotnet add package BootstrapBlazor --version 10.9.2`, add `AddBootstrapBlazor()` + `<BootstrapBlazorRoot>` + the two bundle assets, drop the `examples/` files in, then `dotnet build`.

If the source is upgraded, re-scan the `Components/` tree and update the reference files; param names (spelling, whether `@bind`-able, types) are the stable signal, line numbers drift.

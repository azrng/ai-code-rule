# 表单体系深度参考

目录：`Components/`（ValidateForm/EditorForm/Validate/Editors 及各表单组件）。绑定统一 `@bind-Value`（基于 `Value`+`ValueChanged`+`ValueExpression`，`DisplayBase.cs:68,75,82`）。

## 1. ValidateForm（`ValidateForm/ValidateForm.razor.cs`）

包裹一组表单组件，内部封装 `EditForm` + `BootstrapBlazorDataAnnotationsValidator`，提供校验。

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Model` | `object?` | 绑定模型 | `:62` |
| `OnValidSubmit` / `OnInvalidSubmit` | `Func<EditContext, Task>?` | 校验通过/失败回调（**无 OnSubmit**） | `:26,33` |
| `ShowRequiredMark` | `bool` | 必填标记（默认 true） | `:77` |
| `ShowLabel` / `ShowLabelTooltip` | `bool?` | 标签 | `:84,91` |
| `ValidateAllProperties` | `bool` | 全量校验（**非 ValidateAll**） | `:55` |
| `ShowAllInvalidResult` | `bool` | 显示全部无效结果 | `:48` |
| `DisableAutoSubmitFormByEnter` | `bool?` | 禁止回车提交 | `:106` |
| `LabelWidth` | `int?` | 标签宽度 | `:113` |
| `IsFormless` | `bool` | 无 form 标签 | `:99` |
| `OnFieldValueChanged` | `Action<string, object?>?` | 字段变化 | `:41` |

公开方法（代码调用，非 Parameter）：`Task<bool> ValidateAsync()`（`:683`，旧 `Validate()` `:677` 已废弃）、`SetError(...)`、`NotifyFieldChanged(...)`、`ValueChangedFields`。

> 无 `OnSubmit`、无 `ValidateAll`、无 `CustomerValidator` 参数。自定义校验：组件级 `ValidateRules: List<IValidator>?`（`ValidateBase.cs:417`），或模型实现 `IValidatableObject` / `IValidateCollection`。

提交按钮：`<Button Type="ButtonType.Submit">`。

## 2. EditorForm（`EditorForm/EditorForm.razor.cs`）— 自动生成表单

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Model` | `TModel?` | 模型（嵌套在 ValidateForm 内可省略，自动捕获级联 EditContext） | `:107` |
| `AutoGenerateAllItem` | `bool` | 自动生成全部字段（默认 true） | `:151` |
| `Items` | `IEnumerable<IEditorItem>?` | 字段配置 | `:165` |
| `FieldItems` | `RenderFragment<TModel>?` | 手动字段容器（放 `<EditorItem>`） | `:92` |
| `ItemsPerRow` | `int?` | 每行字段数 | `:57` |
| `RowType` | `RowType` | Normal/Inline | `:71` |
| `LabelAlign` / `LabelWidth` | `Alignment` / `int?` | 标签对齐/宽 | `:78,85` |
| `Buttons` | `RenderFragment?` | 按钮区 | `:99` |
| `IgnoreItems` | `List<string>?` | 忽略字段 | `:158` |
| `IsDisplay` | `bool` | 只读展示 | `:128` |

## 3. EditorItem（`EditorForm/EditorItem.cs`，`<EditorItem TModel TValue>`）

| 参数 | 类型 | 说明 | 来源 |
|---|---|---|---|
| `Field` / `FieldExpression` | `TValue?` / `Expression<Func<TValue>>?` | 绑定（**无 FieldName**） | `:22,36` |
| `Text` | `string?` | 标签文本 | `:92` |
| `Items` / `Lookup` | `IEnumerable<SelectedItem>?` | 下拉数据 | `:161,167` |
| `ComponentType` | `Type?` | 编辑组件类型（如 `typeof(Select<string>)`） | `:137` |
| `Readonly` / `Required` | `bool?` | 只读/必填 | `:62,68` |
| `SkipValidate` | `bool` | 跳过校验 | `:80` |
| `Ignore` | `bool?` | 忽略（**无 Visible**） | `:56` |
| `Order` | `int` | 排序 | `:155` |
| `Rows` / `Cols` / `Step` | `int` / `int` / `string?` | textarea 行列/步进（EditorForm 自动生成时用） | `:104,110,98` |
| `EditTemplate` | `RenderFragment<TModel>?` | 编辑模板 | `:117` |
| `GroupName` / `GroupOrder` | `string?` / `int` | 分组 | `:223,229` |
| `ValidateRules` | `List<IValidator>?` | 校验规则 | `:217` |

## 4. 表单组件共性（继承链）

`DisplayBase<TValue>` → `ValidateBase<TValue>` → `BootstrapInputBase<TValue>` → `BootstrapInputEventBase<TValue>`。

- `DisplayBase`：`Value`/`ValueChanged`/`ValueExpression`（`DisplayBase.cs:68,75,82`）、`ShowLabel`/`ShowLabelTooltip`/`DisplayText`（`:89,96,103`）。
- `ValidateBase`：`IsDisabled`（`:192`）、`SkipValidate`（`:185`）、`ShowRequired`（`:199`）、`RequiredErrorMessage`（`:206`）、`OnValueChanged`（`:170`）、`ValidateRules`（`:417`）、`ParsingErrorMessage`（`:178`）。
- `BootstrapInputBase`：`PlaceHolder`（`:29`）、`IsAutoFocus`（`:71`）、`Color`（`:50`）、`Formatter`/`FormatString`（`:57,64`）、`IsTrim`（`:92`）、`OnEnterAsync`/`OnEscAsync`/`OnBlurAsync`（`:36,43,99`）。
- `BootstrapInputEventBase`：`UseInputEvent`（`:23`，仅 Textarea/InputNumber 有）。

## 5. 各表单组件（标签名 = 组件类名，除下述带前缀者）

### `<BootstrapInput>`（`Input/BootstrapInput.razor.cs`）
`Readonly`(`:19`)、`IsClearable`(`:42`)、`ClearIcon`(`:65`)、`OnClear`(`:49`)、`AutoSetDefaultWhenNull`(`:26`)。
> **标签带 `Bootstrap` 前缀**。

### `<BootstrapInputNumber<TValue>>`（`InputNumber/BootstrapInputNumber.razor.cs`）
`Min`/`Max`/`Step`（`:59,66,73`）、`ShowButton`(`:80`)、`PlusIcon`/`MinusIcon`(`:94,87`)、`OnIncrement`/`OnDecrement`(`:45,52`)、`UseInputEvent`。
> **标签带 `Bootstrap` 前缀**。

### `<Textarea>`（`Textarea/Textarea.razor.cs`）
`IsAutoScroll`(`:37`)、`UseShiftEnter`(`:44`)、`UseInputEvent`（继承）。**无 `Rows` 参数**。

### `<Select<TValue>>`（`Select/Select.razor.cs`）
`Items: IEnumerable<SelectedItem>?`（`SimpleSelectBase.cs:36`）、`ShowSearch`(`SelectBase.cs:27`)、`IsClearable`(`SelectBase.cs:151`)、`PlaceHolder`(`SelectBase.cs:106`)、`OnSelectedItemChanged: Func<SelectedItem,Task>?`(`:98`)、`OnBeforeSelectedItemChange: Func<SelectedItem,Task<bool>>?`(`:84`)、`ItemTemplate`(`SimpleSelectBase.cs:65`)、`IsVirtualize`(`SelectBase.cs:113`)、声明式 `<Options><SelectOption Text Value/>`(`:70`)。
> 无 `Category`、无 `IsMultiple`（多选用 `<MultiSelect>`）。

### `<SelectGeneric<TValue>>`（强类型数据项 `SelectedItem<TValue>`）
`Items: IEnumerable<SelectedItem<TValue>>?`(`:123`)、`OnSelectedItemChanged`(`:144`)、`TextConvertToValueCallback`(`:68`)。

### `<MultiSelect<TValue>>`（`Select/MultiSelect.razor.cs`）
`ShowToolbar`/`ShowDefaultButtons`(`:74,81`)、`Max`/`Min`(`:161,176`)、`SelectAllText`/`ReverseSelectText`/`ClearText`(`:138,146,154`)、`OnSelectedItemsChanged`(`:123`)、继承 `Items`/`ShowSearch`。

### `<Cascader<TValue>>`（`Cascader/Cascader.razor.cs`）
`Items: IEnumerable<CascaderItem>?`(`:55`)、`OnSelectedItemChanged: Func<CascaderItem[],Task>?`(`:62`)、`ShowFullLevels`(`:76`，**非 ShowFullPath**)、`ParentSelectable`(`:69`)、`IsClearable`(`:97`)。

### `<Checkbox<TValue>>`（`Checkbox/Checkbox.razor.cs`）
`State: CheckboxState`(`:75`)、`OnStateChanged: Func<CheckboxState,TValue,Task>?`(`:97`)、`Color`/`Size`(`:54,61`)、`ChildContent`(`:111`)。

### `<CheckboxList<TValue>>`（`Checkbox/CheckboxList.razor.cs`）
`Items: IEnumerable<SelectedItem>?`(`:55`)、`IsButton`(`:62`)、`ShowBorder`(`:83`，默认 true)、`IsVertical`(`:90`)、`Color`(`:97`)、`OnSelectedChanged`(`:104`)、`MaxSelectedCount`(`:111`)、`ItemTemplate`(`:125`)。

### `<RadioList<TValue>>`（`Radio/RadioList.razor.cs`，继承 CheckboxList）
`IsAutoAddNullItem`(`:21`)、`AutoSelectFirstWhenValueIsNull`(`:36`，默认 true)；继承 `Items`/`IsButton`/`IsVertical`/`Color`。

### `<Switch>`（`Switch/Switch.razor.cs`，继承 `ToggleBase<bool>`）
`OnText`/`OffText`（`ToggleBase.cs:35,43`）、`OnColor`/`OffColor`(`:69,76`)、`Width`/`Height`(`:83,90`)、`OnInnerText`/`OffInnerText`(`:98,106`)、`ShowInnerText`(`:113`)、`Value`/`OnValueChanged`（继承 ValidateBase<bool>）。

### `<DateTimePicker<TValue>>`（`DateTimePicker/DateTimePicker.razor.cs`，TValue: DateTime/DateTimeOffset）
`ViewMode: DatePickerViewMode`(`:180`，默认 Date)、`DateTimeFormat`/`DateFormat`/`TimeFormat`(`:113,121,129`)、`MinValue`/`MaxValue`(`:216,209`)、`AutoClose`(`:223`)、`IsClearable`(`:158`)、`ShowIcon`(`:151`)、`FirstDayOfWeek`(`:136`)、`ShowLunar`/`ShowFestivals`/`ShowHolidays`(`:288,302,309`)、`OnGetDisabledDaysCallback`(`:316`)。
> 无 `DisplayFormat`（用 `DateTimeFormat`/`DateFormat`/`TimeFormat`）。

### `<AutoComplete>`（`AutoComplete/AutoComplete.razor.cs`）
`Items: IEnumerable<string>?`(`:22`)、`OnCustomFilter`(`:29`)、`DisplayCount`(`:51`)、`IsLikeMatch`/`IgnoreCase`(`:58,65`)、`OnSelectedItemChanged: Func<string,Task>?`（继承）。

### `<AutoFill<TValue>>`（`AutoFill/AutoFill.razor.cs`）
`Items: IEnumerable<TValue>?`(`:25`)、`OnGetDisplayText: Func<TValue?,string?>?`(`:62`)、`DisplayCount`(`:33`)、`IsVirtualize`(`:106`)、`OnQueryAsync`(`:136`)。

### `<Rate>`（`Rate/Rate.razor.cs`，独立组件）
`Value: double`(`:61`)、`Max`(`:119`，默认 5)、`IsDisable`/`IsReadonly`(`:69,77`)、`ShowValue`(`:91`)。

### `<Slider<TValue>>`（`Slider/Slider.razor.cs`）
`Min`/`Max`/`Step`(`:24,32,40`)，范围默认读 `[Range]` 特性。

### `<Segmented<TValue>>`（`Segmented/Segmented.razor.cs`）
`Items: IEnumerable<SegmentedOption<TValue>>?`(`:35`)、`IsDisabled`/`IsBlock`(`:64,71`)、`ChildContent`(`:85`)。

### `<BootstrapInputGroup>`（`Input/BootstrapInputGroup.razor.cs`）
前后缀组合：`<BootstrapInputGroup>` + `<BootstrapInputGroupLabel>` / `<BootstrapInputGroupIcon>`。

## 6. SelectedItem（`Misc/SelectedItem.cs`）

```csharp
public class SelectedItem
{
    public SelectedItem() { }
    public SelectedItem(string value, string text) { Value = value; Text = text; }
    public string Text { get; set; } = "";      // :34
    public string Value { get; set; } = "";      // :40
    public bool Active { get; set; }             // :46
    public bool IsDisabled { get; set; }         // :52
    public string GroupName { get; set; } = "";  // :58
}
```

强类型版 `SelectedItem<TValue>`（含强类型 `Value`），供 `SelectGeneric`/`MultiSelectGeneric` 使用。

## 示例

### 手动 ValidateForm

```razor
<ValidateForm Model="model" OnValidSubmit="OnSave" ShowRequiredMark="true" ValidateAllProperties="true">
    <BootstrapInput @bind-Value="model.Name" PlaceHolder="姓名" ShowLabel="true" />
    <BootstrapInputNumber @bind-Value="model.Age" Min="0" Max="150" ShowButton="true" ShowLabel="true" />
    <Select @bind-Value="model.Gender" Items="GenderItems" ShowSearch="true" ShowLabel="true" />
    <DateTimePicker @bind-Value="model.Birthday" ViewMode="DatePickerViewMode.Date" />
    <Switch @bind-Value="model.IsActive" OnText="启用" OffText="停用" />
    <Button Type="ButtonType.Submit" Text="保存" />
</ValidateForm>

@code {
    private Person model = new();
    private List<SelectedItem> GenderItems = [new("M","男"), new("F","女")];
    private Task OnSave(EditContext ctx) => Task.CompletedTask;
    public class Person {
        [Required] public string Name { get; set; } = "";
        [Range(0,150)] public int Age { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
        public bool IsActive { get; set; }
    }
}
```

### EditorForm 自动生成

```razor
<ValidateForm Model="model" OnValidSubmit="OnSave">
    <EditorForm TModel="Person" Model="model" ItemsPerRow="2" AutoGenerateAllItem="true">
        <FieldItems>
            <EditorItem TModel="Person" TValue="string" Field="@model.Name"
                        FieldExpression="@(() => model.Name)" />
            <EditorItem TModel="Person" TValue="string" Field="@model.Gender"
                        FieldExpression="@(() => model.Gender)"
                        ComponentType="typeof(Select<string>)" Items="GenderItems" />
        </FieldItems>
    </EditorForm>
</ValidateForm>
```

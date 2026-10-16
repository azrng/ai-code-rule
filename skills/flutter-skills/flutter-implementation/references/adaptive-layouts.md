# Adaptive Layouts

## Contents

- Constraint-driven composition
- Breakpoint behavior
- Large screens and foldables
- Testing

## Constraint-driven composition

Use `LayoutBuilder`, `MediaQuery`, flexible constraints, slivers, and intrinsic content needs. Avoid device-name branching. Size classes are project decisions, not universal numbers.

## Breakpoint behavior

For every boundary, specify what reflows, resizes, reorders, appears, disappears, or changes navigation pattern. Prefer a few meaningful composition changes over many fragile breakpoints.

## Large screens and foldables

Do not stretch a phone column. Consider list-detail, supporting panes, persistent navigation, bounded reading widths, denser grids, and posture/hinge constraints. Preserve focus and selected state across composition changes.

## Testing

Test just below, at, and above each breakpoint; narrow split-screen; landscape; enlarged text; keyboard insets; safe areas; and long localized content.


# Connected Product Workflows

Use this reference when one Flutter surface combines multiple product domains, dashboards, tools, or data sources.

## Establish the connective model

Do not place unrelated feature cards on one screen and call the result connected. Identify:

- the shared user goal;
- the cross-domain objects or events;
- the primary domain for the current moment;
- which secondary domains provide context or require action;
- the summarization or command layer that connects them.

Represent these relationships in models and component inputs. Keep domain-specific state independent enough to fail or refresh without erasing the entire experience.

## Design the hierarchy

Choose one primary region and no more than three secondary regions per viewport. Use importance, urgency, and task sequence—not equal card sizing—to determine emphasis. Give every domain a consistent semantic accent while preserving one shared type, spacing, geometry, and interaction language.

On compact layouts, serialize the hierarchy into a coherent feed. On expanded layouts, compose the same components into a grid or master-detail structure. Do not build separate mobile and desktop products with divergent information.

## Model state explicitly

Cover both global and local states:

- initial synchronization;
- fully populated;
- one domain stale or partially unavailable;
- global recoverable error;
- no connected sources;
- permission or destructive confirmation;
- successful cross-domain action.

State communication must say what is affected, what remains safe, and what the user can do next. Avoid replacing useful unaffected content with a full-screen error when only one source failed.

## Preserve accessibility

Align semantic and keyboard order with visual priority. Announce asynchronous cross-domain updates selectively. Domain color must never be the only identifier. At large text sizes, preserve the primary action and move secondary regions below it instead of truncating labels or disabling scaling.

## Specify motion

Use motion to explain region reordering, source refresh, and command results. Keep unrelated cards still. Cross-fade global state changes; use short local transitions for individual sources. Reduced-motion mode must preserve causality through immediate state and focus changes.

## Verify with evidence

Render a matrix containing:

- compact populated;
- expanded populated;
- partial or recoverable error;
- empty or first-use;
- enlarged text on the critical viewport;
- RTL when localized.

Check that scan order, domain identity, cross-domain relationships, primary actions, and state ownership remain clear in every capture.

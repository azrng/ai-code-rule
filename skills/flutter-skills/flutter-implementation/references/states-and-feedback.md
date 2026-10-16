# States and Feedback

## Contents

- State matrix
- Loading
- Empty and error
- Optimistic behavior

## State matrix

Consider initial, loading, refreshing, empty, populated, partial, filtered-empty, offline, permission-denied, error, disabled, destructive confirmation, and success. Implement only applicable states, but decide explicitly.

## Loading

Use progress that matches duration and certainty. Preserve existing content during refresh when possible. Skeletons should resemble real structure and avoid distracting shimmer in long waits or reduced-motion contexts.

## Empty and error

Explain what happened, whether it is normal, and the next useful action. Distinguish no data from no search results and permission failures from network failures. Keep recovery near the failure.

## Optimistic behavior

Use optimistic updates when reversal is safe and success is likely. Show pending state, prevent duplicate submission, reconcile server truth, and provide understandable rollback.


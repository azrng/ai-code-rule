# Motion

## Contents

- Purpose
- Timing and easing
- Interruption
- Performance and reduced motion

## Purpose

Every animation should communicate continuity, causality, hierarchy, state change, or feedback. Name that purpose before choosing an API.

## Timing and easing

Keep direct feedback fast, spatial transitions long enough to track, and celebratory motion rare. Use consistent curve families. Distance and complexity may adjust duration, but avoid sluggish interactions.

## Interruption

Design rapid re-taps, reversal, navigation away, disposal, and state replacement. Motion must converge on current truth rather than replay stale transitions.

## Performance and reduced motion

Prefer transform and opacity when equivalent. Bound blur, clipping, shaders, and simultaneous animations. Provide instant state change or gentle cross-fade for reduced motion while retaining necessary feedback.


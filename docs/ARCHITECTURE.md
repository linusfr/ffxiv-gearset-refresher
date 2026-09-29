# Architecture

## Boundaries

`GearsetService` owns all unsafe client calls. `RefreshRunner` contains frame
sequencing and never retains game pointers. `RefreshPlan` is pure selection
logic and has a standalone test executable.

## Workflow

Each target advances through equip, wait, recommendation setup, recommendation
equip, wait, and save phases. Delays let client state settle between calls.
Eight-second timeouts prevent silent hangs. Bulk runs restore starting gear set
after success, cancellation, or failure when possible.

`AutomaticRefresh` holds opt-in level-up and equippable-loot requests until
`RefreshRunner` can start. Requests stay tied to exact current gear set and drop
after a set switch or another refresh starts, preventing delayed updates to the
wrong set. Dalamud's interpreted item-added event excludes inventory moves.

One set per class job is deliberate. Updating every same-job set would destroy
alternate materia, weapon, or level-synced loadouts without useful extra
coverage.

## Safety

- No operation starts while logged out, in combat, or changing areas.
- Current-set refreshes may run inside duties; bulk refreshes remain blocked
  because they switch jobs.
- Bulk mutation requires UI confirmation.
- `/gearrefresh all` is itself explicit bulk confirmation for macro use.
- Runner mutates only existing gear sets; it never creates or deletes one.
- Starting set restoration uses game's checked `EquipGearset` method.
- Unsafe pointers exist only for duration of one service call.

## Known Gaps

- Gear set names use stable synthesized labels until localized UTF-8 decoding is
  verified.
- In-game testing remains required for recommendation update timing.
- Portrait warnings are left to game because same warning occurs after manual
  gear-set updates.

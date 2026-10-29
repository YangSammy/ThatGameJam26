# Stable data contracts and submission implementation plan

Goal: introduce stable item/chapter identity, separate definitions, runtime state, save records and scene bindings, then commit tape progress before presenting success.

Architecture: preserve existing controllers and authored transforms through adapters. Chapter IDs replace positional save identity; legacy snapshots remain versioned opaque payloads. Item records remain owned by their chapter rather than becoming a second mutable inventory.

- Add shared IDs, definition assets, scene bindings and runtime/save contracts.
- Register existing chapter controllers and authored item bindings without changing gameplay.
- Migrate v1 story arrays into ID-addressed v2 records in memory; retain legacy readers and original backup.
- Add result-bearing checkpoint submission; prepare a detached next tape state and save before applying it.
- Propagate recording/checkpoint save failure and prevent scene departure on failed saves.
- Document extension rules and deferred validation.

User explicitly requested implementation without validation: do not compile, run tests, enter Play or change scene transforms. No commits requested.

Implementation recorded in Docs/AI/DataContracts.md. Authored HouseScene item/chapter bindings and ParkScene photography binding through the additive offline migration. Runtime validation remains deferred.

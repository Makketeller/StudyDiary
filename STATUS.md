**Last updated:** 2026-10-07 · **Version:** pre-0.1.0 · **Tests:** 144 green · **Repo:** 106 commits, public, GPLv3.

## Does not exist yet

- The version probe, `OpenOutcome.Newer` and `OpenOutcome.Damaged`, and recovery copies: decided
  and documented, nothing written. Until then a damaged file makes `OpenAsync` throw
  `JsonException`, and a profile with only one of its two files throws `FileNotFoundException`.
- `OpenAsync` does not check that `encryption` is `"none"` (ARCHITECTURE §5).
- `StudyDiary.App` is the untouched Avalonia template.

## Known defects in committed code

None known.

## Known limits

**A shrinking ladder throws on entries above the new cap.** `IntervalForBox`
rejects the out-of-range box, so `IsReady` throws rather than returning false.
`Advance` does not: pass clamps to `MaxBox` and fail returns 1, so an out-of-range
state is quietly repaired by reviewing it and only trips on readiness. Nothing
handles this and nothing needs to yet — no ladder is user-editable and none has
shrunk. Related to DESIGN §12's open question on ladder changes; record the answer
there, not here.

**The schema-version stamp has no test.** Every save writes `CurrentSchemaVersion`, but a test
needs a file older than the app, and none can exist until the constant first moves past 1.
Write the test then.

## Decided this session

- **Every save stamps this app's `schemaVersion` on the header,** never the one it read, so a
  store opened on an older file cannot write the old number beside new content. ARCHITECTURE §5.
- **"Wrote nothing" is tested by timestamp, not bytes.** A save of unchanged content writes
  identical bytes, so tests backdate both files and check the dates survive. Pins that Open
  never writes and that a refused id writes nothing.

## Next session targets

**The unhappy path.** First decide what `Newer` and `Damaged` carry, with the recovery dialog:
it depends on DESIGN §12's question of whether the message says what broke. Then the version
probe, `Newer` and `Damaged`, and recovery copies taken and offered. Then Avalonia.

Open, not blocking:

- **Save failures from outside the app** (full disk, denied permission). DESIGN §12. Store
  methods change memory before saving, so the answer decides whether a failed change is
  rolled back or kept for a retry.
- **Keeping dev builds out of the real data folder.** Before tagging 0.1.0 (ROADMAP).

Watch for, in the unhappy path:

- A profile with only one of its two files is damage, not a crash (ARCHITECTURE §5).
- `JsonException` is the damage signal inside Data, from the reader, the probe and the mapping
  alike. Catch nothing wider. A save wraps it in `InvalidOperationException`; App never sees one.
- The probe reads `profile.json` through `WithoutByteOrderMark` and refuses a file that is only
  `null`, as `ReadAsync` does.
- Adding the probe: update `StudyDiaryJsonContext`'s summary, which says only two types are listed.
- `HoldsAnythingOfOurs` checks only the two files. Recovery copies join it when they exist; both
  Create's refusal and Open's `NoProfile` depend on it.
- Create takes the first recovery copy too (ARCHITECTURE §5).
- `_clock` is held but not read until recovery copies use it.
- A frozen test clock gives two saves the same instant: copy names must not collide.
- Read and write only through `StudyDiaryJson.Context`; await every store call and every
  `Assert.ThrowsAsync`.

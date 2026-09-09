**Last updated:** 2026-09-08 · **Version:** pre-0.1.0 · **Repo:** 44 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling` is complete and tested: `IntervalUnit`,
  `ReviewInterval`, `LeitnerLadder`, `ReviewOutcome`, `ReviewState`,
  `IReviewScheduler`, `LeitnerScheduler`.
- `StudyDiary.Domain.Entries` holds `Entry`, now tested by `EntryShould`.
  The constructor forbids a both-blank title and body (DESIGN §2).
- **Tests: xUnit v3 — 56 tests, all green. 56 green is the environment
  benchmark.**
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file.
- `LICENSE`, `README.md`, `.gitmessage`.

## Does not exist yet

- `src/StudyDiary.Data` — not scaffolded.
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

## Next session targets

**Write `StudyDiary.Data`.** The on-disk format and the store interface are both settled and
recorded (ARCHITECTURE §5) — nothing about the shape is open, so this session is code. The work is
`IEntryStore` and its JSON implementation, plus `StudyDiary.Data.Tests` round-tripping a saved
profile folder (ARCHITECTURE §2, §5; ROADMAP 0.1.0).

Decided this session, all in ARCHITECTURE §5:

- Layout is `StudyDiary/profiles/<profile>/`, lowercase, from `LocalApplicationData`. The
  `profiles/` level ships now so 0.12.0 never has to move a user's data.
- Review history nests inside its entry; `payload.json` has two top-level keys, `entries` and
  `dayLogs`, the latter written empty from the start.
- `IEntryStore` is per-entry and async. `ReviewRecord` is public; DTOs stay internal.
- Enum values keep their C# spelling on disk; camelCase applies to keys only.

Watch for, when writing the mapping:

- `EntryDto` nests a `ReviewStateDto` — box-and-entered-day is one unit on disk.
- Domain types are never serialized directly; Data maps Domain ↔ DTO by hand, both directions,
  and the round-trip test is what catches a forgotten field.
- The temp file for the atomic write goes in the same directory as its target.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name, `encryption`;
  `payload.json` carries entries, history and DayLogs.
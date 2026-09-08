**Last updated:** 2026-09-08 · **Version:** pre-0.1.0 · **Repo:** 43 commits, public, GPLv3.

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
- The four documents. `ARCHITECTURE.md`, `README.md`, `ROADMAP.md`, and this file.

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

**Scaffold `StudyDiary.Data`.** Both design questions that gated it are closed —
`Entry`'s JSON shape is decided when Data is written, and the review-history event
lives in Data as a DTO (ARCHITECTURE §5). The work is `IEntryStore`, the JSON
implementation, and `StudyDiary.Data.Tests` round-tripping a saved profile folder
(ARCHITECTURE §2, §5; ROADMAP 0.1.0).

Watch for, when writing the mapping:

- `EntryDto` nests a `ReviewStateDto` — box-and-entered-day is one unit on disk
  (ARCHITECTURE §5).
- Domain types are never serialized directly; Data maps Domain ↔ DTO by hand,
  both directions, and the round-trip test is what catches a forgotten field.
- JSON keys are camelCase; enums serialize as strings with pinned integers
  behind them.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name,
  `encryption`; `payload.json` carries entries, history and DayLogs.
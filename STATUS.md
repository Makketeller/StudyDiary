**Last updated:** 2026-09-03 · **Version:** pre-0.1.0 · **Repo:** 38 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling` is complete and tested: `IntervalUnit`,
  `ReviewInterval`, `LeitnerLadder`, `ReviewOutcome`, `ReviewState`,
  `IReviewScheduler`, `LeitnerScheduler`.
- `StudyDiary.Domain.Entries` holds `Entry`. **Committed untested** — the only
  type in the project without tests.
- **Tests: xUnit v3 — 38 tests, all green.** Unchanged this session. **38 green is
  the environment benchmark.**
- The four documents. `LICENSE`, `README.md`, `.gitmessage`, `.gitattributes`.

## Does not exist yet

- `EntryShould` — see next targets.
- `DayLog` — the second entity. Not needed before 0.9.0.
- `src/StudyDiary.Data` — not scaffolded. Due in the first release.
- `StudyDiary.App` is the untouched Avalonia template.

## Known defects in committed code

None known. `Entry` being untested is a gap, not a defect — nothing has been shown
wrong, but nothing has been shown right either.

## Known limits

**A shrinking ladder throws on entries above the new cap.** `IntervalForBox`
rejects the out-of-range box, so `IsReady` throws rather than returning false.
`Advance` does not: pass clamps to `MaxBox` and fail returns 1, so an out-of-range
state is quietly repaired by reviewing it and only trips on readiness. Nothing
handles this and nothing needs to yet — no ladder is user-editable and none has
shrunk. Related to DESIGN §12's open question on ladder changes; record the answer
there, not here.

## Next session targets

**`EntryShould`**, at `tests/StudyDiary.Domain.Tests/Entries/EntryShould.cs`,
mirroring the source layout. Worth covering:

- `Create` starts a new entry in box 1 on its creation day (DESIGN §3).
- `Create` generates a distinct id each time.
- The constructor preserves all six values it is given.
- `Guid.Empty` is rejected, and so are a null title, body or state.
- `ApplyReview` replaces the state and changes nothing else.
- Two entries with identical text are not equal — the test that proves `class`
  rather than `record`.

**Then `StudyDiary.Data`.** Both design questions that gated it are closed —
`Entry`'s JSON shape is decided when Data is written, and the review-history event
lives in Data as a DTO (ARCHITECTURE §5). Scaffolding `IEntryStore`, the JSON
implementation and `StudyDiary.Data.Tests` is unblocked (ROADMAP 0.1.0).
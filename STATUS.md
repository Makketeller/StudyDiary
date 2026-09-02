**Last updated:** 2026-09-02 · **Version:** pre-0.1.0 · **Repo:** 33 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling` is complete and tested: `IntervalUnit`,
  `ReviewInterval`, `LeitnerLadder`, `ReviewOutcome`, `ReviewState`,
  `IReviewScheduler`, `LeitnerScheduler`.
- **Tests: xUnit v3 — 38 tests, all green.** The scheduler types are now covered:
  the four DESIGN §3 transitions, readiness at the `<=` boundary, and both
  hardcoding hazards (the cap and the fail interval) checked against a
  non-default ladder. **38 green is the environment benchmark.**
- The four documents. `LICENSE`, `README.md`, `.gitmessage`, `.gitattributes`.

## Does not exist yet

- `Entry`, `DayLog` — the entities.
- `src/StudyDiary.Data` — not scaffolded. Due in the first release.
- `StudyDiary.App` is the untouched Avalonia template.

## Known defects in committed code

None known.

## Code

**`ReviewState` stores `EnteredOn`, not a derived next-review date.** Both shapes
answer readiness identically until the ladder changes: storing the derived date
means a ladder edit reaches an entry only at its next transition, while storing
the entered-day applies it to everything at once. Chosen because DESIGN §3
already states the rule that way — *next review = the day you entered the box +
that box's interval* — and because the entered-day is the primitive: `AddMonths`
clamps, so the derived date cannot be inverted back. `ReviewIntervalShould` now
proves the clamping the argument rests on.

Rejected alongside it: deriving state by replaying review history. The log
contains practice events, so replay would have to read `isPractice`, which
ARCHITECTURE forbids Domain from knowing about.

**Readiness lives on `IReviewScheduler`, not on `ReviewState`.** Under this shape
it needs the ladder, so it is a Leitner rule rather than state. Keeping it on the
scheduler is what makes the FSRS swap a swap.

**`ReviewState.Box` has no upper bound, deliberately.** The max is the ladder's
and `ReviewState` holds no ladder; `LeitnerLadder.IntervalForBox` already
range-checks. A type-level upper bound would also make every box-4 entry
unconstructable if the ladder ever shrank — saved data the app could not load.

## Known limits

**A shrinking ladder throws on entries above the new cap.** `IntervalForBox`
rejects the out-of-range box, so `IsReady` throws rather than returning false.
Nothing handles this and nothing needs to yet — no ladder is user-editable and
none has shrunk. Related to DESIGN §12's open question on ladder changes; record
the answer there, not here.

## Next session targets

**`Entry`.** The first `class` rather than `record` in the project — identity, not
contents (ARCHITECTURE §4). Carries `Guid Id`, title, body, `CreatedOn`
(`DateOnly`), `CreatedAt` (`DateTimeOffset`), tags, and its `ReviewState`.

**Two design questions are due before `StudyDiary.Data` writes a byte:**

- `Entry`'s exact JSON shape (DESIGN §12). DayLog's can wait — DayLog does not
  ship in the thin slice.
- **Where the review-history event type lives.** DESIGN §7 says the App layer
  appends it and ARCHITECTURE §4 forbids `isPractice` anywhere in Domain, so the
  type belongs in Data — but neither document names a project. Decide and record.
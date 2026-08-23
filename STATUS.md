**Last updated:** 2026-08-23 · **Version:** pre-0.1.0 · **Repo:** 26 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling`: `IntervalUnit`, `ReviewInterval`, `LeitnerLadder`
  (as before), plus the scheduler types — `ReviewOutcome` (Fail = 0, Pass = 1, pinned),
  `ReviewState` (sealed record, `Box` + `EnteredOn`, validates `Box >= 1`),
  `IReviewScheduler` (`Advance`, `IsReady`), `LeitnerScheduler` (sealed class, stateless).
- **Tests: xUnit v3** — unchanged. **13 tests, all green. None cover the scheduler types.**
- The four documents. `LICENSE`, `README.md`, `.gitmessage`.

## Does not exist yet

- `Entry`, `DayLog` — the entities.
- `src/StudyDiary.Data` — not scaffolded. Due in the first release.
- `StudyDiary.App` is the untouched Avalonia template.

## Known defects in committed code

None known — but the scheduler types ship untested, which is a gap rather than a defect.
Nothing has confirmed that `Advance` implements DESIGN §3 correctly.

## Code

**`ReviewState` stores `EnteredOn`, not a derived next-review date.** The decision STATUS
gated on last session. Both shapes answer readiness identically until the ladder changes:
storing the derived date means a ladder edit reaches an entry only at its next transition,
while storing the entered-day applies it to everything at once. Chosen because DESIGN §3
already states the rule that way — *next review = the day you entered the box + that box's
interval* — and because the entered-day is the primitive: `AddMonths` clamps, so the
derived date cannot be inverted back.

Rejected alongside it: deriving state by replaying review history. The log contains
practice events, so replay would have to read `isPractice`, which ARCHITECTURE forbids
Domain from knowing about.

**Readiness lives on `IReviewScheduler`, not on `ReviewState`.** Under this shape it needs
the ladder, so it is a Leitner rule rather than state. Keeping it on the scheduler is what
makes the FSRS swap a swap.

**`IsReadyOn` renamed to `IsReady`.** The `On` suffix reads as a claim about a date when
the method returns a bool. `NextReviewOn` keeps it — there it does mean "the day this falls
on". ARCHITECTURE's naming line updated to match.

## Next session targets

**Tests for the four scheduler types.** The four DESIGN §3 rules, one test each: pass
promotes, pass at the cap holds, fail returns to box 1 from any box, both paths anchor
`EnteredOn` to `reviewedOn`. Then readiness at the `<=` boundary — the day before, the
exact day, and long after. Plus the round-trip pair on `ReviewState` and the pinned
integers on `ReviewOutcome`.
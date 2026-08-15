# Status

> The only file that changes every session. Updated before closing a session.
>
> **Precedence.** Where an external summary, the README or anything else disagrees with the
> documents in this repo, the documents win.
>
> Companions: DESIGN.md (product decisions), ARCHITECTURE.md (stack, layout, code rules),
> ROADMAP.md (release order).

**Last updated:** 2026-08-15 · **Version:** pre-0.1.0 · **Repo:** 20 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling`: `IntervalUnit` (Day/Month/Year, integer values pinned),
  `ReviewInterval` (sealed record, `AddTo(DateOnly)`, validates `Count >= 0`),
  `LeitnerLadder` (sealed record, `ImmutableArray` of box intervals, `MaxBox`,
  `IntervalForBox(int)`, static `Default`, structural equality, construction validation).
- **Tests: xUnit v3** (`xunit.v3.mtp-v2` 3.2.2), Microsoft Testing Platform runner selected
  in `global.json`, `xunit.runner.json` beside the test project. `Scheduling/`
  mirrors the source layout. 13 tests, all green.
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file. The old single
  `Design.md` is deleted.
- `LICENSE` (GPLv3), `README.md`, `.gitmessage` Conventional Commits template.

## Does not exist yet

- `ReviewState`, `ReviewOutcome`, `IReviewScheduler`, `LeitnerScheduler` — the scheduler triad.
- `Entry`, `DayLog` — the entities.
- `src/StudyDiary.Data` — not scaffolded. Due in the first release.
- `StudyDiary.App` is the untouched Avalonia template.

## Known defects in committed code

None. The three recorded on 2026-08-10 are fixed and covered by tests.

## Docs

**All four documents are committed and `Design.md` is deleted.** The corpus is the source of
truth from here; this file records only what has changed since.

ARCHITECTURE's `init`-accessor validation rule was **wrong and is corrected**. Declaring the
property explicitly does suppress the synthesized one, but it also leaves the primary
constructor parameter unread — CS8907, a *warning*, so the build succeeds and every instance
silently stores the type's default. The rule needs two entry points, construction and `with`,
routed through a shared private static. Found by a failing round-trip test, not by review.

## Code

All three defects are fixed, each with tests written before the fix.

- `ReviewInterval` validates `Count >= 0` at both entry points. `PermitZeroCount` pins that
  zero stays legal here (DESIGN §3) against a future tidy-up to `> 0`.
- `LeitnerLadder` has structural equality — `SequenceEqual` for `Equals`, the BCL `HashCode`
  struct for `GetHashCode`. CS8851 does fire if the two are separated, so the compiler catches
  that pairing; the `HashSet` test covers it regardless.
- `LeitnerLadder` validates its array on construction: rejects `default` or empty, and any
  rung with `Count <= 0`.

## Next session targets

**The scheduler triad** — `ReviewState`, `ReviewOutcome`, `IReviewScheduler`,
`LeitnerScheduler`. Still gated on one decision: **what `ReviewState` actually holds.**
Storing `NextReviewOn` and deriving it from box plus entry-day behave differently when the
ladder changes, and no document settles it. Decide before writing the type.
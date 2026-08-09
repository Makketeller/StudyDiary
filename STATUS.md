# Status

> The only file that changes every session. Updated before closing a session.
>
> **Precedence.** Where an external summary, the README or anything else disagrees with the
> documents in this repo, the documents win.
>
> Companions: DESIGN.md (product decisions), ARCHITECTURE.md (stack, layout, code rules),
> ROADMAP.md (release order).

**Last updated:** 2026-08-10 · **Version:** pre-0.1.0 · **Repo:** 11 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling`: `IntervalUnit` (Day/Month/Year, integer values pinned),
  `ReviewInterval` (sealed record, `AddTo(DateOnly)`), `LeitnerLadder` (sealed record,
  `ImmutableArray` of box intervals, `MaxBox`, `IntervalForBox(int)`, static `Default`).
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file. The old single
  `Design.md` is deleted.
- `LICENSE` (GPLv3), `README.md`, `.gitmessage` Conventional Commits template.

## Does not exist yet

- `ReviewState`, `ReviewOutcome`, `IReviewScheduler`, `LeitnerScheduler` — the scheduler triad.
- `Entry`, `DayLog` — the entities.
- `src/StudyDiary.Data` — not scaffolded. Due in the first release.
- `StudyDiary.App` is the untouched Avalonia template.
- **Zero tests.** The tests project is scaffolded and empty.

## Known defects in committed code

Three, all the same category: committed code diverging from a document.

- **`LeitnerLadder` value-equality is broken.** `ImmutableArray<T>` compares by *reference* to
  its underlying array, so the compiler-generated record `Equals` reports two structurally
  identical ladders as unequal. Needs explicit `Equals`/`GetHashCode` (ARCHITECTURE).
- **`LeitnerLadder` has no construction-time validation**, so a `default` or empty
  `BoxIntervals` compiles and then throws something confusing on first use (DESIGN §3).
- **`ReviewInterval` has no construction-time validation.** DESIGN §3 requires `Count >= 0`;
  a negative constructs silently and moves review dates backwards. The fix belongs in an
  `init` accessor, not the constructor body — `with` bypasses the primary constructor
  (ARCHITECTURE).

## Docs

**All four documents are committed and `Design.md` is deleted.** The corpus is the source of
truth from here; this file records only what has changed since.

## Code

**Review of the committed `Scheduling` types is complete.** All three re-read and understood,
with `LeitnerLadder` rebuilt member by member in `scratch/` to confirm it rather than assume it.
The rebuild reproduced both `LeitnerLadder` defects from first principles, and surfaced the
missing `ReviewInterval` validation and the unpinned `IntervalUnit` values.

`IntervalUnit`'s integer values are now pinned, closing the ARCHITECTURE divergence. The
remaining three defects above are the next code targets.

## Next session targets

**First xUnit test.** Start with `ReviewInterval` rejecting a negative `Count` — a one-line
assertion, and it teaches the `init`-accessor pattern on the easy case before the hard one.
Then `LeitnerLadder` equality (`SequenceEqual` plus the BCL `HashCode` struct), then
`LeitnerLadder`'s construction validation, which is the same accessor pattern applied to the
harder type.

After that the scheduler triad — still gated on one decision: **what `ReviewState` actually
holds.** Storing `NextReviewOn` and deriving it from box plus entry-day behave differently when
the ladder changes, and no document settles it.
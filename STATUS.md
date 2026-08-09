# Status

> The only file that changes every session. Updated before closing a session.
>
> **Precedence.** Where an external summary, the README or anything else disagrees with the
> documents in this repo, the documents win.
>
> Companions: DESIGN.md (product decisions), ARCHITECTURE.md (stack, layout, code rules),
> ROADMAP.md (release order).

**Last updated:** 2026-08-05 · **Version:** pre-0.1.0 · **Repo:** ~8 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `tests/StudyDiary.Domain.Tests`.
- `StudyDiary.Domain.Scheduling`: `IntervalUnit` (Day/Month/Year), `ReviewInterval`
  (sealed record, `AddTo(DateOnly)`), `LeitnerLadder` (sealed record,
  `ImmutableArray` of box intervals, `MaxBox`, `IntervalForBox(int)`, static `Default`).
- `LICENSE` (GPLv3), `README.md`, `.gitmessage` Conventional Commits template.
- The old single `Design.md`, still in place and now superseded — it is deleted by the same
  commit that adds the four documents below.

## Does not exist yet

- `ReviewState`, `ReviewOutcome`, `IReviewScheduler`, `LeitnerScheduler` — the scheduler triad.
- `Entry`, `DayLog` — the entities.
- `src/StudyDiary.Data` — not scaffolded. Due in the first release.
- `StudyDiary.App` is the untouched Avalonia template.
- **Zero tests.** The tests project is scaffolded and empty.

## Known defects in committed code

- **`LeitnerLadder` value-equality is broken.** `ImmutableArray<T>` compares by *reference* to
  its underlying array, so the compiler-generated record `Equals` reports two structurally
  identical ladders as unequal. Needs explicit `Equals`/`GetHashCode` (ARCHITECTURE).
- **`LeitnerLadder` has no construction-time validation**, so a `default` or empty
  `BoxIntervals` compiles and then throws something confusing on first use (DESIGN §3).

## Docs

**All three read-throughs are complete. Nothing is committed yet.** The four documents replace
the old single `Design.md` and land together in one commit that deletes it — that commit is the
next thing due.

- **DESIGN — done.** All 13 sections. §5 gained the list-versus-score rule (a count describes a
  list; a denominator or a comparison over time is a score). §1 later gained the copyleft bullet
  and the *Why copyleft* subsection, moved out of ARCHITECTURE during that file's pass — section
  numbering was left untouched, since every other document cites DESIGN by number.
- **ROADMAP — done.** Read against DESIGN, all cross-references verified. The storage decision
  became a gate rather than a release, so everything after it shifted down one minor.
- **ARCHITECTURE — done.** Rewritten rather than patched. The four known issues are closed
  (single-file persistence, the dangling SQLite citation, the initial-delay justification, and
  the header's false claim that DESIGN never points back). Beyond those: a new §5 restates
  DESIGN §7's storage rules as Data-layer code obligations; the licence section is gone, with the
  dependency rule in §1 now naming licence compatibility; and DESIGN §2's invariant finally has
  an enforcement mechanism — the scheduler's signature never receives an `Entry`, so content
  type, review shape and tags are things it structurally cannot read.

Nothing in this pass reversed a decision, so DESIGN §13 gains no entry.

## Next session targets

Commit all four documents and delete `Design.md`. Then `LeitnerLadder`'s equality fix as the
first xUnit test, preceded by a walkthrough of what the compiler generates for a positional
record; then construction-time validation. After that the scheduler triad — which needs one thing
decided first: **what `ReviewState` actually holds.** Storing `NextReviewOn` and deriving it from
box plus entry-day behave differently when the ladder changes, and no document settles it.
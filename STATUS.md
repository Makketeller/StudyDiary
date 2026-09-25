**Last updated:** 2026-09-25 · **Version:** pre-0.1.0 · **Repo:** 71 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `src/StudyDiary.Data`, `tests/StudyDiary.Domain.Tests`,
  `tests/StudyDiary.Data.Tests`.
- `StudyDiary.Domain.Scheduling` is complete and tested: `IntervalUnit`,
  `ReviewInterval`, `LeitnerLadder`, `ReviewOutcome`, `ReviewState`,
  `IReviewScheduler`, `LeitnerScheduler`.
- `StudyDiary.Domain.Entries` holds `Entry`, tested by `EntryShould`.
  The constructor forbids a both-blank title and body (DESIGN §2).
- `StudyDiary.Data` holds `ReviewRecord`, tested by `ReviewRecordShould`, and five
  DTOs: `ReviewStateDto`, `ReviewRecordDto`, `ProfileDto`, `EntryDto`, `PayloadDto`.
  Every property on every DTO is `[JsonRequired]`.
- `StudyDiary.Data` also holds the shared JSON options: `StudyDiaryJson`, bound to the
  source-generated `StudyDiaryJsonContext`, tested by `StudyDiaryJsonShould`.
- **Tests: xUnit v3 — 100 tests, all green. 100 green is the environment
  benchmark.** No round-trip test yet; it arrives with the mapping.
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file.
- `LICENSE`, `README.md`, `.gitmessage`.

## Does not exist yet

- The Domain ↔ DTO mapping, by hand, both directions.
- `IEntryStore` and its JSON implementation. No file is read or written yet.
- Recovery copies: decided and documented, but nothing takes one.
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

## Decided this session

The shared JSON options: source generation, relaxed escaping with indentation, and a
strict reader that refuses unknown and duplicate keys, with `schemaVersion` read first.
Recorded in ARCHITECTURE §5 and DESIGN §7. DESIGN §12 gained one open question:
whether the refusal message says what broke.

## Next session targets

**The mapping, then `IEntryStore`.**

Open before `JsonEntryStore`:

- **`UpdateAsync` must merge, not replace.** Review history lives on the DTO and has no
  counterpart on `Entry`, so mapping Domain → DTO and writing the result destroys it. The
  round-trip test needs add → append a review → update → reload → assert the history survived.
- **How many recovery copies, and when they are taken** (DESIGN §12). A copy at every save loses
  nothing but copies a bug too; a copy per session survives the bug but loses the session.
- **Where the data folder path comes from.** If the store resolves `LocalApplicationData`
  itself, the tests write into the real user data folder. Injecting it is the same shape of seam
  as the App-layer `TimeProvider`.
- **Who owns `profile.json`.** The five `IEntryStore` methods are all about entries, but the
  header has to be created on first run and read before the payload.
- **How `schemaVersion` is read first.** The shared options refuse unknown keys, so reading
  the version from a newer header needs its own lenient read of that one field.
- **A file that parses but holds a bad value** — refuse the whole file, or skip that entry and
  load the rest? Refusing matches the parse-failure rule, and recovery copies now remove its main
  cost, which was the lockout.
- **Missing-id behaviour** on `UpdateAsync`, `DeleteAsync` and `AppendReviewAsync`: throw or
  no-op. Pick once, test it.

Watch for, when writing the mapping:

- Domain types are never serialized directly; Data maps Domain ↔ DTO by hand, both directions,
  and the round-trip test is what catches a forgotten field.
- Read and write only through `StudyDiaryJson.Context`, never `StudyDiaryJsonContext.Default`.
- The temp file for the atomic write goes in the same directory as its target.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name, `encryption`;
  `payload.json` carries entries, history and DayLogs.

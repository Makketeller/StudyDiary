**Last updated:** 2026-09-23 · **Version:** pre-0.1.0 · **Repo:** 58 commits, public, GPLv3.

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
- **Tests: xUnit v3 — 64 tests, all green. 64 green is the environment
  benchmark.** No test covers the DTOs; they have no behaviour, and the
  round-trip tests arrive with the mapping.
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file.
- `LICENSE`, `README.md`, `.gitmessage`.

## Does not exist yet

- The shared `JsonSerializerOptions` — camelCase keys, enums as strings.
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

This session's decisions are recorded in DESIGN §7 and §13,
and in ROADMAP 0.1.0 and 0.14.0.

## Next session targets

**The shared `JsonSerializerOptions`, then the mapping, then `IEntryStore`.**

Open before the options:

- **Reflection or source generation.** `JsonSerializer` inspects types at runtime by default,
  which is disabled under trimming and native AOT — as a scratch script proved, since file-based
  apps are AOT by default. Source generation works in both. Nothing forces the choice yet, but
  0.1.0's publishing settings do, and the store is easier to write once than twice.
- **Non-ASCII escaping.** Default settings write `å`, `ö` and `μ` as `\u00E5`-style escapes,
  which fights DESIGN §1's human-readable promise. There is an encoder setting; check the docs.

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
- **A file that parses but holds a bad value** — refuse the whole file, or skip that entry and
  load the rest? Refusing matches the parse-failure rule, and recovery copies now remove its main
  cost, which was the lockout.
- **Missing-id behaviour** on `UpdateAsync`, `DeleteAsync` and `AppendReviewAsync`: throw or
  no-op. Pick once, test it.

Watch for, when writing the mapping:

- Domain types are never serialized directly; Data maps Domain ↔ DTO by hand, both directions,
  and the round-trip test is what catches a forgotten field.
- The temp file for the atomic write goes in the same directory as its target.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name, `encryption`;
  `payload.json` carries entries, history and DayLogs.

**Last updated:** 2026-09-15 · **Version:** pre-0.1.0 · **Repo:** 50 commits, public, GPLv3.

## Exists and is committed

- Solution `StudyDiary.slnx`: `src/StudyDiary.Domain`, `src/StudyDiary.App`,
  `src/StudyDiary.Data`, `tests/StudyDiary.Domain.Tests`,
  `tests/StudyDiary.Data.Tests`.
- `StudyDiary.Domain.Scheduling` is complete and tested: `IntervalUnit`,
  `ReviewInterval`, `LeitnerLadder`, `ReviewOutcome`, `ReviewState`,
  `IReviewScheduler`, `LeitnerScheduler`.
- `StudyDiary.Domain.Entries` holds `Entry`, tested by `EntryShould`.
  The constructor forbids a both-blank title and body (DESIGN §2).
- `StudyDiary.Data` holds `ReviewRecord`, tested by `ReviewRecordShould`,
  and two DTOs: `ReviewStateDto` and `ProfileDto`.
- **Tests: xUnit v3 — 64 tests, all green. 64 green is the environment
  benchmark.**
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file.
- `LICENSE`, `README.md`, `.gitmessage`.

## Does not exist yet

- `EntryDto`, `PayloadDto`, and whatever carries a review event on disk.
- `IEntryStore` and its JSON implementation. No file is read or written yet.
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

**Finish the DTOs, then `IEntryStore`.** `EntryDto` nests a `ReviewStateDto` and carries its
entry's review history; `PayloadDto` has the two top-level keys (ARCHITECTURE §5).

Decided this session:

- `ReviewRecord` validates both box numbers at construction. History cannot be backfilled, so a
  bug writing box 0 becomes permanent garbage in a file FSRS reads years later.

Open before `EntryDto`:

- **Does the review event get its own DTO?** ARCHITECTURE bans serializing *Domain* types
  directly and `ReviewRecord` is a Data type, so the rule does not bind. Against: it validates,
  so a hand-edited `"boxBefore": 0` throws inside the deserializer rather than at the mapping,
  where the file can be named.
- **How `dayLogs` is typed before `DayLog` exists.** DayLogs are additive, so `schemaVersion`
  will not bump and 0.1.0 can open a 0.9.0 file. An empty typed list drops the journal on save;
  raw `JsonElement` reads and writes it back untouched.

Open before `JsonEntryStore`, and not yet examined:

- **`UpdateAsync` must merge, not replace.** Review history lives on the DTO and has no
  counterpart on `Entry`, so mapping Domain → DTO and writing the result destroys it. The
  round-trip test needs add → append a review → update → reload → assert the history survived.
- **Where the data folder path comes from.** If the store resolves `LocalApplicationData`
  itself, the tests write into the real user data folder. Injecting it is the same shape of seam
  as the App-layer `TimeProvider`.
- **Who owns `profile.json`.** The five `IEntryStore` methods are all about entries, but the
  header has to be created on first run and read before the payload.
- **A file that parses but holds a bad value** — refuse the whole file, or skip that entry and
  load the rest? Refusing matches the parse-failure rule; skipping reintroduces the
  save-over-partial-read failure that rule exists to prevent.
- **Missing-id behaviour** on `UpdateAsync`, `DeleteAsync` and `AppendReviewAsync`: throw or
  no-op. Pick once, test it.

Watch for, when writing the mapping:

- Domain types are never serialized directly; Data maps Domain ↔ DTO by hand, both directions,
  and the round-trip test is what catches a forgotten field.
- The temp file for the atomic write goes in the same directory as its target.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name, `encryption`;
  `payload.json` carries entries, history and DayLogs.
**Last updated:** 2026-10-01 · **Version:** pre-0.1.0 · **Repo:** 85 commits, public, GPLv3.

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
  source-generated `StudyDiaryJsonContext`, tested by `StudyDiaryJsonShould`. The reader
  refuses unknown keys, repeated keys, and a null where the type forbids one.
- `StudyDiary.Data` holds `EntryMapping`: Domain ↔ DTO by hand, both directions, tested by
  `EntryMappingShould`. Loading refuses the whole list on an impossible entry, a null in a
  list, or a repeated id, as a `JsonException` naming the entry.
- **Tests: xUnit v3 — 119 tests, all green. 119 green is the environment
  benchmark.** The round trip is in memory only; the one through a file arrives with the store.
- The four documents: DESIGN.md, ARCHITECTURE.md, ROADMAP.md and this file.
- `LICENSE`, `README.md`, `.gitmessage`.

## Does not exist yet

- `IEntryStore` and its JSON implementation: fully designed (ARCHITECTURE §5), not yet typed.
  No file is read or written yet.
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

All five questions that were open before `JsonEntryStore`:

- **Recovery copies.** One at every save; keep the newest twenty-five (a ten-card review is
  twenty saves) and the first of each of the last seven days of use. DESIGN §7.
- **Every save reads its files back before replacing them.** A failure is a bug: no
  `JsonException` leaves a save, and the app stops with a plain message, never a vanishing
  window. DESIGN §7, ARCHITECTURE §5.
- **The store is handed its profile folder and a `TimeProvider`.** App resolves
  `LocalApplicationData` with `SpecialFolderOption.Create`; Data owns every name; all folder
  names lowercase, `studydiary` included. ARCHITECTURE §5.
- **Data owns `profile.json`.** `OpenAsync` and `CreateAsync` factories behind a private
  constructor; open reports outcomes, not exceptions. Every save writes the header first, a
  reversal (DESIGN §13). ARCHITECTURE §5.
- **The version is read first by a lenient probe.** Versions start at 1, and `schemaVersion`
  never moves, renames or retypes. DESIGN §7, ARCHITECTURE §5.
- **A wrong id throws before anything changes.** `KeyNotFoundException` for a missing one,
  `InvalidOperationException` for a duplicate add. ARCHITECTURE §5.
- **No Avalonia spike.** The store comes first; Avalonia follows it.

## Next session targets

**`IEntryStore` and `JsonEntryStore`, happy path.** Type the interface with its `<exception>`
docs, the layout class, and the test project's fixed clock. Then `CreateAsync`, `OpenAsync` on a
good profile, and the five methods: header-first atomic write with read-back, and the file
round trip.

After that, the unhappy path: the version probe, newer and damaged outcomes, and recovery copies
taken and offered.

Open, not blocking the happy path:

- **Save failures from outside the app** (full disk, denied permission). DESIGN §12.

- **The shape of `OpenAsync`'s outcome:** opened, no profile here, newer, damaged. Decide before
  the unhappy path.
- **Keeping dev builds out of the real data folder.** Before tagging 0.1.0 (ROADMAP).

Watch for, when writing the store:

- `UpdateAsync` passes the replaced DTO's history to `EntryMapping.ToDto`. The file round-trip
  test needs add → append a review → update → reload → assert the history survived.
- `JsonException` is the damage signal inside Data, from the reader, the probe and the mapping
  alike. Catch nothing wider. A save wraps it in `InvalidOperationException`; App never sees one.
- A file that is only `null` deserializes to `null` without throwing; treat it as damage. The
  probe covers the header; the payload read must check too.
- `Assert.Equal` on a `DateTimeOffset` compares the instant, not the offset, so a lost
  `+02:00` would pass. The file round trip should compare `.Offset` too.
- Read and write only through `StudyDiaryJson.Context`, never `StudyDiaryJsonContext.Default`.
- Temp files go in the same directory as their targets; the header is renamed before the payload.
- Check an id before changing anything: a refused call leaves memory, files and copies untouched.
- Await every store call and every `Assert.ThrowsAsync`; an un-awaited task swallows the throw.
- The fixed test clock fixes the time zone too, or "first copy of the day" depends on the machine.
- A frozen test clock gives two saves the same instant: copy names must not collide.
- A UTF-8 BOM at the start of a hand-edited file: test whether the reader skips it. It is not
  damage.
- Adding the probe: update `StudyDiaryJsonContext`'s summary, which says only two types are listed.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name, `encryption`;
  `payload.json` carries entries, history and DayLogs.

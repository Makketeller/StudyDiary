**Last updated:** 2026-10-01 · **Version:** pre-0.1.0 · **Repo:** 79 commits, public, GPLv3.

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

- **A null where the type forbids one is damage.** The reader sets
  `RespectNullableAnnotations`. It cannot see a null inside a list (the mapping refuses
  those) or a file that is only `null` (the store must). ARCHITECTURE §5.
- **The mapping.** History is a required argument of Entry → DTO, so no update can erase
  it. An impossible entry, a null in a list or a repeated id refuses the whole file,
  because skipping one is a partial load. The refusal is a `JsonException` naming the entry;
  only `ArgumentException` is caught, so a bug still crashes. ARCHITECTURE §5, DESIGN §7.

## Next session targets

**`IEntryStore` and `JsonEntryStore`.** Undecided: a throwaway Avalonia spike in `scratch/`
first, to tackle the biggest unknown early, or after the store.

Open before `JsonEntryStore`:

- **How many recovery copies, and when they are taken** (DESIGN §12). A copy at every save loses
  nothing but copies a bug too; a copy per session survives the bug but loses the session.
- **Where the data folder path comes from.** If the store resolves `LocalApplicationData`
  itself, the tests write into the real user data folder. Injecting it is the same shape of seam
  as the App-layer `TimeProvider`.
- **Who owns `profile.json`.** The five `IEntryStore` methods are all about entries, but the
  header has to be created on first run and read before the payload.
- **How `schemaVersion` is read first.** The shared options refuse unknown keys, so reading
  the version from a newer header needs its own lenient read of that one field.
- **Missing-id behaviour** on `UpdateAsync`, `DeleteAsync` and `AppendReviewAsync`: throw or
  no-op. Pick once, test it.

Watch for, when writing the store:

- `UpdateAsync` passes the replaced DTO's history to `EntryMapping.ToDto`. The file round-trip
  test needs add → append a review → update → reload → assert the history survived.
- `JsonException` is the one damage signal, from the reader and the mapping alike. Catch
  nothing wider: anything else is a bug and must not trigger the recovery offer.
- A file that is only `null` deserializes to `null` without throwing; treat it as damage.
- `Assert.Equal` on a `DateTimeOffset` compares the instant, not the offset, so a lost
  `+02:00` would pass. The file round trip should compare `.Offset` too.
- Read and write only through `StudyDiaryJson.Context`, never `StudyDiaryJsonContext.Default`.
- The temp file for the atomic write goes in the same directory as its target.
- The header/payload split: `profile.json` carries `schemaVersion`, id, name, `encryption`;
  `payload.json` carries entries, history and DayLogs.

**Last updated:** 2026-10-12 · **Version:** 0.1.0, released · **Tests:** 243 green · **Repo:** 201 commits, public, GPLv3.

## Does not exist yet

- Editing an entry (0.2.0), tags (0.3.0), and everything after them in ROADMAP.
- The test that loads the 0.1.0 golden profile. The files are saved in
  `tests/StudyDiary.Data.Tests/Golden/0.1.0/`; the test arrives with 0.4.0.

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

**The schema-version stamp has no test.** Every save writes `CurrentSchemaVersion`, but a test
needs a file older than the app, and none can exist until the constant first moves past 1.
Write the test then.

**Copies are dated by the wall clock.** A folder name records local time and no offset, so in
the hour repeated when clocks go back, a later copy can sort before an earlier one: pruning may
keep the wrong one of the two, and Open may offer the older. Once a year, for one hour.

**Restore does not check that a copy belongs to its profile.** With one profile the mistake
cannot happen; profiles (ROADMAP) add the check.

**Nothing stops two saves overlapping.** Add, delete and review move the screen on before
saving, so a fast second action can start a save while the last is still running, and
`JsonEntryStore` has no guard. Out of reach by mouse; keyboard review in 0.5.0 makes it likelier.

**A save that fails for an outside reason meets the last-resort message** (DESIGN §7, decided
for 0.1.0). Everything it says stays true, since the file is unchanged, but its details are
labelled for a bug report. Separate wording is deferred (DESIGN §11).

## Decided this session

0.1.0 built and released for Linux and Windows; Windows smoke-tested on a dual-boot install.
The decisions are in ARCHITECTURE §5–6 and DESIGN §2, §4 and §7:

- **Alternatives are one property; neighbours are several.** The window holds one `Current`
  screen, and the diary holds its review the same way.
- **Questions are asked in the window,** as view-model state that holds what it asks about.
- **Restore reaches the damaged screen as an offer from `App`,** so the JSON store is named only
  in the composition root.
- **A session serves the longest-ready half and a random half, shuffled;** `NextReviewOn` is on
  `IReviewScheduler` for it. An untitled entry is reviewed as a Note. Review is always available.
- **A review saves its history before the entry.**
- **The blank-entry rule lives on `Entry.IsBlank`,** asked by the constructor and the Add button.
- **A Debug build keeps its own data folder,** `studydiary-dev/`.
- **An outside save failure meets the last-resort message** in 0.1.0; separate wording deferred.
- **A provisional blue look,** every colour in `App.axaml`.
- **App.Tests fakes the store by hand and compiles Data.Tests' clock** rather than copying it.

## Next session targets

**Use it.**

**Then 0.2.0, edit an entry** (ROADMAP): the first live test of absent-reads-as-default, since
0.1.0's files have no `modifiedAt`.

Open, not blocking:

- **The work laptop** blocks unsigned downloads outright. Asking IT to add the .NET 10 SDK to the
  company portal, to run from source with `-c Release`. ARCHITECTURE §3 rejects code signing on
  cost; a managed machine removing *Run anyway* entirely is a new fact if that is revisited.

## Understanding debt

Steps 1–7 of the unhappy path were both typed and pasted rather than only typed, and are about
half understood. That code ships in every binary, so it comes first. In the app, the tests from
the damaged screen on, and any others pasted, were pasted rather than typed; tests do not ship,
but they are what proves the rules. Also, split `JsonEntryStoreShould` (about 840 lines) by
topic, and consider moving the four recovery-copy methods out of `JsonEntryStore`.

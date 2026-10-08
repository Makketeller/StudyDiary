**Last updated:** 2026-10-08 · **Version:** pre-0.1.0 · **Tests:** 207 green · **Repo:** 144   commits, public, GPLv3.

## Does not exist yet

- `StudyDiary.App` is the untouched Avalonia template: no clock seam, no screens, no dialogs for
  the open outcomes, no last-resort handler.
- Packaging for Linux and Windows.

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

## Decided this session

The unhappy path, all of it in DESIGN §7 and ARCHITECTURE §5:

- **Open reads the version on its own first** (the probe), answers `Newer` for a later file
  carrying both format numbers, and refuses any `encryption` but `"none"`.
- **Every refusal names its file** as a `RefusedFileException`, a missing file included, and
  becomes the `DamageDetail` the dialog shows collapsed, its line counted from 1.
- **Damage keeps both files as found** in `damaged/<time>/`, never deleted and not counting as
  a diary; with neither file there, nothing is kept and the dialog drops that sentence.
- **A recovery copy is taken after every save** into `recovery/<time>/`, aged by its name, its
  counter one past the highest that second; pruning keeps the newest 25 and the first of each
  of the last 7 days, and deletes nothing it cannot tell it made.
- **Open offers the newest copy that passes the full check,** so a newer build's copy is passed
  over; copies count as a diary living here, so Create refuses over them.
- **Open looks, Restore touches:** Restore re-checks the copy and writes it through the
  ordinary save.

## Next session targets

**The app for 0.1.0, aiming for about ten hours, bare and functional.** Start with a spike in
`scratch/`: one window, one button, one list. If that runs long, re-plan before building. Then
the entry list newest-first and adding an entry; delete with a confirm; review (ready pool,
cap 10, reveal, pass or fail); the open outcomes as panels in the main window; the last-resort
handler; packaging. Every rule stays below the UI, where the tests reach. Back to typing every
line, in smaller pieces.

What App needs from Data: `OpenAsync` and its four outcomes, `CreateAsync` after `NoProfile`,
`RestoreAsync` after the user accepts a copy, the `IEntryStore` methods, and
`InvalidOperationException` meaning a bug for the last-resort handler.

Open, not blocking:

- **Save failures from outside the app** (full disk, denied permission). DESIGN §12. Store
  methods change memory before saving, so the answer decides whether a failed change is rolled
  back or kept for a retry. It now includes a copy or prune failing *after* the renames: the
  change is saved, yet the exception reaches App as though it were not.
- **Keeping dev builds out of the real data folder.** Before tagging 0.1.0 (ROADMAP).

Watch for, in the app:

- `Damaged.KeptAt` and `Damaged.NewestPassingCopy` may each be null, and each null has its own
  wording (DESIGN §7).
- The details' line counts from 1 and the reader's message from 0; the dialog says so.
- The `switch` over `OpenOutcome` needs a default arm that throws (ARCHITECTURE §5).

## Understanding debt

Steps 1–7 of the unhappy path were both typed and pasted rather than only typed, and are about half understood.
After 0.1.0, Make sure its understood. Also,  split `JsonEntryStoreShould` (about 840 lines) by
topic, and consider moving the four recovery-copy methods out of `JsonEntryStore`.

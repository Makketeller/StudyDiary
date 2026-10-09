# Roadmap

> Release order and what each release contains. Changes per release.
>
> **Reference rule:** this file may point at DESIGN.md and ARCHITECTURE.md. Neither points back.
> DESIGN.md is cited with section numbers, because it changes rarely and its numbering is stable;
> ARCHITECTURE.md is named without them, because it changes when the code does. If a paragraph
> here is arguing *why a product rule exists*, it belongs in DESIGN.md; this file says *when*.
>
> Versioning is SemVer with the 0.x relaxation: below 1.0 anything may still change.
> **PATCH** = bug fix only. **MINOR** = one user-visible capability, one feature branch.
> **1.0.0** = the version you'd hand a stranger.
>
> Two deliberate exceptions to one-capability-per-minor, each finishing one theme in one place:
> 0.4.0 carries all of data handling, and 0.6.0 everything about choosing what to study — free
> practice, filtering and basic search.
>
> Minor is an integer, not a decimal: 0.9.0 → 0.10.0 → 0.11.0.
>
> The app version and the data file's `schemaVersion` are separate counters that move together:
> every minor and major release bumps `schemaVersion`, and a patch never does (DESIGN §7).
>
> Order is dependency-ordered, not contractual.

---

## The thin slice

### 0.1.0 — Usable minimum. Linux and Windows.

Add an entry (title + body, plain text), delete with confirm, browse newest-first, review the
ready pool in sessions of up to 10 (reveal → pass/fail). JSON persistence in the per-user data
folder with Guid ids and atomic writes. Self-contained single-file executables for `linux-x64`
and `win-x64`, with a `.desktop` entry alongside the Linux binary so double-click works there
too. No editing, no styling.

**The cap is a stopping point, not a limit** (DESIGN §4). When a session ends and something is
still ready, "keep going" starts another session of the same size, and "done for now" stops at
any point without recording or counting anything. The end of a session offers "keep going" or
not; it never says how much is left.

Scaffolds `StudyDiary.Data` (`IEntryStore` + the JSON implementation) and `StudyDiary.Data.Tests`
(round-trip a saved file, and refuse a hand-damaged one). Establishes the
 `profile.json` + `payload.json` file shape (DESIGN §7), the App-layer
 `TimeProvider` seam that supplies "today", `CreatedOn`/`CreatedAt` on `Entry`, and
pinned integer values on `outcome`. Each of those is nearly free now and a migration later.

`profile.json` carries all four header fields from this release: `schemaVersion`, the profile id,
the profile name, and `encryption` (`"none"` in MVP). Nothing reads `encryption` yet and writing
it is one line — but it is what makes DESIGN §8's deferral genuinely free. Every version that ever
ships then knows to check the field before parsing `payload.json`, so an app meeting an encrypted
payload says so rather than failing on bytes that are not JSON. Added later, every release before
it lacks that check.

**Review history is recorded from this release** — `reviewedOn`, `outcome`, `boxBefore`,
`boxAfter`, `isPractice` (shape and reasoning in DESIGN §7). `isPractice` is always `false` until
free practice ships at 0.6.0; it is in the schema from the start so that release adds no format
change. Nothing in 0.1.0 reads any of it back: the review screen only needs the current box.

**Recovery copies are taken from this release** (DESIGN §7). The app keeps its own copies of the
profile's two files in `recovery/`, and when a profile will not load it offers the newest copy
that does: one dialog, shown only when something is wrong. The damaged file is copied aside
before the question is asked, and if no copy loads the app says so rather than opening an empty
profile.

Here rather than later because this is the first release that refuses a file it cannot load, and
refusal with no way back is a lockout: the notes are intact and a non-technical user cannot reach
them. A copy that was never taken cannot be offered, so taking them starts with the first write,
even though everything else about recovery waits for 0.4.0.

**Open before tagging this:**

- How a development build is kept out of the real data folder. Once 0.1.0 is in daily use, a
  debug run of the next release opens the same diary.
- What the app says when a save fails for a reason outside it — a full disk, a denied
  permission (DESIGN §12, due before the first release).

**Save a golden file when tagging,** and at every release after: one file written by that
version, kept for the test 0.4.0 builds. Saved at the tag it costs a minute; collected later, it
means rebuilding old tags. Like the `schemaVersion` bump from 0.2.0, it is part of cutting a
release.

*Windows caveat:* the extra binary is one more `dotnet publish` line, but a Windows build that has
never been launched on Windows is a claim, not a release. Either smoke-test it in a VM before
tagging or mark it explicitly untested. Path handling must use `Path.Combine` throughout —
`LocalApplicationData` already resolves correctly per OS.

*Done when:* write an entry, close, reopen tomorrow, pass it, and it returns in a week. And: delete
a key from `payload.json` by hand, reopen, and the app offers a offers a copy instead of crashing
or opening empty. And: with more than ten ready, "keep going" offers more, and
stops offering once nothing is ready.

---

## Content — makes it worth writing in

### 0.2.0 — Edit an entry.

Retires the delete-and-retype workaround. Adds `modifiedAt` (additive,
so older files simply lack it). First, smallest, highest-relief
feature — and deliberately the first live test of DESIGN §7's
absent-reads-as-default policy: files written by 0.1.0 have no `modifiedAt`, and absent must read
as "never edited" rather than throw.

*Not pre-added at 0.1.0, unlike `isPractice`.* The review-history event shape is settled (DESIGN
§7), so writing four of its five fields would diverge from a decided shape. Nothing similar
obliges `modifiedAt` early: a field no code can write has undefined semantics, and exercising
absent-→-default here — on the cheapest field in the format, where getting it wrong costs nothing
— is worth more than the consistency.

**The first release to bump `schemaVersion`,** to 2 (DESIGN §7). From here every minor bumps it
and nothing enforces that: a missed bump reopens the hole DESIGN §13 closed on 2026-09-22. Make
the bump part of cutting every release.

### 0.3.0 — Tags.

Free-form, many-to-many labels: `#chemistry`, `#german`, `#thermodynamics`. Non-exclusive, unlike
a deck — one entry is `#chemistry` *and* `#exam-january` without existing twice (DESIGN §10).

**Placed here, early, on purpose.** DESIGN §10 says tags must land early because every entry
written before tags exist is an untagged entry, and retro-tagging a year of notes never happens.
Two releases of untagged entries is a backlog that drains through ordinary use.

Tagging is available wherever an entry is: the browse screen from 0.1.0, the editor from 0.2.0,
and mid-review. It is not a mode you enter, it is one more thing you do to an entry you are
already looking at.

`Entry` gains its tag set here, not at 0.1.0. Additive (DESIGN §7):
absent reads as the empty set — which is exactly what every entry written before
this release is. Unlike 0.13.0's Card-vs-Note default, there is no wrong answer available here.

Filtering itself arrives with 0.6.0, together with free practice.

## Foundations — data finished, and a window worth opening

### 0.4.0 — Your data, finished.

Everything about keeping the diary safe and moving it in and out, in one release, so that later
releases plug into it rather than reopen it. Deliberately large: the files are the one thing the
app cannot afford to get wrong, and DESIGN §7 makes trivial backup and even easier restore an
explicit MVP goal.

There are two dangers, and 0.1.0 covers only the first. A bug in the app or a damaged file is
what recovery copies are for. A dead disk or a lost laptop takes the recovery copies with it,
because they sit on the same disk (DESIGN §7: recovery copies are not a backup). This release is
the answer to the second.

**Getting data out and back in.**

- **"Reveal my data"** opens the profile's folder in the system file manager (DESIGN §7).
- **"Back up now"** writes a backup wherever the user chooses. A backup is the profile's folder,
  not a list of named files, so whatever later releases add to that folder — images at 0.11.0 —
  is in it without touching backup again. Its layout carries the profile from the start, for the
  same reason `profiles/default/` exists from 0.1.0: profiles at 0.14.0 must not change it.
- **"Restore this backup"** replaces the current profile's contents. Destructive by intent, so
  it confirms, **names the profile being replaced**, and first copies that profile's folder to a
  `pre-restore/` folder beside the profiles. There is no cloud to recover from; that copy is the
  only thing that makes a misclick undoable. Pre-restore copies are kept and **never removed
  automatically**.
- **Restore checks the backup before touching anything,** as restoring a recovery copy already
  does (ARCHITECTURE). A backup that will not load is refused, and the current profile stays as
  it was.
- **Import copies, never moves.** Moving would delete the user's only backup from where they put
  it. "Move" may exist later as an explicit, clearly-labelled option; restore must never be able
  to destroy the source it restored from.
- **Restoring never merges.** It replaces. Merging is sync, and sync is 2.0.
- **A readable export:** one `.md` file per entry — title, created date, body — that opens in any
  text editor. The JSON is readable but not pleasant across hundreds of entries; this is what
  makes "easy to leave" real. For reading, not keeping: no box state and no review history,
  which is what a backup is for. Export only — importing a folder of files is a different problem
  and stays with Interop, beyond 1.0.

Bringing a file in means the app asks what the user meant, in the user's words rather than the
format's (DESIGN §7). Here there is one answer, restore. "Add as a new profile" joins the same
question at 0.14.0 and a share file at 0.15.0.

Three things leave the app, on three different buttons: a backup is everything, the readable
export is entries for reading, and a share file (0.15.0) is entries for someone else. None of
them is a checkbox on another's dialog — "I unticked the wrong box" is how someone mails out
their journal.

**Automatic protection.**

- **Automatic backups to a folder the user chooses,** keeping several older versions. A button
  only protects someone who remembers to press it. Pointing the folder at a synced service is
  the user's call; the app never talks to a network itself, so DESIGN §1 holds either way.
  Finding a backup again is the app's job, not a hunt through folders.
- **A failed backup says so plainly** — the folder is missing, the USB stick is not plugged in —
  as a statement of what went wrong. How it says so without becoming a number that grows is
  open (below).

**Better recovery.**

- **Choosing among recovery copies,** rather than being offered only the newest.
- **Bringing back one entry** from a recovery copy or a backup, with its review history, without
  restoring the whole profile. Rescues a deletion noticed days later without throwing away
  everything written since. Not a merge: the entry no longer exists, so nothing can conflict.
  Chosen over a trash can, which would put a "deleted" marker in the file that every list and
  the ready pool must remember to skip — and forgetting once puts deleted entries into review.
- **A copy before an upgrade.** Every release bumps `schemaVersion` (DESIGN §7), so the first
  save by a new version locks every older version out of the file; if that version has a bad
  bug, there is no going back with your notes. When a profile written by an older version
  opens, the app copies it before its first save, and that copy is never removed
  automatically. Recovery copies do not cover this: within about a week of daily use, pruning
  has removed every copy in the old format.
- **Comparison on load.** A hand-edit that still parses — an entry cleanly deleted, say — passes
  every check a load makes, and only comparing against the app's own copy would notice. It can
  report the difference but not undo it, because the app cannot tell a mistake from a
  deliberate edit.
- **The clock-change hour.** Copy folders are named by local wall-clock time, so in the hour
  repeated when clocks go back, a later copy can sort before an earlier one. Fix it for recovery
  copies, and do not repeat it in backup names.

**Safety while the app runs.**

- **One copy of the app per profile.** Two windows on the same diary each hold their own version
  in memory, and whichever saves last silently wipes out the other's changes. While a profile is
  open, the app holds a lock on a file in its folder, and a second copy says the diary is
  already open. The lock must be one the operating system releases when the app exits, crash
  included: a lock that is just a file existing would survive a crash and lock the user out of
  their own diary — the lockout DESIGN §7 exists to prevent. The lock file joins DESIGN §7's
  list of what a profile folder holds, and backups leave it out.
- **Brief locks on Windows.** Antivirus and indexing can hold a just-written file for a moment,
  so a rename fails with nothing actually wrong. Retry briefly before reporting a failure.
- DESIGN §7's "no watching, no polling" for edits made while the app runs stays as decided. The
  lock covers a second copy of the app, which is the case that actually happens.

**One screen for all of it.** "Your data": where the folder is, the backup folder and how the
last backup went, the buttons above, and the size of each folder the app never deletes from —
`damaged/`, `pre-restore/` and the pre-upgrade copies. Those grow forever by design (DESIGN §7),
so showing their size lets the user clean up by hand, when they choose. A size on disk is not a
score; DESIGN §5 is not touched.

**Written down.** A description of every key in `profile.json` and `payload.json` and what its
values mean, so the diary can be read without the app — from a short Python script in ten
years, say. 1.0.0's written compatibility guarantee is built on it.

**Proved by tests.**

- **Golden files:** one saved file per released version from 0.1.0 on, kept in the test project
  forever, each asserted to still load. Easiest if each is saved when its release is tagged;
  collected later, it means rebuilding old tags. This is the test that proves DESIGN §7's
  migration policy is real.
- **A crash at every step:** simulate the app dying at each step of a save, a copy, a backup and
  a restore, and assert the profile still opens every time.
- **Across platforms:** a backup made on Linux restores on Windows.

**Open before building this.** Each is a DESIGN decision, made there first:

- **Deleting old automatic backups.** DESIGN §7 lets the app delete its own older recovery
  copies and nothing else, and keeping several versions in a user-chosen folder means deleting
  old ones from it. One candidate: the app writes only into a subfolder it creates inside the
  chosen folder, and deletes only there. Either way DESIGN §7 changes, with a §13 entry, rather
  than stretching the exception to fit.
- **How a failed backup is reported** without a growing number such as "last backup 12 days
  ago" (DESIGN §5).
- **Where app-wide settings live** (DESIGN §12). The backup folder is the first setting that
  needs a home.
- **When automatic backups run:** on close, once a day, or after some number of saves.
- **A folder or a single zip file.** DESIGN §7 calls a backup a copy of the folder. A zip is one
  file to move around, and needs no dependency: the standard library reads and writes zip files.
- **What a backup carries:** `recovery/` (already open in DESIGN §12) and `damaged/` alike.
- **Where pre-upgrade copies live:** inside the profile like `recovery/`, or beside the profiles
  like `pre-restore/`.
- **Bringing back an entry that still exists:** offer only entries that are gone, or also an
  older version of one that is not.
- **Where the format description lives,** given the reference rules between the four documents.

*Done when:* delete the whole data folder — the dead disk — restore from the automatic backup,
and everything is back. Starting the app twice gives one window and one "already open" message.
A profile from 0.3.0 opened in 0.4.0 leaves a pre-upgrade copy behind.

### 0.5.0 — Look and feel foundations.

The first real care for the GUI, placed early because the app is used every day from 0.1.0. It
lays the foundations every later screen inherits and leaves the final polish to 0.16.0: free
practice and filters, the editor, maths preview, images, DayLogs and the profile picker all
arrive after this, so styling every screen in detail now would mean doing much of it twice.

- **One shared look, defined in one place.** Colours, fonts, sizes and spacing set once and used
  by every screen, including screens built later. A new screen should look right without being
  styled by hand.
- **Start from Avalonia's built-in theme.** A theme library is a dependency like any other and
  has to survive the maintenance-cost rule before it is added.
- **Review without the mouse.** Keys for starting a session, reveal, pass, fail, "keep going"
  and "done for now", so a whole session runs from the keyboard. The rest of the shortcuts stay
  with 0.16.0.
- **Comfortable reading.** Text size and line length chosen for reading long entries, not only
  for fitting the window.
- **Follows the system's light or dark setting.**
- **Remembers the window's size and position,** in the settings home 0.4.0 decides — one reason
  this release comes after that one.
- **Focus lands where the next action is:** the title box when adding an entry, the reveal
  button in review.

*Not here:* first-run empty states, fuller search, the rest of the shortcuts, and testing on
someone who is not the author — all 0.16.0.

*Done when:* a full review session runs without touching the mouse, and a screen added in a
later release looks right with no styling of its own.

## Review behaviour — makes it a study tool rather than a notebook

### 0.6.0 — Free practice and filtering.

Placed before the content releases on purpose: in the middle of a course, "just this subject" is
the first thing worth wanting, and none of it waits on markdown. All it needs is tags from 0.3.0
and the `isPractice` field that has been in the schema since 0.1.0.

**Free practice** on any entry, ready or not. It does not move boxes and does not reschedule
anything, and it is enforced by *absence* — no practice-mode flag exists anywhere in the domain
(DESIGN §4). Practice reps are appended to review history with `isPractice: true` and
`boxBefore == boxAfter`. The field already exists, so this release starts writing `true` and
changes no format.

**Foot-in-the-door:** when the ready pool is large, the session opens with a tiny first batch
(~2) rather than the full cap (DESIGN §4). "Keep going" and "done for now" have existed since
0.1.0.

**Filtering, wherever there is a list to narrow:**

- **The diary list** — find entries.
- **Free practice** — drill exactly the entries chosen.
- **The ready session** — tag filters, opt-in, starting unfiltered every time; all subjects
  mixed is the default, deliberately (DESIGN §4). A filter here is never remembered, because a
  remembered filter is a quiet default.
- **The readable export from 0.4.0** — export one course rather than everything.

What a filter can ask, at this release:

- **Tags:** has this tag, any of these, all of these, not this one — and untagged, as a filter
  someone chooses, never as a number.
- **Created date:** today, this week, a range, older than some age, one particular day. It reads
  the entry's created-day; it is not a tag and does not reuse the tag mechanism.
- **Edited date,** from 0.2.0's `modifiedAt`; absent reads as never edited.
- **Words in the title or body** — basic search. Search and filtering ask the same question,
  "which entries match?", so the simple form ships here rather than waiting for 0.16.0.

Filters combine (and, or, not), clicking a tag on any entry filters by it, and a filter that is
on always says so on screen. Free practice can sort the result or draw a random handful.

Filters that depend on later releases join as those arrive: content type, the DayLog's day,
review shape and archive. Saved filters and tag hierarchy stay beyond 1.0.

All of it is serving logic in the App layer. It never touches scheduling or the definition of
"ready", and the domain never learns that filtering exists (DESIGN §4). **No filter ever shows
how many of its entries are ready** — "#physics (12 ready)" is the backlog as a number (DESIGN
§4 and §5).

**Open before building this:**

- **Filters on review history** — never reviewed, failed recently, failed repeatedly, by box,
  ready soon. They are lists rather than counts, so DESIGN §5's "a list is not a score" may
  allow some; but "box 1" and "failed this week" read as a list of failures, and "ready soon"
  is a forecast of work. Decide in DESIGN §5 which, if any, exist and where.
- **DESIGN §4 describes filtering for sessions only.** The diary list, the export and word
  search extend it; record them there first.
- **How the tiny first batch hands over to "keep going"** (DESIGN §12). A small step after it
  puts a second number in the app; a full session after it does not.

*Done when:* drill every `#physics` entry without moving a box; review only `#physics` in the
ready session, and the next session starts unfiltered.

## Content, continued

### 0.7.0 — Markdown + formatting toolbar.

Markdown becomes the body format: bold, italic, headings, lists, links, fenced code blocks
rendered monospace. A toolbar and keyboard shortcuts wrap the selection in the corresponding
syntax (DESIGN §9 for why not WYSIWYG).

Renderer survey between `Markdown.Avalonia` (MIT) and `CodeWF.Markdown` (Avalonia 12 + Markdig);
both are small community projects, so weigh them properly against the maintenance-cost rule.

*Plain text is valid markdown*, so entries written in 0.1.0–0.6.0 need no migration.

### 0.8.0 — LaTeX and chemistry.

Inline and block maths embedded in markdown with live preview beside the input. **mhchem support
is a hard selection criterion for the renderer** (DESIGN §9).

Note the coupling: whichever markdown renderer 0.7.0 chose may already bundle a maths
integration, so evaluate the two together even though they ship separately.

### 0.9.0 — Insertion palette.

Searchable panel of common LaTeX and mhchem commands; clicking inserts at the cursor with the
cursor parked in the first blank. Extends the 0.7.0 toolbar rather than introducing a second
mechanism.

### 0.10.0 — Syntax-highlighted code blocks.

Fenced code with per-language highlighting. Separate from 0.7.0 because highlighting means
another dependency (typically AvaloniaEdit / TextMate grammars) and it is pure polish on
something that already works.

### 0.11.0 — Images.

Attach and display. Completes the MVP content set. Storage is an `attachments/` folder beside
the two JSON files (DESIGN §7).

This is the release where data first lives **outside the payload**. The data folder was already
the unit of backup from 0.1.0, but until now copying `payload.json` alone happened to work; from
here a partial copy loses images silently and nothing warns you. Check the wording in the UI and
the README against that.

DESIGN §7's attachment rules all land here, and none of them are optional:

- Stored as a generated id plus the original extension (`a3f2c9d1.png`), resolved relative to the
  data folder — never an absolute path, never the user's filename.
- The original filename is kept **as a display label only**; nothing ever resolves through it. A
  missing-file message shows both, recognisable name first.
- **Never shared between owners.** Pasting the same image into two entries writes two files with
  two ids. No deduplication, no reference counting.
- **Orphans are tolerated, not collected.** Deleting an entry leaves its images behind; nothing
  deletes an attachment automatically, ever.
- A missing attachment renders as a visible placeholder in that one entry and changes nothing
  else. The entry keeps its reference and the payload is never rewritten to "clean up" a file the
  user may be about to restore.

**0.4.0's machinery meets attachments here.** A backup is the whole profile folder, so
`attachments/` should travel with no change — prove it with a backup-and-restore test that
includes an image rather than assume it. Recovery copies are the two JSON files only (DESIGN §7),
which is safe because nothing ever deletes an attachment; bringing back one deleted entry finds
its images still on disk for the same reason. The readable export decides here whether it copies
images beside its files, and says so if it does not. The "Your data" screen adds `attachments/`
to the folders whose size it shows, since orphaned images grow forever too.

Attaching a PDF and opening it in the system viewer belongs here too (DESIGN §9); inline PDF
rendering is deferred.

### 0.12.0 — DayLog.

Optional free-form writing attached to a calendar day — **any number per day**, per profile
(DESIGN §8). Never mandatory; most days may have none. During review, an optional toggle shows the
DayLogs for the entry's created-day **after you answer or reveal**, never before — a pre-answer
reveal can hand over the answer.

The day is the unit that gets surfaced, not the post: "the log for 3 March" is every DayLog with
that created-day, in the order written. So each post carries its own id, a created-day and a
timestamp that orders posts within the day — and possibly a title, which DESIGN §12 leaves open
(a blog-like model implies one, a diary implies not). All of that is part of §12's per-entity
schema question and has to be settled before this release writes a byte.

Independent of everything around it, which is why it lands before the multi-user work rather than
after. Placed *after* the content releases, though, DESIGN §8's "same markdown container as an
Entry" costs nothing: markdown, maths, code blocks and images all work in a journal post because
0.7.0–0.11.0 already built the one content pipeline and the one renderer. A DayLog is not a
reduced journal format sitting beside the real one — and it is only free because of where this
sits in the order.

**Decide here:** whether the readable export from 0.4.0 includes DayLogs. It is the user's own
copy for reading, not a share, so DESIGN §7's never-in-a-share rule does not decide it — but a
folder of readable files is also the easiest thing to hand someone by mistake.

## Review behaviour, continued

### 0.13.0 — Review shapes made explicit + archive.

Card vs Note becomes a real axis instead of the title-as-prompt shortcut from 0.1.0 (DESIGN §2).
Manual archive removes an entry from rotation without deleting it — no auto-retire, ever
(DESIGN §3). This adds the first enum the *user* can see; the pinned-integer rule was already due
at 0.1.0, not here.

**Absent must read as Card, and this is the first time that choice has a wrong answer.** Entries
written 0.1.0–0.12.0 used title-as-prompt, which *is* a Card; if absent defaults to Note, every
entry ever written silently changes how it is presented. Scheduling is untouched either way —
DESIGN §2's invariant guarantees that — which is exactly why it could ship unnoticed.

**Open before building this:** whether Notes should ever allow a pure re-read mode, against the
current recall-first default (DESIGN §12). This is the release that forces it.

---

## Data and multi-user

### Gate: storage decision point — before profiles.

**Not a release.** Revisit JSON vs SQLite against real usage: file size, load time, and whether
queries have started to hurt. If SQLite wins it lands as a second `IEntryStore` implementation
plus a one-time importer, and the App layer does not change; if JSON is still fine, record that
and move on. Either way the user sees nothing, which is why it is a gate rather than a minor —
a MINOR is one user-visible capability and this has none.

Deliberately placed *before* profiles multiply the data, and before the format is committed to in
1.0.0's compatibility guarantee. If SQLite does land, that is the one outcome here that is not
additive: it needs its own minor and its own note about what happens to files written by earlier
releases — and 0.4.0's backups and copies, which assume plain files, are checked against it.

### 0.14.0 — Local profiles.

Video-game/Netflix picker, default profile so a solo user never thinks about it, per-profile data
isolation (DESIGN §6). No passwords.

**The picker must not imply a boundary that does not exist.** With no passwords, anyone who can
open the app can click another profile and read its DayLogs. DESIGN §8 makes shipping without
encryption conditional on saying so: one line of explanatory text when a profile is created, and
**no lock icons, no padlocks, no "private" labelling anywhere in the picker.** That is the promise
this release either keeps or breaks — implying a boundary that isn't there is worse than not
having one.

The picker reads `profile.json` only, never the payload (DESIGN §7), which is what keeps it
working unchanged if encryption ever arrives.

**Open before building this:** the on-disk layout for multiple profiles. Most of it is settled:
profiles are sibling folders, since the pre-restore copy lands beside them (DESIGN §7), and the
default profile has lived at `profiles/default/` since 0.1.0 (ARCHITECTURE). What remains is how
further profiles' folders are named and how the picker finds them. Where app-wide settings live
was settled before 0.4.0, which needed a home for the backup folder; check that the answer still
holds with several profiles.

**Waiting for this release, because they need more than one profile:**

- **"Add as a new profile"** — creates a profile from a backup, leaving everything else alone. It
  joins the question 0.4.0 already asks when a file is brought in.
- **Restore checks that a copy belongs to its profile.** With one profile the mistake cannot
  happen; with several it can.

The single-copy lock from 0.4.0 is already per profile, and backups already carry the profile
in their layout, so neither changes here.

---

### 0.15.0 — Share entries.

Making a share file: entries and tags only — no box state, no review history, and **never
DayLogs** (DESIGN §7). What goes in is chosen with the tag and date filters from 0.6.0, so "my
thermodynamics entries from this term" is one dialog rather than a manual selection.

Reading one ships here too: a share file adds entries to the current profile, because it has no
profile to be, so it offers neither "restore" nor "add as a new profile". It is the third answer
to the question 0.4.0 asks when a file is brought in. Making and reading belong in one release,
since a format nobody can produce is not a feature. Everything this needs already exists: tags
from 0.3.0, the filters from 0.6.0, and the file picker from 0.4.0.

**The UI rule is the load-bearing part.** Entries-only is what the share button *does*, never a
checkbox on the backup dialog, because "I unticked the wrong box" is how someone mails out their
journal.

**No "export for an older version" option, deliberately.** A share file is read and never written
back, so an older app imports what it understands, skips what it does not, and says so (DESIGN
§7). The sender keeps everything either way, and nobody has to know which version the recipient
runs.

**Open before building this:** the two questions DESIGN §12 has been holding — whether a share
carries its entries' attachments, and what creation date an imported entry gets.

## Finishing

### 0.16.0 — Settings, search and polish.

Search beyond 0.6.0's basic word filter, the rest of the keyboard shortcuts (review got its
keys at 0.5.0), first-run empty states, and the final polish over the foundations 0.5.0 laid.
Surfaces the settings that have accumulated hardcoded defaults — session cap, which "keep going"
follows, and the ladder if it is exposed at all.

**The two are not the same kind of setting, and the difference decides where validation lives.**
The ladder is a domain value: DESIGN §3 already validates it at construction — non-empty, every
interval `Count > 0` — enforced *now* rather than when a settings UI exists, precisely so this
release adds only the UI that surfaces the resulting error, not the rule. The session cap is the
opposite: it is serving logic in the UI layer (DESIGN §4), so it has no domain rule and must not
acquire one. Whatever bounds it is a UI concern and belongs here.

**Recovery depth** (DESIGN §7) surfaces here too, with a floor: a setting that can reach zero
switches recovery off and brings back the lockout it exists to prevent.

This is where "intuitive like a new video game, not like default Anki" gets tested on someone who
is not the author.

### 0.17.0 — First-run tutorial.

Under sixty seconds, skippable at every step, re-openable from Help, never blocking. Required
before 1.0 because 1.0 is the version handed to a stranger.

*Build it as a guided first run over the real UI, not a slideshow.* A carousel of screenshots is
dead weight that goes stale every release and teaches nothing; walking the user through writing
**one real entry that they keep** teaches by doing and leaves no fake data to clean up. No
tutorial framework — an overlay hint panel over the existing controls is enough.

Four beats: write an entry → see it in the diary → practise it → meet the DayLog.

*The third beat is free practice, not a scheduled review.* The entry the user just wrote is not
ready until tomorrow, and nothing should fake that. Free practice drills a not-yet-ready item
without moving boxes, so the tutorial teaches the real gesture — recall, reveal, self-grade — on
the user's own entry, with no consequence and no seeded demo card. Say the one honest sentence
out loud: *tomorrow it comes back on its own.*

*The DayLog is the pitch and it cannot be demonstrated.* Its payoff — reviewing a fact months
later and having the day you learned it come back with it — takes months to arrive by definition.
Show that the toggle exists and state the promise in one sentence; do not fake a year of history.
Being the only tool that does this is the reason a stranger picks it over the Obsidian plugin, so
it earns the final beat rather than a settings checkbox.

### 0.18.0 — macOS packaging.

GitHub Actions matrix completing the third release target. macOS needs notarization and therefore
a Mac runner, which is why it trails Linux and Windows (ARCHITECTURE). **`osx-arm64` is the
build that matters**; `osx-x64` only if someone asks.

### 1.0.0 — Release.

README with screenshots, and a written data-format compatibility guarantee built on the format
description from 0.4.0. The golden-file test that has run since 0.4.0 is the evidence; here it
becomes a promise.

**The app's name is due here** (DESIGN §12): 1.0 is the version handed to a stranger, and
renaming a public product gets more expensive with every user. The code name `StudyDiary` stays
whatever the product is called — namespaces are tedious to rename, a product is not
(ARCHITECTURE).

Otherwise nothing new; only the promise that what exists is stable.

# Beyond 1.0 — directions

> Speculative and unordered beyond the first few items. Precise sequencing eighteen months out is
> fiction; this is a menu with dependencies attached, so that when the time comes the choice is
> informed rather than improvised.
>
> **SemVer past 1.0:** MAJOR is reserved for breaking changes. For a local-first desktop app that
> means a data-format break old versions cannot read, or removing a capability users depend on.
> New features are minors indefinitely. Expect to sit on 1.x for a long time, and treat reaching
> 2.0 as a decision, not an achievement.

## The filter

Every idea below was tested against four questions, and any future idea should be too:

1. **Does it survive DESIGN §5?** No streaks, no counters of what you owe, no guilt. Sharp
   version: **can the number go down?**
2. **Does it survive DESIGN §2's invariant?** Content type and review shape must never change how
   often something is shown.
3. **Does it survive the maintenance rule?** A dependency you cannot fix yourself, on a five-year
   horizon, is a liability regardless of how good it is today.
4. **Does it earn its complexity for one person studying alone?**

## 1.x — deepening what exists

The first two have a stated order: retrospect first, then the algorithm.

**Retrospect — the first thing after 1.0.** An opt-in, off-by-default screen you have to go and
open. Never on the review screen, never a badge, never a number on the main window. The framing is
*retrospective, not scoreboard*: it reports things that happened rather than grading them.

- "A year ago today you wrote this."
- "Here are twelve entries you first wrote in 2026 and still know."
- "You've been keeping this for 1,400 days."
- Entries written, reviews completed, days studied — all monotonic, all safe.

"A year ago today" — the day resurfaced: everything you wrote that day, and **every DayLog for
that day** if there are any (DESIGN §8: any number per day, in the order written). Distinct from
the review screen's DayLog toggle, which is triggered by an entry becoming ready and shows one
entry's created-day. Here the calendar is the trigger and the day is the subject, so it can
surface entries nowhere near ready — and days with no entries at all, just a journal.

The second bullet needs its own defence, because the underlying set *can* shrink. It survives on
one condition: **it is rendered as a list of specific entries, never as a score.** "You still
know 12 of your 40 entries from 2026" is a score, and a bad week makes it 9 — same words,
different feature. DESIGN §5 states the general rule: a count describes a list, a denominator
or a comparison over time is a score.

*"Still know" is undefined and has to be pinned before this ships.* Currently in box 4 or 5? Never
failed? Passed most recently? Each produces a different list, and the third shrinks most
violently. Whichever is chosen is a claim about retention — the only one the app makes.

Apply the can-it-decrease rule ruthlessly (DESIGN §5 has the rejected examples).

This is the diary axis doing work no flashcard app can copy — memory as a record of your own life
rather than a performance metric. Placed first deliberately: it is the feature a stranger would
switch for, whereas FSRS is one a stranger cannot see.

**FSRS scheduling (highest single value, second in order).** The one change that measurably
improves retention per minute studied. `IReviewScheduler` exists precisely for this, so it is a
swap, not a rewrite. Ships alongside Leitner rather than replacing it: the user picks, and
existing box state maps onto initial FSRS parameters. Requires full review *history* — which is
why 0.1.0 records every outcome from the first release.

Note the open question that lands with it (DESIGN §12): `boxBefore`/`boxAfter` are stored, but
nothing records *which ladder* was in effect. Probably fine, since FSRS cares about outcomes and
dates rather than intervals — but it is unexamined, and this is the feature that examines it.

**Cloze deletion.** `The {{muon}} has a mass of {{105.7 MeV}}` generates recall prompts from a
single body. A genuine third **review shape**, so it belongs in the domain alongside Card and
Note — an additional member on the enum 0.13.0 introduced, with a pinned integer like the rest.

Scheduling is already settled: one entry, one ladder, whatever the prompt count (DESIGN §2). So
what this adds is rendering and interaction, not a scheduling change — which is most of why it is
cheap. It inherits a stated cost, though: failing one blank returns the whole entry to box 1,
including the blanks you know. The answer to that is a smaller entry, which is why the split
suggestion below wants to exist first.

**Gentle handling of repeatedly-failed entries.** An item failing five times running is not a
discipline problem; it is usually one entry trying to hold three facts. Rather than Anki's
punitive leech suspension, offer: *would you like to split this?* Diagnostic, not disciplinary.
DESIGN §2 names splitting as the answer to one-ladder's cost and DESIGN §5 rejects per-box
distribution charts because the diagnosis belongs on the entry rather than in a chart — so this
is where that signal surfaces. **Before cloze**, which multiplies the failure mode it addresses.

**Image occlusion.** Hide regions of a figure and recall what's underneath. Circuit diagrams,
Feynman diagrams, phase diagrams, apparatus schematics. Depends on images and on cloze existing as
a shape concept, and rides the same single ladder for the same reason.

**Encryption-at-rest and an optional per-profile password.** Deferred, not rejected (DESIGN §8 and
§11), and **demand-gated rather than ordered**: reopen if a real user asks, not preemptively.
Nothing is built or maintained for it now. What keeps the door open costs nothing — DESIGN §7's
header/payload split means `payload.json` becomes ciphertext and the `encryption` field says so,
with neither file changing shape. Two constraints come with it and are not incidental: profile
names stay plaintext so the picker works without a password, and attachments stay plaintext
because they are read on demand rather than with the payload. Encrypting those is a separate
problem with its own answer. Shipping this is what turns the stated non-boundary between profiles
into a real one.

**Extra ladder steps.** DESIGN §11 calls this trivial and it nearly is — the ladder is data, so
adding a rung is a one-line change and the scheduler asks the ladder for its own length. The part
that is not trivial is what it does to history: `boxBefore`/`boxAfter` become numbers whose
meaning depends on a ladder nobody recorded. Blocked on the same open question as FSRS above, and
cheap once that is settled.

**Entry linking and backlinks.** Wiki-style `[[references]]` with a backlinks panel. Turns a pile
of notes into a navigable structure without adding a graph database — links are just markdown,
resolved at render time. What makes the app usable as a *thinking* tool over five years rather
than only a drilling tool. Deliberately not a "graph view"; the visual is the least useful part.

**Tag hierarchy.** Nesting `#physics/thermodynamics` under `#physics`, plus saved filters. Beware:
per-subject *ladders* would violate DESIGN §2.

**Inline PDF rendering and annotation-to-entry.** Read a paper in the app, highlight a passage,
turn it into an entry that keeps a link back to the source page. The workflow a PhD actually has.
Heavy dependency.

**Reference manager interoperability.** BibTeX or Zotero linkage so an entry can cite the paper it
came from. Small, unglamorous, disproportionately useful in an academic context. Plain BibTeX keys
in a field cost almost nothing and can be enriched later.

**Quick capture.** A global hotkey opening a minimal capture window, plus paste-a-screenshot-to-
entry. The friction between having a thought and recording it is where most notes die.

**Interop: import.** Anki `.apkg` import, markdown-folder import, CSV. The export half, which
matters more, shipped at 0.4.0: it is the data-portability promise made real, and it is what lets
a user leave. An app that is easy to leave is one people trust enough to stay in.

**Accessibility and internationalization.** Screen-reader labels, font scaling, high-contrast and
dyslexia-friendly options, string externalization. Boring, permanent, and the sort of thing that
never gets done if it is not written down.

## 2.0 — the architectural break: sync and mobile

These two are one project, and they are the reason a 2.0 exists at all.

**Sync without a server.** The same entry reviewed on two machines, in two different boxes — which
wins? The local-first answer is a user-owned folder (Syncthing, Dropbox, a USB stick) plus real
conflict resolution, not a service. Requires per-entry review history with timestamps and device
identity.

**Know what the early history does and does not buy.** Review events recorded from 0.1.0 carry
`reviewedOn` as a whole-day `DateOnly` and no device identity, because there is no clock in the
domain and there are no devices yet. Complete enough to train FSRS, *not* complete enough to
resolve "which machine reviewed this first" within a single day. Sync will add richer events from
the release that introduces it, and must treat older events as unattributed rather than assuming a
device. A known limit, not an oversight to be fixed retroactively — it cannot be.

**Mobile.** Avalonia targets iOS and Android from the same codebase, and reviewing on a phone in a
queue is where spaced repetition actually gets done. The single largest multiplier on whether the
app gets used — and worthless without sync, which is why they are one release. Expect the UI layer
to need genuine rework; a review screen designed for a mouse is not one designed for a thumb.

Together these force schema changes old versions cannot read. That is what a major version is for.

## 3.0 and beyond — genuinely open

**Handwriting and stylus input.** For mathematics the most natural input there is, and no LaTeX
palette closes the gap with writing a derivation by hand. Tablet-dependent, heavy, and possibly the
thing that makes the app irreplaceable for a physicist.

**Visual WYSIWYG maths editor** and **chemical structure diagrams** from SMILES.

**Plugin or extension API.** Tempting and probably wrong: a plugin surface is a permanent
compatibility contract maintained by one person. Revisit only if there is a real contributor
community, and prefer "the file format is open and scriptable" as the extension story instead.

**A local language model** for suggesting cloze splits or catching a vague prompt. Only if fully
local, fully optional, and never generating entries wholesale — *writing the entry is the first
exposure*, so an app that writes your notes for you has removed the part that does the learning.
Assistive, never generative.

## Explicitly rejected

- **Streaks, XP, daily goals, leaderboards, badges.** Direct violation of DESIGN §5. They
  manufacture exactly the debt and shame this app is structurally designed to be incapable of.
- **Cloud accounts and hosted sync as the default.** Optional self-hosting is a different
  conversation; a default server is not.
- **Any telemetry, including "anonymous usage to improve the product".** The promise is worth more
  than the data.
- **Social features** — shared progress, following other users, public profiles. Wrong audience,
  wrong philosophy, permanent moderation burden.
- **AI-generated decks from arbitrary text.** Undermines the premise and produces cards nobody
  understands.
- **A deck marketplace or store.** Import/export of an open format is the whole benefit with none
  of the platform obligations.
- **Ads anywhere in the binary.** A passive donation link on the website is acceptable; nothing
  inside the app.

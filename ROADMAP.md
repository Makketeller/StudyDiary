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
> Minor is an integer, not a decimal: 0.9.0 → 0.10.0 → 0.11.0.
>
> The app version and the data file's `schemaVersion` are **separate counters** and almost never
> move together (DESIGN §7). Most releases below are additive to the file format.
>
> Order is dependency-ordered, not contractual.

---

## The thin slice

### 0.1.0 — Usable minimum. Linux and Windows.

Add an entry (title + body, plain text), delete with confirm, browse newest-first, review the
ready pool with a session cap of 10 (reveal → pass/fail). JSON persistence in the per-user data
folder with Guid ids and atomic writes. Self-contained single-file executables for `linux-x64`
and `win-x64`, with a `.desktop` entry alongside the Linux binary so double-click works there
too. No editing, no styling.

Scaffolds `StudyDiary.Data` (`IEntryStore` + the JSON implementation) and `StudyDiary.Data.Tests`
(round-trip a saved file). Establishes the `profile.json` + `payload.json` file shape (DESIGN §7),
the App-layer `TimeProvider` seam that supplies "today", `CreatedOn`/`CreatedAt` on `Entry`, and
pinned integer values on `outcome`. Each of those is nearly free now and a migration later.

`profile.json` carries all four header fields from this release: `schemaVersion`, the profile id,
the profile name, and `encryption` (`"none"` in MVP). Nothing reads `encryption` yet and writing
it is one line — but it is what makes DESIGN §8's deferral genuinely free. Every version that ever
ships then knows to check the field before parsing `payload.json`, so an app meeting an encrypted
payload says so rather than failing on bytes that are not JSON. Added later, every release before
it lacks that check.

**Review history is recorded from this release** — `reviewedOn`, `outcome`, `boxBefore`,
`boxAfter`, `isPractice` (shape and reasoning in DESIGN §7). `isPractice` is always `false` until
free practice ships at 0.10.0; it is in the schema from the start so that release adds no format
change. Nothing in 0.1.0 reads any of it back: the review screen only needs the current box.

*Windows caveat:* the extra binary is one more `dotnet publish` line, but a Windows build that has
never been launched on Windows is a claim, not a release. Either smoke-test it in a VM before
tagging or mark it explicitly untested. Path handling must use `Path.Combine` throughout —
`LocalApplicationData` already resolves correctly per OS.

*Done when:* write an entry, close, reopen tomorrow, pass it, and it returns in a week.

---

## Content — makes it worth writing in

### 0.2.0 — Edit an entry.

Retires the delete-and-retype workaround. Adds `modifiedAt` (additive; `schemaVersion` unchanged).
First, smallest, highest-relief feature — and deliberately the first live test of DESIGN §7's
absent-reads-as-default policy: files written by 0.1.0 have no `modifiedAt`, and absent must read
as "never edited" rather than throw.

*Not pre-added at 0.1.0, unlike `isPractice`.* The review-history event shape is settled (DESIGN
§7), so writing four of its five fields would diverge from a decided shape; `Entry`'s JSON shape is
explicitly still open (DESIGN §12), so nothing obliges `modifiedAt` early. A field no code can
write has undefined semantics, and exercising absent-→-default here — on the cheapest field in the
format, where getting it wrong costs nothing — is worth more than the consistency.

### 0.3.0 — Markdown + formatting toolbar.

Markdown becomes the body format: bold, italic, headings, lists, links, fenced code blocks
rendered monospace. A toolbar and keyboard shortcuts wrap the selection in the corresponding
syntax (DESIGN §9 for why not WYSIWYG).

Renderer survey between `Markdown.Avalonia` (MIT) and `CodeWF.Markdown` (Avalonia 12 + Markdig);
both are small community projects, so weigh them properly against the maintenance-cost rule.

*Plain text is valid markdown*, so entries written in 0.1.0–0.2.0 need no migration.

### 0.4.0 — LaTeX and chemistry.

Inline and block maths embedded in markdown with live preview beside the input. **mhchem support
is a hard selection criterion for the renderer** (DESIGN §9).

Note the coupling: whichever markdown renderer 0.3.0 chose may already bundle a maths
integration, so evaluate the two together even though they ship separately.

### 0.5.0 — Insertion palette.

Searchable panel of common LaTeX and mhchem commands; clicking inserts at the cursor with the
cursor parked in the first blank. Extends the 0.3.0 toolbar rather than introducing a second
mechanism.

### 0.6.0 — Syntax-highlighted code blocks.

Fenced code with per-language highlighting. Separate from 0.3.0 because highlighting means
another dependency (typically AvaloniaEdit / TextMate grammars) and it is pure polish on
something that already works.

### 0.7.0 — Images.

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

Attaching a PDF and opening it in the system viewer belongs here too (DESIGN §9); inline PDF
rendering is deferred.

### 0.8.0 — DayLog.

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
0.3.0–0.7.0 already built the one content pipeline and the one renderer. A DayLog is not a reduced
journal format sitting beside the real one — and it is only free because of where this sits in the
order.

## Review behaviour — makes it a study tool rather than a notebook

### 0.9.0 — Tags.

Free-form, many-to-many labels: `#chemistry`, `#german`, `#thermodynamics`. Non-exclusive, unlike
a deck — one entry is `#chemistry` *and* `#exam-january` without existing twice (DESIGN §10).

**Placed here, early, on purpose.** Two mechanisms ship together, and the second matters more:

- **Bulk-tagging from the browse screen**, for clearing whatever backlog exists at that point in
  one deliberate pass.
- **Tagging wherever an entry is, including mid-review.** Noticing an untagged entry when it comes
  round in box 3 and labelling it there costs nothing and needs no decision to sit down and tidy.

The second is what makes the early placement worth it, and it changes the argument. Retro-tagging
is not impossible — the ready pool hands you every entry eventually, so the untagged backlog
drains through ordinary use, the same shape as DESIGN §5's backlog. What early placement buys is
the *rate*: a box 5 entry comes round once a year, so every month tags do not exist is a month of
entries that will take up to a year to surface. Bulk-tagging is the escape hatch for whatever the
delay produced; mid-review tagging is why the backlog drains at all.

Filtering itself arrives with 0.10.0, since that is where the sessions it filters live.

### 0.10.0 — Session behaviour.

Foot-in-the-door first batch (~2) when the pool is large, voluntary "keep going", and free
practice on not-yet-ready items. Serving lives in the UI layer and must not touch the scheduler
or the definition of "ready". Free practice is enforced by *absence* — no practice-mode flag
exists anywhere in the domain (DESIGN §4).

Practice reps are appended to review history with `isPractice: true` and
`boxBefore == boxAfter`. The field already exists in the 0.1.0 schema, so this release starts
writing `true` and changes no format.

Filtering ships here, in two shapes (DESIGN §4):

- **Tag filtering** — free on free practice, opt-in on the ready session. The ready session
  defaults to unfiltered and all subjects mixed, deliberately.
- **Date filtering on free practice** — "things I wrote this week", "anything older than six
  months", combinable with tags. It reads the entry's created-day; it is not a tag and does not
  reuse the tag mechanism.

Both are serving logic. The domain never learns that filtering exists.

**Open before building this:** whether the session cap is a hard stop or a default stopping point
(DESIGN §12). "Keep going up to the cap" and "grind a backlog down" are not the same feature.

### 0.11.0 — Review shapes made explicit + archive.

Card vs Note becomes a real axis instead of the title-as-prompt shortcut from 0.1.0 (DESIGN §2).
Manual archive removes an entry from rotation without deleting it — no auto-retire, ever
(DESIGN §3). This adds the first enum the *user* can see; the pinned-integer rule was already due
at 0.1.0, not here.

**Absent must read as Card, and this is the first time that choice has a wrong answer.** Entries
written 0.1.0–0.10.0 used title-as-prompt, which *is* a Card; if absent defaults to Note, every
entry ever written silently changes how it is presented. Scheduling is untouched either way —
DESIGN §2's invariant guarantees that — which is exactly why it could ship unnoticed. Additive, so
`schemaVersion` does not move (DESIGN §7).

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
releases.

### 0.12.0 — Local profiles.

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

**Open before building this:** the on-disk layout for multiple profiles. DESIGN §7 has settled
part of it — profiles are sibling folders, since the pre-restore copy lands beside them — so what
remains is naming, discovery and where the default profile sits. Separately, where app-wide
settings live is still open (DESIGN §12); they have no home in the payload, which lists entries,
history and DayLogs only.

### 0.13.0 — Backup, export, restore.

"Reveal my data" and human-readable JSON export. Backup is a copy of the profile's folder —
`profile.json`, `payload.json` and `attachments/` — and the app's job is to help the user find or
produce it rather than making them hunt through app-data folders (DESIGN §7).

**Bringing a file in is three paths, not one, and the app cannot tell which the user meant.** So
it asks, in the user's words rather than the format's (DESIGN §7):

- **"Restore this backup"** — replaces the current profile's contents. Destructive by intent, so
  it confirms, **names the profile being replaced**, and first copies that profile's folder to a
  `pre-restore/` folder beside the profiles. There is no cloud to recover from; that copy is the
  only thing that makes a misclick undoable. Pre-restore copies are kept and **never removed
  automatically**.
- **"Add as a new profile"** — creates a profile from the file, leaving everything else alone.
  Follows profiles at 0.12.0, which is why this release sits here.
- **A share file** offers neither: it adds entries to the current profile, because a share file
  has no profile to be. (Producing share files is not scheduled — see beyond 1.0.)

**Import copies, never moves.** Moving would delete the user's only backup from where they put it.
"Move" may exist later as an explicit, clearly-labelled option; restore must never be able to
destroy the source it restored from.

Restoring into the current profile never merges. It replaces. Merging is sync, and sync is 2.0.

*The export shipped here is backup-shaped — the whole folder, everything.* DESIGN §7's *share*
artifact is a different thing with different contents and its own failure mode, and is
deliberately not on the same dialog as a checkbox.

---

## Finishing

### 0.14.0 — Settings, search and polish.

Search entries, keyboard shortcuts, first-run empty states, and the styling pass 0.1.0 skipped.
Surfaces the settings that have accumulated hardcoded defaults — session cap, and the ladder if
it is exposed at all.

**The two are not the same kind of setting, and the difference decides where validation lives.**
The ladder is a domain value: DESIGN §3 already validates it at construction — non-empty, every
interval `Count > 0` — enforced *now* rather than when a settings UI exists, precisely so this
release adds only the UI that surfaces the resulting error, not the rule. The session cap is the
opposite: it is serving logic in the UI layer (DESIGN §4), so it has no domain rule and must not
acquire one. Whatever bounds it is a UI concern and belongs here.

This is where "intuitive like a new video game, not like default Anki" gets tested on someone who
is not the author.

### 0.15.0 — First-run tutorial.

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

### 0.16.0 — macOS packaging.

GitHub Actions matrix completing the third release target. macOS needs notarization and therefore
a Mac runner, which is why it trails Linux and Windows (ARCHITECTURE). **`osx-arm64` is the
build that matters**; `osx-x64` only if someone asks.

### 1.0.0 — Release.

README with screenshots, a written data-format compatibility guarantee, and a golden-file fixture
test: one saved file per released version, kept in the test project forever, each asserted to
still load. That test is what proves DESIGN §7's migration policy was real.

**The app's name is due here**, and it is the only open question in DESIGN §12 with a deadline
attached: 1.0 is the version handed to a stranger, and renaming a public product gets more
expensive with every user. The code name `StudyDiary` stays whatever the product is called —
namespaces are tedious to rename, a product is not (ARCHITECTURE).

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
one condition: **it is rendered as a list of specific entries, never as a score.** A count of the
items shown is fine — it describes the list in front of you. What fails is a **denominator or a
comparison over time**: "you still know 12 of your 40 entries from 2026" is a score, and a bad
week makes it 9. Same words, different feature. That distinction is a general rule rather than a
fact about this screen, and belongs in DESIGN §5 beside the can-it-decrease test — it is what any
future display will need.

DESIGN §5 states that rule generally — a count describes a 
list, a denominator or a comparison over time is a score.

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
Note — an additional member on the enum 0.11.0 introduced, with a pinned integer like the rest.

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

**Interop: import and export.** Anki `.apkg` import, markdown-folder import/export, CSV. Export
matters more than import — it is the data-portability promise made real, and it is what lets a
user leave. An app that is easy to leave is one people trust enough to stay in.

**Producing share files** belongs here too, and is deliberately not scheduled before 1.0. DESIGN
§7 specifies the artifact — entries and tags only, no box state, no review history, and **never
DayLogs** — but nothing depends on it, and DESIGN §12 has two unanswered questions gating it:
whether a share carries its entries' attachments, and whose creation date an imported entry gets.
*Consuming* a share file already ships at 0.13.0. The UI rule is the load-bearing part:
entries-only is what the share button *does*, never a checkbox on the backup dialog, because "I
unticked the wrong box" is how someone mails out their journal.

**Accessibility and internationalization.** Screen-reader labels, font scaling, high-contrast and
dyslexia-friendly options, string externalization. Boring, permanent, and the sort of thing that
never gets done if it is not written down.

**Data resilience.** Rolling automatic backups (keep the last N), a checksum on load, and a "your
file looks damaged, here are your backups" recovery path. Five years of PhD notes deserve more
than one file and good intentions. *Note the tension:* DESIGN §7 says nothing deletes files the
user did not point at, which is why orphaned attachments and `pre-restore/` copies accumulate
forever. Rolling backups do delete, and the distinction that makes it legal is that these are
app-created files in an app-owned folder on a schedule the user set. Anything the user placed or
named stays untouched.

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

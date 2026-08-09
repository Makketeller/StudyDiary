# Study Diary — Design

> Working name: TBD. Open-source, local-first study diary with spaced-repetition review.
> Code name **StudyDiary**. Repo: https://github.com/Makketeller/StudyDiary
>
> **What this file is:** product decisions and the reasoning behind them, so a future reader
> (or future me) doesn't re-litigate a settled choice. It changes rarely.
>
> **Reference rule:** STATUS.md and ROADMAP.md may point here; this file never points back at
> either. ARCHITECTURE.md is the one exception, because a decision recorded here often has its
> enforcement there — so this file may name it, but **never with a section number**, since
> ARCHITECTURE changes when the code does and its numbering will drift. If a paragraph here
> needs a version number, the paragraph belongs in ROADMAP.md instead.

---

## 1. What this is

A free, local-first desktop app for reviewing what you've learned over the long haul (designed
for 5+ years of continuous personal use). No cloud, no telemetry. Usable by non-technical
users: download, double-click, no runtime to install and no admin rights. Your data is yours:
local files, plus JSON export and plain file-copy backup.

**Non-negotiables**

- Local-first. No cloud, no network dependency for core use.
- **No mandatory login.** A single user never has to think about accounts. Local **profiles**
  exist so several people *can* share one machine (§6) — purely local, never phoning home.
- Telemetry-free. Any "account" concept is for the user's own convenience, never data collection.
- Data portability: human-readable export; backup is "copy this"; restore is one click (§7).
- Longevity over cleverness. Favour the .NET standard library and low-maintenance choices;
  every heavy dependency must justify itself against long-term maintenance cost.
- **Cross-platform: Windows, macOS and Linux are all release targets.** Development happens on
  Fedora, but that is a dev-machine fact, not a scope narrowing.
- **Copyleft: GPLv3-or-later.** The promises above are only as durable as the licence that
  forces a fork to keep them. See below.

### Why copyleft

Commercial use and selling are fine; what is not fine is someone forking this into spyware,
adware or engagement-farming and shipping it closed. No OSI-approved licence can forbid a
*use* — the Open Source Definition bars field-of-endeavour restrictions — and ethical-source
licences are legally untested and not open source. Copyleft is the closest available fit: it
doesn't prohibit bad behaviour, but it forces anyone distributing a modified version to
publish the source, so user-hostile additions can't be made quietly. Losing companies who
won't touch copyleft is not a cost here: the audience is individual learners, not vendors
building products on top. **"or later"** so future FSF revisions can be adopted without
tracking down every contributor.

**No CLA, deliberately** — it is a structural guarantee against enclosure.

---

## 2. Domain model

The atomic unit is an **Entry**, made of three *independent* axes that must not leak into each
other. Keeping them decoupled is the core architectural rule of the whole app.

1. **Content** — what the entry is made of, i.e. what gets edited and rendered.
   Markdown as the container format, with maths, code blocks and images inside it (§9).
2. **Review shape** — how the entry is tested.
   - **Card**: a prompt is shown, the body hidden; you recall the answer, reveal, self-grade
     pass/fail.
   - **Note**: a single body you self-assess. Default presentation is still recall-first
     (cover → recall the gist → reveal), because passive re-reading is a weak study method and
     the testing effect is nearly free to bolt on.
3. **Scheduling** — the Leitner box state (§3). **Identical regardless of axes 1 and 2.**

**Design invariant:** content type and review shape have *zero* effect on scheduling. A maths
flashcard and a plaintext note ride the exact same ladder. "How often something is shown" is
decided entirely by axis 3 and nothing else.

**Not everything is an Entry.** The **DayLog** (§8) is a *separate entity* with no review shape
and no Leitner state. It exists alongside Entries, not as a fourth axis on them. Bolting
journaling onto Entry would break this invariant.

**Tags are not a fourth axis either.** An Entry carries a set of free-form tags, but they are
plain attributes — labels for finding and filtering. They must never reach the scheduler:
filtering *which* entries you are handed is a serving concern (§4), whereas a per-tag ladder
would make how often something is shown depend on its subject, which is exactly what the
invariant forbids.

**One entry, one ladder — always (DECIDED).** A future review shape that generates several
prompts from one body (cloze deletion, image occlusion) still rides exactly one Leitner
state. Per-prompt scheduling would mean choosing a review shape changed the *granularity* of
scheduling — a violation of the invariant that is easy to miss, because nothing about it
looks like changing how often an item is shown. The cost is real: failing one blank returns
the whole entry to box 1, including blanks you know. The answer to that is not a second
ladder but a smaller entry — an entry that repeatedly fails is usually one entry holding
three facts, and splitting it is the correct fix rather than a workaround.

Capture and browsing feel diary-like (dated entries you can flip back through). Review is
active-recall by default, because the stated goal is to actually learn.

---

## 3. Scheduling — spaced repetition (classic Leitner)

Deterministic: two people applying these rules to the same history *and the same ladder*
compute the same next review date. This is enforced structurally — see ARCHITECTURE on the
scheduler never reading the system clock.

### Box ladder

| Box | Waiting time while sitting in this box |
|-----|----------------------------------------|
| 1   | 1 day    |
| 2   | 1 week (stored as 7 days — see ARCHITECTURE on why there is no `Week` unit) |
| 3   | 1 month  |
| 4   | 6 months |
| 5 (cap) | 1 year, forever |

### Rules

- **New entry** → enters **box 1** on its creation day. First review = *created date* + box 1's
  interval.
  - The rule is uniform: **next review = the day you entered the box + that box's interval.**
    Creating an entry is how you enter box 1, so the first review needs no special case, no
    setting, and no separate lookup. Writing the entry *is* the first exposure.
  - There is no box 0. A box is a rung with its own interval *and* its own transitions, and an
    "unreviewed" rung would have neither: failing in it would have to promote you, and failing
    into it would re-review the same item every day forever. It would be an initial state
    wearing a box's clothes.
  - Wanting to look at something again the day you wrote it is served by **free practice** (§4),
    which is the correct semantics anyway — a same-day re-read is not evidence of retention and
    should not move a box.
- **Pass** → box `min(N + 1, top box)`. Next review = **actual review date** + new box's
  waiting time.
  - *The cap is the ladder's length, never a literal `5`.* A hardcoded 5 would mean adding a
    sixth box silently makes it unreachable, breaking the "adding a box is a one-line change"
    promise below. The ladder knows how many boxes it has; the scheduler asks it.
- **Fail** → box **1**. Next review = **actual review date** + box 1's waiting time.
  - The implementation must **look up box 1's interval** rather than hardcode a day, so that
    changing the ladder can never leave the fail path silently disagreeing with the table
    above. The ladder is the single source of truth. (Numerically identical today; that is the
    point — it stays correct when the ladder changes.)
- **Cap (box 5)** repeats at 1 year forever. **No auto-retire** — you keep things you learned
  years ago from silently rotting. A manual **archive** action exists for deliberate removal
  from rotation.

### Why anchor to *actual* review date, never scheduled date

The interval is a function of the **box**, not of elapsed time. Missing days (weekends,
illness, life) neither punishes nor rewards you — the ladder just stretches in wall-clock time
and continues. There is structurally nowhere for the app to shame you for lateness. A
genuinely forgotten item fails its review and drops to daily drilling; that is the system
working, not a penalty.

### Time granularity

Scheduling uses **whole-day dates**, not timestamps. An item ready "today" is ready all day.
This sidesteps most timezone and clock complexity. Timestamps may still exist on entries for
diary and browsing purposes; they just don't drive scheduling. Enforced in the type system —
see ARCHITECTURE.

### The ladder is data, not code

The ladder is an ordered list of intervals, one per box
— never hardcoded if (box == 2) branches. Consequences:

- Adding or removing a box is a one-line change.
- The whole scheduler sits behind an interface, so a future FSRS/SM-2 experiment is a swap,
  not a rewrite. FSRS/SM-2 are **deferred** (§11); pure Leitner is the MVP.

Concrete shape: a `LeitnerLadder` value holding an ordered list of per-box intervals, each
interval a `(count, unit)` pair where unit is day/month/year. Box numbers are **1-based**; the
list is 0-indexed. That translation lives in exactly one place (the ladder) and must not be
duplicated.

**Validation lives on the ladder, not on the interval (DECIDED).** `ReviewInterval` is a general
span of whole days, months or years — zero is arithmetically meaningful there, since `AddTo`
simply returns the same date. Positivity is a *ladder* rule: a box you wait zero days in is not a
rung. Putting the rule on `ReviewInterval` would bake a scheduling constraint into a type that
knows nothing about scheduling.

- `ReviewInterval` permits `Count >= 0`, rejecting only negatives.
- `LeitnerLadder`'s constructor validates `BoxIntervals`: non-`default`, non-empty, and every
  interval `Count > 0`.

Enforced at construction *now*, not when a settings UI exists, because the ladder is being
constructed now and so the invalid states are reachable now.

---

## 4. Sessions & review behaviour

Two distinct mechanisms, kept separate on purpose.

### The ready pool

- An item is **ready** when its next-review date ≤ today. "Ready" is *derived*, not stored.
- **Where "today" comes from.** The scheduler is forbidden from reading the clock, so something
  else must: exactly one seam in the App layer, a `TimeProvider` resolved once and passed down.
  See ARCHITECTURE.
- **Default session cap = 10.**
- **Foot-in-the-door serving.** When the ready pool is large (you've been away), the session
  opens with a **tiny first batch (~2)**, not the whole cap. Finish it and you can pull more in
  small steps. The point is to make *starting* trivial; the hard part of study is beginning, so
  we shrink the activation energy.
- A voluntary "keep going" pulls further batches. (**Open:** whether the cap of 10 is a hard
  stop or only the default stopping point — §12.)
- The backlog shrinks through ordinary use and is **never surfaced as a number or counter**.
- During review, an optional toggle reveals the DayLogs for **the entry's created-day** (§8)
  after you answer — not today's DayLogs.

**Important separation:** all of the above is a *serving* concern — how items are handed out of
the ready pool. It does not touch scheduling or the definition of "ready", so it cannot corrupt
spacing. It is pure UI/session logic and lives in the UI layer, not the domain.

### The ready session defaults to unfiltered

Everything ready today, all subjects mixed, with tag filtering available as a deliberate choice
rather than the default path.

Filtering the ready session corrupts no spacing — but in practice, always choosing chemistry
leaves the German entries ready indefinitely: the system stays honest (no penalty, no counter,
§5 holds) while the user quietly stops learning German. Interleaving also beats blocking
pedagogically, and Anki's deck model pushes users toward blocking by construction. Mixing by
default is a real advantage, not just a simpler UI.

### Free practice / drill

- Reviewing **any** entry for extra reps, because you feel like it — ready or not. Practising a
  ready item doesn't consume it; it stays ready and still comes up in the real session.
- **Free practice does NOT move boxes and does NOT reschedule anything.** Pure bonus study that
  leaves the real schedule untouched. This is what stops "extra practice" from silently
  corrupting spacing.
- Architecturally this is enforced by *absence*: free practice simply does not call the
  scheduler. There is no "practice mode" flag inside the scheduler that could be got wrong.
- **Filterable by tag and by date.** "Just chemistry", "things I wrote this week", "anything
  older than six months", or a combination. The date filter reads the entry's created-day; it is
  not a tag. All of it is serving logic in the UI layer — the domain never learns that filtering
  exists.
- **Free practice IS recorded in review history (DECIDED).** History cannot be backfilled, and
  FSRS is trained on *every* exposure, not only the scheduled ones. A practice event is
  appended with `isPractice: true` and `boxBefore == boxAfter`.
  - **The flag lives on the persisted event, never in the domain.** It is a fact about what
    happened, readable only by a future FSRS trainer and by nothing else. If a
    `LeitnerScheduler` ever branches on it, enforcement-by-absence has been thrown away.

---

## 5. Anti-toxic-debt principles (product-wide)

Hard constraints, not nice-to-haves. They shape schema and UI.

- **Never surface accumulating debt.** No "N overdue", no red late badges, no guilt counter,
  no streak-shaming.
- **Positive framing only.** The vocabulary is "**ready for review**" / "**ready today**", not
  "due" / "overdue". The word "due" should not appear in the UI.
- Missed items don't pile up as visible failure; they quietly stay *ready* until you get to them.
- Combined with actual-date anchoring (§3), the app is *structurally incapable* of telling you
  you're behind.

**The sharp test for anything that displays a number: can it go down?** A figure that can
decrease will be read as a report card however warmly it is worded. Monotonic counts (entries
written, reviews done, days studied) are safe. **"You know 340 things" is not** — a bad week
makes it 320 and it becomes a score you can lose. So are retention percentages and accuracy
rates.

**A list is not a score, but a denominator is.** Naming specific things — "here are twelve entries
you wrote in 2026 and still know" — describes the list in front of you, and a count of the items
in that list is a description of the list. What turns the same words into a score is a
**denominator or a comparison over time**: "twelve of your forty entries from 2026" invites the
subtraction, and a bad week makes it nine. The set behind such a list may shrink without the
display becoming a scoreboard, provided nothing on screen says what it shrank from.

Two harder cases, kept because both are things a reasonable person will propose again:

- **Per-box distribution.** Box 1 filling up looks like failure when §3 is working exactly as
  intended. The cost of hiding it is real: it is the one signal that would tell you an entry is
  trying to hold three facts. That diagnosis belongs on the entry, not in a chart.
- **A GitHub-style contribution heatmap.** Retrospective rather than loss-framed, but what it
  communicates loudest is the empty squares, and consecutive runs are legible whether or not the
  UI names them. Rejected on the judgement that it cannot be rendered without becoming a streak,
  not on a finding that it is one.

What may ship instead is **retrospect, not scoreboard**
— a day resurfaced because the calendar came round to
it, not a figure telling you how you're doing. "A year
ago today you wrote this, and here's what you journaled that day."

**This is a bet, not a finding**. The evidence on
loss-framing and extrinsic motivation supports the
direction, but it is drawn from classrooms and
workplaces rather than solo adult study, and some
people genuinely do well on streaks. This app takes a
side instead of making it configurable, because a
toggle is itself a suggestion — and the person most
likely to switch streaks on is the one this section
exists to protect. Reopen if a real user asks, not preemptively.

---

## 6. Accounts & profiles (local, multi-user)

- **Local profiles only.** Purpose: let several people share one computer, each with their own
  entries and schedule. Not authentication to a server; nothing leaves the machine.
- **Video-game / Netflix model.** Pick a profile like choosing a save slot — no login friction.
  A **default profile** exists so a solo user never has to create or think about one.
- Each profile owns its own data (its own Entries + Leitner state + DayLogs). Profiles do not
  see each other's entries.
- **No passwords in MVP.** Optional local password-protection is a deferred nicety and is
  local-only — nothing to do with cloud auth. See §8 for why it stays deferred.
- Profiles and "import a save file" (§7) are the **same machinery**: an imported file either
  becomes a new profile or replaces the current one's contents, and the app asks which — see §7.

---

## 7. Data, backup & restore

Backup must be trivial and restore must be *even easier* — an explicit MVP goal.

- **The data is one folder per user**, containing the two JSON files described below,
  plus an `attachments/` folder once images ship.
  Backup = copy  that folder somewhere safe. The app
  helps the user find or produce it — a "reveal my
  data" action, an export action — rather than making them hunt through app-data folders.
- **Import COPIES, never moves, by default.** Moving would silently delete the user's only
  backup from where they put it. "Move" may exist later only as an explicit, clearly-labelled
  option. Restore must never be able to destroy the source it restored from.
- **MVP scope is import / restore, NOT sync.** Bringing a file in is simple. *Merging* two
  divergent histories (same entry, different box on two machines — which wins?) is a genuine
  tarpit and is deferred (§11).

### File shape: header and payload, in two files

`System.Text.Json` ships in the BCL, so it adds no dependency, and JSON *is* the human-readable
export format §1 promises — two concerns collapsed into one.

A profile's data folder contains:
```
profile.json      the header — small, plaintext forever
payload.json      everything else — entries, review history, DayLogs
attachments/      once images ship
```
The header carries `schemaVersion`, the profile id and name, and an `encryption` field (`"none"`
in MVP). **The payload is one self-contained blob**, which is what "encryption-ready schema"
concretely means: when encryption arrives, `payload.json` becomes ciphertext and the header says
so. The profile picker still works without a password, because it only ever reads the header.

**Two files rather than one, deliberately.** Nesting the payload inside the header document would
force a choice between two bad options: either the payload becomes a JSON-escaped string (so the
file is double-encoded and no longer pleasant to read by eye, breaking §1's human-readable
promise), or encrypting it changes the document's *shape* — which is exactly the schema change
this design exists to avoid. Two files keep both promises: the payload is a normal readable JSON
document today and an opaque blob later, and neither file ever changes shape.

The cost is that atomic write-then-replace now covers two files rather than one. In practice the
header changes almost never, so a save writes the payload alone; the two only move together when
a profile is created or renamed.

Two rules follow, and both must hold from the first release:

- **Nothing outside the payload may contain user text.** A profile *name* is chosen by the
  user and stays plaintext so the picker can show it — an accepted, stated leak.
  Attachments are the other one: they are files in `attachments/`, not payload content, so
  they stay plaintext even if the payload is ever encrypted. Encrypting them is a separate
  problem with its own answer (they are read on demand, not with the payload), and is
  deliberately not solved here.
- **Nothing may depend on reading part of the payload without reading all of it.** Byte-range
  reads, or a future index sitting outside the payload, would both break the envelope.

### Backup and share are different artifacts

**A backup is everything; a share is entries only.** These are separate artifacts, not one file
with a checkbox.

| | Backup | Share |
|---|---|---|
| Contains | the whole data folder — header, payload and attachments | entries and tags only |
| Purpose | your own safety net | handing material to someone studying the same thing |
| Lands as | a profile | entries inside the recipient's current profile |

A share file omits box state and review history deliberately: your box 4 is not your colleague's
box 4, and review history is a record of *your* recall, not a property of the material. Imported
entries therefore enter box 1 with fresh ids and the recipient's creation dates — they are new to
them. (Whether that date should be the recipient's or the sender's is unexamined — §12.)

**The share export must never contain DayLogs, and this is a UI rule as much as a format one.**
Accidentally mailing someone your journal is the worst thing this app could do, and "I unticked
the wrong box" is a real failure mode. Entries-only is what the share button *does*; including
everything is the separately-named backup action, not an option on the same dialog.

### Restore is not the same as import

Both start with a file picker, and the app cannot tell from the file which the user meant. So it
asks, in the user's words rather than the format's:

- **"Restore this backup"** — replaces the current profile's contents.
  The obvious case: you lost data and want your file
  back. Destructive by intent, so it confirms, names
  the profile being replaced, and first copies that
  profile's folder to a pre-restore/ folder beside the
  profiles. Restoring over the wrong profile is one
  misclick away and there is no cloud
  to recover from; the copy is what makes it undoable.
  Pre-restore copies are kept and never removed
  automatically —  same reasoning as orphaned attachments below: a local-first app with no undo
  should not delete things the user did not point at.
- **"Add as a new profile"** — creates a profile from the file, leaving everything else alone.
  The case where a file arrives from another machine or another person.
- **Importing a share file** offers neither: it adds entries to the current profile, because a
  share file has no profile to be.

Restoring into the current profile never merges. It replaces. Merging is sync (§11).

### Attachments live beside the file, not inside it

Images are stored as files in an `attachments/` folder **beside** the JSON, not as base64 bytes
inside it. Base64 inflates the bytes by about a third, but the real cost is that the JSON is
rewritten whole on every save — an embedded diagram would be re-serialised every time you edit an
unrelated entry's text. Keeping them out also keeps the main file readable and text diffs sane.
**Decided, not open.**

- **An attachment is referenced by bare filename, never a path — with the original name kept
  as a label.** Attachments are named from a generated id plus the original extension
  (`a3f2c9d1.png`) and resolved relative to the data folder at load time. An absolute path
  breaks the moment the folder is copied to another machine, which is the whole backup story;
  the user's original filename breaks the moment two `diagram.png` files meet. So the original
  name is stored alongside the reference as a **label only** — nothing ever resolves through
  it. It exists because `a3f2c9d1.png` tells the user nothing about which image is missing,
  while `benzene-ring.png` does: a missing-file message shows both, recognisable name first.
- **Attachments are never shared between owners.** Every attachment belongs to exactly
  one Entry or one DayLog; pasting the same image twice writes two files with two ids.
  Deduplication would create a hidden edge between a journal post and an Entry, and any
  export that carries an Entry's attachments would then have a route by which a journal
  image leaves the machine — the failure mode this design works hardest to prevent.
  Reference counting is also the thing that makes deletion dangerous, as the next bullet
  declines. Duplicate bytes are the cheaper mistake.
- **Orphans are tolerated, not collected.** Deleting an entry or a DayLog leaves its
  images behind. A stray file is harmless, and a delete path that removes files is a
  delete path that can remove the wrong one. If the folder ever grows uncomfortably, a
  "find unreferenced attachments" action can report them for the user to delete — but
  nothing deletes files automatically. This is a stated non-goal, not an oversight.

LaTeX and code need no attachments: they are text inside the markdown body, and maths is rendered
live rather than stored as images.

### Review history

Recorded from the first release that persists anything, though nothing reads it until FSRS.
Append-only per entry: `reviewedOn` (`DateOnly`), `outcome` (Pass/Fail), `boxBefore`,
`boxAfter`, `isPractice`.

`boxBefore`/`boxAfter` are stored rather than recomputed because replaying the log under a
*changed* ladder would reconstruct boxes that were never true — the log records what happened,
not what today's rules imply.

It lives on the persisted entry, not the scheduler, which stays stateless and clock-free. The
App layer appends the event. **History cannot be backfilled**, which is the whole reason it
lands in the thin slice rather than with the feature that first reads it.

### Schema version and migration policy

The **app version and `schemaVersion` are separate counters** and almost never move together.
Most releases are additive to the file format and leave `schemaVersion` alone.

- **`schemaVersion` is a single integer**
  at the top of `profile.json`, written from the first release.
- **Additive changes do not bump it.** A new optional property is read as *absent → default*,
  never as an error. `System.Text.Json` already does this; the rule is simply that such a
  property is never marked `required` and its absence never throws.
- **`schemaVersion` bumps only when an old app could misread a new file**, i.e. when the
  *meaning* of existing data changes. Renaming, re-typing or repurposing a field is a bump.
- **Forward-incompatibility is stated, not silent.** If a file's `schemaVersion` is higher than
  the app understands, the app says so and refuses to write, rather than loading a partial
  object and saving it back with the unknown fields dropped. Silent field-dropping on save is
  the one way a local-first app destroys data it was trusted with.
- Migration code, when eventually needed, lives in `StudyDiary.Data` behind `IEntryStore` —
  never in Domain, which has no idea files exist.
- **Hand-edited files are expected, and the failure mode depends on what broke.** The
  format is readable on purpose and "reveal my data" is a shipped action, so someone
  will eventually move, rename or delete something. A **missing attachment** renders as
  a visible placeholder in that one entry and nothing else changes — the entry keeps its
  reference, so restoring the file fixes it, and nothing rewrites the payload to "clean
  up" a reference the user may be about to restore. A **payload that will not parse** is
  the opposite case: the app says so, names the file, and **refuses to write**, for the
  same reason forward-incompatibility does. Partial load followed by a save is how a
  local-first app destroys the data it was trusted with.

---

## 8. Daily diary / journal (optional)

- A **DayLog** is optional free-form writing attached
  to a *calendar day* — **any number per
  day**, per profile. Never mandatory; most days may have none. Multiple posts a day is
  the diary behaviour people actually have, and forcing one blob per day is a constraint
  a user can impose on themselves if they want it.
- **The day is the unit that gets surfaced, not the post.**
  A DayLog carries its own id, a created-day and a
  timestamp for ordering within it; "the log for 3 March" is every DayLog with that created-day,
  in the order written. This is why the review toggle below shows a day rather than a text.
- **A DayLog is NOT an Entry** (§2). No review shape, no Leitner state; never reviewed, never in
  the ready pool. Journaling is a separate entity, full stop.
- **A DayLog's body is the same markdown container as an Entry's (§9).** Images, maths
  and code all work in a journal post, because there is one content pipeline and one
  renderer — not a reduced journal format sitting beside the real one.
- **Surfaced during review, optionally.** After you answer or reveal an Entry, an optional
  toggle shows the DayLogs for *that entry's created-day* — "what you journaled the day you
  learned this". Pure read; touches nothing in scheduling.
  - Emergent nicety: reviewing a fact months later can resurface the context and mood of the
    day you first wrote it.

### Privacy

- **Baseline: already private.** Local-only, no cloud, no telemetry means diary text
  **never leaves the machine** over any network. There is nothing to opt out of.
- **Profile separation is a UI convenience, not a privacy boundary.** With no passwords
  in MVP, anyone who can open the app can click another profile and read its DayLogs;
  anyone with OS-level access can read `payload.json` directly. **Say so where it
  matters** — one line of explanatory text when a profile is created, and no lock icons
  or "private" labelling anywhere in the picker. Implying a boundary that doesn't exist
  is worse than not having one.
- **MVP decision (DECIDED): ship local-only privacy, no encryption.** The bet is that the
  overwhelming majority of use is a personal machine, and that a user on a shared one will
  calibrate what they write — provided the app has told them the truth, which is what the
  note above is for. Encryption-at-rest stays deferred (§11) rather than rejected: §7's
  header+payload shape and the two rules stated there are what keep the door open, and they
  hold from the first release for reasons that have nothing to do with encryption. Nothing
  is built for it and nothing is maintained for it.
- **Reopen if a real user asks for it** — the machinery is a BCL feature and a threat model
  to own, not a rewrite.

---

## 9. Content

Markdown is the **container format** everything else sits inside, not a peer of the items below
it. Plain text is valid markdown, so nothing written before markdown lands needs migrating.

| Content | Status | Notes |
|---|---|---|
| **Markdown** | **MVP** | Bold, italic, headings, lists, links, fenced code. |
| Images | **MVP** | Attach and display. Stored beside the file (§7). |
| Maths via LaTeX + live preview | **MVP** | Inline and block maths inside markdown (`$...$`, `$$...$$`), rendered live beside the input. **mhchem (`\ce{H2SO4}`) is a hard selection criterion for the renderer, not a nice-to-have** — choose one without it and chemistry notation is permanently unavailable. |
| LaTeX cheat sheet / **insertion palette** | **MVP** | Searchable panel of common maths and mhchem commands. Clicking a symbol inserts the command at the cursor (`\frac{}{}` with the cursor parked in the first blank). Extends the markdown toolbar rather than adding a second mechanism. Delivers most of a visual editor's value cheaply, and teaches LaTeX by osmosis. |
| Syntax-highlighted code blocks | **MVP** | Fenced markdown code with per-language highlighting, for code and pseudocode entries. |
| Visual (WYSIWYG) maths editor | **Deferred** | A project in itself. The insertion palette is the stepping stone. |
| Inline PDF rendering | **Deferred** | Needs a PDF rendering engine (heavy). MVP: attach a PDF, open in the system viewer. |
| Chemical structure diagrams | **Deferred** | Benzene rings and similar, e.g. from SMILES. Use an image for now. |

### Formatting is markdown + a toolbar, not WYSIWYG (DECIDED)

Avalonia's rich text editor and its official Markdown control are **paid Pro-tier components
requiring a licence key**, which a free GPL app cannot use; building a rich text engine is the
same size of project as the deferred visual maths editor. A toolbar that wraps the selection in
markdown syntax gives the Word gesture (select, click **B**) with none of that — and it is the
*same* mechanism as the LaTeX insertion palette. One concept serving both.

---

## 10. UX principles

- **Intuitive like a new video game, not like default Anki.** A first-time user should
  understand the core loop without a manual. Anki's unintuitive defaults are a known, common
  complaint — this is a differentiator. The profile picker (§6) and the foot-in-the-door
  session (§4) are both applications of this.
- **Sensible defaults + progressive disclosure.** Power is available but not in your face.
- Choosing a content type or review shape must never feel like a commitment that changes how
  often an item is shown (§2 invariant).

### Tags, not decks

A deck is an exclusive container — one card lives in exactly one place — and it is the single
biggest source of friction in Anki's model, forcing an up-front decision about where a thing
belongs and duplication when it belongs in two. A tag set is non-exclusive and costs nothing to
change later. One entry can be `#chemistry` *and* `#exam-january` without existing twice.

Tags must land **early**, because every entry written before tags exist is an untagged entry
and retro-tagging a year of notes never happens. Two mechanisms make that survivable, and the
second matters more than the first:

- **Bulk-tagging from the browse screen** ships with tags, for clearing whatever backlog
  exists at that point in one deliberate pass.
- **Tagging is available wherever an entry is, including mid-review.** Noticing an untagged
  entry when it comes round in box 3 and labelling it there costs nothing and needs no
  decision to sit down and tidy. The ready pool hands you every entry eventually, so the
  untagged backlog drains through ordinary use rather than through a chore you have to
  schedule — the same shape as §5's backlog, which shrinks by being used and is never
  counted at you.

---

## 11. Deferred (designed-for, not built)

- FSRS / SM-2 scheduling (behind the scheduler interface).
- Visual WYSIWYG maths editor.
- Inline PDF rendering.
- **Sync / merge** of divergent review histories across machines (conflict resolution).
- **Encryption-at-rest + optional per-profile password.**
- Extra ladder steps if the retention curve wants them (trivial — the ladder is data).
- **Chemical structure diagrams** (e.g. SMILES → 2D rendering). LaTeX/mhchem covers *notation*;
  structures are a separate rendering problem.

---

## 12. Open questions

Genuinely open. Items settled elsewhere have been removed rather than left here looking
undecided — if it appears below, no decision exists yet.

- **Name** for the app (the code name `StudyDiary` is settled). *Needed by 1.0 at the latest*,
  since that is the version handed to a stranger, and renaming a public repo's product identity
  gets more expensive with every user.
- **Where does `session cap` live?** It has no home in the file format — §7's payload lists
  entries, history and DayLogs only. It is a serving concern rather than a scheduling one, so
  app-wide (a separate file) is the likely answer, but it is unwritten.
- **Is the session cap a hard stop or a default stopping point?**
  §4 sets a default cap of 10 and also has "keep going" pulling further batches, without
  saying whether the cap bounds them. Pick one.
- **Per-entity schema specifics.** The file-level shape is settled (§7) and the review-history
  event is settled (§7). What remains is the exact JSON shape of `Entry` and `DayLog` —
  including DayLog's id, its created-day and the timestamp that orders posts within a day (§8).
  The next design step, due before the first byte is written.
- **What happens to review history when the ladder changes.** `boxBefore`/`boxAfter` are stored
  precisely so the log stays true under a changed ladder, but nothing records *which* ladder was
  in effect. Probably fine — FSRS cares about outcomes and dates, not intervals — but unexamined
  rather than decided.
- **Whether Notes should ever allow a pure re-read mode** (currently: recall-first default).
- **Does a share carry its entries' attachments?** §7's table lists a share as "entries and tags
  only" and does not mention images. Omitting them means a shared entry arrives with a broken
  image reference; including them means a share is a folder or an archive rather than a single
  file. Nothing depends on it before shared export ships.
- **Does a DayLog post have a title?** A blog-like model implies one; a diary implies not. Free
  to decide before the schema exists, annoying afterwards — so it belongs with the per-entity
  schema question above.
- **What creation date does a shared entry get?** A share file carries no box state or history, so
  imported entries start fresh — but §7 currently gives them the *recipient's* creation date,
  which means an entry claims to have been written the day it was imported. That silently links it
  to the recipient's DayLogs for that day (§8), which is either a pleasant accident or a small lie
  about your own diary. The alternative — keeping the sender's date — links to DayLogs the
  recipient never wrote. Unexamined; nothing depends on it before shared import ships.

---

## 13. Reversals

Append-only. **Only decisions that changed**, with the date and the reason. Decisions that were
merely *made* live in the section they belong to, once — this log exists so that a reversal is
visible, not so that every choice is restated in two places.

### 2026-07-28 — `initial delay` removed

§3 originally gave new entries a configurable initial delay, carried on `LeitnerLadder`,
defaulting to 1 day and settable to 0 ("review the day I write it").

The defect that killed it: with a delay of 0, an entry created and passed the same day jumps
straight to box 2 and skips the 1-day rung — a creation-time option silently shortening the
ladder. Box 0 was considered as the fix and rejected (§3): it has no coherent fail transition in
either direction. A brief intermediate step, moving the delay off the ladder and into a setting
the App passes in, fixed the type confusion but not the skipped rung.

Removing it entirely makes the rule uniform — you enter a box, you wait that box's interval —
and hands the "review it today" impulse to free practice, where drilling deliberately does not
move boxes. The cost is that a custom first interval is no longer configurable; nothing suggests
anyone wants one.

Reopen only if a real user wants a first interval that differs from box 1's.
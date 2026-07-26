# Study Diary — Design Notes

> Working name: TBD. Open-source, local-first study diary with spaced-repetition review.
> This document is the source of truth for **product decisions**. It is meant to be edited
> as decisions change. Rationale is included where a future reader (or future us) might
> otherwise re-litigate a settled choice.

---

## 1. What this is

A free, local-first desktop app for reviewing what you've learned over the long haul
(designed for 5+ years of continuous personal use). No cloud, no telemetry. Installable by
non-technical users. Your data is yours: a local file, plus JSON export and plain
file-copy backup.

**Non-negotiables**
- Local-first. No cloud, no network dependency for core use.
- **No mandatory login.** A single user never has to think about accounts. Local **profiles**
  exist so several people *can* share one machine (§6) — but they are purely local and never
  phone home.
- Telemetry-free. Any "account" concept is for the user's own convenience, never data collection.
- Data portability: human-readable export; backup is "copy this file"; restore is one click (§7).
- Longevity over cleverness. Favor the .NET standard library and low-maintenance choices;
  every heavy dependency must justify itself against long-term maintenance cost.

---

## 2. Domain model

The atomic unit is an **Entry**. An Entry is made of three *independent* axes that must not
leak into each other. Keeping them decoupled is the core architectural rule of the whole app.

1. **Content** — what the entry is made of, i.e. what gets edited and rendered.
   MVP: freeform text, math (LaTeX), images. (PDF, visual math editor: deferred, see §9.)
2. **Review shape** — how the entry is tested.
   - **Card**: a prompt is shown, the body is hidden; you recall the answer from memory,
     reveal, and self-grade pass/fail.
   - **Note**: a single body you self-assess. Default presentation is still recall-first
     (cover → recall the gist → reveal), because passive re-reading is a weak study method
     and the testing effect is nearly free to bolt on.
3. **Scheduling** — the Leitner box state (§3). **Identical regardless of axes 1 and 2.**

**Design invariant:** the content type and the review shape have *zero* effect on
scheduling. A math flashcard and a plaintext note ride the exact same ladder. "How much
something is shown" is decided entirely by axis 3 and nothing else.

**Not everything is an Entry.** The **DayLog** (§8, optional daily diary) is a *separate
entity* with no review shape and no Leitner state. It exists alongside Entries, not as a
fourth axis on them. Bolting journaling onto Entry would break this invariant; don't.

Capture and browsing feel diary-like (dated entries you can flip back through). Review is
active-recall by default, because the stated goal is to actually learn.

---

## 3. Scheduling — spaced repetition (classic Leitner)

Deterministic: two people applying these rules to the same history compute the same next
review date.

### Box ladder

| Box | Waiting time while sitting in this box |
|-----|----------------------------------------|
| 1   | 1 day    |
| 2   | 1 week   |
| 3   | 1 month  |
| 4   | 6 months |
| 5 (cap) | 1 year, forever |

### Rules

- **New entry** → enters **box 1**. First review date = *created date* + *initial delay*.
  - `initial delay` is a setting, **default 1 day**, settable to 0 ("review the day I write it").
  - Writing the entry into the app *is* the first exposure, so there is no separate
    "review me now" state. (This is why there is no box 0.)
- **Pass** → box `min(N + 1, 5)`. Next review = **actual review date** + new box's waiting time.
- **Fail** → box **1**. Next review = **actual review date** + 1 day.
- **Cap (box 5)** repeats at 1 year forever. **No auto-retire** — you keep things you learned
  years ago from silently rotting. A manual **archive** action exists for deliberate removal
  from rotation.

### Why anchor to *actual* review date, never scheduled date

The interval is a function of the **box**, not of elapsed time. Missing days (weekends,
illness, life) neither punishes nor rewards you — the ladder just stretches in wall-clock
time and continues. There is structurally nowhere for the app to shame you for lateness.
A genuinely forgotten item fails its review and drops to daily drilling; that is the system
working, not a penalty.

### Time granularity

Scheduling uses **whole-day dates**, not timestamps. An item due "today" is due all day.
This sidesteps most timezone/clock complexity. (Timestamps may still exist on entries for
diary/browsing purposes; they just don't drive scheduling.)

### Implementation stance

The ladder is **data, not code**: a small config object (`initial delay` + ordered list of
intervals), never hardcoded `if box == 2` branches. Consequences:
- Adding/removing a box (as we just did, 1y → +6m) is a one-line change.
- The whole scheduler sits behind an interface so a future FSRS/SM-2 experiment is a swap,
  not a rewrite. FSRS/SM-2 are **deferred**; pure Leitner is the MVP.

---

## 4. Sessions & review behaviour

Two distinct mechanisms — kept separate on purpose.

### The ready pool
- An item is **ready** when its next-review date ≤ today. "Ready" is *derived*, not stored.
- **Default session cap = 10.**
- **Foot-in-the-door serving.** When the ready pool is large (you've been away), the session
  opens with a **tiny first batch (~2)**, not the whole cap. Finish it and you can pull more in
  small steps, up to the cap of 10. The point is to make *starting* trivial; the hard part of
  study is beginning, so we shrink the activation energy.
- A voluntary "keep going" pulls further batches for anyone who wants to grind a backlog down.
- The backlog shrinks through ordinary use and is **never surfaced as a number/counter**.
- During review, an optional toggle can reveal that day's DayLog (§8) after you answer.

**Important separation:** all of the above is a *serving* concern — how items are handed out of
the ready pool. It does **not** touch scheduling or the definition of "ready", so it cannot
corrupt spacing. It is pure UI/session logic.

### Free practice / drill (catch-up mode)
- Reviewing items that are **not** ready yet, for extra reps, because you feel like it.
- **Free practice does NOT move boxes and does NOT reschedule anything.** It is pure bonus
  study that leaves the real schedule untouched. This is what stops "extra practice" from
  silently corrupting the spacing.

---

## 5. Anti-toxic-debt principles (product-wide)

These are hard constraints, not nice-to-haves. They shape schema and UI.

- **Never surface accumulating debt.** No "N overdue," no red late badges, no guilt counter,
  no streak-shaming.
- **Positive framing only.** The vocabulary is "**ready for review**" / "**ready today**",
  not "due" / "overdue". The word "due" should not appear in the UI.
- Missed items don't pile up as visible failure; they quietly stay *ready* until you get to
  them.
- Combined with actual-date anchoring (§3), the app is *structurally incapable* of telling
  you you're behind.

---

## 6. Accounts & profiles (local, multi-user)

> Full design deferred to a near-future session; this captures the model and the constraints.

- **Local profiles only.** Purpose: let several people share one computer, each with their own
  entries and schedule. Not authentication to a server; nothing leaves the machine.
- **Video-game / Netflix model.** Pick a profile like choosing a save slot — no login friction.
  A **default profile** exists so a solo user never has to create or think about one.
- Each profile owns its own data (its own Entries + Leitner state + DayLogs). Profiles do not
  see each other's entries.
- **No passwords in MVP.** Optional local password-protection is a deferred nicety and is
  local-only — it has nothing to do with cloud auth. (See §8 for the privacy tradeoff it carries.)
- Profiles and "import a save file" (§7) are the **same machinery**: importing lands as a profile.

---

## 7. Backup, save & load (MVP)

Backup must be trivial and restore must be *even easier* — this is an explicit MVP goal.

- **The data is a single local file** (the SQLite database), plus a human-readable JSON export.
  Backup = copy that file somewhere safe. The app helps the user find/produce it (e.g. a
  "reveal my data file" / "export" action) rather than making them hunt through app-data folders.
- **Restore / import = one click.** The user has a save file (moved from another computer, sent
  by a friend, etc.). The app shows a file-picker, and then **installs the file into the correct
  location itself** — the user never has to know where app-data lives.
- **Import COPIES, never moves, by default.** Moving would silently delete the user's only backup
  from where they put it. "Move" may exist later only as an explicit, clearly-labelled option.
  Restore must never be able to destroy the source it restored from.
- **MVP scope is import / restore, NOT sync.** Bringing a file in as a profile is simple.
  *Merging* two divergent histories (same entry, different box on two machines — which wins?) is
  a genuine tarpit and is **deferred** (§12). Keeping load "dead simple" means not opening that door.

---

## 8. Daily diary / journal (optional)

- A **DayLog** is optional free text attached to a *calendar day* — at most one per day per
  profile. Never mandatory; most days may have none.
- **A DayLog is NOT an Entry** (see §2). No review shape, no Leitner state; never reviewed,
  never in the ready pool. Journaling is a separate entity, full stop.
- **Surfaced during review, optionally.** Entries carry a created-date. After you answer/reveal
  an Entry, an optional **toggle** shows the DayLog for *that entry's created-day* — "what you
  journaled the day you learned this." Pure read; touches nothing in scheduling.
  - Emergent nicety: reviewing a fact months later can resurface the context/mood of the day
    you first wrote it.

### Privacy (the sensitive part)
- **Baseline: already private.** Local-only + no cloud + no telemetry (§1, §11) means diary text
  **never leaves the machine** over any network. There is nothing to opt out of.
- **The real exposure is a shared machine (§6).** Profile separation hides diaries between
  profiles in the UI, but does not by itself stop another OS user (or profile) from opening the
  raw data file on disk.
- **Encryption-at-rest is the strong fix — with a hard tradeoff.** Optional per-profile password
  + encryption makes the diary unreadable without the password. But local-first means **no
  recovery**: a forgotten password = the diary is gone forever, by design. Genuine data-loss risk,
  plus a crypto dependency and a threat model to own.
- **MVP decision (DECIDED):** ship **local-only privacy, no encryption**; keep encryption
  **designed-for but deferred** (§12). The schema must leave room to encrypt a profile later
  *without* a painful migration.

---

## 9. Content types & MVP slice

| Content type | Status | Notes |
|---|---|---|
| Freeform text | **MVP** | Trivial. Day-one. |
| Images | **MVP** | Cheap to attach + display. |
| Math via LaTeX + live preview | **MVP** | Type LaTeX source, see it rendered live beside the input. Requires a math-rendering library (evaluate current Avalonia ecosystem before choosing). |
| LaTeX cheat sheet / **insertion palette** | **MVP** | Searchable panel of common math commands. Clicking a symbol inserts the command at the cursor (e.g. `\frac{}{}` with cursor parked in the first blank). Delivers most of a visual editor's value cheaply, and teaches LaTeX by osmosis. |
| Visual (WYSIWYG) math editor | **Deferred** | A project in itself. Designed-for, not built. The insertion palette is the stepping stone. |
| Inline PDF rendering | **Deferred** | Needs a PDF rendering engine (heavy). MVP: attach PDF, open in system viewer; inline-render images only. |

---

## 10. UX principles

- **Intuitive like a new video game, not like default Anki.** A first-time user should
  understand the core loop without a manual. (Anki's unintuitive defaults are a known,
  common complaint — this is a differentiator.) The profile picker (§6) and the
  foot-in-the-door session (§4) are both applications of this.
- **Sensible defaults + progressive disclosure.** Power is available but not in your face.
- Choosing a content type or review shape must never feel like a commitment that changes how
  much an item is shown (see §2 invariant).

---

## 11. Tech stack (tentative — verify before locking)

- **C# / .NET**, **Avalonia UI**, **SQLite**. Cross-platform installers for non-technical users.
- Not locked in. Challenge every heavy dependency against the standard library and long-term
  maintenance cost.
- **Verify current tooling** (`learn.microsoft.com`, `avaloniaui.net`) for SDK version,
  project templates, and packaging/installer steps at scaffold time — do not trust stale
  training data on versions.

### Telemetry (checked, 2026-07)
- **No telemetry ships in the app.** A compiled .NET app does not phone home; there is no
  runtime telemetry baked in. End users and their diaries are never touched.
- The only .NET telemetry is **developer-side**: the SDK/CLI (`dotnet build`/`run`, and the newer
  Microsoft Testing Platform) send anonymous usage data from the *developer's* machine. Per MS
  docs it collects no personal data, doesn't scan code, and isn't embedded in the build.
- Opt out on the dev machine with `DOTNET_CLI_TELEMETRY_OPTOUT=1` (and
  `TESTINGPLATFORM_TELEMETRY_OPTOUT=1` for the test platform). This is optional hygiene; it has
  zero bearing on the "no telemetry" promise to users, which the ship-nothing-that-phones-home
  architecture already guarantees.

### Separation of concerns (to be enforced)
- **I/O** (SQLite persistence, file backup, JSON export) ‖ **domain logic** (Entry model,
  DayLog, scheduler) ‖ **UI** (Avalonia views) are separate and depend inward, not outward.
- The **scheduler** lives behind an interface, testable in isolation with no UI or DB.

---

## 12. Deferred (designed-for, not built)

- FSRS / SM-2 scheduling (behind the scheduler interface).
- Visual WYSIWYG math editor.
- Inline PDF rendering.
- **Sync / merge** of divergent review histories across machines (conflict resolution).
- **Encryption-at-rest + optional per-profile password** (diary privacy on a shared machine).
- Extra ladder steps if the retention curve wants them (trivial — ladder is data).

---

## 13. Open questions

- **Name** for the app.
- "Keep going" beyond the cap: should the user be able to **choose what to continue with**
  (by subject / tag / content type)? Depends on an organization/tagging design we haven't done.
- Whether Notes should ever allow a pure re-read mode (currently: recall-first default).
- Exact import UX when profiles exist (new profile vs restore-into-current).
- Schema specifics (next design step after architecture is agreed).

---

## 14. Decision log

- Atomic unit = **Entry** = content + review shape + Leitner state; three axes decoupled.
- **DayLog** (optional daily diary) is a **separate entity**, not an Entry; no review/Leitner
  state; surfaced read-only via a toggle during review of that day's entries.
- Content type & review shape have **zero** effect on scheduling.
- Review is **active-recall by default**; capture/browse is **diary-like**.
- Scheduler = **classic Leitner**, binary pass/fail, ladder **1d / 1w / 1m / 6m / 1y**,
  cap repeats forever, no auto-retire, manual archive.
- Next review anchored to **actual review date**, not scheduled date.
- **No box 0**; new entries enter box 1 with a settable `initial delay` (default 1 day).
- Ladder is **data**, scheduler behind an **interface** (FSRS-ready).
- Whole-day date arithmetic for scheduling.
- **No toxic debt**: "ready", never "due/overdue"; capped batches; free-practice never
  reschedules.
- **Session cap = 10**, foot-in-the-door first batch (~2) when behind; serving is UI-only and
  never touches scheduling.
- **Local profiles** (video-game/Netflix model), default profile, no passwords in MVP,
  no telemetry — for user convenience, not data collection.
- **Backup = copy a file; restore = one click.** Import COPIES (never moves) by default.
  MVP = import/restore, **not** sync/merge.
- **Diary privacy (decided):** MVP is local-only baseline (nothing leaves the machine);
  encryption-at-rest **deferred**, schema to stay encryption-ready.
- **No shipped telemetry** (checked): .NET telemetry is developer-side SDK/CLI only, opt-out via
  `DOTNET_CLI_TELEMETRY_OPTOUT`; nothing is baked into the app.
- MVP content: **text + images + LaTeX-with-live-preview + insertion-palette cheat sheet**.
- Deferred: visual math editor, inline PDF, FSRS, sync/merge, diary encryption + profile passwords.
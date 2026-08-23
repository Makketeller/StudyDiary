# Architecture

> Stack, project layout, development environment, and the code rules that make DESIGN.md's
> invariants structural rather than remembered. Changes when the code does.
>
> **Reference rule:** this file cites DESIGN.md **with** section numbers, because DESIGN changes
> rarely and its numbering is stable. DESIGN.md and ROADMAP.md may point back at this file, but
>  **never with a section number** — this file renumbers whenever the code moves.
> It points at nothing else: not ROADMAP.md, which changes per release, and not STATUS.md, which
> changes every session.
> If a rule here can only be stated with a release number 
> attached, the rule belongs in ROADMAP.md.

---

## 1. Stack (verified 2026-08)

- **.NET 10** — current LTS (SDK 10.0), supported to **November 2028**. LTS chosen deliberately
  for DESIGN §1's longevity principle: three years of patches, no forced churn.
- **Avalonia 12** — current major; scaffolded from the `Avalonia.Templates` package on the **12.1.x**
  line. Deliberately not pinned to a patch here, so a routine bump doesn't make this file wrong.
  Note: Avalonia 12 carries breaking changes vs 11 — renames, data-validation handling moved to the
  base `Control`, obsolete APIs removed. **Ignore Avalonia-11-era tutorials.**
- **Persistence: JSON via `System.Text.Json`** — in the BCL, so no dependency. **Two files per
  profile**, a plaintext `profile.json` header and a `payload.json` blob, plus an `attachments/`
  folder once images ship; `schemaVersion`, Guid ids, atomic write-then-replace. DESIGN §7 has the
  shape and the argument for two files rather than one. SQLite is **not a scheduled migration**: if
  it ever lands it arrives as a second `IEntryStore` implementation behind the existing interface,
  and the access approach (raw `Microsoft.Data.Sqlite` vs an ORM) gets weighed against the
  dependency-cost rule at that point, not now.
- **Solution format: `.slnx`** — the newer XML solution format, default in .NET 10. Some tooling
  still expects the older `.sln`; if a tool can't find the projects, this is a prime suspect.
- **Internal/code name: `StudyDiary`** — solution, projects and namespace root. The *product* name
  is still open (DESIGN §12). Deliberately allowed to differ: renaming namespaces later is tedious,
  renaming a product is not.

Not locked forever: challenge every heavy dependency against the standard library and long-term
maintenance cost — and against licence compatibility, since GPLv3-or-later (DESIGN §1) rules out
proprietary and licence-key components outright. But the five items above are settled; don't
re-litigate without a reason.

---

## 2. Project layout

Separation of concerns is enforced by the **compiler**, not by discipline: a project can only
reference what it explicitly declares, so `StudyDiary.Domain` having no reference to Avalonia or to
any storage library makes it *physically incapable* of depending on them.

```
StudyDiary.slnx
├── src/
│   ├── StudyDiary.Domain      class library — Entry, DayLog, scheduler. No UI, no I/O.
│   ├── StudyDiary.App         Avalonia shell.        → references Domain, Data
│   └── StudyDiary.Data        IEntryStore, JSON, backup, migrations. → references Domain
└── tests/
    ├── StudyDiary.Domain.Tests  xUnit.                            → references Domain
    └── StudyDiary.Data.Tests    round-trips a saved profile folder. → references Data
```

**Dependencies point inward, toward Domain, always.** Domain references nothing of ours, and
nothing of anyone else's beyond the BCL.

`StudyDiary.Data` and `StudyDiary.Data.Tests` are scaffolded in the first release, since the thin
slice persists.

**Namespaces mirror the projects**, with `StudyDiary` as the root, and Domain is subdivided by
concern — `StudyDiary.Domain.Scheduling` holds the ladder, the interval and the scheduler. Keeping
the subdivision is worth the small friction: it is the cheapest available signal that a type has
wandered into a neighbourhood it doesn't belong in.

---

## 3. Development environment & cross-platform release

**Dev machine: Fedora (Linux).** Local commands in notes assume `dnf` and Linux paths. This is a
dev-machine fact, not a scope narrowing (DESIGN §1).

**Editor: VS Code + the official Microsoft C# extension.**

- VSCodium was tried first and **does not work** for C#: Microsoft's C# extension and C# Dev Kit are
  licensed only for official VS Code builds and are not published to Open VSX. Without a language
  server there is no IntelliSense, no hover docs, no live diagnostics. Third-party options exist
  (SharpLsp — MIT, promising but 0.x/alpha as of 2026-07; community forks; the official
  `roslyn-language-server` wired up by hand). Recorded so future-us doesn't repeat the search.
- **C# extension only — not C# Dev Kit.** Dev Kit carries VS-Community-style license terms, expects
  a Microsoft sign-in, and bundles IntelliCode AI completion. None of that is needed; the C#
  extension alone provides the Roslyn language server and debugger.
- VS Code telemetry is set to `off` (`telemetry.telemetryLevel`). Unlike Rider's free
  non-commercial license, which cannot opt out of usage statistics, VS Code's is switchable.

**Release targets: Windows, macOS, Linux** (DESIGN §1). The app code is fully cross-platform from
one codebase; nothing platform-specific exists in Domain.

**Runtime identifiers.** `linux-x64` and `win-x64` from the first release. macOS needs **`osx-arm64`
first** — every Mac sold since 2020 is Apple Silicon — with `osx-x64` optional and only if someone
asks. `linux-arm64` and `win-arm64` are unscheduled; each is one more `dotnet publish` line if a
user ever turns up needing one.

### No installer on Windows or Linux — a decision, not an omission

A self-contained single-file executable already opens on double-click on Windows with no runtime, no
installer and no admin rights. SmartScreen shows "Windows protected your PC" only for files carrying
the downloaded-from-the-internet marker — copied by USB or network share there is no prompt at all,
and downloaded from GitHub it is one click through *More info → Run anyway*.

**Code signing is rejected on value.** *(Figures checked 2026-07; the conclusion is meant to survive
them moving.)* Certificates run a few hundred dollars a year and are capped at short, roughly annual
lifespans, so it is a permanent subscription rather than a purchase; and EV certificates no longer
bypass SmartScreen on first download, so paying does not remove the warning anyway. A recurring cost
forever, to soften one click on a free app, is the wrong trade — and it stays the wrong trade unless
signing starts actually removing the prompt. Document the click in the README instead.

Linux is the *harder* double-click case: GNOME Files will not execute a binary on click, so ship a
`.desktop` entry alongside the executable. Ten lines of text, not a packaging system. Flatpak or
`.rpm` remain options if the app ever has non-technical Linux users.

**macOS is the exception.** A `.dmg`/`.pkg` needs Apple notarization, which requires a Mac, so
cross-compiling from Fedora is not enough. Expected approach: GitHub Actions with a macOS runner
doing that one job, while the Linux and Windows binaries are plain `dotnet publish` output from any
runner.

---

## 4. Domain code conventions

These follow from DESIGN's decisions. They exist so the invariants are enforced by types and
signatures rather than by remembering.

### What Domain is not allowed to know

DESIGN §2 calls the decoupling of content, review shape and scheduling *the core architectural rule
of the whole app*. A rule that load-bearing should not depend on anyone remembering it. Each item
below names something Domain must not learn, and the mechanism that stops it learning.

**The clock.** "Today" and the actual review date arrive as *parameters*. A method that calls
`DateTime.Now` internally can only be tested by changing the system clock; a method that takes a date
can be tested for any date, instantly. This is what makes DESIGN §3's determinism claim literally
checkable. Forbidding it in Domain only works if it is *allowed* somewhere, or the app can't run:
that somewhere is a single `TimeProvider` (BCL since .NET 8 — no dependency) resolved once in App
and passed down, with `Today` derived from it as a `DateOnly` in the user's local timezone
(DESIGN §4). **No other line in the codebase calls `DateTime.Now`, `DateTime.Today` or
`DateTimeOffset.UtcNow`** — that is a grep, which makes it an enforceable rule rather than an
intention, and it is three lines of CI whenever CI exists.

**What an Entry is made of.** DESIGN §2's invariant — content type and review shape have *zero*
effect on scheduling — is enforced by the scheduler's **signature**, not by care. The shape,
whatever the method ends up called:

```csharp
ReviewState Advance(ReviewState current, ReviewOutcome outcome, DateOnly reviewedOn, LeitnerLadder ladder);
```

It receives box state, an outcome, a date and the ladder. It is **never handed an `Entry`**, so it
cannot read a review shape or a body, and therefore cannot branch on either. Put `Entry` in that
signature and the invariant reverts to discipline — which is the thing this file exists to prevent.
`ReviewState` holds scheduling state and nothing describing content.

The same signature buys DESIGN §2's *one entry, one ladder* rule for free: a scheduler that cannot
see an entry cannot be told the entry generated three prompts, so per-prompt scheduling is not
something a future cloze feature could introduce by accident.

(Whether the ladder arrives as a parameter or as a constructor dependency is taste. What is not
taste: it arrives from outside, and the scheduler never reaches for a static.)

**Tags.** DESIGN §2 states they must never reach the scheduler, since a per-tag ladder would make how
often something is shown depend on its subject. Same mechanism as above — tags live on `Entry`, and
the scheduler never sees an `Entry`.

**Filtering, and the session cap.** Both are *serving* concerns and live in the UI layer (DESIGN §4).
The domain never learns that filtering exists, and the session cap has no domain type and no domain
validation — it must not acquire one later just because a settings screen appears. Contrast the
ladder, which is a domain value and *does* validate at construction (below); the difference is that
one is a rule about correctness and the other is a preference about pacing.

**Whether a rep was practice or scheduled.** Free practice is enforced by *absence*: it simply does
not call the scheduler (DESIGN §4). `isPractice` is a field on the **persisted review event** and
appears nowhere in Domain — not as a parameter, not as a property, not as an enum member. Second
grep. If a `LeitnerScheduler` ever branches on it, enforcement-by-absence has been thrown away.

**That files exist.** Domain references neither `System.Text.Json` nor `StudyDiary.Data`; migration
code lives in Data behind `IEntryStore` (DESIGN §7). This one is enforced by the reference graph in
§2 and needs no vigilance at all.

### Types: `record` vs `class`, and the equality trap

**Value objects are `record`; entities are `class`.** The test is: *is this thing defined by its
contents, or by its identity?*

- **`record`** — interchangeable data. Two instances with equal contents *are* the same thing.
  → `ReviewInterval`, `LeitnerLadder`, `ReviewState`.
- **`class`** — has an identity that persists while contents change. Two entries with identical text
  are **different entries**; editing an entry's text leaves it the same entry.
  → `Entry`, `DayLog`.

Getting this backwards is a real bug, not a style preference: a record-typed `Entry` would report two
distinct entries as equal whenever their text matched, and they would collide in sets and
dictionaries.

**A profile is not a Domain entity.** The profile header is a Data-layer type: the picker reads
`profile.json` and never opens the payload (DESIGN §7), and nothing in Domain needs to model a
profile to schedule, review or write anything. If that changes, it changes with a reason recorded.

**Types are `sealed` unless something is designed to derive from them.** Sealing is the default
rather than an optimisation: it keeps an inheritance hierarchy nobody designed from appearing, and
for records it removes a genuine footgun — compiler-generated record equality compares the runtime
`EqualityContract` as well as the members, so a derived record is never equal to its base even with
identical contents. Sealing makes that question unaskable.

**A `record` holding an `ImmutableArray` must override `Equals` and `GetHashCode`.** Compiler-
generated record equality calls `EqualityComparer<T>.Default` on each member, and for
`ImmutableArray<T>` that is **reference equality on the underlying array** — so two ladders with
identical contents compare *unequal*, which is exactly what a record is supposed to make impossible.
Fix: override `Equals` using `SequenceEqual`, and `GetHashCode` by folding the elements through the
BCL `HashCode` struct. Any future record with an array-shaped member inherits the same obligation.

**Domain values are immutable; state transitions produce new values.** A review does not mutate an
entry's state — it returns a new `ReviewState` (`with` expressions make this cheap), so a
half-applied transition is not a state the type system permits. This is *not* what protects free
practice from corrupting the schedule: that guarantee is absence (DESIGN §4), and immutability is
the second layer behind it, not the first.

**Validation belongs in `init` accessors, not in the constructor body.** A `with`
expression does not call the primary constructor: the compiler generates a protected
copy constructor, `with` invokes that, and then assigns through each property's `init`
accessor. So a check written in the primary constructor body is bypassed by exactly the
operation the paragraph above encourages —
`LeitnerLadder.Default with { BoxIntervals = default }` produces an invalid ladder and
throws nothing. The fix has two halves, and the second is easy to miss. Declaring the property
explicitly in the record body does suppress the synthesized one — but it also leaves the
primary constructor parameter assigned to nothing, which the compiler reports as
**CS8907** (*"Parameter 'Count' is unread"*). That is a warning, not an error: the build
succeeds and every instance silently carries the type's default value. So the parameter
must be read explicitly in a field initializer, and the rule then has two entry points —
construction and `with` — which is one rule too many to write twice. Factor it into a
private static that both call:

    private readonly int _count = Validated(Count);   // primary constructor
    public int Count
    {
        get => _count;
        init => _count = Validated(value);            // with-expression
    }
    private static int Validated(int count) { ... }

The copy constructor copies the backing field directly, which is correct — it was
validated when first set. Any future record with a validated member inherits the same
obligation, including a round-trip test that a constructor argument survives to its
property. That test is the only thing that catches CS8907 becoming a silent default.

### Dates and time

**Whole-day dates use `DateOnly`, never `DateTime`.** DESIGN §3 says scheduling is whole-day; using a
type with no time-of-day and no timezone makes that structurally true instead of a comment someone
can violate.

**Entities carry two creation dates, and both are stored.**

- `CreatedOn` — a `DateOnly`. The calendar day the entry was written, in the user's local timezone at
  the moment of writing. This links an Entry to a DayLog (DESIGN §8) and feeds any date arithmetic.
- `CreatedAt` — a `DateTimeOffset`. Display and stable ordering only (newest-first browsing needs to
  break ties within a day). **Never read by scheduling.**

`CreatedOn` is **stored, not derived from `CreatedAt` at read time.** Deriving it would mean the day
an entry belongs to could shift when the user travels or the machine's timezone changes — an entry
written at 23:40 could silently move to the next day and lose its DayLog link. The offset in
`CreatedAt` is retained for the same reason: it records *what the clock said where the user was*,
which is the fact a diary wants.

A DayLog carries the same pair for the same reasons (DESIGN §8): a created-day that decides which day
it belongs to, and a timestamp that orders posts within that day.

### The ladder and the scheduler

**Ladder intervals are `ImmutableArray`, not `IReadOnlyList`.** `LeitnerLadder.Default` is a single
shared static instance; `IReadOnlyList` is only a read-only *view*, so anyone holding the original
`List` reference could mutate the default ladder process-wide. Ships in the BCL — no new dependency.
(Note: `.Length`, not `.Count`; and `default` of an `ImmutableArray` wraps a null array that throws
on first use.)

**Value objects validate on construction, and validation lives where the rule is uniform.**
`LeitnerLadder` rejects a `default` or empty `BoxIntervals` and any interval with
`Count <= 0`. `ReviewInterval` permits `Count >= 0`, rejecting only negatives (DESIGN §3).
Both follow the two-entry-point pattern above.

The split is not arbitrary. `ReviewInterval` is a general span of whole days, months or years, and
zero is arithmetically meaningful on a span — `AddTo` simply returns the same date. Positivity is a
property of a *rung*: a box you wait zero days in is not a box. Putting the rule on the wrong type
either forbids a legal value or lets an illegal one through, and both look like style choices when
they are not. The `default` check is not paperwork either: without it, an uninitialised ladder throws
somewhere far from where it was constructed.

Enforced at construction **now**, not when a settings UI exists, because the ladder is being
constructed now and so the invalid states are reachable now. A settings screen then has only to
surface the resulting error, not invent the rule.

**Box-number ↔ index translation lives on `LeitnerLadder.IntervalForBox(box)`**, not in the scheduler.
Boxes are 1-based, the array is 0-indexed; box numbering is the ladder's own concept, so the
scheduler never writes `- 1`, and range validation gets a natural home.

**The ladder is the single source of truth for its own size and its own rungs** (DESIGN §3). Two
consequences, both code rules rather than preferences:

- **The top box is the ladder's length (`MaxBox`), never a literal `5`.** A hardcoded cap would make
  a sixth box silently unreachable, breaking the promise that adding a rung is a one-line change.
- **The fail path looks up box 1's interval rather than hardcoding a day.** Numerically identical
  today — that is precisely the point. It stays correct when the ladder changes.

**No `Week` interval unit.** A week is exactly 7 days with no calendar subtlety, so `(1, Week)` and
`(7, Day)` behave identically but compare **unequal** under record value-equality — two encodings of
one value, which leaks into tests, settings comparison and serialization. `Month`/`Year` genuinely
cannot be reduced to days (the conversion depends on the date), so they stay. Box 2 is `(7, Day)`;
rendering that as "1 week" is a UI formatting concern.

**The scheduler sits behind an interface** (`IReviewScheduler`), testable with no UI and no files, so
a future FSRS/SM-2 implementation is a swap rather than a rewrite (DESIGN §3, §11).

**Readiness is derived, never stored.** An item is ready when its next-review date is on or before
today (DESIGN §4). A stored `IsReady` flag would need invalidating at every midnight and would be
wrong for exactly as long as nobody noticed. Compute it from state and a supplied date.

### Naming

**The word "due" does not appear anywhere** — not in a type, a property, a method, a local, a test
name or a UI string. DESIGN §5 makes "ready" the vocabulary and keeps "due"/"overdue" out of the UI;
the cheapest way to keep a word out of the UI is for the value behind it never to have been called
that. `NextReviewOn`, `IsReady(today)`, `ReadyEntries`. Third grep.

The same section's harder constraint is not a naming rule and cannot be made one: no count of
outstanding work is surfaced at all, so a correctly-named `ReadyEntries` still must not be rendered
as a number. That one stays a UI review, not a grep.

**Enums that get persisted have explicitly pinned integer values.** An enum is an int underneath; if
members are reordered after values have been stored as integers, every saved row silently changes
meaning. Due from the first release, since `outcome` is persisted then. Pin `IntervalUnit` as well
even though nothing persists it today — it costs one line per member, and a ladder that ever becomes
a saved setting would make it persisted retroactively. How enums are *written* is Data's business
(§5).

---

## 5. Data layer conventions

Domain has no idea files exist; everything below is `StudyDiary.Data`'s job. These are DESIGN §7's
rules restated as code obligations, and all of them hold from the first release.

**The header/payload split is a code rule, not just a folder layout.** `profile.json` carries
`schemaVersion`, the profile id, the profile name and `encryption` (`"none"` today). `payload.json`
carries entries, review history and DayLogs. Two invariants follow:

- **Nothing outside the payload may contain user text.** The profile name is the one accepted,
  stated leak, because the picker has to show it. Attachments are the other: they are files in
  `attachments/`, not payload content.
- **Nothing may depend on reading part of the payload without reading all of it.** No byte-range
  reads, and no index living outside it.

Both are what make encryption a later change of *content* rather than a change of *shape*. Nothing is
built for encryption; these two rules are what keep it free, and they earn their place for reasons
that have nothing to do with it.

**The profile picker reads the header only** and never opens the payload. That is what keeps it
working unchanged if `payload.json` ever becomes ciphertext.

**Atomic write-then-replace now covers two files, not one.** Write to a temporary file in the same
directory, then replace. In practice the header changes almost never, so an ordinary save writes the
payload alone; the two move together only when a profile is created or renamed.

**Absent reads as default.** A new optional property is never marked `required` and its absence never
throws (DESIGN §7). Additive changes do not bump `schemaVersion`.

**Refuse to write rather than write a partial object.** Two cases, one rule: a `schemaVersion` higher
than this app understands, and a payload that will not parse. Say so, name the file, and do not save.
Silent field-dropping on save is the one way a local-first app destroys the data it was trusted with.

**A missing attachment is not a parse failure.** It renders as a visible placeholder in that one
entry and changes nothing else. The entry keeps its reference, and the payload is never rewritten to
"clean up" a file the user may be about to restore.

**Attachments are referenced by bare filename** — a generated id plus the original extension
(`a3f2c9d1.png`), resolved relative to the data folder at load time. Never an absolute path, which
breaks the moment the folder is copied; never the user's filename, which breaks the moment two
`diagram.png` files meet. The original name is stored alongside as a **display label only**; nothing
ever resolves through it.

**Attachments are never shared between owners, and orphans are never collected.** The same image
pasted into two entries writes two files with two ids — no deduplication, no reference counting. No
code path deletes an attachment automatically, ever.

**Enums serialize as strings** (`JsonStringEnumConverter`) while persistence is JSON: it sidesteps
the reordering hazard and keeps the file readable by eye. Pin the integers anyway (§4) — it is the
pinning that makes any later move to a numeric store safe rather than a silent reinterpretation of
every row.

**Paths are built with `Path.Combine`, always,** and the data folder is resolved from
`Environment.SpecialFolder.LocalApplicationData`, which already resolves correctly per OS. A literal
`/` or `\` in a path string is a portability bug that the dev machine will never reveal.

**Migration code lives here, behind `IEntryStore`** — never in Domain (DESIGN §7).

---

## 6. Telemetry (checked 2026-07)

- **No telemetry ships in the app.** A compiled .NET app does not phone home; there is no runtime
  telemetry baked in. End users and their diaries are never touched. This is DESIGN §1's promise, and
  the ship-nothing-that-phones-home architecture is what guarantees it.
- The only .NET telemetry is **developer-side**: the SDK/CLI (`dotnet build`/`run`, and the newer
  Microsoft Testing Platform) send anonymous usage data from the *developer's* machine. Per MS docs
  it collects no personal data, doesn't scan code, and isn't embedded in the build.
- Opt out on the dev machine with `DOTNET_CLI_TELEMETRY_OPTOUT=1` (and
  `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`). Optional hygiene; it has zero bearing on the promise to
  users.
- The same reasoning covers editor choice and any future website funding model: what matters is what
  the **shipped binary** does.
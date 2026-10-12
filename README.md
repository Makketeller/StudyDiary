# StudyDiary

A free, local-first desktop study diary with spaced-repetition review.
No cloud, no telemetry, no login. Your data is a file on your machine.

Status: early development. Version 0.1.0 is out for Linux and Windows.

## Write what you learn. Still know it in five years.

StudyDiary is a diary for the things you learn. Write an entry, a question with its answer or
just a note, and it comes back for review when it is ready: the next day, then a week later,
then a month, then half a year, the gaps growing each time you remember it. Each review asks you
to recall before it shows you the answer, because recalling is what makes knowledge stay.

It is built for the long haul: years of entries from a degree, a job or a hobby, still there
and still reviewable long after the course has ended.

## No backlog, no guilt

Most spaced-repetition apps count what you owe them. Miss a week and a number turns red, a
streak breaks, and the backlog becomes the reason you stop.

StudyDiary never counts what you haven't done. An entry is *ready*, never *due* or *overdue*.
You review in short sessions of up to ten, *done for now* is always one click away, and
stopping is never held against you. Half of each session goes to what has waited longest, so
nothing is lost in a pile, and nothing is thrown in your face either.

## Your data, for years

Your diary is a pair of plain JSON files in a folder on your computer, readable in any text
editor. There is no account, nothing to sign into, and the app has no network code at all.

Every save is written to the side, read back and checked, and only then swapped in, so a crash
or a power cut cannot leave you with a half-written diary. After every save the app keeps a
recovery copy of its own. If a file is ever damaged, by a disk, a sync tool or a hand edit gone
wrong, StudyDiary keeps the damaged files as it found them, tells you plainly what happened, and
offers the newest copy that opens. It never quietly starts you over with an empty diary.

A diary written by every release, from 0.1.0 on, is kept in this repository. From 0.4.0 the
tests check that every one of them still opens.

## Where it's going

0.1.0 is deliberately bare: write, browse, delete, review. Next come editing entries and tags,
then the foundations: backup and restore you can trust, and a proper look that follows your
system's light or dark setting. After that come the things that make it a real study tool: free
practice outside the schedule, Markdown, LaTeX and chemistry notation, code blocks, images, and
an optional daily log. [ROADMAP.md](ROADMAP.md) has the order and the reasoning.

## Download

Version 0.1.0 is on the [Releases page](https://github.com/Makketeller/StudyDiary/releases): one
file per platform, with nothing to install and no admin rights needed.

**Windows:** download `StudyDiary.exe` and double-click it. The file isn't signed, so Windows
shows "Windows protected your PC" the first time: choose *More info*, then *Run anyway*. A
computer managed by an employer or school may not offer *Run anyway* at all; that is its
owner's policy, and only they can allow it.

**Linux:** download `StudyDiary`, make it runnable with `chmod +x StudyDiary`, and start it. To
get it in your app menu, download `studydiary.desktop` too, change its `Exec=` line to the full
path of `StudyDiary`, and copy it to `~/.local/share/applications/`.

Your diary is kept in `%LOCALAPPDATA%\studydiary` on Windows and `~/.local/share/studydiary` on
Linux. Copy that folder and you have copied everything.

## Why this exists

I'm a physics PhD student, and before that I was an optical engineer. I built StudyDiary because
I needed it: one place to keep what I learn, and to still know it five years from now. That's
why it's designed for a five-year horizon rather than a demo. I intend to be the user with the
oldest data in it.

This is free-time work, an hour a day, and it will take hundreds of hours. The design documents
in this repository, [DESIGN.md](DESIGN.md), [ARCHITECTURE.md](ARCHITECTURE.md) and
[ROADMAP.md](ROADMAP.md), record every decision and the reasoning behind it, if you want to see
how it's thought through.

## How it's built

I'm new to C#, and this project is also how I'm learning it. I work with Claude in conversation
as a design partner: it challenges my decisions, explains the language, and often drafts the
code we've agreed on. I type that code in myself, in small pieces, and ask until I understand
it. Where I've pasted something before understanding it, it is listed openly as understanding
debt in [STATUS.md](STATUS.md) and worked off on fixed days each week. A line I don't
understand is a line I can't maintain in year three.

I don't use coding agents, tools that write and commit code on their own, and I won't. Partly
because nothing here is meant to be rushed, and partly because of the telemetry promise: I can
only promise there is none because I have read every line that ships, and there is no network
code to send anything with. The dependencies are few, and they are large, well-known
open-source projects: .NET and Avalonia.

## License

Copyright (C) 2026 Markus Wallin

This program is free software: you can redistribute it and/or modify it
under the terms of the GNU General Public License as published by the
Free Software Foundation, either version 3 of the License, or (at your
option) any later version. See [LICENSE](LICENSE) for the full text.

GPLv3 was chosen deliberately. Anyone may use, study, modify and sell
this software — but anyone who distributes a modified version must
publish their source under the same terms. That makes it impossible to
quietly fork this into something with telemetry, dark patterns or
adware, which is the specific outcome this project exists to avoid.

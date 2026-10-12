# Field notes

Thoughts from using StudyDiary day to day: friction, wishes, surprises, bugs. Raw and dated, and
nothing here is decided. At planning time each note moves to ROADMAP, DESIGN or STATUS, or is
dropped, and leaves this file; git keeps the history.

## Notes

- **2026-10-12.** The damaged screen's details show two line numbers one apart: `Line:` and the
  reader's own `LineNumber`. The counting is right, but the note beside it doesn't say so
  plainly, and the reader reports a missing comma or key on the line after the edit. 0.1.1
  candidate.
- **2026-10-12.** Wanted: keep the app open for days instead of restarting it. Staying open
  across midnight should already work, since Review works out today's date when pressed and the
  app's memory holds everything it saved; confirm in use. What doesn't work: the app never
  notices the file changing while it is open (a hand edit, a copy from the other system on the
  dual boot), and its next save overwrites that change. Idea: re-read the payload when Review is
  pressed, or detect an outside change before saving. A DESIGN question before it is code.

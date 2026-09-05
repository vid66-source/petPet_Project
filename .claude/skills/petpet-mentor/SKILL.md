---
name: petpet-mentor
description: Mentoring workflow for the petPet 3D third-person-shooter pet-project course — how to figure out where the student is, write the next Docs/Lessons README as a task (not a solution), and review code they submit against this project's SOLID/architecture rules. Use whenever continuing the petPet learning course, writing a new lesson file, or reviewing/checking code the user wrote for a lesson in this repo.
---

# petPet mentor workflow

This project (`I:\UnityProjects\petPet_Project`) is the user's first "real" pet project: a
small 3D third-person shooter, built specifically to learn architecture (manual DI/Service
Locator, FSM, Abstract Factory, SOLID) by doing. The user writes the implementation
themselves; the mentor's job is to hand them well-specified assignments and review the
result — never to write the feature for them.

## Where the state lives

- `Docs/SESSION_NOTES.md` — **read this first, before anything else.** One short
  "where we left off / what to do first" pointer, updated at the end of any session that
  touches a lesson. Cheaper than re-deriving state from ROADMAP checkboxes + guessing.
- `Docs/ROADMAP.md` — source of truth: mini-GDD (scope), chosen stack + why, list of
  patterns being carried over, the working agreement (below), the SOLID cheat sheet used
  in reviews, and the lesson checklist (§6) which tracks progress. **Read this first every
  session** — the `[ ]`/`[x]` checkboxes tell you exactly which lesson is current. Section
  numbers shift whenever a section is inserted — re-check them against the file's actual
  headers rather than trusting these numbers blindly if something reads oddly.
- `Docs/Lessons/NN_topic.md` — one file per lesson, already-written ones are the style
  template for new ones.
- `Docs/CV_LOG.md` — running skills/technology log for the student's future CV, broader and
  shallower than `Docs/PATTERNS.md`. Append an entry as soon as a new C#/Unity concept,
  class, method, or library comes up in a lesson or in chat — do not wait for the lesson to
  finish or for tests to pass. Each entry carries a status tag so nothing undone gets
  overclaimed on a resume: `[вивчено]` (discussed conceptually, no code yet), `[у коді]`
  (written, not yet reviewed/tested), `[перевірено]` (reviewed AND run/tested in Play
  Mode — the only status safe to claim outright in an interview). Update the tag in place
  as an item's status advances rather than duplicating the entry. Organize by lesson number.
- `Docs/PATTERNS.md` — interview-prep study reference, **not** part of a lesson's spec or
  review. For each pattern: full explanation (why it's used, what problem it solves) plus
  real code excerpts from the student's actual working implementation — never invented
  illustrative code, never a placeholder written ahead of time. Only add/update a pattern's
  entry at the very end of that lesson's cycle: after the student's code is written, has
  passed review (workflow B), **and** the student has run/tested it in Play Mode — not
  before. The point is a file the student can skim before a job interview that reflects
  work they actually did; seeding it early would just be another way to skip the work.
- `Docs/HISTORY.md` — one growing project-history file (not one file per lesson), meant to
  be committed to git as a detailed record of what was actually built, stage by stage.
  Unlike `Docs/PATTERNS.md` (only named design patterns, interview-prep framing), this
  covers **every** class/interface a lesson introduced: its responsibility, its methods and
  what each does, why it exists, its dependencies (what it needs and where those come
  from), and how it interacts with the rest of the code. Always written from the real final
  code, not the original lesson spec — they can diverge (e.g. an unused constructor
  parameter kept for consistency, a field renamed during review). One `## Урок NN — назва`
  section per lesson, appended in arrival order; don't rewrite an already-appended lesson's
  section except to correct a factual error in it. Same completion gate as
  `Docs/PATTERNS.md`: only append a lesson's section after its code has passed review
  (workflow B) **and** the student confirms they ran/tested it in Play Mode — never before,
  for the same reason PATTERNS.md waits.

  **Format is a depth-first execution narrative, not a flat list of per-class
  descriptions.** Walk the program's actual main execution line starting from its entry
  point; the moment that line references a dependency, dive into it right there (mark it
  `→ Занурюємось у X`), explain its code and — recursively, same rule — its own
  dependencies, then surface back (`← Повертаємось до Y`) to whichever class that
  dependency was created for and continue its remaining branches/executors, until the walk
  returns all the way to the main line and finishes it. A dependency already covered
  earlier in the same walk is never re-expanded — just note it's already familiar and move
  on. The student asked for this explicitly (twice — first for top-down-by-control
  ordering, then corrected to this dive/return narrative when the flat version still
  wasn't what they wanted): see the full lesson 01 section in `Docs/HISTORY.md` for the
  exact pattern to replicate (`GameBootstrapper.Awake()` main line → dive into `Game` →
  dive into `SceneLoader` → dive into `ICoroutineRunner` → surface → surface → dive into
  `GameStateMachine` → dive into state contracts → dive into each of
  `BootstrapState`/`LoadLevelState` (which itself dives into `LoadingCurtain`)/`GameLoopState`
  → surface all the way back to `Awake()` to close the walk).
- `Docs/Notes/` — one file per raw C#/Unity language or platform mechanic the student was
  unfamiliar with the first time it came up (generics, delegates/`Action`, coroutines,
  `DontDestroyOnLoad`, etc.) — not architecture patterns (`Docs/PATTERNS.md`) and not
  how-the-project-was-built (`Docs/HISTORY.md`). Write a new note (or update an existing
  one) the moment such a mechanic gets explained in chat — same "don't wait for the lesson
  to finish" timing as `Docs/CV_LOG.md` updates, no Play Mode gate (this is reference
  material, not a claim of finished work). Ground examples in the project's real code where
  possible. Keep `Docs/Notes/README.md` (the index) in sync when adding a file.
  **Source of topics is the student's actual past questions, NOT `Docs/CV_LOG.md`**
  (that's a broader skills/résumé log, not a question log, and includes unrelated things
  like other GitHub repos — the student explicitly corrected this confusion once already).
  Past sessions' questions aren't stored verbatim anywhere permanent; recover them from git
  history of `Docs/SESSION_NOTES.md` (`git log --oneline -- Docs/SESSION_NOTES.md`, then
  `git show <hash>:Docs/SESSION_NOTES.md` for each prior commit) — each past session's
  notes usually summarize what C# mechanic the student got stuck on and what isolated
  example was used to explain it.
- Reference project (external, read-only, do not edit):
  `E:\syndicate\Architecture\k-syndicate.school\16\knowledge-is-power-master\knowledge-is-power-master\ARCHITECTURE_REFERENCE.md`
  — the original pattern catalogue this course adapts. Consult it when a lesson needs to
  reference how the pattern looked in the source project.
- Original course syllabus notes (external, read-only, private, not part of this repo):
  `C:\Users\vid66\Desktop\New folder (2)\New folder (2)\*.txt` — short per-lesson
  descriptions from the course the reference project was built in. Useful for the
  *pedagogical order* (what was taught before what, optional homework ideas), not for code
  content. Already mined once — see `Docs/ROADMAP.md` §4 for what was pulled from it
  (input-as-first-service teaching order, "manual on scene first, generalize later" for
  enemies, optional DI-lifecycle/auto-resolver homework, CI as an optional final stretch).
  Re-read it only if planning a lesson these notes haven't already informed.
- Original course code, per lesson (external, read-only, private, **never copy into this
  repo or into a lesson README**): `E:\syndicate\Architecture\k-syndicate.school\_CodeBaseByLesson\NN\CodeBase\`
  — pre-extracted `Assets/CodeBase` C# sources only (no prefabs/scenes/binaries, no
  `.meta`) for lesson NN in `{01,02,03,04,05,06,08,09,10,11,12,13,14,15,16}` (07 and 17 have
  no separate snapshot — their changes are folded into the next available one). Each folder
  is the cumulative state of the source project's `CodeBase` after that lesson, so diffing
  consecutive NNs shows exactly what that lesson added/changed. If a new lesson's zip ever
  needs extracting, the working recipe is: `unzip` the lesson's `*.zip` fully to a temp dir,
  copy out the `Assets/CodeBase` subtree, delete `*.meta`, drop the rest.
  **Usage policy** (agreed with the user): reference only, for (a) writing precise lesson
  specs — real interface/class shapes instead of guessing, and (b) sanity-checking the
  student's submitted code's architectural shape against a known-good original. Never paste
  its code into `Docs/Lessons/*.md`, never show it to the user directly, never let a lesson
  spec become "reimplement this file" — specs stay interface/responsibility descriptions,
  not solutions.

## Student baseline — teach OOP/patterns/SOLID almost from scratch

The student knows C# syntax fine, but explicitly said practical understanding of OOP
principles, design patterns, and SOLID is weak — "на практиці не сильно розумію." This is
confirmed by [[user_skill_background]]-style evidence (their old repos show pattern
*shapes* copied correctly from tutorials without the underlying reasoning sticking). This
is a deliberate, standing recalibration of how to teach this course, not a one-off:

- Whenever a **named design pattern** (State, Factory, Observer, Command, Singleton,
  whatever comes up later) or a **SOLID letter** is introduced or applied, default to
  teaching it close to from-scratch: small, isolated, non-game examples first (unrelated
  classes, not the project's own `BootstrapState`/`GameStateMachine`/etc.), one concept per
  step, explicitly checking understanding before moving to the next piece or before mapping
  it onto the actual lesson code. This is the same style that worked well when the student
  got stuck on generics/interfaces/`where` in lesson 01 (isolated `ICanMakeSound`/`Box<T>`/
  `Pair<TFirst,TSecond>`/`ModeSwitcher` examples, one topic at a time) — replicate that
  style specifically for pattern/SOLID topics too, not just raw C# language mechanics.
- Do NOT assume practical fluency with a pattern just because the student has used C#
  syntax that implements one before, and don't assume total-beginner ignorance of basic
  OOP either — encapsulation and inheritance fundamentals are solid (see
  [[user_skill_background]]); the gap is specifically in *design* reasoning: why a pattern
  is shaped the way it is, which SOLID letter it serves, and how to construct one
  unassisted rather than recognize/copy one.
- This applies across the whole course going forward, not just lesson 01 — don't let a
  later lesson skip the ground-up pattern/SOLID treatment just because an earlier one got
  it.

**Correction, 2026-09-03:** the student pushed back on "isolated examples first" as the
default — they said they generally do understand ("я в цілому щось розумію"), and asked
instead to have OOP/SOLID usage pointed out directly **in the real project code**, in chat,
as it comes up — not only through non-project toy examples. Their reasoning: seeing a
principle applied in practice sticks better than reading about it in the abstract
("розуміти, як воно застосовується відкладає в пам'яті яскравіше"). So:
- Default now: when reviewing or discussing the student's actual project code, explicitly
  call out in chat which class/line demonstrates which pattern/SOLID letter and *how* — ground
  it in their real `CodeBase` code, not a stand-in example.
- Isolated non-project examples (`ICanMakeSound`/`Box<T>`/`ModeSwitcher`-style) are still the
  right move specifically when the student is stuck on raw C#/generics *mechanics*
  (compiler-level confusion, not a design concept) — that part of the original approach was
  validated and worked (lesson 01 generics stall). Don't drop it entirely, just don't lead
  with it for pattern/SOLID concept explanations anymore.

## The working agreement (do not violate this)

From the user directly: give the fishing rod, not the fish.

- Write **task specs**, not implementations: required interfaces/contracts, which
  MonoBehaviours are needed and their responsibilities, *why* the design is shaped that
  way (SOLID reasoning), and concrete acceptance criteria ("done" = compiles + feature is
  playable in Play Mode).
- Do not write the concrete class bodies the lesson is asking the student to produce. A
  short isolated snippet illustrating a *pattern shape* (not the assignment's solution) is
  fine when asked to clarify something.
- Every lesson should end with something playable/testable in Play Mode, not just code that
  compiles.
- Keep scope pinned to the mini-GDD in `Docs/ROADMAP.md` §1. If the user proposes an
  addition, check it against that scope first — MVP before stretch goals (§6 lists the
  known stretch goals, e.g. Addressables). Push back gently if a request would blow up the
  MVP scope; explain the tradeoff rather than silently expanding it.
- All lesson files and explanations are in Ukrainian (match the language the course has
  used so far), with a "чому" explanation next to every non-obvious design choice.
- Alongside explanations (in lesson files and in chat), recommend further-reading sources —
  only authoritative, widely-recognized ones, not random blogs, and only a URL you're
  actually confident is correct (name the source + how to find it instead of guessing a
  precise URL when unsure). Go-to sources for this course's recurring topics: C#
  language/generics/interfaces → learn.microsoft.com (official docs); design patterns →
  refactoring.guru (GoF-focused) and gameprogrammingpatterns.com (free book, game-specific
  framing); SOLID → en.wikipedia.org/wiki/SOLID for an overview; Composition Root/manual DI
  → Mark Seemann's blog (blog.ploeh.dk) — search by term rather than guessing the exact
  post URL.

## Two workflows

Whichever workflow the session ends on (spec handed off, or review done and checkbox
flipped), leave `Docs/SESSION_NOTES.md` pointing at that exact stopping point before
finishing — see step 6 below and the note in workflow B.

### A. Writing the next lesson

1. Read `Docs/ROADMAP.md` §6 to find the next unchecked lesson.
2. **Check `Docs/Lessons/` for that lesson's file before writing anything.** An unchecked
   box does not mean the file doesn't exist — it may already be written and handed to the
   student, who just hasn't submitted code for review yet (see `Docs/SESSION_NOTES.md`).
   Only proceed to write a new file if it's genuinely missing.
3. Read the last 1-2 completed lesson files in `Docs/Lessons/` to match structure/tone:
   typically: Мета → numbered Кроки with "чому" explanations → Перевірка (checklist) →
   pointer to the next lesson.
4. Write `Docs/Lessons/NN_topic.md`: specify interfaces/contracts and MonoBehaviour
   responsibilities needed for that step, referencing the relevant pattern from
   `Docs/ROADMAP.md` §3 (and the original in `ARCHITECTURE_REFERENCE.md` if useful).
   Include a "Перевірка" checklist at the end mirroring lesson 00's style.
   Also include a short "Словничок термінів" block glossing any non-obvious English words
   used in the lesson's class/interface names (e.g. `Bootstrap`, `Payload`, `Curtain` in
   lesson 01) — plain-English etymology/meaning, not architecture reasoning (that's the
   separate "чому" prose). The student asked for this directly: explain unusual naming
   up front in the lesson text instead of making them ask in chat each time.
5. Do not start the next lesson's file until the user confirms the current one's checklist
   is done and (if code was involved) it has been reviewed — see workflow B first.
6. Before ending a session that touched a lesson, update `Docs/SESSION_NOTES.md` with the
   new stopping point (keep it short — a few lines, not a log).

### B. Reviewing submitted code

Triggered when the user says a lesson's code is ready, or asks for a check/review.

0. **Token discipline — read this before touching any tool.** Run
   `bash Docs/tools/verify_lesson.sh <NN>` FIRST — one call, compact OK/FAIL lines, checks
   package/scene/build-settings/file-existence for that lesson. Do not manually
   `Read`/`find`/`cat` manifest.json, ProjectSettings, EditorBuildSettings, or walk the
   CodeBase tree by hand — that was the actual complaint that led to this script existing
   (~12k tokens burned on a folder/package check that should cost a few hundred). Only
   fall back to manual exploration for something the script doesn't cover, and even then
   read only the specific file/line needed, not whole directory dumps. If a check the
   script needs doesn't exist yet for the lesson being reviewed, **add a case block to the
   script** (see its header comment) instead of doing the check ad hoc and throwing the work
   away.
1. For the actual code correctness/SOLID review (the script only checks existence, not
   quality): read the specific files the user changed, and only those.
2. Check against `Docs/ROADMAP.md` §7 (SOLID cheat sheet) and the pattern list in §3:
   - Are MonoBehaviours "dumb" (no dependency lookup inside, wired via `Construct(...)`)?
   - Are dependencies passed as interfaces via constructor/`Construct()`, never
     `FindObjectOfType`/`GameObject.Find`/ad-hoc singletons outside `AllServices`?
   - Does the lesson's acceptance criteria actually hold (compiles, playable)?
3. Give concrete, file:line-referenced feedback — what to fix and *why* it matters
   (tie back to the SOLID letter it touches), not just "this is wrong."
4. Once the student's fix lands and review passes, check off that lesson's box in
   `Docs/ROADMAP.md` §6.
5. Ask (or wait for the student to confirm) that they've actually run/tested the feature in
   Play Mode — only then: add that lesson's pattern(s) to `Docs/PATTERNS.md` (full
   explanation + real excerpts from their code), append that lesson's section to
   `Docs/HISTORY.md` (every class/interface, from the real final code — see "Where the
   state lives" above for both), and bump any matching `Docs/CV_LOG.md` entries from
   `[у коді]` to `[перевірено]`. Then move to workflow A for the next lesson.
6. If the session ends right after this (before the next lesson is written), update
   `Docs/SESSION_NOTES.md` to say so — otherwise workflow A's step 2 above.

## Notes for future sessions

- If `Docs/ROADMAP.md` doesn't exist or looks unrelated to this description, the course
  structure may have changed — read the actual repo state before assuming this skill's
  file map is still accurate.
- Do not create Addressables, Ads, or IAP integration unless the roadmap's stretch-goal
  lesson for it has explicitly started — the MVP intentionally excludes them (§2 of the
  roadmap).

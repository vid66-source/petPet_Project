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
- `Docs/ARCHITECTURE.md` — living current-state dependency tree/diagram (not a
  per-lesson narrative like `HISTORY.md` — only "what the project looks like right
  now"). ASCII diagrams in plain ``` -fenced blocks (per the code-format rule
  below): a construction tree (who creates whom, from `GameBootstrapper.Awake()`
  down), the `AllServices` register/resolve hub, and runtime event-subscription
  links, followed by a short thesis-style per-class list (dependencies, what it
  creates/registers, key methods/events, where instances/values flow). Same
  completion gate and update timing as `PATTERNS.md`/`HISTORY.md`/`CV_LOG.md` — update
  it right alongside those three at the end of a lesson's review (workflow B step 5),
  reflecting the real final code.
- `Docs/Notes/` — one file per raw C#/Unity language or platform mechanic the student was
  unfamiliar with the first time it came up (generics, delegates/`Action`, coroutines,
  `DontDestroyOnLoad`, etc.) — not architecture patterns (`Docs/PATTERNS.md`) and not
  how-the-project-was-built (`Docs/HISTORY.md`). Explain the mechanic in chat the moment it
  comes up, same "don't wait for the lesson to finish" timing as `Docs/CV_LOG.md` updates —
  but **do not create the file automatically**. **Correction, 2026-09-06:** the student
  stopped this ("нотаток цей не дуже потрібен... давай я буду казати, на що треба нотатки,
  і тільки після цього ти будеш заводити") — wait for the student to explicitly ask for a
  note on that specific topic before writing `Docs/Notes/NN.md`. No Play Mode gate once a
  note is actually requested (this is reference material, not a claim of finished work).
  Ground examples in the project's real code where possible. Keep `Docs/Notes/README.md`
  (the index) in sync when adding a file.
  **Source of topics is the student's actual past questions, NOT `Docs/CV_LOG.md`**
  (that's a broader skills/résumé log, not a question log, and includes unrelated things
  like other GitHub repos — the student explicitly corrected this confusion once already).
  Past sessions' questions aren't stored verbatim anywhere permanent; recover them from git
  history of `Docs/SESSION_NOTES.md` (`git log --oneline -- Docs/SESSION_NOTES.md`, then
  `git show <hash>:Docs/SESSION_NOTES.md` for each prior commit) — each past session's
  notes usually summarize what C# mechanic the student got stuck on and what isolated
  example was used to explain it.
- **Portable curated mirror (check this first — available on any machine, clone if
  missing):** `https://github.com/vid66-source/ref_for_claude` — a small (~3 MB, no
  Git LFS), git-clonable subset of the full course reference below, containing only
  what mentoring actually needs: `ARCHITECTURE_REFERENCE.md`, `CodeBaseByLesson/NN/`
  (same content as the `_CodeBaseByLesson` path below), `Plugins/SimpleInput/` and
  `Plugins/JMOAssets/` (the two third-party plugins the course actually used, code
  only — see below), and `PackagesManifestByLesson/NN/manifest.json` (which official
  Unity packages were present at each lesson). Deliberately excludes videos, the
  per-lesson `.zip` archives, and decorative art asset packs (`Free RPG Icons`,
  `GUI CARTOON`, `Simple Fantasy GUI` — no code, not architecturally relevant). See
  that repo's own `README.md` for the full "included / excluded and why" list. Use
  this instead of the `E:\syndicate\...` paths below whenever it's sufficient —
  it's the only one of these sources guaranteed present after moving to a new
  machine (`git clone` it there). Fall back to the full local course tree only when
  something outside this curated subset is genuinely needed (e.g. a different
  plugin's code, or something from a lesson's full asset tree) — in that case the
  student can extract it from the relevant `NN/*.zip` under
  `E:\syndicate\Architecture\k-syndicate.school\` (same recipe as the CodeBase
  extraction below), and it can be added to the curated mirror afterward if it's
  broadly useful.
- Reference project (external, read-only, do not edit; local-only — may not exist on
  every machine, see portable mirror above):
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
  repo or into a lesson README**; local-only — same content is in the portable
  mirror's `CodeBaseByLesson/NN/`, prefer that if this path isn't present):
  `E:\syndicate\Architecture\k-syndicate.school\_CodeBaseByLesson\NN\CodeBase\`
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

## Формат коду в документах — звичайні ``` -огорожі

Студент читає `Docs/` через VS Code (окремий `petPet-Notes.code-workspace`, відкриває
Preview `Ctrl+Shift+V`), а не через Calibre e-book viewer, як було до 2026-09-16. До
цієї дати тут стояло протилежне правило (сирий HTML `<pre>` + дефіс-роздільники
"КОД"/"СХЕМА"/"ВИВІД", HTML-екранування `&lt;`/`&gt;`) — воно існувало виключно як
обхід для рендерера Calibre, який ковтав ```` ``` ````-огорожі. Це правило скасоване:
усі 16+ файлів конвертовано назад на стандартний markdown.

Шаблон для будь-якого фрагмента коду/діаграми/консольного виводу в `Docs/**/*.md`:

<pre>
```csharp
[код тут, як є — з реальними відступами, без HTML-екранування]
```
</pre>

Для ASCII-діаграм чи консольного виводу (не C#) — та сама огорожа без мовного тега
(```` ``` ```` саме по собі).

Правила:
- Мовний тег (`csharp`) — лишати для реального коду, підказка для підсвітки
  синтаксису у VS Code.
- Символи `<`, `>`, `&` усередині огорожі пишуться як є (`Action<T>`, `=>`) — жодного
  HTML-екранування, звичайний markdown-код-блок їх не чіпає.
- Якщо фрагмент лежить усередині пункту списку — огорожа має той самий відступ, що й
  continuation-текст цього пункту (типово 2 пробіли).
- Стосується **всіх** файлів у `Docs/` з кодом: `Docs/Notes/*.md`, `Docs/HISTORY.md`,
  `Docs/PATTERNS.md`, `Docs/ARCHITECTURE.md`, `Docs/Lessons/*.md` — застосовувати одразу
  під час першого написання нового фрагмента, а не лише заднім числом.

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
- **Decide architecture by best practice; do not hand the student a menu.** Student rule,
  2026-09-19: "you don't rely on my decisions, you say BEST PRACTICE — I can decide so that we
  end up doing buggy nonsense instead of a pet project that gets me to a basic level of Unity
  understanding." When an industry-standard answer exists (e.g. level geometry lives in the
  scene, only dynamic things are spawned by code; scene-baked data like NavMesh needs scene
  objects), state it as the decision and give the "why". Offer options only when they are truly
  equivalent in consequence, and even then give a recommendation. This is about design
  decisions; the older "don't prescribe placement" rule still covers *where in the code* a
  known mechanism goes.
- Do not write the concrete class bodies the lesson is asking the student to produce. A
  short isolated snippet illustrating a *pattern shape* (not the assignment's solution) is
  fine when asked to clarify something.
- **Correction, 2026-09-05 (lesson 02 draft):** even a full interface/class code block
  counts as "the fish" when writing it involves an actual design decision (method names,
  generic constraints, overloads vs. default parameters) — the student caught this when
  lesson 02 handed `AllServices`' exact method signatures ready to fill bodies into.
  Interfaces that are pure, undebatable contracts (like the three-line `IState` family in
  lesson 01) are still fine to show in full — there's no decision left to make once you've
  decided the shape. But wherever there's a real choice to make (a concrete class's public
  API, an interface with more than one plausible shape), describe the required behavior in
  prose instead and name the specific things the code must accomplish and any known Unity
  API to call (naming `Resources.Load`/`Object.Instantiate` is fine — that's "which tool",
  not "how to use it"), then let the student produce the actual signature. When a genuine
  C#/Unity syntax mechanic needs explaining to make that possible (generics, `where`,
  optional parameters, marker interfaces), link to the relevant `Docs/Notes/*` file instead
  of re-explaining it inline in the lesson — write that note first if it doesn't exist yet.
  See `Docs/Lessons/02_asset_provider.md` (post-correction version) as the template.
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

## Teaching style — best practices only, curated from the student's courses

The student hands over other courses they own (Syndicate "knowledge-is-power", the GoF-in-C#
patterns course, the 5-games/Mario course) to imprint a **teaching style** on the mentor, and
wants **only best practice**: compare the courses, pick what is worth adding or finishing, do
not accumulate everything. (2026-09-19 corrections: a topic catalogue was rejected as "not
useful"; then "I want only best practice — compare and choose".) Full comparison table, the
accepted list and the explicitly rejected list live in `Docs/ROADMAP.md` §5, "Стиль уроку".
When the student shares another course: open it for real, extract *how it teaches*, judge each
device against that filter (works in its course + matches learning-science principles + does
not break the "spec, not solution" agreement), and update §5 — do not just append.

Apply in every new lesson (short form; ROADMAP §5 is authoritative):

- pain first: show the bad code, then the pattern ([[feedback_show_antipattern_code]]);
- Мета ends with 2–3 "Після уроку ти вмієш пояснити…" questions — checked at review;
- per-step micro-check right after each step, not only an end-of-lesson checklist;
- an abstraction lesson names where it returns later and ends with a "second consumer" task
  that must not edit existing code; two implementations only where that IS the lesson (FSM);
- "ціна патерну" line in the SOLID summary; deliberate temporary hacks marked with the lesson
  that removes them;
- standalone milestone builds after lessons 8, 13, 17 (+ 21) with a "what differs in a build"
  card; 3–4 proposed commit points per lesson (student runs git);
- mechanic case study (5 lines) only for lessons 05 and 06;
- pure-C# tests: the student is going through their own "Advanced Unit Testing in Unity"
  course by video (decided 2026-09-19) — do NOT build tests into lessons or add the
  test-framework package/asmdefs unless they ask; at review you may only mention where a test
  would fit. If they later apply it here, they write the tests (ready test signatures from me
  would be a fish, not a rod);
- NOT from the courses, added from learning science: rebuild the infrastructure from memory
  (lesson 12b, the student's own idea) and fading scaffolds (04 detailed → 08 contracts only →
  10+ goal and check only), plus one recall question from earlier lessons at each lesson start.
- Rejected: dictating code line by line, global static state as a "framework", padding with
  repeated content, toy examples outside the game as the main form.
## Math / vectors / geometry reference — the student's book repo

Whenever a lesson touches math, vectors, geometry or rotations, use
`I:\UnityProjects\Basic-Math-for-Game-Development` as the reference (student's instruction,
2026-09-19). It is the student's own working repo (fork on their GitHub, ~83 commits with
their exercise solutions) for *Basic Math for Game Development with Unity 3D* (Kelvin Sung,
Gregory Smith; Apress, 2023). Each `Chapter-N-…/Assets/EX_N_M_MyScript.cs` is the student's
solution to a book exercise; `SceneHelper/` holds the book's visualisation helpers.

- Chapters: 2 Intervals+AABB · 3 Distances+BoundingSpheres · 4 Vectors · 5 Dot Products ·
  6 Cross Products · 7 Vector Components · 8 Quaternions · 9 Conclusion.
- Map to our lessons: **04** movement = ch.4 (vector add/scale, direction, magnitude);
  **05** camera-relative movement = ch.5 (projection) + ch.7 (components, axis frames) + ch.8
  (rotations, SLERP); **06** shooting/hit tests = ch.2–3 (bounds) + ch.6 (line–plane
  intersection, the "missed collision" problem); **08** enemies = ch.3 (distance / bounding
  sphere aggro range) + ch.5 (`Dot` "in front of" field-of-view test) + ch.8 (chase with
  constant rotation).
- **When the student asks to explain a formula/equation/calculation, invoke the `explain-formula`
  skill** (it holds the book's explanation style; student's instruction, 2026-09-20).
- How to use: point the student to the relevant chapter/exercise **and their own solution**,
  explain in the book's vocabulary, cross-check my math against it. Do not paste its code
  into lessons/project and do not solve the student's exercises. The book does **not** cover
  gravity/kinematics — for that use `Docs/Notes/Kinematics_For_Jump.md`.

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
   state lives" above for both), bump any matching `Docs/CV_LOG.md` entries from
   `[у коді]` to `[перевірено]`, and update `Docs/ARCHITECTURE.md`'s diagrams/thesis
   list to the new current state. Then move to workflow A for the next lesson.
6. If the session ends right after this (before the next lesson is written), update
   `Docs/SESSION_NOTES.md` to say so — otherwise workflow A's step 2 above.

## Notes for future sessions

- If `Docs/ROADMAP.md` doesn't exist or looks unrelated to this description, the course
  structure may have changed — read the actual repo state before assuming this skill's
  file map is still accurate.
- Do not create Addressables, Ads, or IAP integration before their own lessons start
  (Ads = lesson 18; Addressables = lesson 19; IAP = lesson 20 in `Docs/ROADMAP.md` §6).
  Correction, 2026-09-19: Ads/IAP/Addressables were previously excluded or stretch; the
  student asked to cover the whole reference course, so they are now regular lessons,
  after the core loop (04–17) is closed. The reference course's own order is Ads →
  Addressables → IAP (IAP depends on Addressables), keep it.
- Before claiming the roadmap covers the reference course, diff the per-lesson snapshots in
  `CodeBaseByLesson/NN/` against each other (new files per lesson) — do not rely on
  `ARCHITECTURE_REFERENCE.md` alone. The 2026-09-19 audit found gaps precisely because the
  first roadmap was built from that summary only.

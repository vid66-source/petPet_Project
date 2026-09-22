---
name: explain-formula
description: How to explain a formula, equation, math/physics step or "how does this calculation work" to the petPet student, in the style of the Apress book "Basic Math for Game Development with Unity 3D" (outcomes → definition box → picture → numbers first → formula → units → map to code → step trace → what breaks → take-aways). Use whenever the student asks to explain, derive, break down or "розпиши" a formula, equation, vector/geometry/physics step, or asks why a calculation in their code is written a certain way ("поясни формулу", "чому саме так рахується", "як це працює на числах", "що таке dt/v/g", "розпиши кожен кадр").
---

# Explaining formulas — the book's style, adapted to this project

Source of the style: the student's own book repo `I:\UnityProjects\Basic-Math-for-Game-Development`
(Kelvin Sung, Gregory Smith — slides in `BookPPT/Chap4.pptx`, Chap5.pptx; the student's
solutions in `Chapter-N-…/Assets/EX_N_M_MyScript.cs`). Plus what worked (and what failed) with
this student in lesson 04. Answers are in **Ukrainian**.

## What the student needs (learned the hard way)

- Starts from **numbers, not from the formula.** Formulas first made them say "ніхріна не
  зрозумів"; the numeric table and the frame-by-frame trace are what worked.
- Does not read dense tables of jargon. One new idea at a time, each defined before use.
- Takes figurative speech literally ("ти прийшов до ідеї" confused them). Use plain, literal wording.
- Knows school-level physics/math but has forgotten it; do not assume more, do not talk down.
- Learns best when the explanation uses **the names from their own code** (`_verticalSpeed`,
  `Time.deltaTime`, `moveDirection`), not abstract symbols only.

## The template (adapt sections; do not pad)

Mirror the book's rhythm — *Outcomes → Take note → picture → Example → Implementation → Take Away
→ Summary*:

1. **Outcome (1–2 lines).** "Після цього ти зможеш …" (book: "Outcomes: you will be able to").
2. **Definition box ("Take note").** Symbol → meaning in words → name in code → type → units →
   does it change? One row per symbol. Every symbol the formula uses must appear here first.
3. **A real-world picture (one or two sentences).** Book: "travel north-east for an hour, cover X
   miles". Physical, everyday, then map it to the game.
4. **Numbers first.** A small table with **round numbers** (e.g. `g = 10`, `v0 = 10`), one row per
   step. Derive/read the formula off the table. Only after that switch to real values (`9.81`).
5. **Formula** in words, then symbols, then **"For example"** with the numbers from step 4, then
   **"True in general"** in one line (book: "For example … / True in general …").
6. **Units check.** Show that the units of both sides match (m, m/s, m/s²).
7. **Implementation — map to code.** Exact expression → method → what it computes. Say which lines
   matter and which to ignore (book: "focus on Update(); ignore the visualisation block"); say how
   few lines the real work is.
8. **Step/frame trace** for anything that repeats: one row per step, one column per variable, using
   code names. This is the part that made things click.
9. **What breaks** (only if useful): symptom → cause, preferably the student's own real bugs.
10. **Take away** (max 3 bullets) + **one question** the student answers in their own words (not a
    quiz; check understanding, don't assume it from working code).

Start the reply with a **5-line short answer**, then the detail. If the student says they did not
understand a specific part, restart *that part* from numbers; do not repeat the same wording louder.

## Rules

- **Correctness first.** Any example with more than three numbers: compute it with a script (node
  or bash is available) before writing it. State the rounding. Say plainly if a result is
  approximate (discrete-frame error, etc.).
- **Consistent notation and the book's vocabulary for vectors/geometry** (position vector,
  direction = normalized vector, magnitude/length, scaling, dot/cross product, projection,
  axis frame, bounding sphere). Point to the relevant chapter and to the student's own exercise
  in the book repo (map: 4 Vectors → movement; 5 Dot → field of view/projection; 6 Cross →
  planes/line-plane; 7 Components/axis frames → camera-relative movement; 8 Quaternions →
  rotation/SLERP; 2–3 Bounds/Distances → aggro/hit tests). The book has **no** gravity/kinematics —
  for that use `Docs/Notes/Kinematics_For_Jump.md` as the model.
- **No homework solution by accident.** Give formulas, symbol ↔ code mapping and tools (`Mathf.Sqrt`,
  `Physics.gravity`); do not hand over the finished line of the student's current assignment
  unless they explicitly ask after trying. Never paste the book's code into the project.
- **Small.** One idea per section, short sentences, tables only where they clarify. If the answer is
  turning into a wall, cut.
- **Notes files only on request.** Explain in chat first. Write to `Docs/Notes/` only when the
  student asks for a conspect; then follow the project's conspect rules: task-based reference
  ("goal → formula → where in code → returns"), isolated sections, real numbers, no narrative
  toward a single answer, Ukrainian, code in fenced blocks, keep `Docs/Notes/README.md` in sync.
- End by stating the next concrete step in the student's code.

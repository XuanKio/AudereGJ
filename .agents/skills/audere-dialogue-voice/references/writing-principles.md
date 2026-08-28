# Audere Writing Principles

## Evidence labels

Use the four labels defined in `SKILL.md` whenever evidence quality matters. A polished line is not automatically canon. A filename is context, not proof of an event.

## Scene construction

- Give each exchange one primary narrative job. Let secondary meaning live in subtext.
- Enter late and leave early. Do not make characters restate an action the player just saw unless their reaction changes its meaning.
- Prefer a concrete next action over abstract reassurance or psychological explanation.
- Let emotional change appear in choices, delays, corrections, interruptions, and willingness to answer.
- Preserve gradual change. One successful interaction may create a small opening; it must not erase an established fear or dependency.

## Spoken Vietnamese

- Favor conversational Vietnamese over translated or literary sentence structure.
- Use particles such as “thôi”, “nhé”, “mà”, “đấy”, and ellipses only when they reveal rhythm or attitude.
- Keep a line short when the speaker is hesitant, tired, pressured, or avoiding commitment.
- Avoid naming an emotion when posture, silence, a partial answer, or a concrete concern can carry it.
- Read exchanges aloud. Remove explanatory clauses that no person in the moment needs to say.

## Dialogue bubble readability

- In every scene and every two-character dialogue that includes Audere, author Audere in the
  `Left` slot and the counterpart in the `Right` slot. Lines spoken by Audere therefore use
  `Left`; lines spoken by Timor, Teacher, Bianca, Khoảng Lặng, or another counterpart use `Right`.
- Do not infer emotional dominance, alignment, or canon relationship meaning from this fixed UI
  placement. It is a presentation contract for consistency.

- Treat one `DialogueData.Line` as one readable speech beat, not a paragraph that relies on
  TMP auto-wrap.
- For the current dialogue prefab, aim for at most `42` visible characters including spaces.
  A longer line must be split or visually verified at the target game resolution.
- Split at a complete thought, reaction, or change of intention. Prefer two short standalone
  sentences over breaking one grammatical sentence into a lowercase continuation.
- Keep a very short beat when it changes timing or emotion (`Xin lỗi!`, a hesitation, a
  correction). Do not split mechanically if it only adds clicks without changing the beat.
- After editing, preview the longest line for both left and right bubbles; character names and
  Vietnamese diacritics must remain inside their authored rectangles.

## Subtext

For each important line, know:

```text
What the character wants now
→ what they cannot or will not say directly
→ the safer thing they say instead
```

Audere commonly protects herself by shortening, delaying, or redirecting an answer. Early Timor commonly packages direction as relief: he reduces the number of decisions Audere must face and makes following him feel safe.

## Tutorial and gameplay dialogue

Keep two responsibilities distinct:

- **UI instruction** communicates the exact input or rule in one direct line.
- **Character dialogue** responds to Audere and gives the action relational or emotional meaning.

Do not make Timor recite interface vocabulary when a UI line can carry it. Do not hide a mandatory control inside flavor dialogue. Use character reactions only at meaningful milestones, mistakes, prolonged hesitation, failure, or completion; do not comment after every click.

At the current Day 1 opening, Timor's help should feel genuinely useful. A controlling tendency may appear as narrowing options, sequencing tasks, or “cứ theo tớ trước đã”, but not as open domination or villain signaling.

## Review questions

1. What changed between the first and last line?
2. Which line carries the relationship subtext?
3. Is any line explaining information both characters already know?
4. Is the speaker naming their own psychology too neatly?
5. Does the tutorial remain understandable if all character flavor is removed?
6. Does the character interaction still make sense if all UI instruction is removed?

# Audere Writing Principles

## Evidence labels

Use the four labels defined in `SKILL.md` whenever evidence quality matters. A polished line is not automatically canon. A filename is context, not proof of an event.

## Scene construction

- Give each exchange one primary narrative job. Let secondary meaning live in subtext.
- Enter late and leave early. Do not make characters restate an action the player just saw unless their reaction changes its meaning.
- Prefer a concrete next action over abstract reassurance or psychological explanation.
- Let emotional change appear in choices, delays, corrections, interruptions, and willingness to answer.
- Preserve gradual change. One successful interaction may create a small opening; it must not erase an established fear or dependency.

## Emotional pacing takes priority over the word target

Xuân's direction (2026-09-20): shorten dialogue by about 20% overall while keeping it moving,
emotionally affecting, and unhurried at the important turns. The percentage is a soft target;
do not take a needed response or hesitation out merely to reach it.

Cut repeated explanation first. Preserve the sequence **feeling → acknowledgement → choice**
where the acknowledgement lets the other character's words land. Silence, a name, `Ừ`, or `Tớ biết`
can be a real relationship beat; judge its function before treating it as filler.

| Protected turn | Keep room for |
| --- | --- |
| D1 accepting Bianca and thanking Timor | Audere finding the first word; still trembling; Timor hearing her; gratitude after that |
| D1 night defeat → D2 morning | Exhaustion before yielding; Timor's soothing surface; the next morning's wary greeting before monitoring resumes |
| D2 asking Bianca → asking Timor at home | Calling the person, getting a response, risking the question, receiving the answer, then doubting the old interpretation |
| D3 care and company | The teacher's acknowledgement, Audere's chosen disclosure, permission before touch, Bianca leaving the choice open |
| D4 help and reconciliation | Receiving help before asking more widely; remembering Timor's help, his fear, Audere's boundary, and a tentative agreement |

Do not shorten `WaitStep`, fades, auto-dialogue minimum duration, or scene motion timings as a
side effect of the copy edit. Even with unchanged minima, fewer lines can shorten a scene:
review the complete exchange and adjacent action rather than relying on the timing fields alone.
Do not merge across a speaker's response, portrait turn, or staging action just to save clicks.
Do not add empty clicks or ellipses solely to imitate slowness; retain a specific reaction or choice.
Keep final uncertainty in Audere's voice; a concise rhetorical victory can erase the effort of her choice.

Protected scene context lives in [Story chronology](../../../../Docs/Story/chronology.md).
Record cuts and retained emotional beats in [dialogue revision](../../../../Docs/Story/dialogue-revision.md).

## Spoken Vietnamese

- Favor conversational Vietnamese over translated or literary sentence structure.
- Use particles such as “thôi”, “nhé”, “mà”, “đấy”, and ellipses only when they reveal rhythm or attitude.
- Keep a line short when the speaker is hesitant, tired, pressured, or avoiding commitment.
- Avoid naming an emotion when posture, silence, a partial answer, or a concrete concern can carry it.
- Read exchanges aloud. Remove explanatory clauses that no person in the moment needs to say.

## Human-writing pass for character dialogue

Use `t1k:human-writing` and `t1k:human-reply` as supporting lenses when requested.
Their business/email register is not the characters' voice; the profiles and story state still lead.
Keep the useful checks: specific lived actions, spoken Vietnamese, varied rhythm, and evidence for
what each person can know. Do not invent an event or make Audere sound certain to strengthen a line.
A hesitation or acknowledgement can carry the relationship; do not delete it as a generic opener.
Read both sides together: each answer must respond to the previous person's actual words.
Cut repeated explanations and polished slogans before cutting vulnerability or a small ordinary joke.

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

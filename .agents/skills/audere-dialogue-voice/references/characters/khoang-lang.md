# Khoảng Lặng

## Status

- **Design Intent:** `Khoảng Lặng` is the display name of the current D1 Classroom combat
  prototype and is placed immediately after Timor takes over Audere's unanswered pause.
- **Established implementation state:** `DialogueCharacterId.KhoangLang = 5` exists as a
  stable technical hook. The catalog temporarily reuses Audere's portrait and must remain marked
  `PLACEHOLDER`. The prototype uses one authored
  `6 HP` combat phase, loops three projectile moves, and uses
  `Enemy_KhoangLang_PLACEHOLDER.prefab`.
- **Established implementation state:** after the isolated tutorial, short Khoảng Lặng lines use
  the standard non-blocking DialogueUI during Aimed Fan and Side Sweep, with Audere on the
  left and Khoảng Lặng on the right. At `2 HP`, four supplied worry-lines repeat as a dense low-opacity
  background field with soft smear and wobble. These assets are production-wired but remain
  `PLACEHOLDER` narrative content.
- **Design Intent:** the current wording pressures Audere with immediate social consequences. It
  is terse and repetitive because it crowds her decision, not because a general enemy voice has
  been established.
- **Design Intent:** the production beat immediately before combat frames the encounter as Audere
  facing the anxiety that tries to choose an answer for her. This does not establish literal
  creature ontology or a voice for Khoảng Lặng.
- **Unresolved:** final psychological or in-world ontology, voice, vocabulary, relationship to
  Audere or Timor, canon dialogue, portrait, final art, final moveset,
  balance, outcome, and whether the display name remains canon.

## Dialogue guardrails

- Do not infer a voice from the name, projectile patterns, phase count, placeholder art, or
  combat placement.
- Do not create additional taunts, tutorial lines, internal monologue, or phase-break dialogue
  beyond the supplied D1 placeholder set unless Xuân explicitly approves the direction.
- The current D1 tutorial is spoken only by Timor/Audere and uses a separate instruction HUD;
  it does not establish a voice, intent, or perception rules for Khoảng Lặng.
- Empty phase-break and mid-phase dialogue hooks are intentional production data, not missing
  lines to fill automatically.
- Do not extrapolate the D1 combat-dialogue vocabulary into a reusable canon voice. Final speaker ontology,
  perception rules and whether the displayed words are literal speech remain `Unresolved`.
- If future dialogue is requested, first resolve who can perceive the speaker, whether it is a
  character or presentation device, and what Audere understands at that point.

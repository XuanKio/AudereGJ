---
name: audere-dialogue-voice
description: Maintain character voice, relationship progression, narrative continuity, subtext, and gameplay clarity for Audere. Use whenever creating, revising, reviewing, or integrating dialogue, DialogueData, narrative sequences, tutorial or combat dialogue, monologue, messages, barks, or character interactions in the Audere Unity project.
---

# Audere Dialogue Voice

Preserve what the project has established while keeping assumptions visibly separate from canon. Apply this skill before changing narrative content or its placement in Unity.

## Read the relevant references

- Always read [writing-principles.md](references/writing-principles.md) and [story-state.md](references/story-state.md).
- Read the profile of every participating character: [Audere](references/characters/audere.md) and/or [Timor](references/characters/timor.md).
- For scenes involving both characters or referring to their bond, read [relationships.md](references/relationships.md).
- When drafting or reviewing voice, read [dialogue-examples.md](references/dialogue-examples.md). Treat the examples as evidence, not templates to copy.

If a requested character or story point has no reference yet, inspect the nearest project sources and mark missing information `Unresolved`; do not invent a profile silently.

## Canon discipline

Classify every material narrative claim as one of:

- **Established Canon**: explicitly present in a current game asset, scene, or authoritative story document.
- **Strongly Implied**: consistently supported by behavior or context but not directly stated.
- **Design Intent**: a direction requested by the designer but not yet established in shipped scene content.
- **Unresolved**: missing, contradictory, legacy, or insufficiently supported.

Do not promote `Strongly Implied`, `Design Intent`, brainstorm text, filenames, test content, or personal inference into `Established Canon`. When sources conflict, expose the conflict and ask Xuân only if resolving it is necessary for the task.

## Required workflow

Before writing or editing dialogue:

1. Locate the scene in the story, including day, event, and whether it is before or after a known milestone.
2. Identify every speaking, present, and materially referenced character.
3. Read each relevant character profile.
4. Read the relevant relationship state.
5. Read the story state for that exact point; do not borrow a later emotional state.
6. Inspect the dialogue immediately before and after the scene when it exists.
7. State the scene's narrative purpose in one sentence.
8. For gameplay or tutorial scenes, separate the mechanical information the player must understand from the character interaction.
9. Draft dialogue at the smallest length that fulfills the scene purpose.
10. Run the checks below.
11. Only after the checks pass, edit `DialogueData`, tutorial text, StoryEvent content, or a Unity scene.

## Mandatory checks

- **Character voice**: Could the line be reassigned to another character unchanged? If yes, make its reaction, rhythm, omission, or word choice more specific.
- **Relationship progression**: Is trust, dependence, resistance, protectiveness, or control changing no faster than the current story state permits?
- **Continuity**: Does the scene agree with adjacent actions, names, facts, and knowledge? Flag known conflicts rather than choosing a convenient version.
- **Subtext**: Prefer behavior, hesitation, redirection, and what is left unsaid over characters explaining their own psychology.
- **Gameplay clarity**: The player must understand the action. Keep technical UI concise; let character lines frame the experience rather than sound like control documentation.

## Unity integration guardrails

- Inspect the target `DialogueData`, `StoryEvent`, tutorial component, and adjacent content before editing.
- Preserve the scene-first Story flow and existing data ownership; this skill does not authorize framework or gameplay changes.
- Do not rewrite nearby dialogue merely to make the new lines stylistically uniform unless the user asks.
- Do not update this bible as canon merely because a draft was proposed. Update references only after the designer accepts a change as canon or explicitly requests bible maintenance.


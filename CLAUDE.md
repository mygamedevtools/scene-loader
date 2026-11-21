## AI skill maintenance (My Scene Manager)

This project ships AI Skills at `Packages/com.mygamedevtools.scene-loader/AIAssistantSkills/` and skill tests at `AISkillTests/`. They are living documents — keep them true:

1. **When code changes:** any change to a public API, menu item, editor window, default value, or workflow that a skill documents must update that SKILL.md in the same change set. Bump its `skill-version` and set `last-verified` to today.
2. **When you (the agent) use My Scene Manager and a skill instruction turns out wrong, unclear, or missing:** fix the skill immediately, then add a note to `AISkillTests/RESULTS.md` describing what was corrected.
3. **When a feature ships:** decide whether it needs a new skill or a new workflow in an existing skill, and add at least one matching test scenario under `AISkillTests/scenarios/`.
4. **Before every Asset Store submission:** re-run all scenarios in skill mode, log results in `RESULTS.md`, and fix failures first.
5. Never delete a skill without replacing its coverage or marking it deprecated in its `description`.
6. Never change the required frontmatter fields or section headings — the structure is a shared Asset Store standard used for cataloging.

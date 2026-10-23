---
name: swlor-release-notes
description: Create, update, or finalize SWLOR Wiki release notes from merged game and asset changes, using established player-facing wording and disclosure rules. Use for SWLOR release-note maintenance or release publication, not general game documentation.
---

# SWLOR release notes

Produce accurate, player-facing release notes for Star Wars: Legends of the Old Republic (SWLOR). These instructions are portable between GPT and Claude: use the Git, GitHub, filesystem, and browser tools available in the current environment. No model-specific tool or companion skill is required.

## Sources and destination

- Game repository: `https://github.com/zunath/SWLOR_NWN`.
- Wiki repository: `https://github.com/zunath/SWLOR_Wiki`.
- Wiki: `https://wiki.starwarsnwn.com`.
- Upcoming page: `/ReleaseNotes/RN_Unreleased`, backed by `ReleaseNotes/RN_Unreleased.html`.
- Release index: `/ReleaseNotes`, backed by `ReleaseNotes.html`.
- Dated releases follow the existing `ReleaseNotes/RN_YYYY-MM-DD.html` convention.

Locate the actual working checkouts and read applicable repository instructions. Known Windows checkout locations are `C:\Projects\SWLOR_NWN` and `C:\Projects\SWLOR_Wiki`, but verify them rather than relying on the current directory. The current directory may be a stale worktree. Check configured remotes and the default branch; do not assume a local checkout is current.

The Wiki repository is the content source for this workflow. Use its existing HTML and metadata format. A separate ChatGPT Page or Space is not the destination.

## Establish coverage before writing

1. Read the current upcoming notes, the latest released notes, and recent Wiki release-note commits. Determine what is already covered and the last game commit reviewed. The previous update's commit message records that coverage boundary (older updates recorded it in their PR descriptions).
2. Refresh the remote refs or query current GitHub state. Identify changes merged into the game's default branch since that boundary. Verify merge status and inspect the actual changes, not only PR titles or an agent's session summary. Include direct default-branch commits only when verified as integrated changes.
3. Inspect linked asset/HAK changes and merged companion PRs where relevant. Icons, appearances, and other player-visible asset updates belong in the notes too; do not limit the review to server code.
4. Map candidate changes against existing notes. Add missing player-visible outcomes, revise superseded descriptions, and consolidate fixes to the same feature. Omit duplicates, reverted work, and changes with no meaningful player impact.
5. Record the reviewed game commit, asset revision, supporting PRs, and relevant omissions in the Wiki commit message body, not in the player-facing page. This gives the next update a reliable boundary.

**Merged is not the same as deployed.** Never infer a deployment from a merge timestamp. An in-progress coding session, open PR, branch, or local change is not eligible. This supersedes the early request to include an in-progress Dantooine dungeon session. Include its results only after they are merged.

If the release cutoff is uncertain, use the strongest available evidence: deployment records, released notes, prior coverage commits, and merge times. Compare the resulting changes against the notes already published. State any material uncertainty to the user; do not invent an exact deployment time or NWSync hash. Ask only when the missing information prevents a reliable result.

## Player-facing writing

Describe what players will experience and what they need to do. Use plain language, recognizable game names, and the actual UI labels where useful. Group related changes under short topical headings and use concise bullets.

- Explain the resulting behavior, benefit, or correction. Avoid class names, database details, resource identifiers, model filenames, internal architecture, debug output, test histories, and PR numbers in public notes.
- Preserve meaningful restrictions or consequences, such as an out-of-combat boarding requirement, a required permission, automatic repair of existing items, or how to regain relinquished access. Verify these from the merged change.
- Include significant icon and appearance changes. Do not let asset updates disappear behind server-code summaries.
- For a newly introduced dungeon, describe its final features. Fold development fixes into that description instead of presenting a sequence of repairs to content players have never received. In later releases, ordinary fixes to an already released dungeon can be listed normally.
- Keep the introduction and release-index summary aligned with the final sections.
- Do not invent commands, shortcuts, item behavior, rewards, deployment status, or player actions that the sources do not establish.

Examples of suitable wording:

- Technical: a ship query no longer truncates permission records. Player-facing: "The ship lists now reliably show all your owned and shared ships."
- Vague enemy adjustment: "Adjusted enemy balance in the dungeon."
- Hidden destination: "Capstone quest givers have been relocated."

## Established disclosure and content rules

Apply these defaults unless the user explicitly revises them:

### Arkanian Dragon

Keep rewards and drops vague. Do not name specific drops or blueprints, quantities, guaranteed rewards, drop rates, precise loot conditions, or exact crafting ingredients. General statements about encounter balance, rewards, and expanded crafting options are appropriate. Enemy stat adjustments should also stay general, without exact values or breakdowns.

The previously approved general wording is:

> The Great Arkanian Dragon encounter and its rewards have received balance adjustments.
>
> Arkanian Dragon crafting options have been expanded, with updates to existing equipment and recipes.

Preserve that level of disclosure across the body, metadata description, index summary, and any public-facing update message. Do not restore details from older drafts or PR descriptions.

### RPXP

Routine release-note updates must not introduce RPXP policy, OOC-message mechanics, penalties, thresholds, additional checks, or logging details. Do not re-add the removed line about how out-of-character chat affects roleplay XP.

The sensitive explanatory draft discussed previously was tabled and was not authorized for publication. If the user resumes that work, provide the complete proposed text for review and wait for explicit approval before publishing it. Do not treat a general request to update release notes as approval of that draft.

The user separately approved an entry stating that the **RPXP Penalty System will be disabled temporarily for further fine tuning**. Preserve that notice where it belongs in the approved release history. Do not copy it into every new release or infer that the system remains disabled today without evidence. A newly requested RPXP statement should follow the user's specific scope and approval instructions.

### Other established wording

- Mention relocated capstone quest givers without giving their specific locations.
- The combat-upgrade rollout included a free rebuild token for all existing players. Keep this in the relevant rollout notes; do not imply a new token grant in each subsequent release.
- Preserve user-approved omissions when incorporating later merged work. Discovering an omitted technical detail is not a reason to restore it.

## Page lifecycle

### Update the upcoming notes

Maintain the existing `RN_Unreleased` page, its published visibility, and its Unreleased index category. Keep the visible status **Upcoming** and leave release date and NWSync hash **TBD** until confirmed. Update the Notes Updated date and metadata timestamp to the actual edit time. Preserve `dateCreated` and unrelated metadata.

### Create a new upcoming set

Use the last confirmed release and prior coverage boundary to identify subsequent merged changes. Create or reuse the conventional upcoming page and add its index entry under Unreleased. Avoid carrying already released changes into the new set.

### Mark a release as released

Do this when requested or when an actual deployment is confirmed within the authorized task. Use the confirmed release date and NWSync hash. Update the page title, metadata, visible status, links, and release-index entry consistently. Move the entry from Unreleased to the correct year/category, following current Wiki conventions. Do not leave a stale Upcoming status or duplicate upcoming entry for the same release. Create another upcoming set only if requested or needed for separately identified pending work.

## Save and publish within the requested scope

A request for a draft, text first, or a skill file does not authorize publishing release notes. Respect explicit review holds. For ordinary Wiki maintenance in an authorized publishing session, complete the requested update end to end; the user's established preference is to push the change directly without requiring them to review it. This skill alone does not grant permission to mutate an external service in a new context.

Do not open a Wiki PR. Commit the validated change in a separate worktree based on the freshly fetched Wiki default branch, then push that commit directly to the default branch. If the push is rejected because the branch moved, rebase onto the new tip, re-check the result, and push again; never force-push. Keep unrelated working changes and other checkouts intact. Do not merge game or asset PRs to make them eligible for notes.

Before publication, check:

- Every new entry is supported by integrated changes and falls within the intended release coverage.
- No already released or reverted change is repeated.
- The wording is player-facing and respects the Dragon, enemy-stat, RPXP, and location rules.
- HTML is well formed, links and IDs are valid, and `git diff --check` passes.
- The index, metadata, visible status, and dates agree. A routine upcoming update leaves released pages unchanged.
- Every changed page's metadata `description` and `title` are at most 255 characters. The Wiki stores them in 255-character columns; a longer value makes Git sync skip that page on every run without reporting an error, while other pages keep syncing.

Prose-only edits do not require game tests. Validate the document and the affected publication path instead.

## Verify the live result

A successful push is not proof that the live page has updated. Confirm the commit is on the remote default branch, then verify the actual live page and affected index entry. Check both the new text and the intended release status/category before reporting completion.

The Wiki uses Git synchronization. If the pushed content has not appeared and the signed-in administration UI is available, its storage page at `/a/storage` provides a **Force Sync** action. Inspect the current UI and run only that normal synchronization action; it respects the configured direction. Do not change storage settings or use Add Untracked Changes, Import Everything, or Purge Local Repository as a shortcut. Allow the asynchronous sync to finish before refreshing the release page; do not repeatedly trigger it while pending. If the sync completes but one page stays on an old version (its page history shows no newer edit), check that page's metadata lengths first and fix the source file rather than editing the page in the Wiki.

Reuse the user's authenticated Wiki session. Do not sign out, close their tab, or alter authentication settings. If sign-in needs the user's participation, hand off that step and preserve the work. Return the tab to the release notes after any administration work.

If the live update cannot be verified, report exactly what is complete and what is blocked. Do not say the live page is updated solely because the push succeeded. Where browser tooling requires evidence, save a screenshot of the verified page and include it in the response.

Finish with a concise link to the updated page and a short account of the added topics and any material limitation. Avoid exposing the sensitive details deliberately omitted from the notes.

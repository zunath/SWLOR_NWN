# Pull Requests

Read before opening, updating, or finishing a pull request.

Sections: Branches · Code review loop · Review threads · Public text · Handoff

## Branches

- Start each request on a new branch from up-to-date `origin/master` unless the user names a different base. Never commit to `master`, and never mix one request's work into another request's branch.
- One branch and one pull request per request; follow-ups are new commits on the same branch. Open a second pull request only for a required submodule companion, and only one per submodule: a haks change that spans several asset groups (for example body animation banks and the robe bridges regenerated from them) stays in that one companion. Separate branches that edit the Design Bible `.xlsx` cannot be merged.
- When a pull request moves a submodule pointer (usually `SWLOR_Haks`), commit and push the submodule branch, open its companion pull request against the branch matching the parent's base, and link the two pull requests in both descriptions. A pushed submodule branch alone is not a complete handoff.

## Code review loop

Every pull request gets a Codex review, repeated until a review of the current head finds nothing. You run the whole loop yourself in the same turn: request a review, wait for Codex to answer, fix what it found, and request again. The user should never have to watch for Codex or tell you a review arrived. Never end your turn, hand off, or report the pull request ready while a review is pending or a finding is open.

1. Push any changes, then request a review of the head and wait for the answer:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File tools/RequestCodexReview.ps1 -PullRequest <pr>
   ```

   It posts `@codex review (head <sha>)` and waits until Codex answers for that commit, usually 4 to 10 minutes; it gives up after 30. Every run posts a new request, so a round where you only answered findings still gets a fresh review. Run it in the foreground with a timeout above 30 minutes or, if your shell caps foreground commands lower, as a background command that notifies you when it exits. Do nothing else on the pull request until it exits. If it is interrupted, rerun it with `-WaitOnly` instead of requesting again.
2. Act on its exit code:
   - `0`: clean. This head is done.
   - `1`: findings, listed by file and line. Fix or answer every one (see Review threads), push the fixes, resolve the threads, and go back to step 1, even if you only answered.
   - `2`: no review of this head. A usage limit ends the loop: report the unreviewed commit to the user. If the head moved during the wait, go back to step 1. Read any other reply that names the current head and act on it; ignore replies for older heads.
   - `3`: no answer within 30 minutes. If it says Codex is still reviewing, rerun it once with `-WaitOnly`. If there is still no answer, run step 1 once more; if that also gets no answer, report the unreviewed commit to the user.
   - `4`: the script itself failed (an error line says why, such as a missing or old `gh` or a network error). Fix the cause and rerun; if it keeps failing, report the unreviewed commit to the user.
3. Repeat until the latest review of the current head of the parent and every companion is clean.

- Each companion pull request runs its own loop with `-Repository zunath/SWLOR_Haks`. Name the other pull request on both with `-Note "companion zunath/SWLOR_NWN#<pr>"`. The loops can run at the same time.
- Do not push while a review is running, or Codex reviews a commit that is no longer the head. Put every fix for one review in one push.
- The script needs an authenticated `gh` on PATH. Without one, tell the user the loop cannot run; do not skip it.
- CodeRabbit reviews automatically. If it reports a rate limit, do not wait for it; handle any findings it posts later the same way.

## Merge conflicts

A pull request with merge conflicts is not ready, however clean its review. Resolving them is part of the job, never something to leave for the user.

- Check every pull request, companions included, with `gh pr view <pr> --repo <owner>/<repo> --json mergeable,mergeStateStatus` before you report it ready, and again whenever you come back to it. `CONFLICTING` or `DIRTY` means it has conflicts. `UNKNOWN` means GitHub is still computing; ask again.
- Resolve them by merging the pull request's own base branch into your branch. Read the base from `gh pr view <pr> --repo <owner>/<repo> --json baseRefName`, refresh it with `git fetch origin +refs/heads/<base>:refs/remotes/origin/<base>` (`Readmes/AgentEnvironment.md`), then `git merge origin/<base>`; don't rebase or force-push a branch under review. Keep both sides' intent, rebuild, rerun the tests that guard the change, and push.
- A conflicting submodule pointer resolves to the companion's current head once the companion branch has its own base merged in.
- The merge commit is a new head, so run the review loop again until the latest review of that head is clean.

## Review threads

Addressing a finding means fixing it or answering it with a concrete reason it does not apply, then resolving its thread. This applies to bot reviewers too. Never resolve a thread just to hide it.

```bash
gh api graphql --paginate -f query='query($o:String!,$r:String!,$n:Int!,$endCursor:String){repository(owner:$o,name:$r){pullRequest(number:$n){reviewThreads(first:100,after:$endCursor){nodes{id isResolved path} pageInfo{hasNextPage endCursor}}}}}' -f o=zunath -f r=SWLOR_NWN -F n=<pr>
gh api graphql -f query='mutation($id:ID!){resolveReviewThread(input:{threadId:$id}){thread{isResolved}}}' -f id=<thread id>
gh api --paginate repos/zunath/SWLOR_NWN/pulls/<pr>/reviews --jq '.[] | {user: .user.login, state, body}'
```

The last command catches findings Codex put in a review body instead of a thread. Body findings cannot be resolved; answer them with a pull request comment naming the fix commit or the reason. For haks companion pull requests, use `-f r=SWLOR_Haks` and `repos/zunath/SWLOR_Haks/...`.

Pull request work is done when the parent and every companion have zero unresolved threads, every body finding is answered, none has merge conflicts with its base, and the latest Codex review of each current head is clean.

## Public text

- The repository is public. Keep secrets, and the RP XP OOC detection specifics (detection rules, allowances, limits, penalties, word lists, findings from chat logs), out of commits, pull request text, and player-facing docs. For RP XP OOC changes, use vague wording such as "Rework the RP XP OOC penalty" and give the specifics to the user in chat. GitHub keeps edit history, so get it right the first time.
- The description holds implementation notes and validation: test filters run, client evidence and its limitations, and artifact locations marked as local. Do not add README files for routine work.

## Handoff

- The working tree holds only the request's changes. Scratch files live outside the repository.
- Tests, generators, and icon tools can rewrite tracked files. Review `git status` and revert side effects you did not intend. Stage explicit paths rather than `git add -A`.

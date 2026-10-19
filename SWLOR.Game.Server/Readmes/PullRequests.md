# Pull Requests

Read before opening, updating, or finishing a pull request.

Sections: Branches · Code review loop · Review threads · Public text · Handoff

## Branches

- Start each request on a new branch from up-to-date `origin/master` unless the user names a different base. Never commit to `master`, and never mix one request's work into another request's branch.
- One branch and one pull request per request; follow-ups are new commits on the same branch. Open a second pull request only for a required submodule companion. Separate branches that edit the Design Bible `.xlsx` cannot be merged.
- When a pull request moves a submodule pointer (usually `SWLOR_Haks`), commit and push the submodule branch, open its companion pull request against the branch matching the parent's base, and link the two pull requests in both descriptions. A pushed submodule branch alone is not a complete handoff.

## Code review loop

Every pull request gets a Codex review, repeated until it finds nothing.

1. After opening the pull request, comment `@codex review` on it, naming the head commit and any companion pull request. Do the same on each companion.
2. Codex answers with a summary comment for the commit it reviewed. Findings arrive as inline review threads or inside the review body. A clean review says "Didn't find any major issues" for the current head.
3. Fix or answer every finding, push, resolve its thread, and comment `@codex review` again for the new head.
4. Repeat until a Codex review of the current head of the parent and every companion finds nothing.

- A reply saying Codex reached its usage limit is not a review. Report which commit went unreviewed.
- CodeRabbit reviews automatically. If it reports a rate limit, do not wait for it; handle any findings it posts later the same way.

## Review threads

Addressing a finding means fixing it or answering it with a concrete reason it does not apply, then resolving its thread. This applies to bot reviewers too. Never resolve a thread just to hide it.

```bash
gh api graphql --paginate -f query='query($o:String!,$r:String!,$n:Int!,$endCursor:String){repository(owner:$o,name:$r){pullRequest(number:$n){reviewThreads(first:100,after:$endCursor){nodes{id isResolved path} pageInfo{hasNextPage endCursor}}}}}' -f o=zunath -f r=SWLOR_NWN -F n=<pr>
gh api graphql -f query='mutation($id:ID!){resolveReviewThread(input:{threadId:$id}){thread{isResolved}}}' -f id=<thread id>
gh api --paginate repos/zunath/SWLOR_NWN/pulls/<pr>/reviews --jq '.[] | {user: .user.login, state, body}'
```

The last command catches findings Codex put in a review body instead of a thread. Body findings cannot be resolved; answer them with a pull request comment naming the fix commit or the reason. For haks companion pull requests, use `-f r=SWLOR_Haks` and `repos/zunath/SWLOR_Haks/...`.

Pull request work is done when the parent and every companion have zero unresolved threads, every body finding is answered, and the latest Codex review of each current head is clean.

## Public text

- The repository is public. Keep secrets, and the RP XP OOC detection specifics (detection rules, allowances, limits, penalties, word lists, findings from chat logs), out of commits, pull request text, and player-facing docs. For RP XP OOC changes, use vague wording such as "Rework the RP XP OOC penalty" and give the specifics to the user in chat. GitHub keeps edit history, so get it right the first time.
- The description holds implementation notes and validation: test filters run, client evidence and its limitations, and artifact locations marked as local. Do not add README files for routine work.

## Handoff

- The working tree holds only the request's changes. Scratch files live outside the repository.
- Tests, generators, and icon tools can rewrite tracked files. Review `git status` and revert side effects you did not intend. Stage explicit paths rather than `git add -A`.

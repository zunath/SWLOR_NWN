<#
.SYNOPSIS
Requests a Codex review of a pull request's head commit and waits for Codex to answer.

.DESCRIPTION
Posts "@codex review (head <sha>)" on the pull request, then polls GitHub until Codex answers
for that commit, reports a usage limit, or the timeout passes. Every run without -WaitOnly posts
a new request, so a round that only answered findings still gets a fresh review.

Codex answers on the reviewed commit with a "Didn't find any major issues" comment or review, or
with a review that holds its findings. The latest answer after the request wins. Its summary
comment is edited in place and is ignored here, as are answers for older commits.

Exit codes: 0 clean, 1 findings, 2 no review of the head (usage limit, the head moved during the
wait, or another reply), 3 no answer before the timeout, 4 the script itself failed (for example a
GitHub CLI or network error), so the head is unreviewed.

.EXAMPLE
powershell -NoProfile -File tools/RequestCodexReview.ps1 -PullRequest 2745

.EXAMPLE
powershell -NoProfile -File tools/RequestCodexReview.ps1 -Repository zunath/SWLOR_Haks -PullRequest 120 -Note "companion zunath/SWLOR_NWN#2745"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$PullRequest,
    [string]$Repository = "zunath/SWLOR_NWN",
    # Extra text for the request comment, such as the companion pull request.
    [string]$Note,
    # Wait on the latest existing request instead of posting a new one.
    [switch]$WaitOnly,
    [int]$TimeoutMinutes = 30,
    [int]$PollSeconds = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$codexLogin = "chatgpt-codex-connector[bot]"

# Exit code 1 means findings, so any other failure gets its own code.
trap {
    "RequestCodexReview failed: $_"
    exit 4
}

try {
    # Decode gh output as UTF-8 so finding titles print intact.
    [Console]::OutputEncoding = [Text.Encoding]::UTF8
}
catch {
}

function Invoke-Gh {
    param([string[]]$Arguments)

    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "gh $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return ($output -join "`n")
}

function Get-GitHubList {
    param([string]$Path)

    # --slurp wraps every page in one outer array.
    $pages = Invoke-Gh @("api", "--paginate", "--slurp", "${Path}?per_page=100") | ConvertFrom-Json
    foreach ($page in $pages) {
        foreach ($item in $page) {
            $item
        }
    }
}

function ConvertTo-UtcTime {
    param($Value)

    if ($Value -is [datetime]) {
        return $Value.ToUniversalTime()
    }

    return [datetime]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AdjustToUniversal)
}

function Get-CodexAnswer {
    # Comment ids only grow, so they order same-second replies that created_at cannot.
    param([datetime]$Since, [long]$SinceId)

    $cleanVerdict = "Didn't find any major issues"
    $findingBadge = "!\[P\d Badge\]"
    $answers = @()
    $reviews = @(Get-GitHubList "repos/$Repository/pulls/$PullRequest/reviews" | Where-Object {
            $_.user.login -eq $codexLogin -and $_.commit_id -eq $head -and $_.submitted_at -and
            (ConvertTo-UtcTime $_.submitted_at) -ge $Since
        })
    foreach ($review in $reviews) {
        $kind = if ($review.body -match $cleanVerdict) { "clean" } else { "findings" }
        $answers += [pscustomobject]@{ Kind = $kind; Time = (ConvertTo-UtcTime $review.submitted_at); Id = [long]0; Item = $review }
    }

    $comments = @(Get-GitHubList "repos/$Repository/issues/$PullRequest/comments" | Where-Object {
            $_.user.login -eq $codexLogin -and $_.id -gt $SinceId -and
            $_.body -notmatch "codex-pull-request-review-summary"
        })
    foreach ($comment in $comments | Where-Object { $_.body -match "codex-review-receipt" -and $_.body -match $shortHead }) {
        # A receipt without a verdict is not an answer; it takes the grace path below.
        $kind = if ($comment.body -match $cleanVerdict) { "clean" } elseif ($comment.body -match $findingBadge) { "findings" } else { $null }
        if ($kind) {
            $answers += [pscustomobject]@{ Kind = $kind; Time = (ConvertTo-UtcTime $comment.created_at); Id = [long]$comment.id; Item = $comment }
        }
    }

    # A finding can also arrive as a plain comment that carries a priority badge. It counts for the
    # head only when it names the head commit; a delayed comment from an older review does not.
    foreach ($comment in $comments | Where-Object { $_.body -notmatch "codex-review-receipt" -and $_.body -match $findingBadge -and $_.body -match $shortHead }) {
        $answers += [pscustomobject]@{ Kind = "findings"; Time = (ConvertTo-UtcTime $comment.created_at); Id = [long]$comment.id; Item = $comment }
    }

    if ($answers.Count -gt 0) {
        # Comment ids break ties between answers posted in the same second.
        $latest = $answers | Sort-Object Time, Id | Select-Object -Last 1
        return @{ Kind = $latest.Kind; Item = $latest.Item; ForHead = $true }
    }

    $limit = $comments | Where-Object { $_.body -match "usage limit" } | Select-Object -Last 1
    if ($limit) {
        return @{ Kind = "limit"; Item = $limit; ForHead = $false }
    }

    $other = $comments | Where-Object {
        $_.body -notmatch "codex-review-receipt" -or
        ($_.body -match $shortHead -and $_.body -notmatch $cleanVerdict -and $_.body -notmatch $findingBadge)
    } | Select-Object -Last 1
    if ($other) {
        return @{ Kind = "other"; Item = $other; ForHead = $false }
    }

    return $null
}

function Get-CodexReviewStatus {
    # The summary comment carries the state of Codex's latest review, such as "running".
    foreach ($comment in Get-GitHubList "repos/$Repository/issues/$PullRequest/comments") {
        if ($comment.user.login -eq $codexLogin -and $comment.body -match "codex-review-state:v1 (\{.*?\}) -->") {
            $state = ($Matches[1] | ConvertFrom-Json).code_review
            if ($state.commit_sha -eq $head) {
                return $state.status
            }
        }
    }

    return $null
}

function Write-Findings {
    param($Review)

    "Codex found issues in ${shortHead} on ${target}: $($Review.html_url)"
    if (-not $Review.PSObject.Properties["commit_id"]) {
        # A comment, not a review: the findings are in its body.
        ($Review.body -replace "(?s)<details>.*?</details>", "" -replace "(?s)<!--.*?-->", "").Trim()
        return
    }

    # The per-review endpoint omits line numbers, so filter the pull request's review comments.
    $threads = @(Get-GitHubList "repos/$Repository/pulls/$PullRequest/comments" | Where-Object { $_.pull_request_review_id -eq $Review.id })
    foreach ($thread in $threads) {
        $firstLine = ($thread.body -split "`r?`n")[0]
        $priority = if ($firstLine -match "!\[(P\d) Badge\]") { $Matches[1] } else { "--" }
        $title = ($firstLine -replace "!\[[^\]]*\]\([^)]*\)", "" -replace "<[^>]+>", "" -replace "\*\*", "").Trim()
        $line = if ($thread.line) { $thread.line } else { $thread.original_line }
        "- $priority $($thread.path):$line $title"
    }

    # Findings can sit in the review body too, whether or not there are inline threads.
    $reviewBody = ($Review.body -replace "(?s)<details>.*?</details>", "" -replace "(?s)<!--.*?-->", "").Trim()
    if ($reviewBody) {
        "Review body:"
        $reviewBody
    }
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "The GitHub CLI (gh) must be on PATH."
}

# Get-GitHubList needs api --slurp, so reject an older gh before anything is posted.
$ghVersionText = (& gh --version | Select-Object -First 1)
if ($ghVersionText -notmatch "(\d+\.\d+\.\d+)" -or [version]$Matches[1] -lt [version]"2.48.0") {
    throw "The GitHub CLI must be version 2.48 or newer (api --slurp); found: $ghVersionText"
}

function Get-HeadSha {
    return (Invoke-Gh @("api", "repos/$Repository/pulls/$PullRequest") | ConvertFrom-Json).head.sha
}

$target = "$Repository#$PullRequest"
$head = Get-HeadSha
$shortHead = $head.Substring(0, 7)

if ($WaitOnly) {
    $request = Get-GitHubList "repos/$Repository/issues/$PullRequest/comments" | Where-Object {
        $_.user.type -ne "Bot" -and $_.body -match "^\s*@codex review"
    } | Select-Object -Last 1
    if (-not $request) {
        throw "$target has no @codex review request to wait on."
    }

    # A request that names another head belongs to a superseded commit.
    if ($request.body -match "head ([0-9a-f]{7,40})" -and -not $head.StartsWith($Matches[1])) {
        "The latest request on $target was for $($Matches[1]), not the current head $shortHead; request a review of the new head."
        exit 2
    }

    $since = ConvertTo-UtcTime $request.created_at
    $sinceId = [long]$request.id
}
else {
    $body = "@codex review (head $($head.Substring(0, 9)))"
    if ($Note) {
        $body += "; $Note"
    }

    $posted = Invoke-Gh @("api", "repos/$Repository/issues/$PullRequest/comments", "-f", "body=$body") | ConvertFrom-Json
    $since = ConvertTo-UtcTime $posted.created_at
    $sinceId = [long]$posted.id
    "Requested a Codex review of $shortHead on $target."
}

"Waiting up to $TimeoutMinutes minutes for Codex to answer for $shortHead on $target."
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
$replySeenAt = $null
while ($true) {
    $answer = Get-CodexAnswer -Since $since -SinceId $sinceId
    # An answer for a superseded head does not review the pull request.
    $currentHead = Get-HeadSha
    if ($currentHead -ne $head) {
        "The head of $target moved from $shortHead to $($currentHead.Substring(0, 7)) during the review; request a review of the new head."
        exit 2
    }

    if ($answer -and $answer.ForHead) {
        break
    }

    if ($answer) {
        # Codex can post an error seconds before the review itself, so give the review time to land.
        if (-not $replySeenAt) {
            $replySeenAt = Get-Date
            # A late reply still gets the whole grace period.
            if ($deadline -lt $replySeenAt.AddSeconds(90)) {
                $deadline = $replySeenAt.AddSeconds(90)
            }
        }
        elseif ((Get-Date) -ge $replySeenAt.AddSeconds(90)) {
            break
        }
    }

    if ((Get-Date) -ge $deadline) {
        break
    }

    Start-Sleep -Seconds $PollSeconds
}

if (-not $answer) {
    "Codex did not answer for $shortHead on $target within $TimeoutMinutes minutes."
    if ((Get-CodexReviewStatus) -eq "running") {
        "Codex is still reviewing $shortHead; rerun with -WaitOnly."
    }
    else {
        "Codex shows no running review of $shortHead."
    }

    exit 3
}

switch ($answer.Kind) {
    "clean" {
        "Codex found no issues in $shortHead on $target."
        exit 0
    }
    "findings" {
        Write-Findings $answer.Item
        exit 1
    }
    "limit" {
        "Codex reached its usage limit; $shortHead on $target is unreviewed."
        exit 2
    }
    default {
        "Codex answered without a review of $shortHead on ${target}:"
        $answer.Item.body
        exit 2
    }
}

<#
.SYNOPSIS
Requests a Codex review of a pull request's head commit and waits for Codex to answer.

.DESCRIPTION
Posts "@codex review (head <sha>)" on the pull request, then polls GitHub until Codex answers
for that commit, reports a usage limit, or the timeout passes. If Codex already answered for
the head commit, prints that answer instead of requesting again.

Codex answers a clean review with a "Didn't find any major issues" comment and findings with a
pull request review on the reviewed commit. Its summary comment is edited in place and is
ignored here, as are answers for older commits.

Exit codes: 0 clean, 1 findings, 2 no review (usage limit or another reply), 3 no answer before
the timeout.

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
    param(
        [datetime]$Since,
        # Only report a review of the head commit, not limits or other replies.
        [switch]$HeadOnly
    )

    $reviews = @(Get-GitHubList "repos/$Repository/pulls/$PullRequest/reviews" | Where-Object {
            $_.user.login -eq $codexLogin -and $_.submitted_at -and (ConvertTo-UtcTime $_.submitted_at) -gt $Since
        })
    $comments = @(Get-GitHubList "repos/$Repository/issues/$PullRequest/comments" | Where-Object {
            $_.user.login -eq $codexLogin -and (ConvertTo-UtcTime $_.created_at) -gt $Since -and
            $_.body -notmatch "codex-pull-request-review-summary"
        })

    $findings = $reviews | Where-Object { $_.commit_id -eq $head } | Select-Object -Last 1
    if ($findings) {
        return @{ Kind = "findings"; Item = $findings }
    }

    $receipt = $comments | Where-Object { $_.body -match "codex-review-receipt" -and $_.body -match $shortHead } | Select-Object -Last 1
    if ($receipt) {
        $kind = if ($receipt.body -match "Didn't find any major issues") { "clean" } else { "other" }
        return @{ Kind = $kind; Item = $receipt }
    }

    if ($HeadOnly) {
        return $null
    }

    $limit = $comments | Where-Object { $_.body -match "usage limit" } | Select-Object -Last 1
    if ($limit) {
        return @{ Kind = "limit"; Item = $limit }
    }

    $other = $comments | Where-Object { $_.body -notmatch "codex-review-receipt" } | Select-Object -Last 1
    if ($other) {
        return @{ Kind = "other"; Item = $other }
    }

    return $null
}

function Write-Findings {
    param($Review)

    "Codex found issues in ${shortHead} on ${target}: $($Review.html_url)"
    # The per-review endpoint omits line numbers, so filter the pull request's review comments.
    $threads = @(Get-GitHubList "repos/$Repository/pulls/$PullRequest/comments" | Where-Object { $_.pull_request_review_id -eq $Review.id })
    foreach ($thread in $threads) {
        $firstLine = ($thread.body -split "`r?`n")[0]
        $priority = if ($firstLine -match "!\[(P\d) Badge\]") { $Matches[1] } else { "--" }
        $title = ($firstLine -replace "!\[[^\]]*\]\([^)]*\)", "" -replace "<[^>]+>", "" -replace "\*\*", "").Trim()
        $line = if ($thread.line) { $thread.line } else { $thread.original_line }
        "- $priority $($thread.path):$line $title"
    }

    if ($threads.Count -eq 0) {
        "The findings are in the review body:"
        ($Review.body -replace "(?s)<details>.*?</details>", "" -replace "(?s)<!--.*?-->", "").Trim()
    }
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "The GitHub CLI (gh) must be on PATH."
}

$target = "$Repository#$PullRequest"
$head = (Invoke-Gh @("api", "repos/$Repository/pulls/$PullRequest") | ConvertFrom-Json).head.sha
$shortHead = $head.Substring(0, 7)
$answer = $null

if ($WaitOnly) {
    $request = Get-GitHubList "repos/$Repository/issues/$PullRequest/comments" | Where-Object {
        $_.user.type -ne "Bot" -and $_.body -match "^\s*@codex review"
    } | Select-Object -Last 1
    if (-not $request) {
        throw "$target has no @codex review request to wait on."
    }

    $since = ConvertTo-UtcTime $request.created_at
}
else {
    $answer = Get-CodexAnswer -Since ([datetime]::MinValue) -HeadOnly
    if (-not $answer) {
        $body = "@codex review (head $($head.Substring(0, 9)))"
        if ($Note) {
            $body += "; $Note"
        }

        $posted = Invoke-Gh @("api", "repos/$Repository/issues/$PullRequest/comments", "-f", "body=$body") | ConvertFrom-Json
        $since = ConvertTo-UtcTime $posted.created_at
        "Requested a Codex review of $shortHead on $target."
    }
}

if (-not $answer) {
    "Waiting up to $TimeoutMinutes minutes for Codex to answer for $shortHead on $target."
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    while ($true) {
        $answer = Get-CodexAnswer -Since $since
        if ($answer -or (Get-Date) -ge $deadline) {
            break
        }

        Start-Sleep -Seconds $PollSeconds
    }
}

if (-not $answer) {
    "Codex did not answer for $shortHead on $target within $TimeoutMinutes minutes."
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

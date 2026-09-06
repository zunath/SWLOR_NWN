#requires -Version 7.0
# Export syntax-level message call sites after reviewing their Production policy.
$ErrorActionPreference = 'Stop'
# PowerShell ships Roslyn for Add-Type, but the parser must also be available in a fresh session.
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$repo = (Get-Location).Path
$sinks = @('SendMessageToPC', 'FloatingTextStringOnCreature', 'FloatingTextStrRefOnCreature', 'SendMessageToPCByStrRef', 'SendMessageToAllPCs', 'SendMessageNearbyToPlayers', 'SendFeedbackString', 'SendFeedbackMessage', 'SendMessage', 'PostString', 'SpeakString', 'ActionSpeakString', 'SendDiagnosticToPlayer', 'ShowDiagnosticFloatingText', 'SendDiagnosticNearby', 'SendResourceRestored', 'SendWarningToPlayer')
$rows = foreach ($file in (rg --files SWLOR.Game.Server -g '*.cs' | Sort-Object)) {
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText((Join-Path $repo $file)))
    $root = [Microsoft.CodeAnalysis.CSharp.CSharpExtensions]::GetCompilationUnitRoot($tree, [Threading.CancellationToken]::None)
    foreach ($node in $root.DescendantNodes($null, $false)) {
        if ($node -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax]) { continue }
        $name = ($node.Expression.ToString() -split '\.')[-1]
        if ($name -cnotin $sinks) { continue }
        $ancestors = @($node.Ancestors($false))
        $member = $ancestors | Where-Object { $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax] } | Select-Object -First 1
        $delivery = 'Retained'
        if ($name -in @('SendDiagnosticToPlayer', 'ShowDiagnosticFloatingText', 'SendDiagnosticNearby', 'SendResourceRestored')) { $delivery = 'Testing only' }
        elseif ($name -eq 'SendWarningToPlayer') { $delivery = 'Rate limited in Production' }
        elseif ($ancestors | Where-Object { $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax] -and $_.Condition.ToString() -eq 'PlayerFeedback.DiagnosticsEnabled' }) { $delivery = 'Testing only' }
        if ($file -match 'Service[\\/](PlayerFeedback|Messaging|Communication|Gui)\.cs$' -and $name -notlike '*Diagnostic*') { $delivery = 'Shared transport; policy at caller' }
        [pscustomobject][ordered]@{
            File = $file.Replace('\', '/')
            Line = $node.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            Member = if ($member) { $member.Identifier.ValueText } else { '' }
            Delivery = $delivery
            Call = $node.ToString() -replace '\r\n', "`n"
        }
    }
}
$rows | ConvertTo-Json -Depth 5 | Set-Content SWLOR.Game.Server/Readmes/PlayerMessageAudit.json -Encoding utf8
$rows | Group-Object Delivery | Select-Object Name, Count
"Files: $(@($rows.File | Sort-Object -Unique).Count)"



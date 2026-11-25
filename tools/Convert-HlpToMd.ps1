<#
.SYNOPSIS
  Convert PennMUSH .hlp help files into Markdown (.md) files compatible with
  PennMUSH.Core.Help.HelpStore (YAML frontmatter + Markdown body).

.EXAMPLE
  .\tools\Convert-HlpToMd.ps1 -DryRun
  .\tools\Convert-HlpToMd.ps1 -InDir game\txt\hlp -OutDir game\txt\hlp
#>
param(
    [string]$InDir = "game/txt/hlp",
    [string]$OutDir = "game/txt/hlp",
    [switch]$DryRun,
    [switch]$DeleteHlp
)

$ErrorActionPreference = "Stop"
if (-not [System.IO.Path]::IsPathRooted($InDir)) { $InDir = Join-Path $PWD.Path $InDir }
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path $PWD.Path $OutDir }
$InDir = [System.IO.Path]::GetFullPath($InDir)
$OutDir = [System.IO.Path]::GetFullPath($OutDir)

function Get-TopicSlug([string]$name) {
    $slug = $name.ToLowerInvariant().Trim() -replace '@', 'at_'
    $slug = ($slug -replace '[^a-z0-9]+', '_').Trim('_')
    if ([string]::IsNullOrEmpty($slug)) { return "topic" }
    if ($slug -match '^(con|prn|aux|nul|com[1-9]|lpt[1-9])$') { return "${slug}_topic" }
    return $slug
}

function Test-PreformattedLine([string]$line) {
    $trimmed = $line.Trim()
    if ([string]::IsNullOrEmpty($trimmed)) { return $false }
    $leading = $line.Length - $line.TrimStart(' ').Length
    if ($leading -ge 4) { return $true }
    if ($trimmed -match '\S {3,}\S' -and $trimmed -notmatch '^[a-zA-Z0-9_]+=<[^>]+>:') { return $true }
    return $false
}

function Format-BodyToMarkdown([string]$primary, [string[]]$lines) {
    $out = [System.Collections.Generic.List[string]]::new()
    $inCode = $false

    foreach ($raw in $lines) {
        $stripped = $raw.Trim()
        if ([string]::IsNullOrEmpty($stripped)) {
            if ($inCode) {
                $out.Add("")
            } elseif ($out.Count -gt 0 -and $out[$out.Count - 1] -ne "") {
                $out.Add("")
            }
            continue
        }

        if (Test-PreformattedLine $raw) {
            if (-not $inCode) {
                $out.Add('```text')
                $inCode = $true
            }
            $out.Add($raw.TrimEnd())
            continue
        }

        if ($inCode) {
            $out.Add('```')
            $out.Add("")
            $inCode = $false
        }

        if ($raw -match '^\s*([A-Za-z0-9_]+=<[^>]+>):\s*(.+)$') {
            $out.Add("- ``$($Matches[1])``: $($Matches[2].Trim())")
            continue
        }

        if ($stripped.ToLowerInvariant().StartsWith("see also:")) {
            $rest = $stripped.Substring(9).Trim()
            $out.Add("**See also:** $rest")
            continue
        }

        $out.Add($raw.TrimStart(' ').TrimEnd())
    }

    if ($inCode) { $out.Add('```') }
    while ($out.Count -gt 0 -and $out[$out.Count - 1] -eq "") {
        $out.RemoveAt($out.Count - 1)
    }
    return ($out -join "`n") + "`n"
}

function Format-TopicsYaml([string]$key, [string[]]$names) {
    $escaped = foreach ($n in $names) {
        if ($n -match '[:#,\[\]@&*''"]') {
            '"' + ($n -replace '"', '\"') + '"'
        } else {
            $n
        }
    }
    return "${key}: [" + ($escaped -join ", ") + "]"
}

function Parse-HlpFile([string]$path) {
    $entries = [System.Collections.Generic.List[object]]::new()
    $names = [System.Collections.Generic.List[string]]::new()
    $adminNames = [System.Collections.Generic.List[string]]::new()
    $body = [System.Collections.Generic.List[string]]::new()
    $inHeader = $false
    $started = $false

    $flush = {
        if (($names.Count -gt 0 -or $adminNames.Count -gt 0) -and $body.Count -gt 0) {
            while ($body.Count -gt 0 -and [string]::IsNullOrWhiteSpace($body[$body.Count - 1])) {
                $body.RemoveAt($body.Count - 1)
            }
            if ($body.Count -gt 0) {
                $entries.Add([PSCustomObject]@{
                    Names      = @($names.ToArray())
                    AdminNames = @($adminNames.ToArray())
                    Body       = @($body.ToArray())
                })
            }
        }
        $names.Clear()
        $adminNames.Clear()
        $body.Clear()
    }

    foreach ($rawLine in [System.IO.File]::ReadAllLines($path)) {
        if ($rawLine.StartsWith('&')) {
            $started = $true
            if (-not $inHeader -and ($names.Count -gt 0 -or $adminNames.Count -gt 0 -or $body.Count -gt 0)) {
                & $flush
            }
            $inHeader = $true
            $afterAmp = $rawLine.Substring(1).TrimStart()
            if ($afterAmp.StartsWith('&')) {
                $topic = ($afterAmp.Substring(1).TrimStart() -split '\s+' | Where-Object { $_ -ne '' }) -join ' '
                if ($topic.Length -gt 0 -and -not $adminNames.Contains($topic)) {
                    $adminNames.Add($topic)
                }
            } else {
                $topic = ($afterAmp -split '\s+' | Where-Object { $_ -ne '' }) -join ' '
                if ($topic.Length -gt 0 -and -not $names.Contains($topic)) {
                    $names.Add($topic)
                }
            }
        } elseif ($started) {
            $inHeader = $false
            $body.Add($rawLine)
        }
    }

    & $flush
    return $entries
}

$hlpFiles = Get-ChildItem -Path $InDir -Filter "*.hlp" | Sort-Object Name
$totalTopics = 0
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

foreach ($hlp in $hlpFiles) {
    $entries = Parse-HlpFile $hlp.FullName
    $categoryDir = Join-Path $OutDir $hlp.BaseName
    $usedSlugs = @{}

    foreach ($entry in $entries) {
        $primary = if ($entry.Names.Count -gt 0) { $entry.Names[0] } else { $entry.AdminNames[0] }
        $baseSlug = Get-TopicSlug $primary
        $count = if ($usedSlugs.ContainsKey($baseSlug)) { $usedSlugs[$baseSlug] + 1 } else { 1 }
        $usedSlugs[$baseSlug] = $count
        $fileName = if ($count -eq 1) { "$baseSlug.md" } else { "${baseSlug}_$count.md" }
        $target = Join-Path $categoryDir $fileName

        $fmLines = [System.Collections.Generic.List[string]]::new()
        $fmLines.Add("---")
        if ($entry.Names.Count -gt 0) { $fmLines.Add((Format-TopicsYaml "topics" $entry.Names)) }
        if ($entry.AdminNames.Count -gt 0) { $fmLines.Add((Format-TopicsYaml "admin_topics" $entry.AdminNames)) }
        $fmLines.Add("---")
        $frontmatter = ($fmLines -join "`n") + "`n`n"
        $mdContent = $frontmatter + (Format-BodyToMarkdown $primary $entry.Body)

        if (-not $DryRun) {
            if (-not (Test-Path $categoryDir)) { New-Item -ItemType Directory -Path $categoryDir -Force | Out-Null }
            [System.IO.File]::WriteAllText($target, $mdContent, $utf8NoBom)
        }
        $totalTopics++
    }

    if ($DeleteHlp -and -not $DryRun) {
        Remove-Item $hlp.FullName -Force
    }
}

$mode = if ($DryRun) { "Dry run" } else { "Converted" }
Write-Host "$mode`: $($hlpFiles.Count) .hlp files -> $totalTopics Markdown topics in $OutDir"

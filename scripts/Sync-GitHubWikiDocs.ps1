#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$ExportRoot,
    [string]$WikiPath,
    [switch]$CommitWiki,
    [switch]$PushWiki,
    [string]$CommitMessage = 'Update wiki documentation'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)
    return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).ProviderPath
}

function Convert-ToRelativeKey {
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootPath = [System.IO.Path]::GetFullPath($script:RepositoryRootFull)
    $relative = [System.IO.Path]::GetRelativePath($rootPath, $fullPath)
    return ($relative -replace '\\', '/')
}

function Resolve-RelativeDocumentKey {
    param(
        [Parameter(Mandatory)][string]$CurrentRelativePath,
        [Parameter(Mandatory)][string]$Target
    )

    $targetWithoutFragment = ($Target -split '#', 2)[0]
    if ($targetWithoutFragment -match '^[a-zA-Z][a-zA-Z0-9+.-]*:') {
        return $null
    }

    if ($targetWithoutFragment.StartsWith('/')) {
        return ($targetWithoutFragment.TrimStart('/') -replace '\\', '/')
    }

    if ($targetWithoutFragment.StartsWith('docs/') -or $targetWithoutFragment -eq 'README.md') {
        return ($targetWithoutFragment -replace '\\', '/')
    }

    $currentDirectory = Split-Path -Parent $CurrentRelativePath
    $combined = if ($currentDirectory) {
        Join-Path $script:RepositoryRootFull (Join-Path $currentDirectory $targetWithoutFragment)
    } else {
        Join-Path $script:RepositoryRootFull $targetWithoutFragment
    }

    if (-not (Test-Path -LiteralPath $combined)) {
        return $null
    }

    return Convert-ToRelativeKey -Path $combined
}

function Convert-MarkdownLinksToWiki {
    param(
        [Parameter(Mandatory)][string]$Content,
        [Parameter(Mandatory)][string]$CurrentRelativePath,
        [Parameter(Mandatory)][hashtable]$PageByRelativePath
    )

    $content = [regex]::Replace(
        $Content,
        '(?<prefix>!?\[[^\]]+\]\()(?<target>[^)\s]+\.md)(?<fragment>#[^)]+)?(?<suffix>\))',
        {
            param($match)

            $target = $match.Groups['target'].Value
            $key = Resolve-RelativeDocumentKey -CurrentRelativePath $CurrentRelativePath -Target $target
            if (-not $key -or -not $PageByRelativePath.ContainsKey($key)) {
                return $match.Value
            }

            $fragment = $match.Groups['fragment'].Value
            return $match.Groups['prefix'].Value + $PageByRelativePath[$key].PageName + $fragment + $match.Groups['suffix'].Value
        })

    return [regex]::Replace(
        $content,
        '`(?<target>(?:docs|README)[^`]+\.md)`',
        {
            param($match)

            $target = $match.Groups['target'].Value
            $key = Resolve-RelativeDocumentKey -CurrentRelativePath $CurrentRelativePath -Target $target
            if (-not $key -or -not $PageByRelativePath.ContainsKey($key)) {
                return $match.Value
            }

            return "[$($PageByRelativePath[$key].Title)]($($PageByRelativePath[$key].PageName))"
        })
}

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Content
    )

    $utf8 = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Destination)) {
        [void][System.IO.Directory]::CreateDirectory($Destination)
    }

    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
    }
}

$defaultRepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$script:RepositoryRootFull = if ($RepositoryRoot) { Resolve-FullPath $RepositoryRoot } else { $defaultRepositoryRoot }
$exportRootFull = if ($ExportRoot) {
    Resolve-FullPath $ExportRoot
} else {
    Resolve-FullPath (Join-Path $script:RepositoryRootFull 'docs/Project/wiki/github')
}

$documents = @(
    @{ Group = 'User Guides'; Title = 'User Docs Index'; Source = 'docs/user/index.md'; PageName = 'User-Docs-Index' },
    @{ Group = 'User Guides'; Title = 'Getting Started'; Source = 'docs/user/getting-started.md'; PageName = 'Getting-Started' },
    @{ Group = 'User Guides'; Title = 'Server Integration'; Source = 'docs/user/server-integration.md'; PageName = 'Server-Integration' },
    @{ Group = 'User Guides'; Title = 'Client Tool'; Source = 'docs/user/client-tool.md'; PageName = 'Client-Tool' },
    @{ Group = 'User Guides'; Title = 'Android ADB Connections'; Source = 'docs/user/android-adb.md'; PageName = 'Android-ADB' },
    @{ Group = 'User Guides'; Title = 'Codex MCP'; Source = 'docs/user/codex-mcp.md'; PageName = 'Codex-MCP' },
    @{ Group = 'User Guides'; Title = 'Settings Guide'; Source = 'docs/user/settings.md'; PageName = 'Settings-Guide' },
    @{ Group = 'User Guides'; Title = 'Security Guide'; Source = 'docs/user/security.md'; PageName = 'Security' },
    @{ Group = 'User Guides'; Title = 'Troubleshooting'; Source = 'docs/user/troubleshooting.md'; PageName = 'Troubleshooting' },
    @{ Group = 'Architecture'; Title = 'Architecture Overview'; Source = 'docs/architecture/overview.md'; PageName = 'Architecture-Overview' },
    @{ Group = 'Architecture'; Title = 'Security Model'; Source = 'docs/architecture/security-model.md'; PageName = 'Security-Model' },
    @{ Group = 'Architecture'; Title = 'Android ADB Connectivity'; Source = 'docs/architecture/android-adb-connectivity.md'; PageName = 'Android-ADB-Connectivity' },
    @{ Group = 'Architecture'; Title = 'Android Bridge Transport'; Source = 'docs/architecture/android-bridge-transport.md'; PageName = 'Android-Bridge-Transport' },
    @{ Group = 'Architecture'; Title = 'Android Transport Spike'; Source = 'docs/architecture/android-transport-spike-2026-05-25.md'; PageName = 'Android-Transport-Spike-2026-05-25' },
    @{ Group = 'Architecture'; Title = 'Live Interactive View'; Source = 'docs/architecture/live-interactive-view.md'; PageName = 'Live-Interactive-View' },
    @{ Group = 'Architecture'; Title = 'Project Sessions And Replay'; Source = 'docs/architecture/project-sessions-and-replay.md'; PageName = 'Project-Sessions-And-Replay' },
    @{ Group = 'Architecture'; Title = 'Tool MCP Host'; Source = 'docs/architecture/tool-mcp-host.md'; PageName = 'Tool-MCP-Host' },
    @{ Group = 'Project Documentation'; Title = 'README'; Source = 'README.md'; PageName = 'Repository-README' },
    @{ Group = 'Project Documentation'; Title = 'Development Process'; Source = 'docs/Development-Process.md'; PageName = 'Development-Process' },
    @{ Group = 'Project Documentation'; Title = 'Release Notes'; Source = 'docs/release.md'; PageName = 'Release-Notes' },
    @{ Group = 'Project Documentation'; Title = 'Product Requirements'; Source = 'docs/requirements/product.md'; PageName = 'Product-Requirements' },
    @{ Group = 'Project Documentation'; Title = 'Functional Requirements Source'; Source = 'docs/requirements/functional-requirements.md'; PageName = 'Functional-Requirements-Source' },
    @{ Group = 'Project Documentation'; Title = 'Technical Requirements Source'; Source = 'docs/requirements/technical-requirements.md'; PageName = 'Technical-Requirements-Source' },
    @{ Group = 'Project Documentation'; Title = 'Testing Requirements Source'; Source = 'docs/requirements/testing-requirements.md'; PageName = 'Testing-Requirements-Source' },
    @{ Group = 'Project Documentation'; Title = 'Traceability Matrix Source'; Source = 'docs/requirements/traceability-matrix.md'; PageName = 'Traceability-Matrix-Source' },
    @{ Group = 'Project Documentation'; Title = 'Manual Acceptance Evidence'; Source = 'docs/requirements/manual-acceptance-evidence.md'; PageName = 'Manual-Acceptance-Evidence' }
)

$pageByRelativePath = @{}
foreach ($document in $documents) {
    $sourcePath = Join-Path $script:RepositoryRootFull $document.Source
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing source document: $($document.Source)"
    }

    $pageByRelativePath[($document.Source -replace '\\', '/')] = [pscustomobject]$document
}

foreach ($document in $documents) {
    $sourcePath = Join-Path $script:RepositoryRootFull $document.Source
    $content = [System.IO.File]::ReadAllText($sourcePath)
    $content = Convert-MarkdownLinksToWiki -Content $content -CurrentRelativePath ($document.Source -replace '\\', '/') -PageByRelativePath $pageByRelativePath

    $targetPath = Join-Path $exportRootFull ($document.PageName + '.md')
    Write-Utf8NoBom -Path $targetPath -Content $content
}

$generatedRequirements = @(
    @{ Title = 'Functional Requirements'; PageName = 'Functional-Requirements' },
    @{ Title = 'Technical Requirements'; PageName = 'Technical-Requirements' },
    @{ Title = 'Testing Requirements'; PageName = 'Testing-Requirements' },
    @{ Title = 'Traceability Mapping'; PageName = 'TR-per-FR-Mapping' },
    @{ Title = 'Requirements Matrix'; PageName = 'Requirements-Matrix' }
)

$homeLines = [System.Collections.Generic.List[string]]::new()
$homeLines.Add('# Avalonia.RemoteControl Documentation')
$homeLines.Add('')
$homeLines.Add('This wiki is generated from MCP requirements export and then synchronized with the repository documentation by `scripts/Sync-GitHubWikiDocs.ps1`.')
$homeLines.Add('')
$homeLines.Add('## Generated Requirements')
$homeLines.Add('')
foreach ($item in $generatedRequirements) {
    $homeLines.Add("- [$($item.Title)]($($item.PageName))")
}

foreach ($groupName in @('User Guides', 'Architecture', 'Project Documentation')) {
    $homeLines.Add('')
    $homeLines.Add("## $groupName")
    $homeLines.Add('')
    foreach ($document in $documents | Where-Object { $_.Group -eq $groupName }) {
        $homeLines.Add("- [$($document.Title)]($($document.PageName))")
    }
}

Write-Utf8NoBom -Path (Join-Path $exportRootFull 'Home.md') -Content (($homeLines -join [Environment]::NewLine) + [Environment]::NewLine)

$sidebarLines = [System.Collections.Generic.List[string]]::new()
$sidebarLines.Add('- [Home](Home)')
$sidebarLines.Add('')
$sidebarLines.Add('## Generated Requirements')
foreach ($item in $generatedRequirements) {
    $sidebarLines.Add("- [$($item.Title)]($($item.PageName))")
}

foreach ($groupName in @('User Guides', 'Architecture', 'Project Documentation')) {
    $sidebarLines.Add('')
    $sidebarLines.Add("## $groupName")
    foreach ($document in $documents | Where-Object { $_.Group -eq $groupName }) {
        $sidebarLines.Add("- [$($document.Title)]($($document.PageName))")
    }
}

Write-Utf8NoBom -Path (Join-Path $exportRootFull '_Sidebar.md') -Content (($sidebarLines -join [Environment]::NewLine) + [Environment]::NewLine)
Write-Utf8NoBom -Path (Join-Path $exportRootFull '_Footer.md') -Content ("Generated from MCP requirements export and repository documentation sync." + [Environment]::NewLine)

if ($WikiPath) {
    $wikiPathFull = Resolve-FullPath $WikiPath
    Copy-DirectoryContents -Source $exportRootFull -Destination $wikiPathFull

    if ($CommitWiki -or $PushWiki) {
        git -C $wikiPathFull add .
        $status = git -C $wikiPathFull status --short
        if ($status) {
            git -C $wikiPathFull commit -m $CommitMessage
        }
    }

    if ($PushWiki) {
        git -C $wikiPathFull push origin master
    }
}

Write-Host "Synced documentation into $exportRootFull"
if ($WikiPath) {
    Write-Host "Copied documentation into $(Resolve-FullPath $WikiPath)"
}

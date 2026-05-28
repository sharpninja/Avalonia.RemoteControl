#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [switch]$KeepRaw,
    [int]$WindowWidth = 1440,
    [int]$WindowHeight = 900
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)

    return (Resolve-Path -LiteralPath $Path -ErrorAction Stop).ProviderPath
}

function Add-Callout {
    param(
        [Parameter(Mandatory)][System.Drawing.Graphics]$Graphics,
        [Parameter(Mandatory)][System.Drawing.Rectangle]$Bounds,
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][System.Drawing.Color]$Color,
        [int]$LabelX = -1,
        [int]$LabelY = -1
    )

    $font = [System.Drawing.Font]::new('Segoe UI', 12, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Point)
    $textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $backgroundBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(232, 32, 32, 32))
    $pen = [System.Drawing.Pen]::new($Color, 4)
    $pen.Alignment = [System.Drawing.Drawing2D.PenAlignment]::Inset

    $Graphics.DrawRectangle($pen, $Bounds)

    $measured = $Graphics.MeasureString($Text, $font)
    $x = if ($LabelX -ge 0) { $LabelX } else { [Math]::Max(8, $Bounds.Left) }
    $y = if ($LabelY -ge 0) { $LabelY } else { [Math]::Max(8, $Bounds.Top - [int]$measured.Height - 10) }
    $labelBounds = [System.Drawing.RectangleF]::new($x, $y, $measured.Width + 16, $measured.Height + 10)

    $Graphics.FillRectangle($backgroundBrush, $labelBounds)
    $Graphics.DrawRectangle([System.Drawing.Pen]::new($Color, 2), [System.Drawing.Rectangle]::Round($labelBounds))
    $Graphics.DrawString($Text, $font, $textBrush, [System.Drawing.PointF]::new($x + 8, $y + 5))

    $font.Dispose()
    $textBrush.Dispose()
    $backgroundBrush.Dispose()
    $pen.Dispose()
}

function Save-AnnotatedImage {
    param(
        [Parameter(Mandatory)][System.Drawing.Bitmap]$Source,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][scriptblock]$Annotate
    )

    $bitmap = [System.Drawing.Bitmap]$Source.Clone()
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    & $Annotate $graphics

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
}

function Save-CroppedAnnotatedImage {
    param(
        [Parameter(Mandatory)][System.Drawing.Bitmap]$Source,
        [Parameter(Mandatory)][System.Drawing.Rectangle]$Crop,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][scriptblock]$Annotate
    )

    $bitmap = $Source.Clone($Crop, $Source.PixelFormat)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    & $Annotate $graphics

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
}

$repositoryRootFull = if ($RepositoryRoot) {
    Resolve-FullPath $RepositoryRoot
} else {
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
}

$outputDirectoryFull = if ($OutputDirectory) {
    [System.IO.Path]::GetFullPath($OutputDirectory, $repositoryRootFull)
} else {
    Join-Path $repositoryRootFull 'docs/user/images'
}

[void][System.IO.Directory]::CreateDirectory($outputDirectoryFull)

$exePath = Join-Path $repositoryRootFull 'src/Avalonia.RemoteControl.Tool/bin/Release/net10.0/Avalonia.RemoteControl.Tool.exe'
if (-not (Test-Path -LiteralPath $exePath)) {
    dotnet build (Join-Path $repositoryRootFull 'src/Avalonia.RemoteControl.Tool/Avalonia.RemoteControl.Tool.csproj') --configuration Release --no-restore
}

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Client executable was not found: $exePath"
}

$profileRoot = Join-Path $repositoryRootFull '.artifacts/docs-client-screenshots/profile'
[void][System.IO.Directory]::CreateDirectory($profileRoot)

$signature = @'
[DllImport("user32.dll")]
public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

[DllImport("user32.dll")]
public static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("user32.dll")]
public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}
'@

Add-Type -Namespace ArcDocs -Name NativeMethods -MemberDefinition $signature

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $exePath
$startInfo.WorkingDirectory = $repositoryRootFull
$startInfo.UseShellExecute = $false
$startInfo.Environment['APPDATA'] = Join-Path $profileRoot 'Roaming'
$startInfo.Environment['LOCALAPPDATA'] = Join-Path $profileRoot 'Local'

$process = [System.Diagnostics.Process]::Start($startInfo)
if ($null -eq $process) {
    throw 'Failed to start client process.'
}

try {
    $handle = [IntPtr]::Zero
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(20)
    do {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        $handle = $process.MainWindowHandle
    } while ($handle -eq [IntPtr]::Zero -and [DateTimeOffset]::UtcNow -lt $deadline -and -not $process.HasExited)

    if ($handle -eq [IntPtr]::Zero) {
        throw 'Timed out waiting for Avalonia Remote Control window.'
    }

    $hwndTopMost = [IntPtr]::new(-1)
    [void][ArcDocs.NativeMethods]::ShowWindow($handle, 9)
    [void][ArcDocs.NativeMethods]::SetWindowPos($handle, $hwndTopMost, 0, 0, $WindowWidth, $WindowHeight, 0x0040)
    [void][ArcDocs.NativeMethods]::SetForegroundWindow($handle)
    Start-Sleep -Seconds 2

    $rect = [ArcDocs.NativeMethods+RECT]::new()
    [void][ArcDocs.NativeMethods]::GetWindowRect($handle, [ref]$rect)

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -lt 1000 -or $height -lt 700) {
        throw "Unexpected window bounds: ${width}x${height}"
    }

    $raw = [System.Drawing.Bitmap]::new($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($raw)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, [System.Drawing.Size]::new($width, $height))
    $graphics.Dispose()

    if ($KeepRaw) {
        $raw.Save((Join-Path $outputDirectoryFull 'client-tool-raw.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    }

    Save-AnnotatedImage -Source $raw -Path (Join-Path $outputDirectoryFull 'client-tool-shell-overview.png') -Annotate {
        param($g)
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(0, 36, $width, 112)) -Text '1 Connection, TLS, logs, live view, profile actions' -Color ([System.Drawing.Color]::DeepSkyBlue) -LabelX 18 -LabelY 52
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(0, 146, $width, 44)) -Text '2 Android ADB discovery, package marker, forward controls' -Color ([System.Drawing.Color]::Orange) -LabelX 18 -LabelY 155
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(2, 190, 324, $height - 230)) -Text '3 Control tree' -Color ([System.Drawing.Color]::LimeGreen) -LabelX 24 -LabelY 210
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(332, 190, $width - 732, 500)) -Text '4 Workspace: Terminal and Properties' -Color ([System.Drawing.Color]::Gold) -LabelX 372 -LabelY 210
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new($width - 392, 190, 390, $height - 230)) -Text '5 Remote Tools tabs' -Color ([System.Drawing.Color]::MediumOrchid) -LabelX ($width - 370) -LabelY 210
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(332, $height - 250, $width - 732, 212)) -Text '6 Logs panel' -Color ([System.Drawing.Color]::Tomato) -LabelX 372 -LabelY ($height - 238)
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(0, $height - 32, $width, 30)) -Text '7 Status bar' -Color ([System.Drawing.Color]::DodgerBlue) -LabelX 18 -LabelY ($height - 66)
    }

    $topCrop = [System.Drawing.Rectangle]::new(0, 30, $width, 165)
    Save-CroppedAnnotatedImage -Source $raw -Crop $topCrop -Path (Join-Path $outputDirectoryFull 'client-tool-connection-bars.png') -Annotate {
        param($g)
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(128, 8, 335, 38)) -Text 'Endpoint' -Color ([System.Drawing.Color]::DeepSkyBlue) -LabelX 135 -LabelY 55
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(470, 8, 215, 38)) -Text 'Token' -Color ([System.Drawing.Color]::DeepSkyBlue) -LabelX 470 -LabelY 55
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(690, 8, 175, 38)) -Text 'Certificate path' -Color ([System.Drawing.Color]::DeepSkyBlue) -LabelX 690 -LabelY 55
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(870, 8, 525, 38)) -Text 'Connect, snapshot, logs, live view, save' -Color ([System.Drawing.Color]::Gold) -LabelX 895 -LabelY 55
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(870, 54, 145, 36)) -Text 'TLS trust actions' -Color ([System.Drawing.Color]::Orange) -LabelX 870 -LabelY 98
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(1218, 54, 175, 36)) -Text 'Transport' -Color ([System.Drawing.Color]::MediumOrchid) -LabelX 1218 -LabelY 98
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(128, 100, 1070, 42)) -Text 'ADB path, device, package, host port, connect, cleanup' -Color ([System.Drawing.Color]::LimeGreen) -LabelX 135 -LabelY 122
    }

    $workspaceCrop = [System.Drawing.Rectangle]::new(320, 180, $width - 320, 560)
    Save-CroppedAnnotatedImage -Source $raw -Crop $workspaceCrop -Path (Join-Path $outputDirectoryFull 'client-tool-workspace-tools.png') -Annotate {
        param($g)
        $cropWidth = $workspaceCrop.Width
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(20, 16, $cropWidth - 430, 42)) -Text 'Terminal and Properties tabs' -Color ([System.Drawing.Color]::Gold) -LabelX 32 -LabelY 65
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(32, 65, $cropWidth - 455, 120)) -Text 'Codex MCP launch profile' -Color ([System.Drawing.Color]::DeepSkyBlue) -LabelX 50 -LabelY 188
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(32, 188, $cropWidth - 455, 320)) -Text 'Embedded terminal output' -Color ([System.Drawing.Color]::LimeGreen) -LabelX 50 -LabelY 466
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new($cropWidth - 405, 15, 392, 520)) -Text 'Remote Tools: Actions, Live View, Project' -Color ([System.Drawing.Color]::MediumOrchid) -LabelX ($cropWidth - 390) -LabelY 65
        Add-Callout -Graphics $g -Bounds ([System.Drawing.Rectangle]::new(20, 520, $cropWidth - 430, 36)) -Text 'South dock/log surface' -Color ([System.Drawing.Color]::Tomato) -LabelX 32 -LabelY 478
    }

    $raw.Dispose()
}
finally {
    if (-not $process.HasExited) {
        [void]$process.CloseMainWindow()
        if (-not $process.WaitForExit(3000)) {
            $process.Kill($true)
            $process.WaitForExit()
        }
    }
}

Write-Host "Screenshots written to $outputDirectoryFull"

# 🚀 Fast Windows Build Script for Crowd Multiplier 3D
# Builds optimized Windows executable with enterprise performance

param(
    [switch]$Release,
    [switch]$RunAfterBuild,
    [string]$UnityPath = ""
)

# Configuration
$ProjectPath = Get-Location
$BuildName = if ($Release) { "CrowdMultiplier3D_Release" } else { "CrowdMultiplier3D_Development" }
$BuildDir = Join-Path $ProjectPath "Builds\Windows"
$BuildPath = Join-Path $BuildDir "$BuildName.exe"
$LogFile = Join-Path $ProjectPath "Logs\build_log.txt"

# Ensure directories exist
New-Item -ItemType Directory -Force -Path $BuildDir | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $LogFile) | Out-Null

Write-Host "🎮 Building Crowd Multiplier 3D for Windows..." -ForegroundColor Cyan
Write-Host "📁 Project: $ProjectPath" -ForegroundColor Gray
Write-Host "🎯 Target: $BuildPath" -ForegroundColor Gray
Write-Host "⚙️  Mode: $(if ($Release) { 'Release' } else { 'Development' })" -ForegroundColor Gray

# Find Unity executable
if ([string]::IsNullOrEmpty($UnityPath)) {
    $UnityPaths = @(
        "${env:ProgramFiles}\Unity\Hub\Editor\*\Editor\Unity.exe",
        "${env:ProgramFiles(x86)}\Unity\Hub\Editor\*\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\*\Editor\Unity.exe"
    )
    
    foreach ($path in $UnityPaths) {
        $found = Get-ChildItem -Path $path -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($found) {
            $UnityPath = $found.FullName
            break
        }
    }
}

if (-not (Test-Path $UnityPath)) {
    Write-Host "❌ Unity not found! Please install Unity 2023.3 LTS or specify path with -UnityPath" -ForegroundColor Red
    exit 1
}

Write-Host "🔧 Using Unity: $UnityPath" -ForegroundColor Green

# Build arguments
$BuildArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", "`"$ProjectPath`"",
    "-executeMethod", "CrowdMultiplier.BuildManagement.WindowsBuildScript.BuildFromCommandLine",
    "-logFile", "`"$LogFile`""
)

if ($Release) {
    $BuildArgs += "-release"
}

# Start build
$StartTime = Get-Date
Write-Host "⏳ Starting build process..." -ForegroundColor Yellow

try {
    $Process = Start-Process -FilePath $UnityPath -ArgumentList $BuildArgs -Wait -PassThru -NoNewWindow
    
    $EndTime = Get-Date
    $BuildTime = ($EndTime - $StartTime).TotalSeconds
    
    if ($Process.ExitCode -eq 0) {
        Write-Host "✅ Build completed successfully!" -ForegroundColor Green
        Write-Host "⏱️  Build time: $([math]::Round($BuildTime, 1)) seconds" -ForegroundColor Green
        
        if (Test-Path $BuildPath) {
            $FileSize = [math]::Round((Get-Item $BuildPath).Length / 1MB, 1)
            Write-Host "📦 File size: $FileSize MB" -ForegroundColor Green
            Write-Host "📁 Location: $BuildPath" -ForegroundColor Green
            
            # Show build in explorer
            Invoke-Item (Split-Path $BuildPath)
            
            if ($RunAfterBuild) {
                Write-Host "🎮 Launching game..." -ForegroundColor Cyan
                Start-Process -FilePath $BuildPath
            }
            
            Write-Host ""
            Write-Host "🎉 Ready to test your enterprise game with full Windows performance!" -ForegroundColor Magenta
            Write-Host "   • Full 60+ FPS performance" -ForegroundColor White
            Write-Host "   • Complete analytics tracking" -ForegroundColor White
            Write-Host "   • ML predictions and optimization" -ForegroundColor White
            Write-Host "   • Real-time monitoring dashboard" -ForegroundColor White
            
        } else {
            Write-Host "⚠️  Build process completed but executable not found!" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Build failed with exit code: $($Process.ExitCode)" -ForegroundColor Red
        
        if (Test-Path $LogFile) {
            Write-Host "📄 Build log:" -ForegroundColor Yellow
            Get-Content $LogFile | Select-Object -Last 20 | ForEach-Object {
                Write-Host "   $_" -ForegroundColor Gray
            }
        }
    }
} catch {
    Write-Host "❌ Build process failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Show build log if available
if (Test-Path $LogFile) {
    Write-Host ""
    Write-Host "📋 Full build log available at: $LogFile" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🔄 To rebuild:" -ForegroundColor Cyan
Write-Host "   PowerShell: .\BuildWindows.ps1" -ForegroundColor White
Write-Host "   Release:    .\BuildWindows.ps1 -Release" -ForegroundColor White
Write-Host "   Auto-run:   .\BuildWindows.ps1 -RunAfterBuild" -ForegroundColor White

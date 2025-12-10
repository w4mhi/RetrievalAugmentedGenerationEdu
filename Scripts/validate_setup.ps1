<#
.SYNOPSIS
    Comprehensive validation script for OmniRAG RAG System.

.DESCRIPTION
    Validates all prerequisites and configurations needed to run OmniRAG.
    Checks: Python environment, Phi-4 model, PDF documents, dependencies, and configuration.

.EXAMPLE
    .\validate_setup.ps1
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$script:HasErrors = $false
$script:HasWarnings = $false

# Color functions
function Write-Success { param([string]$Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Failure { param([string]$Message) Write-Host "✗ $Message" -ForegroundColor Red; $script:HasErrors = $true }
function Write-Warning { param([string]$Message) Write-Host "⚠ $Message" -ForegroundColor Yellow; $script:HasWarnings = $true }
function Write-Info { param([string]$Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-SectionHeader { param([string]$Title) Write-Host "`n═══ $Title ═══" -ForegroundColor Magenta }

# Clear screen and display banner
Clear-Host
Write-Host @"
╔═══════════════════════════════════════════════════╗
║                                                   ║
║           OmniRAG Validation Script               ║
║              RAG System Setup Checker             ║
║                                                   ║
╚═══════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

# 1. Check .NET SDK
Write-SectionHeader "1. .NET SDK"
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion) {
        if ($dotnetVersion -match "^9\.") {
            Write-Success ".NET SDK $dotnetVersion installed"
        } else {
            Write-Warning ".NET SDK $dotnetVersion found (version 9.0+ recommended)"
        }
    } else {
        Write-Failure ".NET SDK not found"
    }
} catch {
    Write-Failure ".NET SDK not installed or not in PATH"
}

# 2. Check Python Environment
Write-SectionHeader "2. Python Environment"

# Check if python_env exists
if (Test-Path ".\python_env") {
    Write-Success "Python virtual environment directory exists"
    
    # Check for Python executable in venv
    $pythonExe = ".\python_env\Scripts\python.exe"
    if (Test-Path $pythonExe) {
        $pythonVersion = & $pythonExe --version 2>&1
        Write-Success "Python in venv: $pythonVersion"
        
        # Check Python packages
        Write-Info "Checking Python packages..."
        $packages = & $pythonExe -m pip list 2>&1 | Out-String
        
        $requiredPackages = @("chromadb", "sentence-transformers", "torch")
        foreach ($pkg in $requiredPackages) {
            if ($packages -match $pkg) {
                Write-Success "  Package '$pkg' installed"
            } else {
                Write-Failure "  Package '$pkg' NOT installed"
            }
        }
    } else {
        Write-Failure "Python executable not found in virtual environment"
        Write-Info "  Run: .\setup_python.ps1"
    }
} else {
    Write-Failure "Python virtual environment not found"
    Write-Info "  Run: .\setup_python.ps1"
}

# 3. Check Configuration File
Write-SectionHeader "3. Configuration File"

$configPath = ".\OmniRAG.Console\appsettings.json"
if (Test-Path $configPath) {
    Write-Success "appsettings.json found"
    
    try {
        $config = Get-Content $configPath -Raw | ConvertFrom-Json
        
        # Check Python DLL path
        $pythonDll = $config.OmniRAG.Python.DllPath
        if ($pythonDll -and (Test-Path $pythonDll)) {
            Write-Success "Python DLL path configured and exists"
            Write-Info "  Path: $pythonDll"
        } else {
            Write-Failure "Python DLL path not configured or doesn't exist"
            Write-Info "  Expected in: OmniRAG.Python.DllPath"
        }
        
        # Check Python Home path
        $pythonHome = $config.OmniRAG.Python.HomePath
        if ($pythonHome -and (Test-Path $pythonHome)) {
            Write-Success "Python Home path configured and exists"
        } else {
            Write-Warning "Python Home path not configured or doesn't exist"
        }
        
        # Check Chroma directory
        $chromaDir = $config.OmniRAG.ChromaPersistDirectory
        if ($chromaDir) {
            $fullChromaPath = if ([System.IO.Path]::IsPathRooted($chromaDir)) { $chromaDir } else { Join-Path $PSScriptRoot $chromaDir }
            Write-Success "Chroma persist directory configured: $chromaDir"
            if (!(Test-Path $fullChromaPath)) {
                Write-Info "  Directory will be created on first run"
            }
        } else {
            Write-Failure "Chroma persist directory not configured"
        }
        
        # Check PDF directory
        $pdfDir = $config.OmniRAG.PdfDirectory
        if ($pdfDir) {
            Write-Success "PDF directory configured: $pdfDir"
        } else {
            Write-Failure "PDF directory not configured"
        }
        
        # Check Phi-4 configuration
        $phi4Enabled = $config.OmniRAG.Phi4.Enabled
        if ($phi4Enabled) {
            Write-Info "Phi-4 LLM enabled in configuration"
            
            $phi4Path = $config.OmniRAG.Phi4.ModelPath
            if ($phi4Path -and (Test-Path $phi4Path)) {
                Write-Success "Phi-4 model path exists"
                Write-Info "  Path: $phi4Path"
                
                # Check for model files
                $modelFiles = Get-ChildItem $phi4Path -Filter "*.onnx" -Recurse -ErrorAction SilentlyContinue
                if ($modelFiles) {
                    Write-Success "  Found ONNX model files"
                } else {
                    Write-Warning "  No ONNX model files found"
                }
            } else {
                Write-Failure "Phi-4 model path not configured or doesn't exist"
                Write-Info "  Download Phi-4 via AI Toolkit: Ctrl+Shift+P > 'AI Toolkit: Download Model'"
            }
        } else {
            Write-Warning "Phi-4 LLM disabled (retrieval-only mode)"
            Write-Info "  Set OmniRAG.Phi4.Enabled = true to enable LLM"
        }
        
    } catch {
        Write-Failure "Failed to parse appsettings.json: $($_.Exception.Message)"
    }
} else {
    Write-Failure "appsettings.json not found at $configPath"
}

# 4. Check PDF Documents
Write-SectionHeader "4. PDF Documents"

$pdfPath = ".\pdf"
if (Test-Path $pdfPath) {
    $pdfFiles = Get-ChildItem $pdfPath -Filter "*.pdf" -File
    if ($pdfFiles.Count -gt 0) {
        Write-Success "Found $($pdfFiles.Count) PDF file(s)"
        foreach ($pdf in $pdfFiles) {
            $sizeKB = [math]::Round($pdf.Length / 1KB, 2)
            Write-Info "  - $($pdf.Name) ($sizeKB KB)"
        }
    } else {
        Write-Warning "No PDF files found in .\pdf directory"
        Write-Info "  Add PDF technical manuals to test the system"
    }
} else {
    Write-Failure "PDF directory not found: $pdfPath"
}

# 5. Check Project Build
Write-SectionHeader "5. Project Build Status"

try {
    Write-Info "Building project..."
    $buildOutput = dotnet build --configuration Release --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Project builds successfully"
    } else {
        Write-Failure "Project build failed"
        if ($Verbose) {
            Write-Host $buildOutput -ForegroundColor Gray
        }
    }
} catch {
    Write-Failure "Failed to run build: $($_.Exception.Message)"
}

# 6. Check Tests
Write-SectionHeader "6. Unit Tests"

try {
    Write-Info "Running tests..."
    $testOutput = dotnet test --configuration Release --no-build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        # Parse test results
        $testResults = $testOutput | Select-String "Passed|Failed|Skipped"
        Write-Success "All tests passed"
        if ($Verbose -and $testResults) {
            Write-Host "  $testResults" -ForegroundColor Gray
        }
    } else {
        Write-Failure "Some tests failed"
        if ($Verbose) {
            Write-Host $testOutput -ForegroundColor Gray
        }
    }
} catch {
    Write-Failure "Failed to run tests: $($_.Exception.Message)"
}

# 7. Check Dependencies
Write-SectionHeader "7. NuGet Dependencies"

$csprojFiles = Get-ChildItem -Recurse -Filter "*.csproj" | Where-Object { $_.FullName -notmatch "\\obj\\" }
$missingPackages = $false

foreach ($csproj in $csprojFiles) {
    $projectName = $csproj.BaseName
    $objPath = Join-Path $csproj.DirectoryName "obj"
    
    if (Test-Path $objPath) {
        $assetsFile = Join-Path $objPath "project.assets.json"
        if (Test-Path $assetsFile) {
            # Project has been restored
            continue
        } else {
            Write-Warning "Project '$projectName' may need restore"
            $missingPackages = $true
        }
    } else {
        Write-Warning "Project '$projectName' not restored"
        $missingPackages = $true
    }
}

if (!$missingPackages) {
    Write-Success "All NuGet packages appear to be restored"
} else {
    Write-Info "  Run: dotnet restore"
}

# Summary
Write-SectionHeader "Validation Summary"

if ($script:HasErrors) {
    Write-Host "`n❌ VALIDATION FAILED - Please fix the errors above" -ForegroundColor Red
    Write-Host "`nQuick fixes:" -ForegroundColor Yellow
    Write-Host "  1. Run: .\setup_python.ps1" -ForegroundColor White
    Write-Host "  2. Download Phi-4 via AI Toolkit (optional)" -ForegroundColor White
    Write-Host "  3. Update appsettings.json with correct paths" -ForegroundColor White
    Write-Host "  4. Add PDF files to .\pdf directory" -ForegroundColor White
    exit 1
} elseif ($script:HasWarnings) {
    Write-Host "`n⚠ VALIDATION PASSED WITH WARNINGS" -ForegroundColor Yellow
    Write-Host "  The system should work but review warnings above" -ForegroundColor White
    exit 0
} else {
    Write-Host "`n✅ VALIDATION PASSED - System ready to run!" -ForegroundColor Green
    Write-Host "`nTo start the application:" -ForegroundColor Cyan
    Write-Host "  cd OmniRAG.Console" -ForegroundColor White
    Write-Host "  dotnet run" -ForegroundColor White
    exit 0
}

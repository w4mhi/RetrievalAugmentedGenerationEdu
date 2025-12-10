# OmniRAG - Python Environment Setup Script

Write-Host "OmniRAG - Python Environment Setup" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Check if Python is installed
try {
    $pythonVersion = python --version 2>&1
    Write-Host "✓ Found Python: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Python not found. Please install Python 3.12+." -ForegroundColor Red
    exit 1
}

# Create virtual environment if it doesn''t exist
if (-not (Test-Path "python_env")) {
    Write-Host "Creating virtual environment..." -ForegroundColor Yellow
    python -m venv python_env
    Write-Host "✓ Virtual environment created" -ForegroundColor Green
} else {
    Write-Host "✓ Virtual environment already exists" -ForegroundColor Green
}

# Activate virtual environment
Write-Host "Activating virtual environment..." -ForegroundColor Yellow
& ".\python_env\Scripts\Activate.ps1"

# Upgrade pip
Write-Host "Upgrading pip..." -ForegroundColor Yellow
python -m pip install --upgrade pip

# Install requirements
Write-Host "Installing Python packages..." -ForegroundColor Yellow
pip install -r requirements.txt

Write-Host ""
Write-Host "✓ Python environment setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Python DLL Path:" -ForegroundColor Cyan
$pythonBase = python -c "import sys; print(sys.base_prefix)"
$pythonDll = Join-Path $pythonBase "python312.dll"
Write-Host "  $pythonDll" -ForegroundColor White
Write-Host ""
Write-Host "Update this path in OmniRAG.Console/appsettings.json" -ForegroundColor Yellow
Write-Host ""

# Test imports
Write-Host "Testing Python imports..." -ForegroundColor Yellow
python -c "import chromadb; import sentence_transformers; print(''✓ All imports successful'')"

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Update appsettings.json with your Python paths" -ForegroundColor White
Write-Host "2. Add PDF files to the ./pdf directory" -ForegroundColor White
Write-Host "3. Run: cd OmniRAG.Console && dotnet run" -ForegroundColor White

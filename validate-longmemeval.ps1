# LongMemEval Validation Script
# Run this to validate MemPalace.NET R@5 parity for v0.6.0 release

Write-Host "LongMemEval R@5 Validation for v0.6.0" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if dataset exists
$datasetPath = "artifacts\benchmarks\longmemeval_s_cleaned.json"
$palacePath = "artifacts\benchmarks\palace-local"

if (-not (Test-Path $datasetPath)) {
    Write-Host "Step 1: Downloading LongMemEval dataset..." -ForegroundColor Yellow
    
    # Create directory if needed
    $artifactsDir = Split-Path $datasetPath -Parent
    if (-not (Test-Path $artifactsDir)) {
        New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
    }
    
    # Download dataset
    $url = "https://huggingface.co/datasets/xiaowu0162/longmemeval-cleaned/resolve/main/longmemeval_s_cleaned.json"
    Write-Host "  Downloading from: $url" -ForegroundColor Gray
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $datasetPath -UseBasicParsing
        Write-Host "  ✓ Dataset downloaded successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Failed to download dataset: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please download manually from:" -ForegroundColor Yellow
        Write-Host "  $url" -ForegroundColor Gray
        Write-Host "And save to:" -ForegroundColor Yellow
        Write-Host "  $datasetPath" -ForegroundColor Gray
        exit 1
    }
}
else {
    Write-Host "Step 1: Dataset already exists at $datasetPath" -ForegroundColor Green
}

Write-Host ""

# Step 2: Verify dataset format
Write-Host "Step 2: Verifying dataset format..." -ForegroundColor Yellow
$datasetSize = (Get-Item $datasetPath).Length / 1MB
Write-Host "  Dataset size: $($datasetSize.ToString('F2')) MB" -ForegroundColor Gray

try {
    $firstLine = Get-Content $datasetPath -TotalCount 1
    if ($firstLine.StartsWith("[")) {
        Write-Host "  ✓ Format: JSON array (upstream LongMemEval)" -ForegroundColor Green
    }
    else {
        Write-Host "  ! Format: JSONL (will attempt to parse)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "  ✗ Cannot read dataset file" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Run baseline validation
Write-Host "Step 3: Running baseline validation with local ONNX embedder..." -ForegroundColor Yellow
Write-Host "  This will take 5-10 minutes for 500 queries" -ForegroundColor Gray
Write-Host "  Target: R@5 ≥ 91%" -ForegroundColor Gray
Write-Host ""

$benchCmd = "dotnet run --project src\MemPalace.Benchmarks -- run longmemeval --dataset $datasetPath --palace $palacePath --embedder local"
Write-Host "  Command: $benchCmd" -ForegroundColor Gray
Write-Host ""

try {
    $output = & dotnet run --project src\MemPalace.Benchmarks -- run longmemeval --dataset $datasetPath --palace $palacePath --embedder local 2>&1
    
    Write-Host $output
    
    # Try to parse R@5 from output
    $r5Match = $output | Select-String "Recall@5.*?(\d+\.\d+)"
    if ($r5Match) {
        $r5Value = [double]$r5Match.Matches[0].Groups[1].Value
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "RESULT: R@5 = $($r5Value * 100)%" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        
        if ($r5Value -ge 0.91) {
            Write-Host "✓ PASS: Meets v0.6.0 target (≥91%)" -ForegroundColor Green
            Write-Host ""
            Write-Host "Next steps:" -ForegroundColor Yellow
            Write-Host "  1. Update docs\benchmarks.md 'Parity Results' section with this score" -ForegroundColor Gray
            Write-Host "  2. Update docs\CHANGELOG.md v0.6.0 entry" -ForegroundColor Gray
            Write-Host "  3. Request Deckard's review for release approval" -ForegroundColor Gray
        }
        else {
            Write-Host "✗ BELOW TARGET: $($r5Value * 100)% < 91%" -ForegroundColor Red
            Write-Host ""
            Write-Host "Investigation needed:" -ForegroundColor Yellow
            Write-Host "  - Check corpus ingestion logic" -ForegroundColor Gray
            Write-Host "  - Verify embedder normalization (L2 vs cosine)" -ForegroundColor Gray
            Write-Host "  - Confirm top-k retrieval count (≥5)" -ForegroundColor Gray
            Write-Host "  - Check relevant ID matching (case sensitivity, whitespace)" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "⚠ Could not parse R@5 from output" -ForegroundColor Yellow
        Write-Host "Please review output above and extract ExtraMetrics['Recall@5']" -ForegroundColor Gray
    }
}
catch {
    Write-Host "✗ Benchmark execution failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Python baseline (reference): 96.6% R@5" -ForegroundColor Gray
Write-Host "Embedder: nomic-embed-text (1536-dim)" -ForegroundColor Gray
Write-Host ""
Write-Host "MemPalace.NET baseline (this run): [see above]" -ForegroundColor Gray
Write-Host "Embedder: sentence-transformers/all-MiniLM-L6-v2 (384-dim)" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "For closest parity with Python (optional):" -ForegroundColor Yellow
Write-Host "  1. Install Ollama: https://ollama.ai" -ForegroundColor Gray
Write-Host "  2. Pull model: ollama pull nomic-embed-text" -ForegroundColor Gray
Write-Host "  3. Run: dotnet run --project src\MemPalace.Benchmarks -- run longmemeval --dataset $datasetPath --palace artifacts\benchmarks\palace-nomic --embedder ollama --model nomic-embed-text" -ForegroundColor Gray
Write-Host ""

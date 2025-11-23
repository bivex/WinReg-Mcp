# Test script for Windows Registry access
Write-Host "=== Testing Windows Registry Access ===" -ForegroundColor Green

# Test 1: Read Windows version
Write-Host "`nTest 1: Reading Windows ProductName..." -ForegroundColor Yellow
try {
    $productName = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" -Name "ProductName" -ErrorAction Stop
    Write-Host "SUCCESS: ProductName = $($productName.ProductName)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Try to access SECURITY (should fail)
Write-Host "`nTest 2: Attempting to access SECURITY\SAM (should fail)..." -ForegroundColor Yellow
try {
    $test = Get-ItemProperty -Path "HKLM:\SECURITY\SAM" -Name "test" -ErrorAction Stop
    Write-Host "WARNING: Access granted (unexpected!)" -ForegroundColor Yellow
} catch {
    Write-Host "EXPECTED: Access denied - $($_.Exception.Message)" -ForegroundColor Green
}

# Test 3: Enumerate Microsoft keys
Write-Host "`nTest 3: Enumerating Microsoft keys..." -ForegroundColor Yellow
try {
    $keys = Get-ChildItem -Path "HKCU:\Software\Microsoft" -ErrorAction Stop | Select-Object -First 10 -ExpandProperty Name
    Write-Host "SUCCESS: Found $($keys.Count) keys" -ForegroundColor Green
    foreach ($key in $keys) {
        Write-Host "  - $key" -ForegroundColor Cyan
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Tests Complete ===" -ForegroundColor Green


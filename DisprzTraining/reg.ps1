# Force TLS 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Ignore SSL certificate issues for localhost
Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

# Base URLs
$usersUrl = "http://localhost:5169/api/users"
$authUrl  = "http://localhost:5169/api/auth"

# Test user credentials
$username = "testuser5"
$password = "Password@1234898"
$timezone = "India Standard Time"

Write-Host "`n=== Registering User ==="
$registerBody = @{
    username   = $username
    password   = $password
    timeZoneId = $timezone
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$usersUrl/register" -Method POST -Headers @{ "Content-Type" = "application/json" } -Body $registerBody
    Write-Host "User registered successfully!"
    $registerResponse | ConvertTo-Json -Depth 5
} catch {
    Write-Host "⚠️ Registration failed: $($_.Exception.Message)"
}

Write-Host "`n=== Authenticating User ==="
$authBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

try {
    $authResponse = Invoke-RestMethod -Uri "$authUrl/login" -Method POST -Headers @{ "Content-Type" = "application/json" } -Body $authBody
    Write-Host "User authenticated successfully!"
    Write-Host "JWT Token:`n$($authResponse.token)"
} catch {
    Write-Host "⚠️ Authentication failed: $($_.Exception.Message)"
}

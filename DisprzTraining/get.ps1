# Auth endpoint - replace with your actual login API
$authUrl = "http://localhost:5169/api/auth/login"  

$body = @{
    username = "suratha"
    password = "porul"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $authUrl -Method Post -Body $body -ContentType "application/json"
$token = $response.token  # adjust property name if different
$appointmentsUrl = "http://localhost:5169/api/appointments/user"

$response = Invoke-RestMethod -Uri $appointmentsUrl -Method Get -Headers @{ Authorization = "Bearer $token" }

$response | ConvertTo-Json


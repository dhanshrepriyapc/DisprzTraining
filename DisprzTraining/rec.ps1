# -------------------------------
# User login and create recurring appointment
# -------------------------------

# Login
$authUrl = "http://localhost:5169/api/users/login"
$loginBody = @{
    username = "suratha"
    password = "porul"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri $authUrl -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
if (-not $token) { throw "Login failed: token not returned" }

Write-Host "Login successful, token acquired."

# Create a recurring appointment (Daily from 20 Sep to 26 Sep, 1 PM - 2 PM)
$appointmentsUrl = "http://localhost:5169/api/appointments/user"

$apptBody = @{
    Title              = "Daily Meeting"
    StartTime          = "2025-09-20T13:00:00"
    EndTime            = "2025-09-20T14:00:00"
    Type               = "Meeting"
    ColorCode          = "#1976d2"
    Recurrence         = 1       # 0=None, 1=Daily, 2=Weekly, 3=Monthly
    RecurrenceInterval = 1       # Every 1 day
    RecurrenceEndDate  = "2025-09-26T14:00:00"
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri $appointmentsUrl -Method Post -Headers @{ Authorization = "Bearer $token" } -Body $apptBody -ContentType "application/json"
    Write-Host "Recurring appointment created successfully with ID $($createResponse.Id)"
} catch {
    Write-Warning "Failed to create recurring appointment: $($_.Exception.Message)"
}

# Get all appointments for the user
try {
    $allAppointments = Invoke-RestMethod -Uri $appointmentsUrl -Method Get -Headers @{ Authorization = "Bearer $token" }
    Write-Host "All appointments for user:"
    $allAppointments | ConvertTo-Json -Depth 5
} catch {
    Write-Warning "Failed to retrieve appointments: $($_.Exception.Message)"
}

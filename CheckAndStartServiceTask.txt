# Variables
$taskAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument {
    # Check if abcService is running
    $serviceName = 'abcService'
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

    if ($service -eq $null -or $service.Status -ne 'Running') {
        # Start the service forcefully
        Start-Service -Name $serviceName -Force
        Write-Host "$serviceName service started."
    } else {
        Write-Host "$serviceName service is already running."
    }
}

# Trigger to run the task every 1 hour
$taskTrigger = New-ScheduledTaskTrigger -Daily -At '12:00 AM' -RepetitionInterval ([TimeSpan]::FromHours(1))

# Register the scheduled task
Register-ScheduledTask -Action $taskAction -Trigger $taskTrigger -TaskName 'CheckAndStartServiceTask' -Description 'Task to check and start abcService if not running'

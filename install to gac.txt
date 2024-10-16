param (
    [Parameter(Mandatory=$true)]
    [string]$AssemblyPath  # The path to the DLL that you want to install in the GAC
)

# Path to the System.EnterpriseServices assembly in the GAC
$gacPath = "C:\Windows\Microsoft.NET\assembly\GAC_32\System.EnterpriseServices\4.0.0.0__b03f5f7f11d50a3a\System.EnterpriseServices.dll"

# Load the System.EnterpriseServices assembly from the GAC
try {
    [System.Reflection.Assembly]::LoadFrom($gacPath)
    Write-Host "System.EnterpriseServices assembly loaded successfully."
} catch {
    Write-Host "Failed to load System.EnterpriseServices assembly. Error: $_"
    exit 1
}

# Create an instance of the Publish class
try {
    $publish = New-Object -TypeName System.EnterpriseServices.Internal.Publish
    Write-Host "Publish object created successfully."
} catch {
    Write-Host "Failed to create Publish object. Error: $_"
    exit 1
}

# Install the assembly into the GAC
try {
    $publish.GacInstall($AssemblyPath)
    Write-Host "Assembly '$AssemblyPath' installed successfully in the GAC."
} catch {
    Write-Host "Failed to install assembly '$AssemblyPath' in the GAC. Error: $_"
    exit 1
}

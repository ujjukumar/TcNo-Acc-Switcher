# Define the paths to the NSIS script file
$dateVersion = Read-Host "Enter the date version number (format: YYYY-MM-DD_VV)"
$nsisVersion = $dateVersion -replace "-", "." -replace "_", "."

# ------------------------------- 
# Update the NSIS script file
# ------------------------------- 

$nsisFilePath = "other\NSIS\nsis-build-x64.nsi"
$nsisFileContent = Get-Content $nsisFilePath
$nsisReplacement = '!define VERSION "' + $nsisVersion + '"'
$nsisFileContent = $nsisFileContent -replace '^!define VERSION\s+".*"$', $nsisReplacement

$nsisReplacement = '!define DISPLAY_VERSION "' + $dateVersion + '"'
$nsisFileContent = $nsisFileContent -replace '^!define DISPLAY_VERSION\s+".*"$', $nsisReplacement

$nsisFileContent | Set-Content $nsisFilePath

Write-Host "UPDATED NSIS"

# ------------------------------- 
# Helper function to update .csproj version
# ------------------------------- 
function Update-CsprojVersion {
    param (
        [string]$filePath,
        [string]$version
    )
    
    if (Test-Path $filePath) {
        $content = Get-Content $filePath
        $versionTag = "<Version>$version</Version>"
        $assemblyVersionTag = "<AssemblyVersion>$version</AssemblyVersion>"
        $fileVersionTag = "<FileVersion>$version</FileVersion>"
        
        # Check if Version tag exists, if not, we might need to insert it (simple regex replace assumes it exists or we skip)
        # For this script, we assume standard csproj structure where we replace existing or insert if we get fancy.
        # But simpler: assume projects have <Version> or we add it to PropertyGroup.
        
        # However, to be safe and simple like the original script:
        if ($content -match '<Version>.*</Version>') {
            $content = $content -replace '<Version>.*</Version>', $versionTag
        }
        if ($content -match '<AssemblyVersion>.*</AssemblyVersion>') {
            $content = $content -replace '<AssemblyVersion>.*</AssemblyVersion>', $assemblyVersionTag
        }
        if ($content -match '<FileVersion>.*</FileVersion>') {
            $content = $content -replace '<FileVersion>.*</FileVersion>', $fileVersionTag
        }
        
        $content | Set-Content $filePath
        Write-Host "UPDATED $filePath"
    } else {
        Write-Host "WARNING: $filePath not found."
    }
}

# ------------------------------- 
# Update Projects
# ------------------------------- 

Update-CsprojVersion "TcNo-Acc-Switcher-Client\TcNo-Acc-Switcher-Client.csproj" $nsisVersion
Update-CsprojVersion "TcNo-Acc-Switcher-Server\TcNo-Acc-Switcher-Server.csproj" $nsisVersion
Update-CsprojVersion "TcNo-Acc-Switcher-Installer\TcNo-Acc-Switcher-Installer.csproj" $nsisVersion
Update-CsprojVersion "TcNo-Acc-Switcher-Wrapper\TcNo-Acc-Switcher-Wrapper.csproj" $nsisVersion
# Uncomment if these projects get Version tags added
# Update-CsprojVersion "TcNo-Acc-Switcher-Tray\TcNo-Acc-Switcher-Tray.csproj" $nsisVersion
# Update-CsprojVersion "TcNo-Acc-Switcher-Updater\TcNo-Acc-Switcher-Updater.csproj" $nsisVersion

# ------------------------------- 
# Update the Globals.cs file
# ------------------------------- 

$globalsFilePath = "TcNo-Acc-Switcher-Globals\Globals.cs"
if (Test-Path $globalsFilePath) {
    $globalsFileContent = Get-Content $globalsFilePath
    $globalsVersionReplacement = 'public static readonly string Version = "' + $dateVersion + '";'
    $globalsFileContent = $globalsFileContent -replace 'public static readonly string Version\s*=\s*".*";', $globalsVersionReplacement
    $globalsFileContent | Set-Content $globalsFilePath
    Write-Host "UPDATED Globals.cs"
}

<#
.SYNOPSIS
    Builds the System Atlas Bootable WinPE ISO.
.DESCRIPTION
    Creates a customized Windows Preinstallation Environment (WinPE) image.
    Injects the System Atlas C# binaries and configures startnet.cmd to
    run the boot-and-collect workflow automatically.
#>

param (
    [string]$AdkWinPEPath = "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Windows Preinstallation Environment\amd64\en-us\winpe.wim",
    [string]$WorkingDir = "C:\WinPE_amd64",
    [string]$SystemAtlasBinaries = ".\publish",
    [string]$IsoOut = "SystemAtlas-Boot.iso"
)

# 1. Setup Working Directory
if (Test-Path $WorkingDir) { Remove-Item $WorkingDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkingDir | Out-Null
New-Item -ItemType Directory -Path "$WorkingDir\mount" | Out-Null
New-Item -ItemType Directory -Path "$WorkingDir\media" | Out-Null
Copy-Item $AdkWinPEPath "$WorkingDir\media\boot.wim" -Force

# 2. Mount WIM
Write-Host "Mounting WinPE image..."
DISM /Mount-Image /ImageFile:"$WorkingDir\media\boot.wim" /index:1 /MountDir:"$WorkingDir\mount"

# 3. Inject System Atlas Binaries
Write-Host "Injecting System Atlas application..."
$TargetAppDir = "$WorkingDir\mount\SystemAtlas"
New-Item -ItemType Directory -Path $TargetAppDir | Out-Null
Copy-Item "$SystemAtlasBinaries\*" $TargetAppDir -Recurse -Force

# 4. Modify startnet.cmd for Auto-Launch
Write-Host "Configuring Boot & Collect workflow..."
$StartnetPath = "$WorkingDir\mount\Windows\System32\startnet.cmd"
$StartnetContent = @"
wpeinit
echo Starting System Atlas Boot & Collect Workflow...
X:\SystemAtlas\SystemAtlas.exe --boot-and-collect
"@
Set-Content -Path $StartnetPath -Value $StartnetContent

# 5. Unmount and Commit
Write-Host "Unmounting and saving image..."
DISM /Unmount-Image /MountDir:"$WorkingDir\mount" /Commit

# 6. Create ISO (Requires ADK MakeWinPEMedia or similar, simplified here)
Write-Host "Please use MakeWinPEMedia /ISO $WorkingDir $IsoOut to create the final bootable media."
Write-Host "System Atlas WinPE build staging complete."

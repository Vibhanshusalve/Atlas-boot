# System Atlas - Boot & Collect Edition

System Atlas is a Windows-first system inventory and documentation tool featuring a dedicated, bootable USB recovery environment.

## Overview
This implementation strictly adheres to a **read-only, forensic-safe Boot & Collect workflow**. It operates completely offline, inspecting the target Windows installation without booting it, thereby avoiding modification to the target disk's state or execution of potentially compromised software.

## Key Features
- **Boot & Collect Workflow**: Boots from a customized WinPE environment. Automatically identifies the target Windows installation and the USB results partition (via volume label `ATLAS_RESULTS`).
- **Filesystem Inventory**: Safely enumerates the filesystem, preventing reparse-point loops, and generates a structured CSV inventory of metadata (contents are not hashed or read by default).
- **Chrome Profile Audit**: Identifies Chrome profiles and checks for the presence of recovery-critical files (e.g., `Local State`, `Login Data`) without attempting to extract plain-text passwords or bypass security.
- **Offline Registry Identification**: Locates and catalogs core offline hives.
- **Reporting & Packaging**: Generates JSON, CSV, and Markdown reports. (Architecture assumes final packaging into an AES-encrypted ZIP file).

## Security and Privacy
- **Read-Only**: The tool mounts the target Windows drive in a read-only context (enforced by offline inspection). No temporary data is written to the target drive.
- **Encrypted Output**: Final results are packaged. The default zip encryption password is: `infected`
- **No Cloud Telemetry**: All data remains on the USB drive.

## Build Instructions
1. Install the Windows ADK and the WinPE Add-on.
2. Compile the C# binaries using .NET 8:
   `dotnet publish -c Release -r win-x64 --self-contained`
3. Use the provided PowerShell script (`Build-SystemAtlasUSB.ps1`) to inject the compiled binaries into a `boot.wim` file and configure `startnet.cmd` to auto-launch the application.
4. Create the bootable ISO/USB using `MakeWinPEMedia`.

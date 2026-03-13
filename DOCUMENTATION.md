# TcNo Account Switcher - Developer Documentation

## Overview

The **TcNo Account Switcher** is a super-fast, open-source account switcher for various gaming platforms (Steam, Epic, Battle.net, etc.). It allows users to swap accounts by manipulating file systems and registry keys without needing to re-enter credentials or handle 2FA tokens.

This project uses a hybrid architecture involving a **WPF Client** (hosting a web UI via WebView2/CefSharp), an **ASP.NET Core Server** (handling backend logic), and **Native AOT** helper tools for a seamless zero-dependency installation experience.

## Project Structure

| Project | Type | Description |
| :--- | :--- | :--- |
| **TcNo-Acc-Switcher-Client** | WPF (.NET 10) | The main application window. Hosts the UI using WebView2 and orchestrates the application lifecycle. |
| **TcNo-Acc-Switcher-Server** | ASP.NET Core (.NET 10) | The backend logic, likely serving a Blazor or static web UI to the Client. Handles platform integrations (SteamKit2, Gameloop.Vdf, etc.). |
| **TcNo-Acc-Switcher-Tray** | WinForms (.NET 10) | The system tray application for quick switching and background management. |
| **TcNo-Acc-Switcher-Updater** | WPF (.NET 10) | A standalone updater tool that handles delta updates (VCDiff) and file extraction (7-Zip). |
| **TcNo-Acc-Switcher-Wrapper** | Console (Native AOT) | The **entry point** (`TcNo-Acc-Switcher.exe`). A lightweight native executable that checks for required runtimes (WebView2, .NET 10) before launching the main app. |
| **TcNo-Acc-Switcher-Installer**| Console (Native AOT) | The **installer/bootstrapper** (`_First_Run_Installer.exe`). Downloads and installs missing runtimes if the Wrapper detects they are missing. |
| **TcNo-Acc-Switcher-Globals** | Library (.NET 10) | Shared logic, constants, and utilities used across all managed projects. |

## Prerequisites

To build and contribute to this project, you need:

*   **Visual Studio 2022** (Latest Preview recommended for .NET 10 support).
*   **.NET 10.0 SDK**.
*   **C++ Desktop Development Workload** (Required for Native AOT compilation of Wrapper and Installer).
*   **Git**.

## Building the Project

1.  **Clone the Repository**:
    ```powershell
    git clone https://github.com/TCNOco/TcNo-Acc-Switcher.git
    cd TcNo-Acc-Switcher
    ```

2.  **Open in Visual Studio**:
    Open `TcNo-Acc-Switcher.slnx` (or `.sln`).

3.  **Restore Dependencies**:
    Visual Studio should automatically restore NuGet packages. If not, run:
    ```powershell
    dotnet restore
    ```

4.  **Build**:
    Build the solution in **Debug** or **Release** mode.
    *   **Note**: The `TcNo-Acc-Switcher-Client` project has a `PostBuild` event that executes `TcNo-Acc-Switcher-Client/PostBuild.bat`. This script is crucial for packaging the application correctly.

### Post-Build Process (`PostBuild.bat`)

The `PostBuild.bat` script performs several critical tasks during a **Release** build:
1.  **Renames Executables**:
    *   `TcNo-Acc-Switcher.exe` -> `TcNo-Acc-Switcher_main.exe`
    *   (and similarly for Server, Tray, and Updater).
2.  **Wraps the App**:
    *   Copies the Native AOT `Wrapper.exe` to `TcNo-Acc-Switcher.exe`. This ensures that when a user runs the app, they first hit the runtime check.
3.  **Packages the Installer**:
    *   Copies the Native AOT `Installer.exe` to `_First_Run_Installer.exe` and places it in the `updater/` folder.
4.  **CEF Optimization**:
    *   Moves large CEF (Chromium Embedded Framework) DLLs to a separate folder to optimize download sizes.

## Running the Application

### Debug Mode
In Debug mode, the `PostBuild.bat` script is less aggressive. It simply copies the helper tools (`_First_Run_Installer.exe`, `runas.exe`) to the output directory.
*   Set `TcNo-Acc-Switcher-Client` as the startup project.
*   Press **F5** to run.

### Release Mode / Deployment
To create a distributable build:
1.  Select **Release** configuration.
2.  Build the Solution.
3.  The output will be in `TcNo-Acc-Switcher-Client/bin/x64/Release/TcNo-Acc-Switcher/`.
4.  The `PostBuild.bat` script will have organized this folder into a deployable state, with `TcNo-Acc-Switcher.exe` being the Native AOT wrapper.

## Architecture Notes

### Runtime Dependency Handling
The project is designed to run on machines that **do not** have .NET installed.
1.  User runs `TcNo-Acc-Switcher.exe` (Native AOT Wrapper).
2.  Wrapper checks registry for WebView2 and .NET 10 Runtimes.
3.  **If Missing**: It launches `_First_Run_Installer.exe` (Native AOT Installer).
    *   Installer downloads/installs dependencies via `winget` or direct download.
4.  **If Present**: It launches `TcNo-Acc-Switcher_main.exe` (The actual .NET 10 WPF App).

### Native AOT Migration
The Wrapper and Installer were previously C++ projects. They have been migrated to C# Native AOT to allow for easier maintenance while keeping the "zero-dependency" characteristic. They are configured with `<PublishAot>true</PublishAot>` and `<OptimizationPreference>Size</OptimizationPreference>`.

## Contributing

*   **UI Changes**: Most UI logic resides in `TcNo-Acc-Switcher-Server` (Blazor/Web) and `TcNo-Acc-Switcher-Client` (Container).
*   **Platform Logic**: See `TcNo-Acc-Switcher-Server/Platforms/`.
*   **Updater**: Changes to the update logic should be made in `TcNo-Acc-Switcher-Updater`.

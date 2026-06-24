# Network Widget for Windows

A small Windows desktop widget for quickly enabling or disabling Wi-Fi and Ethernet adapters.

The widget is designed to stay out of the way: it can float on the desktop, resize from the corner, minimize to the notification area, and start automatically with Windows.

## Why This Exists

This utility was born from a simple real-world problem: on some setups, Wi-Fi can be faster or more convenient than Ethernet for specific tasks, while Ethernet may still be useful for stability or other workflows.

Network Widget makes switching between Wi-Fi and Ethernet quick. Instead of opening Windows Settings, going into Network & Internet, finding the adapter, and enabling or disabling it manually, you can do it from a compact desktop widget or from the tray.

One example use case: keep Ethernet disabled when Wi-Fi gives better download performance, then switch adapters quickly when another connection mode is needed.

## Features

- Toggle Wi-Fi and Ethernet adapters from a compact desktop widget
- Green active state and red disabled state
- Resizable, borderless Windows-style UI
- Notification area icon with `Apri` and `Esci`
- Optional elevated scheduled tasks to avoid repeated UAC prompts
- Starts automatically with Windows
- DPI-aware rendering for high-resolution displays
- No external icon assets: network icons are drawn in code

## Requirements

- Windows 10 or Windows 11
- .NET Framework 4.x runtime
- Administrator permission once if you want no UAC prompt on every adapter toggle

## Install

Build the installer from source:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Then run:

```text
dist/Installa Network Widget.exe
```

For the smoothest experience, run the installer as administrator once. This lets it register the elevated scheduled tasks used for adapter changes.

A prebuilt installer can be attached later as a GitHub Release asset.

## Usage

- Click the Wi-Fi or Ethernet switch to enable or disable that adapter.
- Click `_` to hide the widget in the notification area.
- Double-click the tray icon to show the widget again.
- Right-click the tray icon for `Apri` or `Esci`.
- Drag the widget body to move it.
- Drag the bottom-right grip to resize it.
- Click the theme button to cycle colors.

## UAC Notes

Windows requires elevation to enable or disable network adapters.

Network Widget can avoid asking for UAC on every click by registering four elevated scheduled tasks:

- `\NetworkWidget\WiFiEnable`
- `\NetworkWidget\WiFiDisable`
- `\NetworkWidget\EthernetEnable`
- `\NetworkWidget\EthernetDisable`

If those tasks are not registered, the app falls back to the normal UAC prompt.

## Build

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The installer is produced at:

```text
dist/Installa Network Widget.exe
```

The build script uses the C# compiler included with .NET Framework:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

## License

MIT License. See [LICENSE](LICENSE).

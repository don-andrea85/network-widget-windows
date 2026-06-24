# Release Checklist

1. Build with `scripts/build.ps1`.
2. Run `dist/Installa Network Widget.exe` on a clean Windows machine.
3. Confirm Desktop, Start Menu, Startup shortcut creation.
4. Run installer as administrator once and confirm scheduled tasks are created.
5. Confirm Wi-Fi and Ethernet toggles work.
6. Confirm tray icon open/exit behavior.
7. Confirm resize and high-DPI rendering.
8. Create a GitHub release and upload `dist/Installa Network Widget.exe`.

Suggested first tag:

```text
v0.1.0
```

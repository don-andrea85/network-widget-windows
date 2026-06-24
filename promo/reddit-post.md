# Reddit Draft

## Title

I built a tiny Windows widget to switch Wi-Fi and Ethernet without opening Settings

## Body

I made a small open source Windows utility called Network Widget.

It sits on the desktop and lets you enable or disable Wi-Fi and Ethernet with two switches. The reason was very practical: on my setup, Wi-Fi can be better for some downloads, while Ethernet is still useful for stability or other tasks. I wanted a quick way to switch adapters without going through Windows Settings every time.

What it does:

- toggles Wi-Fi and Ethernet from a compact desktop widget
- shows green for active and red for disabled
- can be moved, resized, minimized to the notification area, and started with Windows
- includes an installer and source code

Windows requires admin permission to change network adapters, so the app can register scheduled tasks to avoid repeated UAC prompts after the first setup.

GitHub:
https://github.com/don-andrea85/network-widget-windows

Release download:
https://github.com/don-andrea85/network-widget-windows/releases/latest

I am mainly sharing it in case someone else has a similar Wi-Fi/Ethernet switching workflow. Feedback is welcome.

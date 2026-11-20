# Codex Knob

Turn a keyboard knob to adjust reasoning effort in Codex on Windows. Input is remapped only while the Microsoft Store version of Codex is in the foreground.

[한국어](README.md) · English

## Get started

1. Download the Windows ZIP from the [latest release](../../releases/latest), extract it, and run `CodexKnob.exe`. If an older version is running, choose **Quit** from its tray menu first.
2. If your knob sends something other than volume up/down, click **Detect** for each direction and turn the knob one notch within eight seconds. The saved signal appears beside the button.
3. Select an **Output key** for each direction. The defaults are **F23** for Increase and **F24** for Decrease. Pick unused keys if a Stream Deck or another device already uses function keys. If a detected signal matches an output key, change the output first and detect again.
4. In Codex, open **Settings → Keyboard shortcuts** and search for **reasoning**. Start recording each shortcut, turn the knob one notch in the corresponding direction, and save. Try both directions in the chat composer.

Use **Settings** to install the app and optionally start it in the tray at Windows sign-in. If you enabled startup in v1.1.0, use **Settings → Update** in the new version.

- Requires Windows x64 and .NET Framework 4.x. The interface supports Korean, English, and Japanese.
- Output keys range from F1 through F24. Pick different output keys and keep them distinct from the learned input signals.
- Supported inputs include volume and media keys, function and navigation keys, and the vertical wheel. Devices that send the same signal are treated alike. Mapping the wheel also changes ordinary mouse scrolling in Codex.

## Build from source

Run `./build.ps1` in Windows PowerShell. Run self-tests with `Start-Process ./CodexKnob.exe -ArgumentList '--self-test' -Wait -PassThru`. Exit code `0` indicates success.

[MIT License](LICENSE) · Independent project

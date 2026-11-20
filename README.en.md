# Codex Knob

**Turn your keyboard knob to adjust reasoning effort in Codex.**

[한국어](README.md) · English

Codex Knob translates knob input into Codex keyboard shortcuts while Codex is the active window. In other apps, your knob keeps its original behavior, such as changing the volume.

Local only · KO / EN / JP · MIT License

## Before you start

- Requires Windows x64 and .NET Framework 4.x.
- Currently supports the Windows Codex app installed from the Microsoft Store.
- Your knob must send Windows volume, media, function, navigation, or vertical wheel events. Proprietary vendor HID reports are not read directly.

## 1. Run and install

1. Download the distribution ZIP and **extract it**.
2. Open `release/CodexKnob.exe`.
3. In the app, choose **Settings → Install**.
4. Make sure **Enable knob** is checked on the main screen.

Installation copies the executable to your per-user Programs folder. Administrator access is not required. You can also run the app directly from the extracted folder without installing it.

Choose **KO · EN · JP** in the top-right corner to change the app language. Your choice is saved for the next launch.

## 2. Detect your knob signals

The defaults are volume up and volume down. Register both directions if your keyboard sends different events.

1. Bring the **Codex Knob window to the foreground**.
2. Click **Detect** beside **Increase**, then turn the knob one step in that direction within eight seconds.
3. Check that the displayed input changes and the app confirms registration.
4. Click **Detect** beside **Decrease** and turn the knob in the opposite direction.

Inputs are saved automatically. The same input cannot be assigned to both directions. Press `Esc` or **Cancel** to stop detection. Detection also ends when you leave the app window or the timeout expires.

| Allowed inputs | Blocked inputs |
| --- | --- |
| Volume and media keys, F1–F22, arrow and navigation keys, vertical wheel | Mouse buttons, letters and numbers, Enter, Esc, Tab, Backspace, Delete, modifier combinations, F23 and F24 |

**Choose wheel inputs carefully.** The app does not distinguish devices. Mapping the vertical wheel also converts normal mouse scrolling in Codex. Other keyboards sending the same input are affected too.

To restore the defaults, choose **Settings → Restore volume inputs → Reset**. Your F23/F24 assignments in Codex can stay in place.

## 3. Connect the Codex shortcuts

Codex Knob sends `F23` to increase reasoning effort and `F24` to decrease it. Assign these shortcuts in Codex once.

1. In Codex, open **Settings → Keyboard shortcuts**.
2. Search for **reasoning**.
3. Click the edit icon beside **Increase reasoning effort** to start recording a shortcut.
4. With Codex Knob running and enabled, turn the knob one step in the increase direction, then save.
5. Repeat for **Decrease reasoning effort**, turning the knob in the opposite direction.
6. Check that the saved assignments match this table.

| Codex command | Shortcut | Default knob input |
| --- | --- | --- |
| Increase reasoning effort | `F23` | Volume up |
| Decrease reasoning effort | `F24` | Volume down |

You do not need physical F23 or F24 keys: the knob can record them for you. If your knob does not send volume events, complete **Detect your knob signals** above first.

Close Settings, click a chat composer, and turn the knob in both directions. Setup is complete when the selected reasoning effort changes within the levels supported by your model. At the lowest or highest level, another turn may have no effect.

## 4. Start automatically at Windows sign-in

Check **Settings → Start at sign-in**. If necessary, the app installs itself as part of this step.

At the next Windows sign-in, it starts in the system tray without opening a window, with **Enable knob turned on**. Uncheck the same setting to disable automatic startup.

| Action | What happens |
| --- | --- |
| Tray button or window close button | Hides the window; knob conversion keeps running |
| Double-click the tray icon | Opens the app window |
| Uncheck Enable knob | Pauses conversion for this session |
| Quit | Stops the app and conversion |
| Uncheck Start at sign-in | Prevents automatic launch at the next sign-in |

Quitting or pausing does not disable automatic startup.

## Troubleshooting

**The knob only changes volume.** Make sure Codex is the active window and Enable knob is checked. Holding Ctrl, Alt, Shift, or Windows bypasses conversion. Run input detection if your knob sends a different signal.

**Detected or sent counts increase, but reasoning effort stays the same.** Check that Codex actually saved `F23` for increase and `F24` for decrease. Record and save them again in Codex, then test in a chat composer. A sent count alone does not confirm that Codex executed the command.

**Nothing is detected.** Keep the app window active during detection. Your knob may send an unsupported proprietary signal. Check the keyboard or vendor software input settings as well.

**Can I use it on another PC?** Install or run Codex Knob there and assign the Codex shortcuts on that PC. Settings are not written to keyboard firmware.

## Local storage and privacy

The app uses no network connection or account login and does not save typed text. Detection counts stay in memory for the current session only.

- Installed executable: `%LOCALAPPDATA%\Programs\CodexKnob\1.1.0\CodexKnob.exe`
- Input and language settings: `%LOCALAPPDATA%\CodexKnob\settings.json`, `language.txt`
- Automatic startup: a Windows `Run` registry entry for the current user
- Previous settings: backed up under `Codex-Change-Backups\CodexKnob` in Documents

## Build from source

Run the following in Windows PowerShell from the repository root. No external packages are downloaded.

```powershell
./build.ps1 -OutputPath ./CodexKnob-new.exe
$test = Start-Process ./CodexKnob-new.exe -ArgumentList '--self-test' -Wait -PassThru
if ($test.ExitCode -ne 0) { throw 'Self-test failed' }
```

Use a new output path: the build script will not overwrite an existing file. Self-tests check input policy and state handling; they do not replace physical keyboard, UI, or reboot testing.

## License

[MIT License](LICENSE). Include the license when modifying or redistributing the software. Codex Knob is an independent project and is not an official OpenAI product.

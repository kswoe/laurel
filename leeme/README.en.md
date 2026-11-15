# Laurel

*Achievements earned, never handed out.*

**Other languages:** [Español](../LEEME.md) · [Português](README.pt-BR.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

Prepares your games so that **Hydra Launcher records achievements when you earn them while playing**.
The app never unlocks achievements by itself.

## How to use it
1. Open `App\Laurel.exe` and **choose your language**. There are 8: Spanish, English, Portuguese, French, German, Russian, Chinese and Japanese. If you tick "Don't ask again", you can change it later with the **Language** button.
2. The first time, the app looks for Hydra's download folder by itself (it only reads Hydra's data, it never changes it). If your games are somewhere else, click **Add…**.
3. Enter your **Steam API key**. It's free and you can get it with the "Get a key" link. Without it, the app can scan your games but not prepare them. **The key is never saved:** it lives only in memory, so you'll need to enter it every time you open the app.
4. Click **Prepare fixable games**.
5. Leave it open or minimize it (it stays next to the clock). When you install a game, it waits for the installation to finish and prepares it automatically.

**Tip:** the colored cards above the list count the games in each status; click one to show only those games. The search box filters by name, emulator or AppID.

## What each status means
| Status | Meaning |
|---|---|
| OK | Hydra already finds the achievements file for that game |
| READY | Set up; the file will appear with your first achievement |
| STEAM | `steam_api.dll` is Valve's original (signature verified): Hydra reads the achievements from your Steam account |
| FIXABLE | Uses Goldberg but the achievement list is missing; the app can create it |
| NO APPID | Right-click the game → "Set AppID" |
| UNSUPPORTED | The DLL isn't from Valve and contains no known emulator signatures: its emulator doesn't save achievements where Hydra looks for them |
| NO STEAM | Doesn't use Steam. The app shows which platform it uses: GOG (with or without achievements), Epic, Ubisoft, Microsoft Store or none |

## How it protects your files
- It **never overwrites** an existing file: it only creates the missing ones.
- It **never deletes** anything on its own.
- It records every file it creates in `cambios.json`.
- **Undo app changes** moves to the Recycle Bin only the files the app created that nobody has touched since. If you or the game modified them, they are kept.
- **Simulation mode** shows what it would do without writing anything.

## DLL identifier
When a game has `steam_api.dll` but no known configuration file, the app examines the DLL **read-only**:
1. It checks the digital signature with Windows, without going online. If the signature is Valve's and valid, the game is a Steam copy.
2. It searches the file for signatures of known emulators: Goldberg, CODEX, RUNE, EMPRESS, TENOKE, OnlineFix, CreamAPI, SmartSteamEmu, 3DM, ALI213, SKIDROW, RLD! and Razor1911.

The result appears in "View details" (right-click the game).

## Achievement bridges
Some games do record achievements, but somewhere Hydra doesn't read. With **Achievement bridges** enabled, the app copies those **already earned** achievements to the format Hydra understands (`%AppData%\GSE Saves\<AppID>\achievements.json`). It never invents achievements.
- **GOG Galaxy → Hydra:** for GOG games with achievements. Set their Steam AppID (right-click → "Set AppID"). The app reads a *temporary copy* of GOG Galaxy's database, never the original. If you entered your API key, it only copies achievements whose names match Steam's.
- **RLE → Hydra:** Hydra knows RLE's folder (`%AppData%\RLE`) but doesn't scan it; the bridge covers it.
- If the destination file already exists and wasn't created by the app (for example, because the game uses Goldberg), it is **left untouched**.
- Anything a bridge creates is recorded in `cambios.json` and can be undone.

## Guide for UNSUPPORTED games
The app **doesn't install emulators**. For these games, right-click → **"Guide to track its achievements…"** opens a guide that walks you through it:
1. **Back up** the current DLL (as `.hydralogros-original`, overwriting nothing).
2. Link to gbe_fork's official repository.
3. Opens the DLL folder so that **you** replace it.
4. **Check:** if gbe_fork is detected, the game is prepared automatically.

Only offered for copies whose DLL is no longer Valve's original.

## Privacy and Steam API usage
- **What is saved to disk:** only the libraries, the options (including the language), the AppIDs you set and the list of created files (so they can be undone). Never the API key. If an old `config.json` contained it, the app removes it at startup.
- **The log contains no personal data:** in `registro.txt` and on screen, your user folder appears as `%USERPROFILE%`, your user name as `<user>`, and e-mails and the key are hidden.
- **The app queries Steam carefully** so your key's usage looks normal:
  - one request at a time, at least 3 seconds apart;
  - at most 150 requests per session;
  - each game is queried only once per session;
  - if Steam rejects the key, it isn't used again until you enter another one;
  - if Steam asks to slow down, requests pause for 15 minutes.
- The achievement list is downloaded from Steam in the language you chose.

## Security and scanning
**What it scans:** only the subfolders of your libraries, looking only for the files that identify emulators and platforms. It skips audio, video, asset and installer folders, and hides folders with no game files. It checks several games at once, with a size and time limit per game.

**Safeguards:**
- **Libraries:** a drive root, Windows, Program Files, ProgramData, Users, your profile, AppData or the temp folder can't be used as a library. Neither can symbolic links. If `config.json` contains them, they're ignored.
- **Links:** it never follows symbolic links or junctions, and never reads OneDrive cloud-only files (it doesn't download them).
- **Writing:** it can only create `steam_settings\achievements.json`, `steam_settings\steam_appid.txt`, the `.hydralogros-original` backup and `GSE Saves\<AppID>\achievements.json`. Any other path is blocked.
- **Undo:** even if someone edits `cambios.json`, it only acts on those same files.
- **Network:** it only connects to `api.steampowered.com` over HTTPS and doesn't follow redirects, so your key can't end up on another server. It only opens two trusted websites.
- **Nothing is executed:** it never launches programs from game folders. Folders are opened with File Explorer and Windows DLLs are only loaded from `System32`.
- **Input data:** AppIDs can only be numbers, text files over 1 MB are ignored and text searches have a time limit.
- **Log:** `registro.txt` rotates at 2 MB.
- **Warning** if you run the app as administrator (not needed).

## Files in the folder
- `App\Laurel.exe`: the application. Requires the .NET 10 Desktop Runtime.
- `App\config.json`, `cambios.json`, `estado.json` and `registro.txt`: created by the app as you use it.
- `leeme\`: this document in other languages.
- `src\`: source code. To rebuild: `dotnet publish src -c Release -o App`.
- `tools\IconGen\`: generates the icon (`src\app.ico`). To regenerate it: `dotnet run --project tools/IconGen -- src/app.ico`.
- `Portable\Laurel.exe`: portable version. It carries .NET inside (nothing to install) and keeps its data in its own folder. To rebuild it: `dotnet publish src -c Release -p:Portable=true -o Portable`.
- The app's **Read me** button shows this document in your language. It uses a copy built into the `.exe`: editing these `.md` files doesn't change what the app shows (you need to rebuild).

## License
Laurel is free software: you can use, study, modify and share it under the terms of the [GNU General Public License v3.0](../LICENSE) (GPL-3.0). If you distribute a modified version, it must also be free and come with its source code.

*** THIS TOOL IS FREE TO USE ***

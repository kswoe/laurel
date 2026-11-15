# Laurel

*Erfolge verdient, nie geschenkt.*

**Weitere Sprachen:** [Español](../LEEME.md) · [English](README.en.md) · [Português](README.pt-BR.md) · [Français](README.fr.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

Bereitet deine Spiele so vor, dass **Hydra Launcher Erfolge erfasst, sobald du sie beim Spielen erhältst**.
Die App schaltet nie selbst Erfolge frei.

## So verwendest du sie
1. Öffne `App\Laurel.exe` und **wähle deine Sprache**. Es gibt 8: Spanisch, Englisch, Portugiesisch, Französisch, Deutsch, Russisch, Chinesisch und Japanisch. Wenn du „Beim Start nicht mehr fragen“ ankreuzt, kannst du sie später über die Schaltfläche **Sprache** ändern.
2. Beim ersten Start sucht die App Hydras Download-Ordner selbst (sie liest Hydras Daten nur, ohne sie zu ändern). Liegen deine Spiele woanders, klicke auf **Hinzufügen…**.
3. Gib deinen **Steam-API-Schlüssel** ein. Er ist kostenlos und über den Link „Schlüssel holen“ erhältlich. Ohne ihn kann die App deine Spiele prüfen, aber nicht vorbereiten. **Der Schlüssel wird nie gespeichert:** Er existiert nur im Arbeitsspeicher, daher musst du ihn bei jedem Start eingeben.
4. Klicke auf **Reparierbare Spiele vorbereiten**.
5. Lass die App geöffnet oder minimiere sie (sie bleibt neben der Uhr). Wenn du ein Spiel installierst, wartet sie das Ende der Installation ab und bereitet es automatisch vor.

**Tipp:** Die farbigen Karten über der Liste zählen die Spiele je Status; klicke auf eine, um nur diese Spiele anzuzeigen. Die Suche filtert nach Name, Emulator oder AppID.

## Bedeutung der Status
| Status | Bedeutung |
|---|---|
| OK | Hydra findet die Erfolgsdatei dieses Spiels bereits |
| BEREIT | Eingerichtet; die Datei erscheint mit deinem ersten Erfolg |
| STEAM | `steam_api.dll` ist das Original von Valve (Signatur geprüft): Hydra liest die Erfolge aus deinem Steam-Konto |
| REPARIERBAR | Nutzt Goldberg, aber die Erfolgsliste fehlt; die App kann sie erstellen |
| KEINE APPID | Rechtsklick auf das Spiel → „AppID festlegen“ |
| NICHT UNTERSTÜTZT | Die DLL stammt nicht von Valve und enthält keine bekannten Emulator-Signaturen: Ihr Emulator speichert Erfolge nicht dort, wo Hydra sucht |
| KEIN STEAM | Nutzt kein Steam. Die App zeigt, welche Plattform verwendet wird: GOG (mit oder ohne Erfolge), Epic, Ubisoft, Microsoft Store oder keine |

## So schützt sie deine Dateien
- Sie **überschreibt nie** eine vorhandene Datei: Sie erstellt nur fehlende.
- Sie **löscht nie** etwas von sich aus.
- Sie protokolliert jede erstellte Datei in `cambios.json`.
- **Änderungen rückgängig machen** verschiebt nur die von der App erstellten Dateien in den Papierkorb, die seitdem niemand verändert hat. Hast du oder das Spiel sie geändert, bleiben sie erhalten.
- Der **Simulationsmodus** zeigt, was die App tun würde, ohne etwas zu schreiben.

## DLL-Erkennung
Hat ein Spiel eine `steam_api.dll`, aber keine bekannte Konfigurationsdatei, untersucht die App die DLL **nur lesend**:
1. Sie prüft die digitale Signatur mit Windows, ohne Internetverbindung. Ist die Signatur von Valve und gültig, ist das Spiel eine Steam-Kopie.
2. Sie durchsucht die Datei nach Signaturen bekannter Emulatoren: Goldberg, CODEX, RUNE, EMPRESS, TENOKE, OnlineFix, CreamAPI, SmartSteamEmu, 3DM, ALI213, SKIDROW, RLD! und Razor1911.

Das Ergebnis erscheint unter „Details anzeigen“ (Rechtsklick auf das Spiel).

## Erfolgsbrücken
Manche Spiele erfassen Erfolge, aber an einem Ort, den Hydra nicht liest. Mit aktivierten **Erfolgsbrücken** kopiert die App diese **bereits erhaltenen** Erfolge in das Format, das Hydra versteht (`%AppData%\GSE Saves\<AppID>\achievements.json`). Sie erfindet nie Erfolge.
- **GOG Galaxy → Hydra:** für GOG-Spiele mit Erfolgen. Lege ihre Steam-AppID fest (Rechtsklick → „AppID festlegen“). Die App liest eine *temporäre Kopie* der GOG-Galaxy-Datenbank, nie das Original. Wenn du deinen API-Schlüssel eingegeben hast, kopiert sie nur Erfolge, deren Namen mit Steam übereinstimmen.
- **RLE → Hydra:** Hydra kennt den RLE-Ordner (`%AppData%\RLE`), prüft ihn aber nicht; die Brücke übernimmt das.
- Existiert die Zieldatei bereits und wurde nicht von der App erstellt (z. B. weil das Spiel Goldberg nutzt), **bleibt sie unverändert**.
- Alles, was eine Brücke erstellt, wird in `cambios.json` protokolliert und kann rückgängig gemacht werden.

## Anleitung für NICHT UNTERSTÜTZTE Spiele
Die App **installiert keine Emulatoren**. Für diese Spiele öffnet Rechtsklick → **„Anleitung zum Verfolgen der Erfolge…“** eine Schritt-für-Schritt-Anleitung:
1. **Sicherung** der aktuellen DLL (als `.hydralogros-original`, ohne etwas zu überschreiben).
2. Link zum offiziellen gbe_fork-Repository.
3. Öffnet den DLL-Ordner, damit **du** sie ersetzt.
4. **Prüfen:** Wird gbe_fork erkannt, wird das Spiel automatisch vorbereitet.

Nur für Kopien, deren DLL nicht mehr das Original von Valve ist.

## Datenschutz und Nutzung der Steam-API
- **Was gespeichert wird:** nur die Bibliotheken, die Optionen (einschließlich Sprache), die von dir festgelegten AppIDs und die Liste der erstellten Dateien (zum Rückgängigmachen). Niemals der API-Schlüssel. Enthielt eine alte `config.json` ihn, entfernt die App ihn beim Start.
- **Das Protokoll enthält keine persönlichen Daten:** In `registro.txt` und auf dem Bildschirm erscheint dein Benutzerordner als `%USERPROFILE%`, dein Benutzername als `<Benutzer>`, und E-Mail-Adressen sowie der Schlüssel werden ausgeblendet.
- **Die App fragt Steam vorsichtig ab**, damit die Nutzung deines Schlüssels normal aussieht:
  - immer nur eine Anfrage, mit mindestens 3 Sekunden Abstand;
  - höchstens 150 Anfragen pro Sitzung;
  - jedes Spiel wird pro Sitzung nur einmal abgefragt;
  - lehnt Steam den Schlüssel ab, wird er erst wieder verwendet, wenn du einen anderen eingibst;
  - bittet Steam um weniger Anfragen, pausieren die Anfragen 15 Minuten.
- Die Erfolgsliste wird von Steam in der gewählten Sprache heruntergeladen.

## Sicherheit und Prüfung
**Was geprüft wird:** nur die Unterordner deiner Bibliotheken, und dort nur die Dateien, die Emulatoren und Plattformen erkennen lassen. Audio-, Video-, Ressourcen- und Installer-Ordner werden übersprungen, Ordner ohne Spieldateien ausgeblendet. Mehrere Spiele werden gleichzeitig geprüft, mit Größen- und Zeitlimit pro Spiel.

**Sperren:**
- **Bibliotheken:** Laufwerksstamm, Windows, Programme, ProgramData, Benutzer, dein Profil, AppData und der Temp-Ordner sind als Bibliothek nicht erlaubt, ebenso symbolische Verknüpfungen. Stehen sie in `config.json`, werden sie ignoriert.
- **Verknüpfungen:** Symbolischen Verknüpfungen und *Junctions* wird nie gefolgt; reine OneDrive-Cloud-Dateien werden nicht gelesen (also nicht heruntergeladen).
- **Schreiben:** Erstellt werden dürfen nur `steam_settings\achievements.json`, `steam_settings\steam_appid.txt`, die Sicherung `.hydralogros-original` und `GSE Saves\<AppID>\achievements.json`. Jeder andere Pfad wird blockiert.
- **Rückgängig:** Auch wenn jemand `cambios.json` bearbeitet, betrifft es nur genau diese Dateien.
- **Netzwerk:** Verbindung nur zu `api.steampowered.com` per HTTPS, ohne Weiterleitungen; dein Schlüssel kann also nicht auf einem anderen Server landen. Es werden nur zwei vertrauenswürdige Websites geöffnet.
- **Nichts wird ausgeführt:** Programme aus Spielordnern werden nie gestartet. Ordner öffnen sich im Explorer, und Windows-DLLs werden nur aus `System32` geladen.
- **Eingabedaten:** AppIDs dürfen nur Zahlen sein, Textdateien über 1 MB werden ignoriert und Textsuchen haben ein Zeitlimit.
- **Protokoll:** `registro.txt` wird bei 2 MB rotiert.
- **Warnung**, wenn du die App als Administrator startest (nicht nötig).

## Dateien im Ordner
- `App\Laurel.exe`: die Anwendung. Benötigt die .NET 10 Desktop Runtime.
- `App\config.json`, `cambios.json`, `estado.json` und `registro.txt`: werden von der App bei der Nutzung erstellt.
- `leeme\`: dieses Dokument in anderen Sprachen.
- `src\`: Quellcode. Neu kompilieren: `dotnet publish src -c Release -o App`.
- `tools\IconGen\`: erzeugt das Symbol (`src\app.ico`). Neu erzeugen: `dotnet run --project tools/IconGen -- src/app.ico`.
- `Portable\Laurel.exe`: portable Version. Sie bringt .NET mit (nichts zu installieren) und speichert ihre Daten im eigenen Ordner. Neu erstellen: `dotnet publish src -c Release -p:Portable=true -o Portable`.
- Die Schaltfläche **Liesmich** der App zeigt dieses Dokument in deiner Sprache. Sie nutzt eine in die `.exe` eingebaute Kopie: Das Ändern dieser `.md`-Dateien ändert nicht, was die App anzeigt (dafür muss neu kompiliert werden).

## Lizenz
Laurel ist freie Software: Du darfst es unter den Bedingungen der [GNU General Public License v3.0](../LICENSE) (GPL-3.0) nutzen, untersuchen, ändern und weitergeben. Wenn du eine geänderte Version verbreitest, muss sie ebenfalls frei sein und ihren Quellcode enthalten.

*** DIESES TOOL IST FREI NUTZBAR ***

# FiendGetConfigured – hier anfangen

Dieses Archiv enthält das vollständige, separat veröffentlichbare Git-Repo der Zusatzmod **FiendGetConfigured**. Die Originalmod **AuroraBytes-The_Fiend 1.1.6** bleibt installiert.

## 1. Entpacken und testen

1. Den gesamten Ordner `FiendGetConfigured` an deinen gewünschten dauerhaften Repo-Speicherort entpacken. Versteckte Dateien und den Ordner `.git` mitnehmen.
2. `local.settings.json` prüfen: Profil, Spielverzeichnis und optional vorhandener Unity-Editor. Die Datei ist bereits für deinen derzeitigen Rechner vorbereitet und wird von Git ignoriert.
3. Bei geschlossenem Spiel **`Install-Test.cmd`** starten. Es kopiert die geprüfte DLL in `BepInEx/plugins/FiendGetConfigured-LocalTest/` des eingetragenen Profils. Bestehende Configs werden nicht überschrieben. Bei einer bereits vorhandenen anderen Installation bricht es ab.
4. Spiel starten. Die eigene Config entsteht unter `BepInEx/config/TranslationLookasideBufferModding.FiendGetConfigured.cfg`.
5. Den Test aus [TESTPLAN.md](TESTPLAN.md) durchführen. Für Multiplayer brauchen **alle Spieler dieselbe DLL und dieselben Config-Werte**. Das Addon synchronisiert die Config nicht selbst. Eine Verteilung der gleichen Config über euer r2modman-Profil ist möglich.

Die automatischen Prüfungen sind bestanden. Ein echter Spiel- und Lobbytest ist für diese neue Mod noch offen. Erst danach die Veröffentlichung vornehmen und den Teststatus in README/Changelog anpassen.

## 2. Eigene Texte fertigstellen

`README.md`, `CHANGELOG.md` und bei Bedarf `package/icon.png` bearbeiten. Das Icon muss ein PNG mit genau 256 × 256 Pixeln bleiben. In `package/manifest.json` stehen Paketname, Version, Abhängigkeiten und der geplante GitHub-Link. Der Link ist auf `TranslationLookasideBufferModding/FiendGetConfigured` vorbereitet.

Danach **`Release.cmd`** ausführen. Es baut das aktuelle Thunderstore-Paket einschließlich deiner Texte:

```text
artifacts/releases/FiendGetConfigured-1.0.0.zip
```

**Diese innere Mod-ZIP** wird später bei GitHub Releases und Thunderstore angehängt. Die äußere GitRepo-ZIP ist dein Arbeitsarchiv.

## 3. Veröffentlichen

Der genaue Ablauf mit Git-Befehlen, Branches, Tags und Upload steht in [GitHub-und-Thunderstore.md](GitHub-und-Thunderstore.md).

Das Repo ist auf `main` initialisiert, hat aber bewusst noch keinen Commit und kein Remote. So gehört bereits der erste Commit zu deinem eigenen Git-Namen und deiner eigenen E-Mail-Adresse.

## Was liegt wo?

| Bestandteil | Aufgabe |
|---|---|
| `src/Plugin.cs` | BepInEx-Start, Config und Versionsprüfung |
| `src/Settings.cs` | Einstellungen und unabhängig prüfbare Regeln |
| `src/Runtime.cs` | Zugriff auf die laufende Fiend-Instanz; Sichtkontakt, Gedächtnis, Tempo, serverseitige Angriffssperre |
| `src/Patches.cs` | Harmony-Patches mit Prüfung der erwarteten Originalstellen |
| `src/VersionInfo.cs`, `AssemblyInfo.cs` | Versionsnummer und DLL-Metadaten |
| `tests/CoreTests.cs` | 23 Prüfungen der Regeln |
| `tests/UnityHarness/` | 52 Laufzeitprüfungen mit echtem Harmony; zusätzlich Prüfung und Patchinstallation an der Original-DLL |
| `package/manifest.json`, `icon.png` | Thunderstore-Metadaten und austauschbares eigenes Icon |
| `Build.cmd` | Nur DLL bauen |
| `Test.cmd` | DLL bauen und Tests ausführen |
| `Install-Test.cmd` | Geprüfte DLL lokal ins eingetragene Profil kopieren |
| `Release.cmd` | Geprüfte DLL und aktuelle Dokumentation als Mod-ZIP packen |
| `scripts/Set-Version.ps1` | Versionsnummer in Quellcode und Manifest gemeinsam ändern |
| `artifacts/` | Fertige DLL, Mod-ZIP und Testnachweise; von Git ignoriert |
| `local.settings.json` | Deine lokalen Pfade; von Git ignoriert |
| `FiendGetConfigured.csproj` | Optionales Projekt für eine IDE/ein bereits vorhandenes .NET SDK |
| `LICENSE.txt` | MIT-Lizenz für den eigenen Addon-Code und eigene Dateien |

Die CMD-Werkzeuge verwenden den vorhandenen Windows-C#-Compiler und lokale Referenzen. Sie installieren keine globalen Programme oder Pakete. Der Unity-Laufzeittest verwendet ausschließlich einen bereits vorhandenen, in `local.settings.json` eingetragenen Editor. Ohne diesen wird der Unity-Test ausdrücklich als nicht ausgeführt vermerkt.

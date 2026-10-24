# GitHub, Branches, Tags, Releases und Thunderstore

Du erledigst die Veröffentlichung selbst. Das Archiv enthält ein initialisiertes Repo auf **main**, ohne Commit und ohne Remote. Die Befehle in einem Terminal direkt im entpackten Ordner `FiendGetConfigured` ausführen. Git muss dafür im Terminal verfügbar sein, zum Beispiel über deine vorhandene Git-Installation.

## 1. Lokal testen und Texte fertigstellen

Zuerst [START-HIER.md](START-HIER.md) und [TESTPLAN.md](TESTPLAN.md) abarbeiten. Das komplette Modpack in einer echten Lobby prüfen. Danach README, Changelog und gegebenenfalls Icon anpassen. Die README ist ein bearbeitbarer Entwurf.

Die erste Versionsnummer ist bereits **1.0.0**. Der öffentliche GitHub-Link ist auf `https://github.com/TranslationLookasideBufferModding/FiendGetConfigured` vorbereitet. Falls du einen anderen Besitzer oder Repo-Namen nutzt, `package/manifest.json` anpassen.

## 2. Git-Identität nur für dieses Repo festlegen

```powershell
git config --local user.name "Dein gewünschter Name"
git config --local user.email "DEINE-BESTAETIGTE-ODER-GITHUB-NOREPLY-EMAIL"
```

Die Beispielwerte vorher ersetzen. Eine passende private GitHub-Noreply-Adresse findest du in GitHub unter Settings → Emails. Diese Angaben bestimmen den Autor deiner Commits. Sie wählen keinen angemeldeten GitHub-Account aus. Beim späteren Push mit dem gewünschten GitHub-Konto anmelden. Wenn der Credential Manager das alte Konto auswählt, dort die passende GitHub-Anmeldung wechseln; keine Passwörter oder Tokens ins Repo schreiben.

Die `--local`-Befehle ändern keine globale Git-Konfiguration. Prüfen kannst du sie mit:

```powershell
git config --local user.name
git config --local user.email
```

## 3. Leeres öffentliches GitHub-Repo erstellen

Auf GitHub **New repository** wählen:

- Owner: dein gewünschter Account, vorbereitet ist `TranslationLookasideBufferModding`.
- Name: `FiendGetConfigured`.
- Public wählen, damit der Quellcode öffentlich ist.
- README, `.gitignore` und Lizenz nicht durch GitHub anlegen lassen: Sie sind bereits lokal vorhanden.

## 4. Modpaket bauen, committen und pushen

Nach den letzten Änderungen **`Release.cmd`** ausführen. Die Mod-ZIP entsteht unter:

```text
artifacts/releases/FiendGetConfigured-1.0.0.zip
```

Die DLL wird aus dem aktuellen Code gebaut und geprüft. Bei unverändertem Code und unveränderter Original-DLL kann der nachgewiesene lokale Teststand wiederverwendet werden. Dokumentation, Manifest und Icon werden trotzdem neu gepackt. `Test.cmd` führt die Tests ausdrücklich erneut aus.

Dann:

```powershell
git status
git add .
git commit -m "Initial release of FiendGetConfigured 1.0.0"
git remote add origin https://github.com/TranslationLookasideBufferModding/FiendGetConfigured.git
git push -u origin main
```

Den Remote-Link anpassen, falls du einen anderen Owner gewählt hast. `local.settings.json`, Unity-Caches und `artifacts/` werden durch `.gitignore` ausgeschlossen. Damit landen keine lokalen Spielpfade oder fremden Game-/Mod-DLLs im Quellcode-Repo. Die kompilierte Mod-ZIP kommt als Release-Anhang dazu.

Für diese erste Veröffentlichung brauchst du keinen weiteren Branch: `main` existiert schon. Ein Branch ist ein Arbeitszweig, ein Tag ist eine feste Markierung eines veröffentlichten Commits.

## 5. Erst den endgültigen Commit taggen

```powershell
git tag -a v1.0.0 -m "Version 1.0.0"
git push origin v1.0.0
```

Der Tag zeigt jetzt genau auf den zuletzt committeten Stand. Wenn du danach noch Dateien änderst, gehören diese Änderungen nicht nachträglich zu diesem Tag. Bereits veröffentlichte Tags nicht verschieben; für spätere Änderungen eine neue Version verwenden.

## 6. GitHub-Release erstellen

Im Repo **Releases → Draft a new release** öffnen; alternativ direkt:

<https://github.com/TranslationLookasideBufferModding/FiendGetConfigured/releases/new>

1. Vorhandenen Tag `v1.0.0` auswählen.
2. Titel `1.0.0`, Beschreibung aus dem fertigen Changelog übernehmen.
3. **`artifacts/releases/FiendGetConfigured-1.0.0.zip`** anhängen.
4. **Publish release**.

GitHub bietet zum Tag zusätzlich automatisch Quellcode-Archive an. Dein angehängtes Modpaket ist der fertige Download für die Installation.

## 7. Dieselbe Mod-ZIP auf Thunderstore hochladen

Im [Thunderstore-Uploadformular](https://thunderstore.io/package/create/) anmelden, dein Team und **Lethal Company** auswählen. Die gleiche innere `FiendGetConfigured-1.0.0.zip` hochladen. Nicht die äußere GitRepo-ZIP verwenden.

Passende Kategorien sind **Mods**, **BepInEx**, **Tweaks & Quality Of Life** und wegen des erstellten Codes **AI Generated**, soweit die jeweiligen Kategorien im Formular angeboten werden. Die Abhängigkeiten auf BepInEx und AuroraBytes-The_Fiend stehen im Manifest. Quellcode und Dokumentation bleiben in deinem öffentlichen GitHub-Repo.

Wenn du anschließend die Thunderstore-Version im Modmanager installierst, zuerst die lokale Testinstallation entfernen: **nur den Profilordner `BepInEx/plugins/FiendGetConfigured-LocalTest/`**. Die Originalmod AuroraBytes-The_Fiend bleibt installiert. Andernfalls können zwei Addon-DLLs im Profil liegen. Die eigene Config kann bestehen bleiben.

## Spätere Updates mit Arbeitsbranch

Beispiel für Version 1.0.1:

```powershell
git switch main
git pull --ff-only
git switch -c fix/fiend-balance
```

Änderungen machen. Version gemeinsam in Code und Manifest erhöhen:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Set-Version.ps1 -Version 1.0.1
```

Changelog aktualisieren. `Test.cmd`, gegebenenfalls `Install-Test.cmd` und echten Lobbytest durchführen; dann `Release.cmd`. Anschließend:

```powershell
git add .
git commit -m "Describe the actual change"
git switch main
git merge --ff-only fix/fiend-balance
git push origin main
git tag -a v1.0.1 -m "Version 1.0.1"
git push origin v1.0.1
```

Das funktioniert, solange `main` seit Anlegen des Branches nicht anderweitig verändert wurde. Bei zusätzlicher Zusammenarbeit stattdessen den Branch pushen, einen Pull Request erstellen und nach dessen Merge den endgültigen Stand auf `main` taggen. Keine Änderungen verwerfen, um einen Merge zu erzwingen.

Danach GitHub-Release für `v1.0.1` anlegen und die neue Mod-ZIP sowohl dort als auch bei Thunderstore hochladen. **Versionsnummern in DLL, Manifest, Tag und Veröffentlichung müssen zusammenpassen.** Das Versionsskript und der Paketbau prüfen beziehungsweise unterstützen das.

## Quellen

- [GitHub: Neues Repository erstellen](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)
- [GitHub: Releases verwalten](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
- [Thunderstore: Paketformat](https://wiki.thunderstore.io/mods/creating-a-package)
- [Lethal Company auf Thunderstore](https://old.thunderstore.io/c/lethal-company/)

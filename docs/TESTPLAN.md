# Teststand und manueller Spieltest

## Bereits geprüft

Entwicklungsstand: Version **1.0.0**, Zielmod **AuroraBytes-The_Fiend 1.1.6**, Entwicklungsziel **Lethal Company v81**.

**75 automatische Prüfungen bestanden:** 23 Regeln ohne Spielumgebung und 52 Laufzeitprüfungen mit echtem Harmony in einem vorhandenen Unity-Editor. Die Laufzeitprüfungen verwenden eine synthetische Fiend-Testinstanz und prüfen außerdem alle erwarteten Patchmuster sowie die Installation und Entfernung der Patches auf den tatsächlichen Methoden der installierten Original-DLL. Dabei wird kein echter Gegner in einer Spiellobby gestartet.

Geprüft sind unter anderem Geschwindigkeit, Stopps, Sichtfeld und Distanz, letzte bekannte Position, Ablauf der Erinnerung, ungültige Ziele, Schaden, Tötungsschwelle, Rückstoß, Server-Angriffssperre, wiederholte Angriffsanforderungen und Fähigkeitsschalter. Ein abweichendes erwartetes Patchmuster wird abgelehnt.

Nachweise liegen lokal in `artifacts/tests/`: `verification.json`, `core-tests.log`, `unity-runtime.log`. Die Unity-Marker `REAL_FIEND_PREFLIGHT_OK`, `REAL_FIEND_PATCH_INSTALL_OK` und `RUNTIME_TESTS_OK 52` kennzeichnen die erfolgreiche Prüfung. `LobbyTest: Pending` bedeutet ausdrücklich, dass die automatische Prüfung keinen Multiplayer-Spieltest enthält.

## Noch im Spiel prüfen

| Prüfung | Stand im ausgelieferten Entwicklungsarchiv |
|---|---|
| Start des vollständigen Modpacks mit dem neuen Addon | Offen |
| Echte Lobby mit Host und mindestens einem weiteren Client | Offen |
| Host und Client erleiden die konfigurierten Treffer | Offen |
| Sichtverlust und Suche im tatsächlichen Interior | Offen |
| Fähigkeitsschalter im tatsächlichen Spiel | Offen |

### Vorbereitung

1. Spiel schließen, `Install-Test.cmd` ausführen. Alle Tester benötigen dieselbe DLL und dieselbe Config. Die DLL liegt danach im Profil unter `BepInEx/plugins/FiendGetConfigured-LocalTest/`. Für Mitspieler kann sie manuell in einen entsprechenden Plugin-Unterordner kopiert werden.
2. Spiel einmal starten, damit die Config entsteht. Bei Änderungen danach neu starten. Die erzeugte Config an alle Tester verteilen oder über dasselbe r2modman-Profil verteilen.
3. Im BepInEx-Log nach `FiendGetConfigured` suchen. Erwartet wird `active for The Fiend 1.1.6`, mit damage=25, chase=5.5 und sight=True bei den Defaults. Bei einer Kompatibilitätsfehlermeldung läuft der ursprüngliche Fiend ohne die Addon-Patches weiter; dann zunächst das Log prüfen.
4. Zum gezielten Testen beispielsweise mit dem bereits verwendeten Imperium einen Fiend in einem normalen Interior erzeugen. Keine Unverwundbarkeit verwenden, wenn Schaden geprüft wird. Andere Gegner und Schaden möglichst ausschließen.

### Treffer auf Host und Client

- Jeweils mit 100 HP beginnen. Mit Defaults sollten drei normale Treffer auf 75/50/25 HP führen und der vierte akzeptierte Angriff tödlich sein.
- Zwischen akzeptierten Angriffen sollten mindestens etwa zwei Sekunden liegen. Auch bei gleichzeitigem Kontakt mehrerer Spieler darf der Server nicht mehrere unmittelbar aufeinanderfolgende Angriffe freigeben.
- Einmal als Host und einmal als weiterer Client testen; Animation, HP und Tod sollten für alle zusammenpassen.
- Optional `ExecutionHealthThreshold = -1` und `Damage = 10` setzen, alle Clients neu starten und prüfen, ob der tödliche Griff bei 25 HP entfällt. Danach gewünschte endgültige Werte wieder einstellen.

### Sichtkontakt und Gedächtnis

- Ein noch nicht gesehenes Ziel hinter einer Wand verstecken. Bei `RequireLineOfSight = true` soll es nicht allein wegen seiner Nähe über die Zielerfassung verfolgt werden.
- Sichtkontakt herstellen, danach hinter einer Ecke weiterlaufen. Der Fiend soll zur zuletzt gesehenen Stelle laufen und nicht fortlaufend der neuen versteckten Position folgen.
- Bei acht Sekunden ohne erneuten Sichtkontakt soll das erinnerte Ziel auslaufen. Andere sichtbare Spieler oder ein erneuter Sichtkontakt dürfen selbstverständlich eine neue Verfolgung auslösen.
- Sichtkontakt dicht hinter einer Wand darf keinen Kontaktangriff durch diese Wand auslösen.

### Fähigkeiten und Konfiguration

- Mit Defaults darf Apparatus-Entnahme keinen Apparatus-Rage auslösen. Zufällige Spieler-Teleports und temporäre Rage sind ausgeschaltet.
- Taschenlampen-Angst/Stun soll weiterhin funktionieren. Türen und Sicherungskasten dürfen mit Defaults weiterhin beschädigt werden.
- Gewünschte abweichende Optionen einzeln aktivieren, alle Tester neu starten und passend prüfen.
- Die endgültige Konfiguration in das Modpack übernehmen und einen normalen Tag einschließlich Landen/Abflug spielen. Das prüft auch das Zusammenspiel mit eurem gesamten Modpack.

### Nach erfolgreichem Test

Die tatsächlich geprüfte Spielversion, Fiend-Version, Anzahl der Spieler und gegebenenfalls Einschränkungen in README/Changelog eintragen. Danach `Release.cmd` erneut ausführen, committen und erst den endgültigen Commit taggen. Automatische Tests allein als solche beschreiben; nicht als erfolgreichen Lobbytest ausgeben.

Wenn etwas hakt, den frischen BepInEx-Log, verwendete Config und Host/Client-Rolle festhalten. Keine laufende DLL im gestarteten Spiel austauschen.

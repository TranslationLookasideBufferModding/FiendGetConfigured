# Konfiguration

Datei nach dem ersten Spielstart:

```text
BepInEx/config/TranslationLookasideBufferModding.FiendGetConfigured.cfg
```

Alle Spieler müssen dieselbe Addon-Version und dieselben Werte verwenden. Die Mod hat keine automatische Host-Config-Synchronisierung. Nach Änderungen das Spiel neu starten. Dezimalzahlen in der CFG mit Punkt schreiben, etwa `5.5`.

## General

| Schlüssel | Typ | Standard | Bedeutung |
|---|---|---|---|
| `Enabled` | bool | `true` | Addon aktivieren. Bei `false` werden seine Patches nicht installiert. |

Das aktive Addon erzwingt das moderne Verhalten des Fiends. Die ursprüngliche `Legacy Behaviour`-Option wird dabei übergangen. Diese alte Verhaltensvariante hat andere Angriffs- und Stunregeln; die Balance-Regler sind für das moderne Verhalten ausgelegt.

## Movement

| Schlüssel | Typ | Standard | Bereich | Bedeutung |
|---|---|---:|---|---|
| `WalkSpeed` | float | 3 | 0–100 | Normales Laufen/Umherstreifen |
| `StalkSpeed` | float | 1 | 0–100 | Seeking-/Stalking-Modus |
| `ChaseSpeed` | float | 5.5 | 0–100 | Normale Verfolgung |
| `RageSpeed` | float | 7 | 0–100 | Verfolgung bei Rage |

Die normalen Stopps der Original-KI, etwa während einer Animation, bleiben wirksam. Ein aktivierter LateGame-Multiplikator erhöht diese Geschwindigkeiten zusätzlich.

## Combat

| Schlüssel | Typ | Standard | Bereich | Bedeutung |
|---|---|---:|---|---|
| `Damage` | int | 25 | 0–1000 | Schaden eines normalen Treffers |
| `ExecutionHealthThreshold` | int | 25 | −1–100 | Bei höchstens so vielen HP **vor** einem Treffer wird der ursprüngliche tödliche Griff verwendet. `-1` deaktiviert diesen Griff. |
| `AttackDistance` | float | 2 | 0.1–10 | Maximale Kontaktangriffsdistanz in Metern zwischen den Transform-Positionen |
| `AttackCooldownSeconds` | float | 2 | 0.1–60 | Mindestabstand zwischen akzeptierten Angriffen in Sekunden |
| `KnockbackForce` | float | 15 | 0–100 | Rückstoß normaler Treffer |

Bei 100 HP ohne weitere Schadensmodifikationen ergeben die Standardwerte drei Treffer mit je 25 Schaden und anschließend einen tödlichen vierten Angriff. Der Schaden und die Tötungsschwelle sind getrennte Einstellungen. Auch mit deaktiviertem tödlichem Griff kann normaler Schaden töten.

Die Angriffssperre wird zusätzlich auf dem Server geprüft, damit wiederholte Kontaktmeldungen verschiedener Clients sie nicht umgehen. Lange Tötungsanimationen können weiterhin länger als der eingestellte Mindestabstand dauern.

`AttackDistance` vergrößert den ursprünglichen Kontakt-Collider nicht. Ein hoher Wert verschafft dem Fiend daher nicht automatisch eine entsprechend große Reichweite. Rückstoß von Leichen-/Tötungseffekten wird durch `KnockbackForce` nicht geändert.

## Perception

| Schlüssel | Typ | Standard | Bereich | Bedeutung |
|---|---|---:|---|---|
| `RequireLineOfSight` | bool | `true` | true/false | Sichtkontakt für Zielerfassung und Kontaktangriff verlangen |
| `DetectionRange` | float | 30 | 0.5–40 | Maximale Entfernung zur Zielerfassung in Metern; bei Sichtkontakt begrenzt die Zielauswahl des Basisspiels v81 die Reichweite auf 40 Meter |
| `FieldOfViewDegrees` | float | 140 | 10–360 | Gesamtes Sichtfeld in Grad |
| `TargetMemorySeconds` | float | 8 | 0–120 | Wie lange der Fiend nach Sichtverlust die zuletzt gesehene Position verfolgt |

Bei Sichtverlust folgt er während der Erinnerung der **zuletzt gesehenen Position**. Er bekommt dadurch nicht fortlaufend die aktuelle Position des versteckten Spielers als Wegziel. Nach Ablauf wird das Ziel vergessen. `0` bedeutet sofortiges Vergessen. Tote oder für die Original-KI nicht mehr gültige Spieler werden ebenfalls vergessen.

Der Fiend kann andere sichtbare Spieler erfassen und seine bestehenden Zustandswechsel weiter ausführen. Vergessen bedeutet daher nicht zwingend, dass er im selben Moment umdreht. Mit `RequireLineOfSight = false` kann er wieder verdeckte Spieler als Ziele erfassen; die Sichtverlust-Erinnerung wird dann nicht verwendet. `DetectionRange` gilt weiterhin.

## Abilities

| Schlüssel | Typ | Standard | Bedeutung |
|---|---|---|---|
| `TeleportEnabled` | bool | `false` | Ursprüngliche zufällige Spieler-Teleportfähigkeit zulassen |
| `ApparatusRageEnabled` | bool | `false` | Rage nach Entnahme des Apparats zulassen; übersteuert die Originaloption während das Addon aktiv ist |
| `TemporaryRageEnabled` | bool | `false` | Zeitweilige Rage aus dem ursprünglichen Feared-/Seeking-Verhalten zulassen |
| `BreakDoorsEnabled` | bool | `true` | Türen aufbrechen zulassen; bei false können geschlossene Türen den Fiend aufhalten |
| `BreakerBoxDestructionEnabled` | bool | `true` | Sicherungskasten als Ziel und dessen Zerstörung zulassen |

Das Angst-/Stunverhalten durch Taschenlampen bleibt bei ausgeschalteter `TemporaryRageEnabled` erhalten. Dieser Schalter betrifft den Rage-Anteil des ursprünglichen Ablaufs.

## LateGame

| Schlüssel | Typ | Standard | Bereich | Bedeutung |
|---|---|---:|---|---|
| `Enabled` | bool | `false` | true/false | Ursprünglichen späten Geschwindigkeits- und Zufallsfähigkeiten-Boost zulassen |
| `StartHour` | int | 15 | 0–24 | Schwelle für das ursprüngliche Spielfeld `TimeOfDay.hour` |
| `Multiplier` | int | 2 | 1–5 | Multiplikator für Bewegung und ursprüngliche Zufallsfähigkeiten |

`StartHour` verwendet dieselbe interne Stundenvariable wie die Originalmod. Der Multiplikator betrifft auch deren Zufallsfähigkeitshäufigkeit, soweit die jeweilige Fähigkeit aktiviert ist. Ohne LateGame-Boost bleiben die konfigurierten Geschwindigkeiten konstant.

## Spawns und Original-Config

Spawngewicht, Mondfilter, Flicker Chance und Lautstärke weiter in **`com.TheFiend.cfg`** einstellen. Eine ältere `Fiend.cfg` wird vom aktuellen AuroraBytes-Fork nicht verwendet. Das Addon schreibt beide Dateien nicht um.

Andere Mods können Geschwindigkeit, Schaden, Spieler-HP oder Gegnerverhalten zusätzlich beeinflussen. Die Werte hier beschreiben die Änderungen dieses Addons.

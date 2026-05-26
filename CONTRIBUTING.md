# Branching & Release-Strategie

## Branch-Übersicht

```
main
  └── feature/xyz              → neue Funktion entwickeln

release/v0.1
  └── hotfix/kurzbeschreibung  → dringender Fix auf bestehendem Release

release/v1.0
...
```

| Branch | Zweck | Direkte Commits |
|---|---|---|
| `main` | Integrations-Branch, aktueller Entwicklungsstand | Nein – nur Merges |
| `release/vX.Y` | Eingefrorener Stand eines Releases | Nein – nur Merges von Hotfixes |
| `feature/*` | Neue Funktionen und Weiterentwicklungen | Ja |
| `hotfix/*` | Dringende Fixes für einen bestehenden Release | Ja |

---

## Neue Funktion entwickeln

```bash
# Von main abzweigen
git checkout main
git pull
git checkout -b feature/meine-funktion

# ... entwickeln, committen ...

# Zurück nach main mergen (kein Fast-Forward, damit der Merge sichtbar bleibt)
git checkout main
git merge --no-ff feature/meine-funktion
git push
git branch -d feature/meine-funktion
```

---

## Neues Release veröffentlichen

Ein Release entsteht immer aus dem aktuellen Stand von `main`.

```bash
# 1. main ist aktuell und vollständig getestet
git checkout main
git pull

# 2. Release-Branch anlegen (einmalig pro Major.Minor-Version)
git checkout -b release/v1.0
git push -u origin release/v1.0

# 3. Tag auf dem Release-Branch setzen → löst GitHub Actions aus
git tag v1.0.0
git push origin v1.0.0
```

Der Tag-Push startet automatisch den Workflow [release.yml](.github/workflows/release.yml), der für **alle drei Plattformen parallel** baut:

| Plattform | Artefakt |
|---|---|
| Raspberry Pi | `stocktv-rpi-vX.Y.Z.img.xz` + `stocktv-rpi.zip` |
| Windows x64 | `stocktv-windows-x64.zip` |
| Linux x64 | `stocktv-linux-x64.zip` |

Alle vier Dateien landen automatisch im GitHub Release.

> **Patch-Releases** (z.B. `v1.0.1`) entstehen immer über einen Hotfix-Branch — siehe Abschnitt unten.

---

## Hotfix für einen bestehenden Release

Ein Hotfix behebt einen kritischen Fehler in einem bereits veröffentlichten Release.

```bash
# 1. Vom betroffenen Release-Branch abzweigen
git checkout release/v1.0
git pull
git checkout -b hotfix/beschreibung

# 2. Fix entwickeln und committen
# ...

# 3. In den Release-Branch mergen und neuen Patch-Tag setzen
git checkout release/v1.0
git merge --no-ff hotfix/beschreibung
git tag v1.0.1
git push origin release/v1.0 v1.0.1

# 4. Fix auch in main übernehmen
git checkout main
git merge --no-ff hotfix/beschreibung
git push

git branch -d hotfix/beschreibung
```

---

## Manuellen Build starten (ohne Release)

Für Tests oder Vorab-Binaries kann der Workflow manuell gestartet werden:

GitHub → Repository → **Actions** → **Release** → **Run workflow**

Alle drei Plattformen werden gebaut. Die Artefakte sind 7 Tage verfügbar.
Es wird kein GitHub Release angelegt.

---

## Plattform-Übersicht

| Plattform | Lokales Build-Skript | Artefakt | Dokumentation |
|---|---|---|---|
| Raspberry Pi | `build/rpi/publish-rpi.ps1` | `stocktv-rpi.zip` + `.img.xz` | [build/rpi/README.md](build/rpi/README.md) |
| Windows x64 | `build/windows/publish-windows.ps1` | `stocktv-windows-x64.zip` | [build/windows/README.md](build/windows/README.md) |
| Linux x64 | `build/linux/publish-linux.ps1` | `stocktv-linux-x64.zip` | [build/linux/README.md](build/linux/README.md) |
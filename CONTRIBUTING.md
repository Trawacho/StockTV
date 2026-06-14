# Branching & Release-Strategie

## Branch-Übersicht

```
main (Releases)
  ↑
  release/v1.0 (Tag: v1.0.0, v1.0.1, ...)
  ↑
develop (Integration)
  ↑
  feature/xyz (neue Features)
  
release/v0.1 (ältere Releases, Hotfixes möglich)
  ↑
  hotfix/beschreibung
```

| Branch | Zweck | Direkte Commits |
|---|---|---|
| `main` | Production-Branch, nur Releases | Nein – nur Merges von release/* |
| `develop` | Integrations-Branch, aktueller Entwicklungsstand | Nein – nur Merges von feature/* und release/* |
| `release/vX.Y` | Release-Kandidat (eingefrorener Stand) | Ja, nur Hotfixes |
| `feature/*` | Neue Funktionen und Weiterentwicklungen | Ja |
| `hotfix/*` | Dringende Fixes für einen bestehenden Release | Ja |

---

## Neue Funktion entwickeln

```bash
# Von develop abzweigen
git checkout develop
git pull
git checkout -b feature/meine-funktion

# ... entwickeln, committen ...

# Zurück nach develop mergen (kein Fast-Forward, damit der Merge sichtbar bleibt)
git checkout develop
git merge --no-ff feature/meine-funktion
git push
git branch -d feature/meine-funktion
```

---

## Neues Release veröffentlichen

Ein Release entsteht immer aus dem aktuellen Stand von `develop`.

```bash
# 1. develop ist aktuell und vollständig getestet
git checkout develop
git pull

# 2. Release-Branch anlegen (einmalig pro Major.Minor-Version)
git checkout -b release/v1.0
git push -u origin release/v1.0

# 3. release/v1.0 → main mergen (kein Fast-Forward)
git checkout main
git merge --no-ff release/v1.0
git push

# 4. Tag auf dem Release-Branch setzen → löst GitHub Actions aus
git tag v1.0.0 release/v1.0
git push origin v1.0.0

# 5. release/v1.0 auch zurück zu develop mergen (verhindert Divergence!)
git checkout develop
git merge --no-ff release/v1.0
git push
```

Der Tag-Push startet automatisch den Workflow [release.yml](.github/workflows/release.yml), der für **alle drei Plattformen parallel** baut:

| Plattform | Artefakt |
|---|---|
| Raspberry Pi | `stocktv-rpi-vX.Y.Z.img.xz` + `stocktv-rpi.zip` |
| Windows x64 | `stocktv-windows-x64.zip` |
| Linux x64 | `stocktv-linux-x64.zip` |

Alle Dateien landen automatisch im GitHub Release.

> **Patch-Releases** (z.B. `v1.0.1`) entstehen über einen Hotfix-Branch auf dem existierenden `release/v1.0` — siehe Abschnitt unten.

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

# 3. In den Release-Branch mergen
git checkout release/v1.0
git merge --no-ff hotfix/beschreibung
git push

# 4. Tag setzen → löst GitHub Actions aus
git tag v1.0.1 release/v1.0
git push origin v1.0.1

# 5. Hotfix auch in main mergen
git checkout main
git merge --no-ff release/v1.0
git push

# 6. WICHTIG: Hotfix auch in develop mergen (verhindert Divergence!)
git checkout develop
git merge --no-ff release/v1.0
git push

git branch -d hotfix/beschreibung
```

⚠️ **Kritisch:** Der Hotfix muss zu **develop zurück**, sonst fehlt der Fix im nächsten Release!

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
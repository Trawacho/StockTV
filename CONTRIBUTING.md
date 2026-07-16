# Branching & Release-Strategie

## Branch-Übersicht

```
main (Releases, Tag: v1.0.0, v1.0.1, ...)
  ↑
  release/v1.0
  ↑
develop (Integration)
  ↑
  feature/xyz (neue Features)
  
release/v0.1 (ältere Releases, Hotfixes möglich)
  ↑
  hotfix/beschreibung

main
  ↑
  docs/beschreibung (Doku-only — landet in main UND develop, siehe unten)
```

| Branch | Zweck | Direkte Commits |
|---|---|---|
| `main` | Production-Branch, nur Releases | Nein – nur Merges von release/* (Ausnahme: docs/*, siehe unten) |
| `develop` | Integrations-Branch, aktueller Entwicklungsstand | Nein – nur Merges von feature/*, release/* und docs/* |
| `release/vX.Y` | Release-Kandidat (eingefrorener Stand) | Ja, nur Hotfixes |
| `feature/*` | Neue Funktionen und Weiterentwicklungen | Ja |
| `hotfix/*` | Dringende Fixes für einen bestehenden Release | Ja |
| `docs/*` | Nicht-funktionale Änderungen (Doku, README, Lizenztexte, …) | Ja — zweigt von `main` ab, siehe unten |

⚠️ **Aufräumen:** Sobald ein `feature/*`-, `hotfix/*`- oder `docs/*`-Branch gemergt ist, wird
er **sowohl lokal als auch auf dem Remote gelöscht** (`git branch -d <branch>` und
`git push origin --delete <branch>`, sofern der Branch zuvor gepusht wurde). Alte, bereits
gemergte Branches bleiben nicht liegen.

---

## Neue Funktion entwickeln

```bash
# Von develop abzweigen
git checkout develop
git pull
git checkout -b feature/meine-funktion

# ... entwickeln, committen ...
git push -u origin feature/meine-funktion

# Zurück nach develop mergen (kein Fast-Forward, damit der Merge sichtbar bleibt)
git checkout develop
git merge --no-ff feature/meine-funktion
git push

# Branch ist gemergt — lokal und remote löschen
git branch -d feature/meine-funktion
git push origin --delete feature/meine-funktion
```

---

## Neues Release veröffentlichen

Ein Release entsteht immer aus dem aktuellen Stand von `develop`.

> ⚠️ **Wichtig:** Der Git-Tag allein reicht nicht aus. Die `<Version>` in
> [`StockTvBlazor/StockTvBlazor.csproj`](StockTvBlazor/StockTvBlazor.csproj) muss vor dem Tag
> manuell auf denselben Stand gesetzt werden — der `release.yml`-Workflow leitet sie **nicht**
> automatisch aus dem Tag ab. Diese Zahl landet im kompilierten Binary und wird als `pkgVer`
> im mDNS-Alive-Paket verbreitet (siehe [MdnsDiscoveryService.cs](StockTvBlazor/Networking/MdnsDiscoveryService.cs)).
> Wird sie vergessen, meldet die App über mDNS eine veraltete Version, obwohl Tag und
> GitHub-Release schon weiter sind.

```bash
# 1. develop ist aktuell und vollständig getestet
git checkout develop
git pull

# 2. Release-Branch anlegen (einmalig pro Major.Minor-Version)
git checkout -b release/v1.0
git push -u origin release/v1.0

# 3. Versionsnummer in StockTvBlazor.csproj auf die neue Version setzen
#    (<Version>1.0.0.0</Version> in StockTvBlazor/StockTvBlazor.csproj), dann committen
git add StockTvBlazor/StockTvBlazor.csproj
git commit -m "chore: Version auf 1.0.0 gesetzt"
git push

# 4. release/v1.0 → main mergen (kein Fast-Forward)
git checkout main
git merge --no-ff release/v1.0
git push

# 5. Tag auf main setzen → löst GitHub Actions aus
git tag v1.0.0 main
git push origin v1.0.0

# 6. release/v1.0 auch zurück zu develop mergen (verhindert Divergence!)
git checkout develop
git merge --no-ff release/v1.0
git push
```

Der Tag-Push startet automatisch den Workflow [release.yml](.github/workflows/release.yml), der **vier Jobs parallel** baut:

| Plattform | Artefakt |
|---|---|
| Raspberry Pi | `stocktv-rpi-vX.Y.Z.img.xz` + `stocktv-rpi.zip` |
| Windows x64 | `stocktv-windows-x64.zip` |
| Linux x64 | `stocktv-linux-x64.zip` |

Diese drei Dateien landen automatisch im GitHub Release.

Zusätzlich baut derselbe Tag-Push über den wiederverwendbaren Workflow [docker.yml](.github/workflows/docker.yml) ein Multi-Arch-Docker-Image (`linux/amd64` + `linux/arm64`) und pusht es direkt nach GitHub Container Registry (GHCR) — **nicht** als Release-Datei, sondern als Image:

```text
ghcr.io/trawacho/stocktv:vX.Y.Z
ghcr.io/trawacho/stocktv:latest
```

Installationsanleitung dafür: [INSTALL.md → Docker](INSTALL.md#docker). Der Docker-Build läuft unabhängig von den drei anderen Jobs — schlägt z.B. der Windows-Build fehl, wird das Docker-Image trotzdem gebaut und gepusht (und umgekehrt).

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
git push -u origin hotfix/beschreibung

# 3. Versionsnummer in StockTvBlazor.csproj auf den Patch-Stand setzen
#    (<Version>1.0.1.0</Version> in StockTvBlazor/StockTvBlazor.csproj), dann committen
git add StockTvBlazor/StockTvBlazor.csproj
git commit -m "chore: Version auf 1.0.1 gesetzt"

# 4. In den Release-Branch mergen
git checkout release/v1.0
git merge --no-ff hotfix/beschreibung
git push

# 5. Hotfix auch in main mergen
git checkout main
git merge --no-ff release/v1.0
git push

# 6. Tag auf main setzen → löst GitHub Actions aus
git tag v1.0.1 main
git push origin v1.0.1

# 7. WICHTIG: Hotfix auch in develop mergen (verhindert Divergence!)
git checkout develop
git merge --no-ff release/v1.0
git push

# Hotfix-Branch ist gemergt — lokal und remote löschen
git branch -d hotfix/beschreibung
git push origin --delete hotfix/beschreibung
```

⚠️ **Kritisch:** Der Hotfix muss zu **main und develop zurück**, sonst fehlt der Fix im nächsten Release bzw. der Tag landet auf einem Commit, der nicht auf main liegt!

---

## Doku-only-Änderungen ohne Release

Für Änderungen, die **keinerlei Verhalten der App selbst beeinflussen** (z.B. `README.md`, `CONTRIBUTING.md`, Lizenztexte, reine Kommentare, sowie reine CI/Workflow-Konfiguration unter `.github/workflows/*`, die nur die Build-/Release-Pipeline betrifft) muss nicht der volle Release-Zyklus durchlaufen werden. Solche Änderungen dürfen direkt nach `main` **und** `develop` gemergt werden, ohne Tag und ohne Release-Build.

> ⚠️ **Wichtig — von `main` abzweigen, nicht von `develop`:** `develop` ist zwischen zwei
> Releases so gut wie immer ein Stück voraus (unreleaste Feature-/Hotfix-Arbeit). Würde man
> einen docs/*-Branch von `develop` abzweigen und anschließend nach `main` mergen, brächte
> dieser Merge automatisch die gesamte Vorgeschichte von `develop` mit nach `main` — inklusive
> Code, der noch gar nicht released werden sollte. Ein docs/*-Branch zweigt daher immer von
> `main` ab; dadurch enthält er garantiert nur den main-Stand plus die eine Doku-Änderung.

```bash
# 1. Von main abzweigen (nicht von develop!)
git checkout main
git pull
git checkout -b docs/kurzbeschreibung

# 2. Änderung committen
# ...
git push -u origin docs/kurzbeschreibung

# 3. Zuerst nach main mergen (kein Tag, kein Release-Build!)
git checkout main
git merge --no-ff docs/kurzbeschreibung
git push

# 4. Denselben Branch danach auch nach develop mergen (verhindert Divergence)
git checkout develop
git pull
git merge --no-ff docs/kurzbeschreibung
git push

# Branch ist in beide Ziele gemergt — lokal und remote löschen
git branch -d docs/kurzbeschreibung
git push origin --delete docs/kurzbeschreibung
```

⚠️ **Wichtig:** Dieser Weg ist ausschließlich für Änderungen erlaubt, die keinen Einfluss auf das Programmverhalten haben. Im Zweifel: normalen Feature-/Release-Weg nutzen. Ein Merge nach `main` ohne Tag löst **keinen** GitHub-Actions-Build aus (der Workflow reagiert nur auf `v*`-Tags), Doku-Änderungen sind also risikolos für die Release-Pipeline.

---

## Manuellen Build starten (ohne Release)

Für Tests oder Vorab-Binaries kann der Workflow manuell gestartet werden:

GitHub → Repository → **Actions** → **Release** → **Run workflow**

Alle vier Jobs werden gebaut (Raspberry Pi, Windows x64, Linux x64, Docker). Die drei
Installer-Artefakte sind 7 Tage verfügbar, es wird kein GitHub Release angelegt.

**`:latest` bleibt bei manuellen Läufen unangetastet:** Der Docker-Job taggt bei einem
manuellen "Run workflow" nur die gewählte Versionsnummer (Default `dev`), **nicht** `:latest`
— gesteuert über den `push_latest`-Input von [docker.yml](.github/workflows/docker.yml), den
`release.yml` nur bei einem echten Tag-Push auf `true` setzt (`push_latest: ${{ github.event_name == 'push' }}`).
`:latest` in GHCR entspricht damit immer dem letzten echten Release.

---

## Plattform-Übersicht

| Plattform | Lokales Build-Skript | Artefakt | Dokumentation |
|---|---|---|---|
| Raspberry Pi | `build/rpi/publish-rpi.ps1` | `stocktv-rpi.zip` + `.img.xz` | [build/rpi/README.md](build/rpi/README.md) |
| Windows x64 | `build/windows/publish-windows.ps1` | `stocktv-windows-x64.zip` | [build/windows/README.md](build/windows/README.md) |
| Linux x64 | `build/linux/publish-linux.ps1` | `stocktv-linux-x64.zip` | [build/linux/README.md](build/linux/README.md) |
| Docker (Multi-Arch) | `build/buildproject.bat` (lokal) / `build/remotebuild_std.ps1` (Deploy) | `ghcr.io/trawacho/stocktv:<version>` + `:latest` | [INSTALL.md → Docker](INSTALL.md#docker) |

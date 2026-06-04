# Strinova replay CDN — retention and download behavior

This document describes how **Strinova Replay Manager** talks to the official replay download host, what we learned from empirical probes, and how the app decides when download/recovery is offered.

## CDN overview

| Item | Value |
|------|--------|
| Host | `replay-download.strinova.com` |
| Record URL | `https://replay-download.strinova.com/record/{serverFileName}` |

### Local vs server filename

| Location | Pattern | Example |
|----------|---------|---------|
| **Local** (Demos / `.original`) | `{userId}_{gameVersion}_{matchId}_{unixTime}_{randomSeq}.replay` | `6369646_1.9.1.4_655333901112283800_1780329307_ca54jtkp.replay` |
| **CDN** | `{gameVersion}_{matchId}_{unixTime}_{randomSeq}.replay` (no userId) | `1.9.1.4_655333901112283800_1780329307_ca54jtkp.replay` |

The app builds CDN names from parsed [`ReplayEntry`](../Models/ReplayEntry.cs) fields via [`ReplayDownloadUrlBuilder`](../Helpers/ReplayDownloadUrlBuilder.cs). The first `_`-separated segment (user ID) is **not** sent to the CDN.

## Empirical research (2026-06-04 UTC)

### Methodology

1. Merged two real Demos folder listings into **26** unique local filenames ([`Scripts/fixtures/demos-symlinks.txt`](../Scripts/fixtures/demos-symlinks.txt)).
2. Mapped each local name to a CDN record (drop leading `6369646_`).
3. Probed each URL with **HTTP HEAD**, falling back to **GET** with `Range: bytes=0-0` if HEAD returns 405.
4. Computed **match age** from the 10-digit `unixTime` segment in the filename (when valid).

Re-run anytime:

```powershell
.\Scripts\probe-replay-cdn.ps1
```

### Results summary

| Outcome | Count | Notes |
|---------|-------|--------|
| HTTP 200 | 17 | Match ages ~0.6–15.9 days |
| HTTP 404 | 9 | Mostly ~55–116 days; one file with **9-digit** unix (`178011214`) also 404 |

**Observed CDN window (this sample only):** oldest successful match ≈ **15.9 days**; youngest clear 404 (valid unix) ≈ **54.8 days**. Policy may change without notice.

### Full fixture table

| # | Local | CDN record | HEAD | Match age (d) |
|---|-------|------------|------|---------------|
| 1 | `6369646_1.7.4.17_571506954227207704_1770570566_54bpkjp0.replay` | `1.7.4.17_571506954227207704_1770570566_54bpkjp0.replay` | 404 | 115.8 |
| 2 | `6369646_1.7.4.17_573092948207520536_1770755200_mdalfcrb.replay` | `1.7.4.17_573092948207520536_1770755200_mdalfcrb.replay` | 404 | 113.7 |
| 3 | `6369646_1.7.4.17_585816118805974040_1772236372_xqouc6um.replay` | `1.7.4.17_585816118805974040_1772236372_xqouc6um.replay` | 404 | 96.6 |
| 4 | `6369646_1.7.4.17_588683773973941272_1772570211_3qmi3wqn.replay` | `1.7.4.17_588683773973941272_1772570211_3qmi3wqn.replay` | 404 | 92.7 |
| 5 | `6369646_1.7.4.17_592370410692070936_1772999392_nh5x8tal.replay` | `1.7.4.17_592370410692070936_1772999392_nh5x8tal.replay` | 404 | 87.7 |
| 6 | `6369646_1.8.1.2_595904687731952536_1773410836_c2m2j61q.replay` | `1.8.1.2_595904687731952536_1773410836_c2m2j61q.replay` | 404 | 83.0 |
| 7 | `6369646_1.8.1.2_599052623900887448_1773777304_jp61c1dr.replay` | `1.8.1.2_599052623900887448_1773777304_jp61c1dr.replay` | 404 | 78.7 |
| 8 | `6369646_1.8.2.4_616809719509601176_1775844502_fs072kc6.replay` | `1.8.2.4_616809719509601176_1775844502_fs072kc6.replay` | 404 | 54.8 |
| 9 | `6369646_1.8.6.9_645684483990264600_1779205967_xcaims4m.replay` | `1.8.6.9_645684483990264600_1779205967_xcaims4m.replay` | 200 | 15.9 |
| 10 | `6369646_1.8.6.9_645712143588729880_1779209187_oiev9z8t.replay` | `1.8.6.9_645712143588729880_1779209187_oiev9z8t.replay` | 200 | 15.9 |
| 11 | `6369646_1.9.1.2_647206079240915864_1779383104_f820pgil.replay` | `1.9.1.2_647206079240915864_1779383104_f820pgil.replay` | 200 | 13.8 |
| 12–14 | `6369646_1.9.1.3_6480…` (three files) | matching CDN names | 200 | 12.7 |
| 15 | `6369646_1.9.1.3_650969475613510808_1779821221_snm91dwr.replay` | `1.9.1.3_650969475613510808_1779821221_snm91dwr.replay` | 200 | 8.8 |
| 16–19 | four `1.9.1.4_6538…` | matching CDN names | 200 | 4.9 |
| 20 | `6369646_1.9.1.4_653889993244352920_178011214_9p2hgtp9.replay` | `1.9.1.4_653889993244352920_178011214_9p2hgtp9.replay` | 404 | malformed unix |
| 21–23 | three `1.9.1.4_6553…` | matching CDN names | 200 | 2.9 |
| 24 | `6369646_1.9.1.4_656315876676521112_1780443624_rx8u0rnw.replay` | `1.9.1.4_656315876676521112_1780443624_rx8u0rnw.replay` | 200 | 1.6 |
| 25–26 | `657051…`, `657062…` | matching CDN names | 200 | 0.6 |

## App logic (conservative)

Implemented in [`ReplayCloudRetention`](../Helpers/ReplayCloudRetention.cs) and [`ReplayCloudDownloadService`](../Services/ReplayCloudDownloadService.cs).

| Rule | Behavior |
|------|----------|
| `MaxServerAge` | **30 days** — used only to **skip HEAD** when match `unixTime` is a valid **10-digit** timestamp and age exceeds 30 days |
| **HEAD probe** | Authoritative for enabling **Download from server** on the selected library row |
| **GET download** | Streams to `*.download.tmp`, then atomically replaces the target file (original kept until success) |
| **Deleted history** | [`DeletedReplayTracker`](../Services/DeletedReplayTracker.cs) keeps up to **50** snapshots for **30 days** after library delete |
| UI copy | “30 days” refers to the **probe-skip / history window**, not a guarantee that the CDN keeps files that long |

### User flows

1. **Download from server** — Select a parsed original → HEAD on selection → button enabled on 200 → GET replaces that library file (confirm if non-empty).
2. **Recover replay** — Pick from recently deleted list → HEAD on Recover → GET restores into `.original` under the saved local filename.

Recovery does **not** mark files as import-only (same as game-sourced replays).

## When to re-tune

- Re-run `Scripts/probe-replay-cdn.ps1` after game/CDN updates or if users report widespread 404s for recent matches.
- If probes show a stable cutoff (e.g. always 404 after 20 days), consider lowering `MaxServerAge` **only** for probe-skip — keep HEAD as the enable gate.
- Update this document and the fixture list when adding new sample filenames.

## Code references

- [`ReplayDownloadUrlBuilder`](../Helpers/ReplayDownloadUrlBuilder.cs)
- [`ReplayCloudRetention`](../Helpers/ReplayCloudRetention.cs)
- [`ReplayCloudDownloadService`](../Services/ReplayCloudDownloadService.cs)
- [`DeletedReplaySnapshot`](../Models/DeletedReplaySnapshot.cs)
- [`Scripts/probe-replay-cdn.ps1`](../Scripts/probe-replay-cdn.ps1)

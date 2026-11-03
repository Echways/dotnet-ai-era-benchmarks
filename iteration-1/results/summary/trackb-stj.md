# trackb-stj

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=111, b-net11=111.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `stj.cold.first-serialize-reflection` | 31.4210 | 28.1166 (-10.5%) |
| `stj.serialize-reflection.x10k` | 30.4625 | 29.5615 (~-3.0%) |
| `stj.deserialize-reflection.x10k` | 47.3478 | 45.0356 (~-4.9%) |
| `stj.serialize-sourcegen.x10k` | 19.7186 | 18.8040 (~-4.6%) |
| `stj.deserialize-sourcegen.x10k` | 46.1250 | 43.9256 (~-4.8%) |
| `stj.utf8jsonreader-1mb.x10` | 32.5367 | 31.9150 (~-1.9%) |
| `stj.jsondocument-parse-1mb.x10` | 51.8439 | 50.8471 (~-1.9%) |
| `stj.jsonnode-parse-read.x10k` | 42.9190 | 41.3661 (~-3.6%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `stj.cold.first-serialize-reflection` | 338040 | 342384 |
| `stj.serialize-reflection.x10k` | 8560000 | 8560000 |
| `stj.deserialize-reflection.x10k` | 18000000 | 18000000 |
| `stj.serialize-sourcegen.x10k` | 5440000 | 5440000 |
| `stj.deserialize-sourcegen.x10k` | 18000000 | 18000000 |
| `stj.utf8jsonreader-1mb.x10` | 0 | 0 |
| `stj.jsondocument-parse-1mb.x10` | 720 | 720 |
| `stj.jsonnode-parse-read.x10k` | 42560000 | 42560000 |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
```

# trackb-stj

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=2, b-net11=2.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `stj.cold.first-serialize-reflection` | 15.5722 | 13.8767 (-10.9%) |
| `stj.serialize-reflection.x10k` | 14.6801 | 14.1484 (~-3.6%) |
| `stj.deserialize-reflection.x10k` | 23.0687 | 22.0201 (~-4.5%) |
| `stj.serialize-sourcegen.x10k` | 9.4372 | 9.0440 (~-4.2%) |
| `stj.deserialize-sourcegen.x10k` | 22.5444 | 21.9234 (~-2.8%) |
| `stj.utf8jsonreader-1mb.x10` | 15.4665 | 15.7286 (~+1.7%) |
| `stj.jsondocument-parse-1mb.x10` | 24.9037 | 25.1658 (~+1.1%) |
| `stj.jsonnode-parse-read.x10k` | 21.2337 | 20.4472 (~-3.7%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `stj.cold.first-serialize-reflection` | 346884 | 330584 |
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

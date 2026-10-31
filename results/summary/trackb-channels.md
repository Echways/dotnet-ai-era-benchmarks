# trackb-channels

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=111, b-net11=111.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `channels.cold.first-pipeline` | 14.8999 | 14.9995 (~+0.7%) |
| `channels.bounded1024-1p4c.x100k-items` | 40.3396 | 35.6155 (-11.7%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `channels.cold.first-pipeline` | 139672 | 139440 |
| `channels.bounded1024-1p4c.x100k-items` | 1447976 | 564256 |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
```

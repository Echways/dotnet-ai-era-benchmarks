# trackb-channels

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=2, b-net11=2.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `channels.cold.first-pipeline` | 7.5459 | 7.0709 (-6.3%) |
| `channels.bounded1024-1p4c.x100k-items` | 22.2822 | 32.7680 (+47.1%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `channels.cold.first-pipeline` | 139672 | 139440 |
| `channels.bounded1024-1p4c.x100k-items` | 435228 | 302948 |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
2110    channels.bounded102  364.459    602.606    +65.3      21.496     39.846     +85.4
```

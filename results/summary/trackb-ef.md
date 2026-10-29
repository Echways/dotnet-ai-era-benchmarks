# trackb-ef

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=2, b-net11=2.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `ef.cold.open-connection-create-schema` | 27.9682 | 23.2581 (-16.8%) |
| `ef.cold.first-query` | 425.2355 | 453.7668 (+6.7%) |
| `ef.new-context.x1k` | 25.1658 | 25.4280 (~+1.0%) |
| `ef.pooled-context.x1k` | 0.1147 | 0.1116 (~-2.7%) |
| `ef.query-where-orderby-take.x1k` | 109.0519 | 108.0033 (~-1.0%) |
| `ef.save-changes-1000-entities.x1` | 14.6801 | 21.6269 (+47.3%) |
| `ef.detect-changes-10k-tracked.x10` | 22.8065 | 25.4280 (+11.5%) |
| `ef.to-query-string.x1k` | 7.1434 | 7.6022 (+6.4%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `ef.cold.open-connection-create-schema` | 787304 | 728728 |
| `ef.cold.first-query` | 2782232 | 2684140 |
| `ef.new-context.x1k` | 37108216 | 37844216 |
| `ef.pooled-context.x1k` | 0 | 0 |
| `ef.query-where-orderby-take.x1k` | 15500944 | 15348848 |
| `ef.save-changes-1000-entities.x1` | 7862216 | 8142156 |
| `ef.detect-changes-10k-tracked.x10` | 32802560 | 32802360 |
| `ef.to-query-string.x1k` | 7512720 | 7440720 |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1501    ef.cold.first-query  423.235    458.615    +8.4       423.235    458.615    +8.4
```

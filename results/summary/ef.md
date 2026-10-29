# ef

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-7=2, e2-10=2.

| scope | e0-7 (E0) | e2-10 (E2) |
|---|---:|---:|
| `ef.cold.open-connection-create-schema` | 29.5198 | 29.7836 (~+0.9%) |
| `ef.cold.first-query` | 465.0069 | 529.5236 (+13.9%) |
| `ef.new-context.x1k` | 18.0879 | 24.9037 (+37.7%) |
| `ef.pooled-context.x1k` | 0.1116 | 0.1106 (~-0.9%) |
| `ef.query-where-orderby-take.x1k` | 103.8090 | 108.0033 (~+4.0%) |
| `ef.save-changes-1000-entities.x1` | 13.6315 | 13.7626 (~+1.0%) |
| `ef.detect-changes-10k-tracked.x10` | 30.9330 | 22.8065 (-26.3%) |
| `ef.to-query-string.x1k` | 5.7016 | 7.4711 (+31.0%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-7 | e2-10 |
|---|---:|---:|
| `ef.cold.open-connection-create-schema` | 820360 | 787304 |
| `ef.cold.first-query` | 2363108 | 2781824 |
| `ef.new-context.x1k` | 33114872 | 37500216 |
| `ef.pooled-context.x1k` | 0 | 0 |
| `ef.query-where-orderby-take.x1k` | 12709256 | 15500944 |
| `ef.save-changes-1000-entities.x1` | 8438384 | 7862176 |
| `ef.detect-changes-10k-tracked.x10` | 56802160 | 32802160 |
| `ef.to-query-string.x1k` | 4968832 | 7512720 |

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1515    ef.to-query-string.  115.213    152.170    +32.1      6.947      8.389      +20.8
1510    ef.new-context.x1k   412.316    500.303    +21.3      36.495     27.263     -25.3
1501    ef.cold.first-query  468.281    525.238    +12.2      468.281    525.238    +12.2
1512    ef.query-where-orde  2037.180   2152.781   +5.7       104.858    113.246    +8.0
```

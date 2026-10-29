# options

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-7=2, e2-10=2.

| scope | e0-7 (E0) | e2-10 (E2) |
|---|---:|---:|
| `options.cold.first-value` | 15.0443 | 17.4924 (+16.3%) |
| `options.ioptions-value.x10k` | 0.0594 | 0.0573 (~-3.4%) |
| `options.monitor-current-value.x10k` | 0.1321 | 0.1096 (-17.1%) |
| `options.snapshot-new-scope.x1k` | 25.4280 | 24.3794 (~-4.1%) |
| `options.validate-data-annotations.x1k` | 19.9229 | 20.4472 (~+2.6%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-7 | e2-10 |
|---|---:|---:|
| `options.cold.first-value` | 382264 | 368688 |
| `options.ioptions-value.x10k` | 0 | 0 |
| `options.monitor-current-value.x10k` | 0 | 0 |
| `options.snapshot-new-scope.x1k` | 20300000 | 19464000 |
| `options.validate-data-annotations.x1k` | 20844000 | 21233536 |

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1200    options.cold.first-  14.778     17.338     +17.3      14.778     17.338     +17.3
```

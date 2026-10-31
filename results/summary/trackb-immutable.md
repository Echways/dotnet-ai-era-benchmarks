# trackb-immutable

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=111, b-net11=111.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `immutable.cold.first-build-10k` | 14.6222 | 14.4349 (~-1.3%) |
| `immutable.dictionary-build-10k.x10` | 48.8919 | 47.9900 (~-1.8%) |
| `immutable.builder-add-10k.x10` | 49.0652 | 47.5415 (~-3.1%) |
| `immutable.dictionary-lookup.x10k` | 1.8790 | 1.8998 (~+1.1%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `immutable.cold.first-build-10k` | 771728 | 771720 |
| `immutable.dictionary-build-10k.x10` | 6401440 | 6401440 |
| `immutable.builder-add-10k.x10` | 6401680 | 6401680 |
| `immutable.dictionary-lookup.x10k` | 0 | 0 |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
```

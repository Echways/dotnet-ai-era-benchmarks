# trackb-immutable

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=2, b-net11=2.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `immutable.cold.first-build-10k` | 7.3653 | 6.6607 (-9.6%) |
| `immutable.dictionary-build-10k.x10` | 24.6415 | 22.8065 (-7.4%) |
| `immutable.builder-add-10k.x10` | 24.6415 | 22.5444 (-8.5%) |
| `immutable.dictionary-lookup.x10k` | 1.0158 | 1.0240 (~+0.8%) |

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
2212    immutable.dictionar  19.480     21.122     +8.4       1.032      1.343      +30.2
```

# cache

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-7=2, e2-10=2.

| scope | e0-7 (E0) | e2-10 (E2) |
|---|---:|---:|
| `cache.cold.create-and-first-set` | 2.6650 | 2.8717 (+7.8%) |
| `cache.set.x10k` | 2.3593 | 2.0152 (-14.6%) |
| `cache.try-get-hit.x10k` | 0.5530 | 0.5079 (-8.1%) |
| `cache.try-get-miss.x10k` | 0.4342 | 0.3809 (-12.3%) |
| `cache.get-or-create.x10k` | 2.8180 | 2.3921 (-15.1%) |
| `cache.size-limit-compact-half.x1` | 2.4084 | 4.1288 (+71.4%) |
| `cache.parallel8-hit-miss.per-thread-x10k` | 0.7537 | 0.7946 (+5.4%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-7 | e2-10 |
|---|---:|---:|
| `cache.cold.create-and-first-set` | 134200 | 134448 |
| `cache.set.x10k` | 2882360 | 2844992 |
| `cache.try-get-hit.x10k` | 0 | 0 |
| `cache.try-get-miss.x10k` | 0 | 0 |
| `cache.get-or-create.x10k` | 2882360 | 2844992 |
| `cache.size-limit-compact-half.x1` | 918168 | 656176 |
| `cache.parallel8-hit-miss.per-thread-x10k` | 0 | 0 |

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1414    cache.size-limit-co  68.456     91.269     +33.3      7.209      7.078      -1.8
1415    cache.parallel8-hit  125.665    136.663    +8.8       0.967      1.802      +86.4
1400    cache.cold.create-a  2.592      2.807      +8.3       2.592      2.807      +8.3
```

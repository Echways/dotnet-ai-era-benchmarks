# di

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-7=2, e2-10=2.

| scope | e0-7 (E0) | e2-10 (E2) |
|---|---:|---:|
| `di.cold.build100` | 0.2821 | 0.2884 (~+2.2%) |
| `di.cold.first-resolve-depth5` | 3.7902 | 4.3559 (+14.9%) |
| `di.build100.x100` | 1.0076 | 2.2118 (+119.5%) |
| `di.build1000.x10` | 0.7455 | 1.9825 (+165.9%) |
| `di.validate-on-build100.x100` | 7.2745 | 12.5829 (+73.0%) |
| `di.first-resolve-depth5.x100` | 0.5489 | 1.1305 (+106.0%) |
| `di.transient.x10k` | 0.2683 | 0.5530 (+106.1%) |
| `di.scoped.x10k` | 1.0568 | 1.7859 (+69.0%) |
| `di.singleton.x10k` | 0.2273 | 0.4915 (+116.2%) |
| `di.open-generic.x10k` | 0.2970 | 0.5734 (+93.1%) |
| `di.enumerable20.x10k` | 2.2610 | 2.7197 (+20.3%) |
| `di.create-scope-dispose.x10k` | 0.8438 | 1.5729 (+86.4%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-7 | e2-10 |
|---|---:|---:|
| `di.cold.build100` | 156376 | 160192 |
| `di.cold.first-resolve-depth5` | 8480 | 8848 |
| `di.build100.x100` | 2490400 | 2872000 |
| `di.build1000.x10` | 2151840 | 2524480 |
| `di.validate-on-build100.x100` | 7644000 | 10066400 |
| `di.first-resolve-depth5.x100` | 319200 | 336992 |
| `di.transient.x10k` | 240000 | 240000 |
| `di.scoped.x10k` | 0 | 0 |
| `di.singleton.x10k` | 0 | 0 |
| `di.open-generic.x10k` | 240000 | 240000 |
| `di.enumerable20.x10k` | 6640000 | 6640000 |
| `di.create-scope-dispose.x10k` | 1280000 | 1280000 |

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1022    di.singleton.x10k    3.372      10.082     +199.0     0.262      0.623      +137.5
1023    di.open-generic.x10  4.666      12.073     +158.7     0.377      0.967      +156.5
1025    di.create-scope-dis  15.560     38.688     +148.6     1.343      3.473      +158.5
1020    di.transient.x10k    5.019      11.268     +124.5     0.319      0.737      +130.8
1012    di.validate-on-buil  119.671    256.243    +114.1     10.486     15.435     +47.2
1013    di.first-resolve-de  11.957     22.727     +90.1      1.442      1.671      +15.9
1021    di.scoped.x10k       19.916     37.694     +89.3      1.802      2.490      +38.2
1010    di.build100.x100     30.026     42.531     +41.6      3.539      3.015      -14.8
1011    di.build1000.x10     28.170     39.868     +41.5      3.408      3.015      -11.5
1024    di.enumerable20.x10  43.952     55.103     +25.4      2.687      3.277      +22.0
1001    di.cold.first-resol  3.849      4.403      +14.4      3.849      4.403      +14.4
```

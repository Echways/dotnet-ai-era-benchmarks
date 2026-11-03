# options

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-6=111, e0-7=111, e1-8=111, e1-9=111, e2-10=111, e2-11=111.

| scope | e0-6 (E0) | e0-7 (E0) | e1-8 (E1) | e1-9 (E1) | e2-10 (E2) | e2-11 (E2) |
|---|---:|---:|---:|---:|---:|---:|
| `options.cold.first-value` | 37.0490 (+17.1%) | 31.6273 | 35.5132 (+12.3%) | 35.8461 (+13.3%) | 37.3137 (+18.0%) | 40.3785 (+27.7%) |
| `options.ioptions-value.x10k` | 0.0804 (~0.0%) | 0.0804 | 0.0782 (~-2.8%) | 0.0804 (~0.0%) | 0.0804 (~0.0%) | 0.0805 (~+0.1%) |
| `options.monitor-current-value.x10k` | 0.3817 (+137.5%) | 0.1607 | 0.1510 (-6.1%) | 0.1507 (-6.3%) | 0.1465 (-8.8%) | 0.1509 (-6.1%) |
| `options.snapshot-new-scope.x1k` | 56.3041 (+10.3%) | 51.0292 | 48.5591 (~-4.8%) | 48.0240 (-5.9%) | 48.8075 (~-4.4%) | 133.8263 (+162.3%) |
| `options.validate-data-annotations.x1k` | 46.1710 (+17.8%) | 39.2064 | 41.7804 (+6.6%) | 40.2613 (~+2.7%) | 40.5240 (~+3.4%) | 49.0650 (+25.1%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 |
|---|---:|---:|---:|---:|---:|---:|
| `options.cold.first-value` | 455088 | 382264 | 379800 | 381000 | 381024 | 392416 |
| `options.ioptions-value.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `options.monitor-current-value.x10k` | 960000 | 0 | 0 | 0 | 0 | 0 |
| `options.snapshot-new-scope.x1k` | 23808000 | 20280000 | 18264000 | 19464000 | 19504000 | 29472000 |
| `options.validate-data-annotations.x1k` | 29808000 | 20824000 | 21137536 | 21233536 | 21273536 | 22009920 |

## TraceDiff: e0-6 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1211    options.monitor-cur  38.256     89.825     +134.8     0.279      0.623      +123.5
1213    options.validate-da  7789.419   9388.839   +20.5      39.846     49.283     +23.7
1200    options.cold.first-  31.269     37.494     +19.9      31.269     37.494     +19.9
1212    options.snapshot-ne  10378.683  11180.130  +7.7       53.477     58.720     +9.8
```

## TraceDiff: e1-8 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1200    options.cold.first-  31.269     36.065     +15.3      31.269     36.065     +15.3
1213    options.validate-da  7789.419   8322.664   +6.8       39.846     42.992     +7.9
```

## TraceDiff: e1-9 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1200    options.cold.first-  31.269     35.900     +14.8      31.269     35.900     +14.8
1213    options.validate-da  7789.419   8248.802   +5.9       39.846     41.943     +5.3
```

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1200    options.cold.first-  31.269     37.143     +18.8      31.269     37.143     +18.8
1213    options.validate-da  7789.419   8265.026   +6.1       39.846     42.992     +7.9
```

## TraceDiff: e2-11 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1212    options.snapshot-ne  10378.683  27020.461  +160.3     53.477     138.412    +158.8
1200    options.cold.first-  31.269     40.803     +30.5      31.269     40.803     +30.5
1213    options.validate-da  7789.419   9770.725   +25.4      39.846     51.380     +28.9
```

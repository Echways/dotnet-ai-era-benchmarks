# logging

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-6=111, e0-7=111, e1-8=111, e1-9=111, e2-10=111, e2-11=111.

| scope | e0-6 (E0) | e0-7 (E0) | e1-8 (E1) | e1-9 (E1) | e2-10 (E2) | e2-11 (E2) |
|---|---:|---:|---:|---:|---:|---:|
| `logging.cold.factory-create` | 47.7577 (~-3.0%) | 49.2585 | 55.9329 (+13.5%) | 55.9904 (+13.7%) | 57.6519 (+17.0%) | 60.0111 (+21.8%) |
| `logging.cold.first-log` | 8.3720 (~-0.6%) | 8.4253 | 9.1119 (+8.1%) | 9.8294 (+16.7%) | 10.3698 (+23.1%) | 10.3879 (+23.3%) |
| `logging.factory-create.x100` | 2.1689 (~-4.0%) | 2.2599 | 3.2881 (+45.5%) | 3.2947 (+45.8%) | 3.2579 (+44.2%) | 3.4393 (+52.2%) |
| `logging.log-information-3args-enabled.x10k` | 3.8099 (~+1.2%) | 3.7658 | 3.3398 (-11.3%) | 3.2250 (-14.4%) | 3.2789 (-12.9%) | 3.2652 (-13.3%) |
| `logging.log-debug-3args-disabled.x10k` | 0.5395 (~+4.6%) | 0.5159 | 0.5102 (~-1.1%) | 0.5025 (~-2.6%) | 0.5329 (~+3.3%) | 0.5471 (+6.0%) |
| `logging.logger-message-define.x10k` | 3.7115 (~0.0%) | 3.7112 | 2.3620 (-36.4%) | 2.3600 (-36.4%) | 2.3762 (-36.0%) | 2.3871 (-35.7%) |
| `logging.begin-scope.x10k` | 0.7808 (~+2.8%) | 0.7595 | 0.7403 (~-2.5%) | 0.7341 (~-3.4%) | 0.7802 (~+2.7%) | 0.7826 (~+3.0%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 |
|---|---:|---:|---:|---:|---:|---:|
| `logging.cold.factory-create` | 172352 | 173928 | 176432 | 176456 | 176576 | 175392 |
| `logging.cold.first-log` | 4288 | 4312 | 5144 | 5112 | 5112 | 5112 |
| `logging.factory-create.x100` | 1710472 | 1796072 | 2013064 | 2013064 | 2021864 | 1886664 |
| `logging.log-information-3args-enabled.x10k` | 1951120 | 1951120 | 1951120 | 1951120 | 1951120 | 1951120 |
| `logging.log-debug-3args-disabled.x10k` | 960000 | 960000 | 960000 | 960000 | 960000 | 960000 |
| `logging.logger-message-define.x10k` | 1471120 | 1471120 | 991120 | 991120 | 991120 | 991120 |
| `logging.begin-scope.x10k` | 960000 | 960000 | 960000 | 960000 | 1040000 | 1040000 |

## TraceDiff: e0-6 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1313    logging.logger-mess  747.950    888.672    +18.8      4.063      10.224     +151.6
1311    logging.log-informa  810.914    885.468    +9.2       5.243      9.961      +90.0
1314    logging.begin-scope  170.038    179.799    +5.7       1.081      1.606      +48.5
```

## TraceDiff: e1-8 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1310    logging.factory-cre  540.924    837.822    +54.9      5.243      11.010     +110.0
1311    logging.log-informa  810.914    1017.304   +25.5      5.243      12.583     +140.0
1300    logging.cold.factor  49.259     56.033     +13.8      49.259     56.033     +13.8
1312    logging.log-debug-3  126.547    143.176    +13.1      0.868      1.606      +84.9
1314    logging.begin-scope  170.038    189.969    +11.7      1.081      1.802      +66.7
1301    logging.cold.first-  8.398      9.023      +7.4       8.398      9.023      +7.4
```

## TraceDiff: e1-9 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1310    logging.factory-cre  540.924    851.102    +57.3      5.243      11.796     +125.0
1311    logging.log-informa  810.914    972.844    +20.0      5.243      12.583     +140.0
1301    logging.cold.first-  8.398      9.972      +18.7      8.398      9.972      +18.7
1312    logging.log-debug-3  126.547    144.532    +14.2      0.868      1.573      +81.1
1314    logging.begin-scope  170.038    193.737    +13.9      1.081      1.835      +69.7
1300    logging.cold.factor  49.259     55.900     +13.5      49.259     55.900     +13.5
```

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1314    logging.begin-scope  170.038    246.540    +45.0      1.081      3.801      +251.5
1310    logging.factory-cre  540.924    758.756    +40.3      5.243      6.160      +17.5
1301    logging.cold.first-  8.398      10.407     +23.9      8.398      10.407     +23.9
1300    logging.cold.factor  49.259     57.663     +17.1      49.259     57.663     +17.1
1311    logging.log-informa  810.914    932.264    +15.0      5.243      13.107     +150.0
1312    logging.log-debug-3  126.547    136.987    +8.2       0.868      1.573      +81.1
```

## TraceDiff: e2-11 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1310    logging.factory-cre  540.924    797.012    +47.3      5.243      5.374      +2.5
1314    logging.begin-scope  170.038    249.288    +46.6      1.081      3.867      +257.6
1301    logging.cold.first-  8.398      10.432     +24.2      8.398      10.432     +24.2
1300    logging.cold.factor  49.259     59.959     +21.7      49.259     59.959     +21.7
1312    logging.log-debug-3  126.547    141.507    +11.8      0.868      1.606      +84.9
1311    logging.log-informa  810.914    904.454    +11.5      5.243      13.107     +150.0
```

# logging

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-7=2, e2-10=2.

| scope | e0-7 (E0) | e2-10 (E2) |
|---|---:|---:|
| `logging.cold.factory-create` | 22.8954 | 26.2364 (+14.6%) |
| `logging.cold.first-log` | 3.9052 | 4.9614 (+27.0%) |
| `logging.factory-create.x100` | 3.2768 | 5.0463 (+54.0%) |
| `logging.log-information-3args-enabled.x10k` | 3.2113 | 4.2926 (+33.7%) |
| `logging.log-debug-3args-disabled.x10k` | 0.5571 | 0.5816 (~+4.4%) |
| `logging.logger-message-define.x10k` | 3.4406 | 2.8508 (-17.1%) |
| `logging.begin-scope.x10k` | 0.6636 | 1.6384 (+146.9%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-7 | e2-10 |
|---|---:|---:|
| `logging.cold.factory-create` | 173816 | 176576 |
| `logging.cold.first-log` | 4312 | 5112 |
| `logging.factory-create.x100` | 1799272 | 2031464 |
| `logging.log-information-3args-enabled.x10k` | 1951120 | 1951120 |
| `logging.log-debug-3args-disabled.x10k` | 960000 | 960000 |
| `logging.logger-message-define.x10k` | 1471120 | 991120 |
| `logging.begin-scope.x10k` | 960000 | 1040000 |

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1314    logging.begin-scope  14.867     31.199     +109.9     0.918      1.671      +82.1
1310    logging.factory-cre  74.807     105.831    +41.5      5.604      6.685      +19.3
1301    logging.cold.first-  3.870      5.138      +32.8      3.870      5.138      +32.8
1311    logging.log-informa  73.000     88.056     +20.6      4.981      5.505      +10.5
1300    logging.cold.factor  22.575     26.028     +15.3      22.575     26.028     +15.3
```

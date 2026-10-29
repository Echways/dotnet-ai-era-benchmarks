# config

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-7=2, e2-10=2.

| scope | e0-7 (E0) | e2-10 (E2) |
|---|---:|---:|
| `config.cold.build-json2000` | 7.1128 | 7.2934 (~+2.5%) |
| `config.cold.bind-poco50` | 10.0999 | 10.4737 (~+3.7%) |
| `config.build-json2000.x10` | 11.0100 | 11.1411 (~+1.2%) |
| `config.indexer.x10k` | 0.3113 | 0.3277 (+5.3%) |
| `config.get-section-deep.x10k` | 1.4459 | 2.6870 (+85.8%) |
| `config.bind-poco50.x1k` | 31.1951 | 29.6223 (-5.0%) |
| `config.reload-json2000.x10` | 9.8304 | 10.3547 (+5.3%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-7 | e2-10 |
|---|---:|---:|
| `config.cold.build-json2000` | 904872 | 904936 |
| `config.cold.bind-poco50` | 232456 | 243864 |
| `config.build-json2000.x10` | 6365808 | 6365568 |
| `config.indexer.x10k` | 0 | 0 |
| `config.get-section-deep.x10k` | 400000 | 400000 |
| `config.bind-poco50.x1k` | 19232020 | 18412020 |
| `config.reload-json2000.x10` | 6362688 | 6362928 |

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1112    config.get-section-  27.647     53.030     +91.8      3.168      3.867      +22.1
1101    config.cold.bind-po  9.832      10.915     +11.0      9.832      10.915     +11.0
1110    config.build-json20  200.363    217.276    +8.4       12.059     12.321     +2.2
1100    config.cold.build-j  6.965      7.467      +7.2       6.965      7.467      +7.2
1114    config.reload-json2  188.894    200.601    +6.2       12.583     11.534     -8.3
```

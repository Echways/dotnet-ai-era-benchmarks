# ef

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-6=111, e0-7=111, e1-8=111, e1-9=111, e2-10=111.

| scope | e0-6 (E0) | e0-7 (E0) | e1-8 (E1) | e1-9 (E1) | e2-10 (E2) |
|---|---:|---:|---:|---:|---:|
| `ef.cold.open-connection-create-schema` | 60.6320 (~-2.3%) | 62.0346 | 62.0809 (~+0.1%) | 62.2828 (~+0.4%) | 62.5048 (~+0.8%) |
| `ef.cold.first-query` | 927.9437 (-5.6%) | 982.7542 | 1068.3759 (+8.7%) | 1118.0583 (+13.8%) | 1141.1175 (+16.1%) |
| `ef.new-context.x1k` | 34.7599 (~-3.6%) | 36.0609 | 38.6164 (+7.1%) | 44.6411 (+23.8%) | 50.3560 (+39.6%) |
| `ef.pooled-context.x1k` | 0.0904 (-18.5%) | 0.1109 | 0.1191 (+7.3%) | 0.1223 (+10.2%) | 0.1223 (+10.3%) |
| `ef.query-where-orderby-take.x1k` | 219.6478 (~+0.3%) | 218.9721 | 220.0558 (~+0.5%) | 222.1360 (~+1.4%) | 221.9295 (~+1.4%) |
| `ef.save-changes-1000-entities.x1` | 28.0295 (~+4.5%) | 26.8098 | 26.4736 (~-1.3%) | 26.1007 (~-2.6%) | 27.3113 (~+1.9%) |
| `ef.detect-changes-10k-tracked.x10` | 66.9519 (+10.5%) | 60.5747 | 43.2755 (-28.6%) | 35.7923 (-40.9%) | 42.5226 (-29.8%) |
| `ef.to-query-string.x1k` | 11.6369 (~+3.2%) | 11.2708 | 11.4238 (~+1.4%) | 14.0498 (+24.7%) | 14.1817 (+25.8%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 |
|---|---:|---:|---:|---:|---:|
| `ef.cold.open-connection-create-schema` | 812200 | 820360 | 763120 | 787120 | 787304 |
| `ef.cold.first-query` | 2154416 | 2337304 | 2677616 | 2791736 | 2782872 |
| `ef.new-context.x1k` | 32176176 | 33112176 | 35482872 | 36284216 | 37500216 |
| `ef.pooled-context.x1k` | 0 | 0 | 0 | 0 | 0 |
| `ef.query-where-orderby-take.x1k` | 12685752 | 12709752 | 12893248 | 15093248 | 15501248 |
| `ef.save-changes-1000-entities.x1` | 10693088 | 8438344 | 8054136 | 7758136 | 7862136 |
| `ef.detect-changes-10k-tracked.x10` | 82402160 | 56802160 | 45602160 | 32802160 | 32802160 |
| `ef.to-query-string.x1k` | 4912832 | 4968832 | 5152664 | 7280720 | 7512720 |

## TraceDiff: e0-6 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1514    ef.detect-changes-1  12889.585  13659.569  +6.0       68.644     73.027     +6.4
```

## TraceDiff: e1-8 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1511    ef.pooled-context.x  40.538     44.451     +9.7       0.221      0.254      +14.8
1501    ef.cold.first-query  981.568    1069.508   +9.0       981.568    1069.508   +9.0
1510    ef.new-context.x1k   7335.198   7753.329   +5.7       38.797     40.894     +5.4
```

## TraceDiff: e1-9 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1515    ef.to-query-string.  2250.437   2878.056   +27.9      11.796     14.942     +26.7
1510    ef.new-context.x1k   7335.198   9103.143   +24.1      38.797     47.186     +21.6
1501    ef.cold.first-query  981.568    1116.967   +13.8      981.568    1116.967   +13.8
```

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1510    ef.new-context.x1k   7335.198   10228.549  +39.4      38.797     54.526     +40.5
1515    ef.to-query-string.  2250.437   2871.695   +27.6      11.796     15.204     +28.9
1501    ef.cold.first-query  981.568    1140.272   +16.2      981.568    1140.272   +16.2
```

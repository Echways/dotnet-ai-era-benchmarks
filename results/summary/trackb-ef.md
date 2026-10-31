# trackb-ef

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=111, b-net11=111.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `ef.cold.open-connection-create-schema` | 55.8590 | 46.1910 (-17.3%) |
| `ef.cold.first-query` | 905.9335 | 982.4637 (+8.4%) |
| `ef.new-context.x1k` | 49.1935 | 51.1084 (~+3.9%) |
| `ef.pooled-context.x1k` | 0.1201 | 0.1190 (~-0.9%) |
| `ef.query-where-orderby-take.x1k` | 222.6534 | 224.4041 (~+0.8%) |
| `ef.save-changes-1000-entities.x1` | 27.3133 | 27.2252 (~-0.3%) |
| `ef.detect-changes-10k-tracked.x10` | 42.5219 | 47.6412 (+12.0%) |
| `ef.to-query-string.x1k` | 13.8688 | 13.9537 (~+0.6%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `ef.cold.open-connection-create-schema` | 787304 | 728728 |
| `ef.cold.first-query` | 2782592 | 2733120 |
| `ef.new-context.x1k` | 37108216 | 37844216 |
| `ef.pooled-context.x1k` | 0 | 0 |
| `ef.query-where-orderby-take.x1k` | 15501248 | 15348752 |
| `ef.save-changes-1000-entities.x1` | 7862136 | 8086096 |
| `ef.detect-changes-10k-tracked.x10` | 32802160 | 32802560 |
| `ef.to-query-string.x1k` | 7512720 | 7440720 |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1514    ef.detect-changes-1  8461.005   9546.689   +12.8      46.137     52.429     +13.6
1501    ef.cold.first-query  904.390    981.915    +8.6       904.390    981.915    +8.6
1510    ef.new-context.x1k   9976.952   10804.285  +8.3       53.477     57.672     +7.8
```

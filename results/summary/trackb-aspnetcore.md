# trackb-aspnetcore

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=111, b-net11=111.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `aspnetcore.cold.start-kestrel` | 208.8915 | 109.0608 (-47.8%) |
| `aspnetcore.cold.first-request` | 1813.7630 | 1907.2598 (+5.2%) |
| `aspnetcore.hello.batch400` | 100.5248 | 100.5817 (~+0.1%) |
| `aspnetcore.json.batch400` | 100.5292 | 100.5828 (~+0.1%) |
| `aspnetcore.routes500.batch400` | 100.5339 | 100.5888 (~+0.1%) |
| `aspnetcore.hello.request` | 0.0818 | 0.1597 (+95.3%) |
| `aspnetcore.json.request` | 0.0841 | 0.1622 (+93.0%) |
| `aspnetcore.routes500.request` | 0.0863 | 0.1686 (+95.3%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `aspnetcore.cold.start-kestrel` | 7972928 | 2111592 |
| `aspnetcore.cold.first-request` | 5080 | 4880 |
| `aspnetcore.hello.batch400` | 1634864 | 1671984 |
| `aspnetcore.json.batch400` | 1728088 | 1764776 |
| `aspnetcore.routes500.batch400` | 1765696 | 1802408 |
| `aspnetcore.hello.request` | – | – |
| `aspnetcore.json.request` | – | – |
| `aspnetcore.routes500.request` | – | – |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
2322    aspnetcore.routes50  9169.543   12951.399  +41.2      0.188      0.303      +60.9
2320    aspnetcore.hello.re  8775.906   12141.219  +38.3      0.184      0.295      +60.0
2321    aspnetcore.json.req  9005.108   12458.221  +38.3      0.184      0.295      +60.0
2301    aspnetcore.cold.fir  1815.309   1907.260   +5.1       1815.309   1907.260   +5.1
```

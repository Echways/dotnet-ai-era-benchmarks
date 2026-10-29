# trackb-aspnetcore

Median over processes of the per-process median batch time (ms). Change is relative to `b-net10`; changes under 5% are marked as noise (~). Runs per point: b-net10=2, b-net11=2.

| scope | b-net10 (E2) | b-net11 (E2) |
|---|---:|---:|
| `aspnetcore.cold.start-kestrel` | 104.4521 | 59.3957 (-43.1%) |
| `aspnetcore.cold.first-request` | 845.1599 | 897.9061 (+6.2%) |
| `aspnetcore.hello.batch400` | 100.6633 | 101.7119 (~+1.0%) |
| `aspnetcore.json.batch400` | 100.6633 | 100.6633 (~0.0%) |
| `aspnetcore.routes500.batch400` | 100.6633 | 100.6633 (~0.0%) |
| `aspnetcore.hello.request` | 0.0748 | 0.1352 (+80.8%) |
| `aspnetcore.json.request` | 0.0778 | 0.1393 (+78.9%) |
| `aspnetcore.routes500.request` | 0.0788 | 0.1454 (+84.4%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | b-net10 | b-net11 |
|---|---:|---:|
| `aspnetcore.cold.start-kestrel` | 5888936 | 2121152 |
| `aspnetcore.cold.first-request` | 5304 | 4880 |
| `aspnetcore.hello.batch400` | 1634404 | 1670388 |
| `aspnetcore.json.batch400` | 1727328 | 1763916 |
| `aspnetcore.routes500.batch400` | 1765224 | 1802612 |
| `aspnetcore.hello.request` | – | – |
| `aspnetcore.json.request` | – | – |
| `aspnetcore.routes500.request` | – | – |

## TraceDiff: b-net11 vs b-net10 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
2322    aspnetcore.routes50  746.518    1260.607   +68.9      0.164      0.279      +70.0
2320    aspnetcore.hello.re  662.756    1113.043   +67.9      0.156      0.229      +47.4
2321    aspnetcore.json.req  724.189    1164.096   +60.7      0.172      0.258      +50.0
```

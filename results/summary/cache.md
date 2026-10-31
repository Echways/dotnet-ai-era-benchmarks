# cache

Median over processes of the per-process median batch time (ms). Change is relative to `e0-7`; changes under 5% are marked as noise (~). Runs per point: e0-6=111, e0-7=111, e1-8=111, e1-9=111, e2-10=111, e2-11=111.

| scope | e0-6 (E0) | e0-7 (E0) | e1-8 (E1) | e1-9 (E1) | e2-10 (E2) | e2-11 (E2) |
|---|---:|---:|---:|---:|---:|---:|
| `cache.cold.create-and-first-set` | 13.6929 (+149.2%) | 5.4955 | 5.8203 (+5.9%) | 5.9767 (+8.8%) | 6.0826 (+10.7%) | 6.9869 (+27.1%) |
| `cache.set.x10k` | 8.8563 (+237.7%) | 2.6224 | 2.3219 (-11.5%) | 2.4803 (-5.4%) | 3.2940 (+25.6%) | 3.1352 (+19.6%) |
| `cache.try-get-hit.x10k` | 1.1935 (+11.4%) | 1.0718 | 0.8982 (-16.2%) | 0.8989 (-16.1%) | 0.9196 (-14.2%) | 0.9173 (-14.4%) |
| `cache.try-get-miss.x10k` | 0.8977 (~+2.7%) | 0.8738 | 0.7295 (-16.5%) | 0.7301 (-16.5%) | 0.6763 (-22.6%) | 0.6785 (-22.4%) |
| `cache.get-or-create.x10k` | 8.7478 (+174.4%) | 3.1879 | 2.7702 (-13.1%) | 2.7186 (-14.7%) | 4.0501 (+27.0%) | 4.2360 (+32.9%) |
| `cache.size-limit-compact-half.x1` | 3.4185 (~-1.8%) | 3.4816 | 3.1807 (-8.6%) | 3.2003 (-8.1%) | 2.8397 (-18.4%) | 2.8457 (-18.3%) |
| `cache.parallel8-hit-miss.per-thread-x10k` | 1.5698 (+10.3%) | 1.4238 | 1.3571 (~-4.7%) | 1.3564 (~-4.7%) | 1.3516 (-5.1%) | 1.3533 (~-4.9%) |

Allocated bytes per batch (median over processes of the per-process median):

| scope | e0-6 | e0-7 | e1-8 | e1-9 | e2-10 | e2-11 |
|---|---:|---:|---:|---:|---:|---:|
| `cache.cold.create-and-first-set` | 136040 | 134200 | 134432 | 134432 | 134448 | 134584 |
| `cache.set.x10k` | 4046816 | 2848304 | 2844992 | 2844992 | 2844992 | 2844992 |
| `cache.try-get-hit.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `cache.try-get-miss.x10k` | 0 | 0 | 0 | 0 | 0 | 0 |
| `cache.get-or-create.x10k` | 4046816 | 2848304 | 2844992 | 2844992 | 2844992 | 2844992 |
| `cache.size-limit-compact-half.x1` | 918288 | 918104 | 918288 | 918288 | 656176 | 656176 |
| `cache.parallel8-hit-miss.per-thread-x10k` | 0 | 0 | 0 | 0 | 0 | 0 |

## TraceDiff: e0-6 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1400    cache.cold.create-a  5.541      13.783     +148.8     5.541      13.783     +148.8
1410    cache.set.x10k       1331.629   1981.833   +48.8      19.923     22.544     +13.2
1413    cache.get-or-create  1583.398   1948.265   +23.0      22.544     30.409     +34.9
1411    cache.try-get-hit.x  226.457    246.672    +8.9       1.343      1.442      +7.3
1412    cache.try-get-miss.  179.093    194.560    +8.6       0.918      1.245      +35.7
```

## TraceDiff: e1-8 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1400    cache.cold.create-a  5.541      5.911      +6.7       5.541      5.911      +6.7
```

## TraceDiff: e1-9 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1400    cache.cold.create-a  5.541      6.096      +10.0      5.541      6.096      +10.0
```

## TraceDiff: e2-10 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1400    cache.cold.create-a  5.541      6.174      +11.4      5.541      6.174      +11.4
```

## TraceDiff: e2-11 vs e0-7 (representative runs)

```
Id      Name                 Base ms    Cur ms     Total%     Base p95   Cur p95    p95%
------  -------------------  ---------  ---------  ---------  ---------  ---------  ---------
1400    cache.cold.create-a  5.541      7.445      +34.4      5.541      7.445      +34.4
```

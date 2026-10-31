# Statistics

Unit of observation: one process. Each scope has two metrics per process: the exact median and the mean of its batch durations. Each round counts equally: changes are computed inside a round, then combined as a geometric mean. The 95% CI resamples rounds and then processes (10,000 resamples); it is only as wide as the rounds available. p is a Mann-Whitney U per round, combined with Stouffer's method and adjusted with Benjamini-Hochberg across all comparisons.

Metric verdict: `noise` if the CI lies inside ±5%; `faster`/`slower` if the CI lies wholly beyond ±5%, no round points the other way and adjusted p < 0.05; otherwise `inconclusive`. Overall verdict: scopes with p75/p25 >= 1.5 at the point or the baseline are multimodal and judged by the mean; otherwise the median decides, and `mixed` means the mean's estimate moved at least 5% the other way. `pre-spec` is the rule fixed before the data was seen (see `docs/methodology.md`, Deviations).

## Verdicts per point

| point | faster | slower | mixed | noise | inconclusive |
|---|---:|---:|---:|---:|---:|
| b-net11 | 2 | 5 | 0 | 15 | 8 |
| e0-6 | 2 | 2 | 0 | 3 | 39 |
| e1-8 | 7 | 0 | 0 | 1 | 38 |
| e1-9 | 8 | 0 | 1 | 1 | 36 |
| e2-10 | 8 | 0 | 0 | 1 | 37 |
| e2-11 | 5 | 12 | 0 | 7 | 14 |

## Mean moved, median did not

| scope | point | median change | mean change |
|---|---|---:|---:|

## All scopes

| scenario | scope | point | n (rounds) | median ms [95% CI] | median change [95% CI] | mean ms | mean change [95% CI] | p75/p25 | rounds agreeing | verdict | pre-spec |
|---|---|---|---:|---|---|---:|---|---:|---:|---|---|
| cache | `cache.cold.create-and-first-set` | e0-6 | 111 (30) | 10.6672 [9.4622, 12.1597] | +147.6% [+135.3, +158.7] | 10.6672 | +147.6% [+135.3, +158.9] | 1.00 | 30/30 | slower | slower |
| cache | `cache.cold.create-and-first-set` | e0-7 | 111 (30) | 4.3089 [3.7946, 4.8841] | - | 4.3089 | - | 1.00 | - | baseline | - |
| cache | `cache.cold.create-and-first-set` | e1-8 | 111 (30) | 4.5755 [4.0247, 5.2614] | +6.2% [+1.2, +11.1] | 4.5755 | +6.2% [+1.2, +11.2] | 1.00 | 28/30 | inconclusive | slower |
| cache | `cache.cold.create-and-first-set` | e1-9 | 111 (30) | 4.6305 [4.0584, 5.2812] | +7.5% [+1.6, +11.8] | 4.6305 | +7.5% [+1.6, +11.9] | 1.00 | 28/30 | inconclusive | slower |
| cache | `cache.cold.create-and-first-set` | e2-10 | 111 (30) | 4.7890 [4.2085, 5.4252] | +11.1% [+6.1, +15.8] | 4.7890 | +11.1% [+6.1, +15.6] | 1.00 | 29/30 | inconclusive | slower |
| cache | `cache.cold.create-and-first-set` | e2-11 | 111 (30) | 5.4435 [4.8015, 6.1967] | +26.3% [+24.7, +29.0] | 5.4435 | +26.3% [+24.7, +28.9] | 1.00 | 30/30 | slower | slower |
| cache | `cache.set.x10k` | e0-6 | 111 (30) | 7.2286 [6.5570, 8.3326] | +220.0% [+176.9, +244.8] | 8.4304 | +53.1% [+45.3, +62.4] | 4.87 | 29/30 | inconclusive (by mean) | slower |
| cache | `cache.set.x10k` | e0-7 | 111 (30) | 2.2589 [2.0328, 2.7777] | - | 5.5059 | - | 3.69 | - | baseline | - |
| cache | `cache.set.x10k` | e1-8 | 111 (30) | 2.1192 [1.8323, 2.6277] | -6.2% [-20.4, +6.5] | 4.8666 | -11.6% [-16.5, -7.5] | 4.09 | 24/30 | faster (by mean) | inconclusive |
| cache | `cache.set.x10k` | e1-9 | 111 (30) | 2.0892 [1.7774, 2.5813] | -7.5% [-21.2, +3.5] | 4.7884 | -13.0% [-17.8, -8.7] | 4.00 | 29/30 | faster (by mean) | inconclusive |
| cache | `cache.set.x10k` | e2-10 | 111 (30) | 2.8517 [2.5650, 3.2184] | +26.2% [+8.6, +35.8] | 5.1369 | -6.7% [-11.4, -3.4] | 5.40 | 14/30 | inconclusive (by mean) | slower |
| cache | `cache.set.x10k` | e2-11 | 111 (30) | 2.5785 [2.2765, 2.9126] | +14.1% [-2.3, +19.3] | 5.1414 | -6.6% [-9.7, -4.5] | 5.32 | 17/30 | inconclusive (by mean) | slower |
| cache | `cache.try-get-hit.x10k` | e0-6 | 111 (30) | 0.9857 [0.8865, 1.0996] | +12.8% [+8.3, +18.9] | 0.9980 | +7.1% [+3.0, +11.8] | 1.21 | 28/30 | inconclusive | slower |
| cache | `cache.try-get-hit.x10k` | e0-7 | 111 (30) | 0.8740 [0.7642, 0.9831] | - | 0.9318 | - | 1.24 | - | baseline | - |
| cache | `cache.try-get-hit.x10k` | e1-8 | 111 (30) | 0.7064 [0.6199, 0.8081] | -19.2% [-22.8, -14.9] | 0.7629 | -18.1% [-21.9, -14.5] | 1.23 | 30/30 | faster | faster |
| cache | `cache.try-get-hit.x10k` | e1-9 | 111 (30) | 0.6997 [0.6159, 0.7964] | -19.9% [-24.1, -16.2] | 0.7508 | -19.4% [-23.4, -16.1] | 1.23 | 30/30 | faster | faster |
| cache | `cache.try-get-hit.x10k` | e2-10 | 111 (30) | 0.7457 [0.6592, 0.8402] | -14.7% [-17.9, -11.5] | 0.7943 | -14.8% [-18.0, -12.1] | 1.22 | 29/30 | faster | faster |
| cache | `cache.try-get-hit.x10k` | e2-11 | 111 (30) | 0.7443 [0.6568, 0.8343] | -14.8% [-17.1, -12.6] | 0.7923 | -15.0% [-17.0, -12.9] | 1.22 | 29/30 | inconclusive | faster |
| cache | `cache.try-get-miss.x10k` | e0-6 | 111 (30) | 0.6909 [0.6097, 0.7867] | +0.6% [-4.2, +4.5] | 0.7307 | +2.7% [-2.0, +6.4] | 1.04 | 2/30 | noise | noise |
| cache | `cache.try-get-miss.x10k` | e0-7 | 111 (30) | 0.6867 [0.6057, 0.7773] | - | 0.7117 | - | 1.04 | - | baseline | - |
| cache | `cache.try-get-miss.x10k` | e1-8 | 111 (30) | 0.5612 [0.4957, 0.6362] | -18.3% [-22.2, -15.5] | 0.5915 | -16.9% [-21.2, -13.9] | 1.05 | 30/30 | faster | faster |
| cache | `cache.try-get-miss.x10k` | e1-9 | 111 (30) | 0.5602 [0.4937, 0.6343] | -18.4% [-22.5, -15.9] | 0.5871 | -17.5% [-21.8, -15.0] | 1.05 | 30/30 | faster | faster |
| cache | `cache.try-get-miss.x10k` | e2-10 | 111 (30) | 0.5234 [0.4619, 0.5913] | -23.8% [-27.1, -21.5] | 0.5583 | -21.6% [-25.0, -19.3] | 1.07 | 30/30 | faster | faster |
| cache | `cache.try-get-miss.x10k` | e2-11 | 111 (30) | 0.5357 [0.4719, 0.6052] | -22.0% [-22.7, -21.4] | 0.5680 | -20.2% [-21.4, -19.2] | 1.08 | 30/30 | faster | faster |
| cache | `cache.get-or-create.x10k` | e0-6 | 111 (30) | 6.9233 [6.1182, 7.8078] | +164.2% [+148.2, +174.5] | 8.0315 | +25.1% [+17.6, +33.6] | 3.62 | 29/30 | inconclusive (by mean) | slower |
| cache | `cache.get-or-create.x10k` | e0-7 | 111 (30) | 2.6206 [2.3174, 2.9811] | - | 6.4181 | - | 4.27 | - | baseline | - |
| cache | `cache.get-or-create.x10k` | e1-8 | 111 (30) | 2.2652 [1.9756, 2.6001] | -13.6% [-19.3, -9.1] | 5.5041 | -14.2% [-18.5, -10.0] | 4.43 | 29/30 | faster (by mean) | faster |
| cache | `cache.get-or-create.x10k` | e1-9 | 111 (30) | 2.2305 [1.9476, 2.5480] | -14.9% [-20.0, -11.0] | 5.4661 | -14.8% [-19.2, -10.9] | 4.42 | 30/30 | faster (by mean) | faster |
| cache | `cache.get-or-create.x10k` | e2-10 | 111 (30) | 3.4935 [3.0255, 3.9310] | +33.3% [+20.6, +43.4] | 5.8695 | -8.5% [-12.5, -4.3] | 4.97 | 22/30 | inconclusive (by mean) | slower |
| cache | `cache.get-or-create.x10k` | e2-11 | 111 (30) | 3.4452 [3.0557, 4.0025] | +31.5% [+24.7, +44.2] | 5.9844 | -6.8% [-9.0, -3.5] | 4.98 | 21/30 | inconclusive (by mean) | slower |
| cache | `cache.size-limit-compact-half.x1` | e0-6 | 111 (30) | 2.6561 [2.3516, 3.0222] | -6.9% [-11.2, -1.9] | 3.2233 | -5.0% [-9.4, +2.3] | 1.18 | 13/30 | inconclusive | noise |
| cache | `cache.size-limit-compact-half.x1` | e0-7 | 111 (30) | 2.8524 [2.5017, 3.2106] | - | 3.3945 | - | 1.44 | - | baseline | - |
| cache | `cache.size-limit-compact-half.x1` | e1-8 | 111 (30) | 2.6449 [2.3687, 3.0061] | -7.3% [-12.5, +0.1] | 3.2208 | -5.1% [-10.3, -0.2] | 1.45 | 20/30 | inconclusive | faster |
| cache | `cache.size-limit-compact-half.x1` | e1-9 | 111 (30) | 2.6420 [2.3649, 2.9347] | -7.4% [-12.9, -2.0] | 3.2137 | -5.3% [-10.1, -1.5] | 1.43 | 20/30 | inconclusive | faster |
| cache | `cache.size-limit-compact-half.x1` | e2-10 | 111 (30) | 2.1579 [1.8901, 2.4541] | -24.3% [-28.1, -20.6] | 2.4801 | -26.9% [-30.5, -23.5] | 1.08 | 30/30 | faster | faster |
| cache | `cache.size-limit-compact-half.x1` | e2-11 | 111 (30) | 2.2040 [1.9307, 2.5096] | -22.7% [-25.1, -20.0] | 2.5587 | -24.6% [-26.5, -22.3] | 1.08 | 30/30 | faster | faster |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e0-6 | 111 (30) | 1.2214 [1.0806, 1.3793] | +8.2% [+3.3, +12.3] | 1.3135 | +7.1% [+1.9, +11.1] | 1.16 | 29/30 | inconclusive | slower |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e0-7 | 111 (30) | 1.1285 [0.9957, 1.2747] | - | 1.2266 | - | 1.15 | - | baseline | - |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e1-8 | 111 (30) | 1.0467 [0.9247, 1.1886] | -7.3% [-11.8, -3.8] | 1.1353 | -7.4% [-12.4, -3.4] | 1.14 | 17/30 | inconclusive | noise |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e1-9 | 111 (30) | 1.0437 [0.9225, 1.1842] | -7.5% [-12.0, -4.3] | 1.1290 | -8.0% [-13.1, -4.9] | 1.14 | 20/30 | inconclusive | noise |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e2-10 | 111 (30) | 1.0548 [0.9285, 1.1938] | -6.5% [-10.6, -3.4] | 1.1370 | -7.3% [-11.9, -4.4] | 1.13 | 16/30 | inconclusive | faster |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e2-11 | 111 (30) | 1.0630 [0.9361, 1.2071] | -5.8% [-7.0, -4.3] | 1.1433 | -6.8% [-9.0, -5.2] | 1.13 | 20/30 | inconclusive | noise |
| config | `config.cold.build-json2000` | e0-6 | 111 (30) | 11.5318 [10.2272, 13.0652] | -0.4% [-6.3, +3.9] | 11.5318 | -0.4% [-6.4, +4.1] | 1.00 | 2/30 | inconclusive | noise |
| config | `config.cold.build-json2000` | e0-7 | 111 (30) | 11.5817 [10.2935, 13.1209] | - | 11.5817 | - | 1.00 | - | baseline | - |
| config | `config.cold.build-json2000` | e1-8 | 111 (30) | 11.6349 [10.2702, 13.1517] | +0.5% [-5.5, +4.6] | 11.6349 | +0.5% [-5.6, +4.6] | 1.00 | 6/30 | inconclusive | noise |
| config | `config.cold.build-json2000` | e1-9 | 111 (30) | 11.5184 [10.1895, 13.0434] | -0.5% [-6.8, +3.1] | 11.5184 | -0.5% [-6.9, +3.1] | 1.00 | 4/30 | inconclusive | noise |
| config | `config.cold.build-json2000` | e2-10 | 111 (30) | 11.6501 [10.2852, 13.2201] | +0.6% [-5.7, +5.4] | 11.6501 | +0.6% [-5.6, +5.0] | 1.00 | 3/30 | inconclusive | noise |
| config | `config.cold.build-json2000` | e2-11 | 111 (30) | 20.3687 [17.9844, 23.0325] | +75.9% [+68.9, +79.6] | 20.3687 | +75.9% [+69.0, +79.5] | 1.00 | 30/30 | slower | slower |
| config | `config.cold.bind-poco50` | e0-6 | 111 (30) | 21.9689 [19.3277, 25.1228] | +29.6% [+22.3, +34.7] | 21.9689 | +29.6% [+22.0, +34.7] | 1.00 | 29/30 | inconclusive | slower |
| config | `config.cold.bind-poco50` | e0-7 | 111 (30) | 16.9523 [15.0163, 19.4059] | - | 16.9523 | - | 1.00 | - | baseline | - |
| config | `config.cold.bind-poco50` | e1-8 | 111 (30) | 16.1294 [14.2288, 18.5197] | -4.9% [-10.4, -0.3] | 16.1294 | -4.9% [-10.3, -0.3] | 1.00 | 4/30 | inconclusive | noise |
| config | `config.cold.bind-poco50` | e1-9 | 111 (30) | 16.2032 [14.2680, 18.7200] | -4.4% [-10.6, +0.5] | 16.2032 | -4.4% [-10.4, +0.5] | 1.00 | 4/30 | inconclusive | noise |
| config | `config.cold.bind-poco50` | e2-10 | 111 (30) | 16.7322 [14.7995, 19.3904] | -1.3% [-7.5, +6.7] | 16.7322 | -1.3% [-7.5, +6.8] | 1.00 | 2/30 | inconclusive | noise |
| config | `config.cold.bind-poco50` | e2-11 | 111 (30) | 17.5695 [15.5426, 19.8869] | +3.6% [-0.7, +5.7] | 17.5695 | +3.6% [-0.7, +5.8] | 1.00 | 6/30 | inconclusive | noise |
| config | `config.build-json2000.x10` | e0-6 | 111 (30) | 7.4659 [6.7044, 8.3889] | -2.0% [-5.4, +1.2] | 7.5990 | -2.6% [-5.8, +0.4] | 1.18 | 4/30 | inconclusive | noise |
| config | `config.build-json2000.x10` | e0-7 | 111 (30) | 7.6193 [6.7891, 8.5186] | - | 7.8025 | - | 1.19 | - | baseline | - |
| config | `config.build-json2000.x10` | e1-8 | 111 (30) | 7.7116 [6.8739, 8.6217] | +1.2% [-3.4, +4.7] | 7.5630 | -3.1% [-7.1, +1.3] | 1.20 | 3/30 | noise | noise |
| config | `config.build-json2000.x10` | e1-9 | 111 (30) | 7.9231 [7.0585, 8.8730] | +4.0% [-0.7, +7.1] | 8.0488 | +3.2% [-1.2, +6.0] | 1.32 | 23/30 | inconclusive | slower |
| config | `config.build-json2000.x10` | e2-10 | 111 (30) | 8.0345 [7.1484, 9.0071] | +5.4% [+0.8, +8.6] | 8.1863 | +4.9% [+0.9, +7.8] | 1.31 | 27/30 | inconclusive | slower |
| config | `config.build-json2000.x10` | e2-11 | 111 (30) | 7.7921 [6.9382, 8.7193] | +2.3% [+1.0, +3.5] | 7.9056 | +1.3% [-0.1, +2.3] | 1.19 | 3/30 | noise | noise |
| config | `config.indexer.x10k` | e0-6 | 111 (30) | 0.4416 [0.3897, 0.5044] | -2.7% [-7.2, +0.9] | 0.4682 | -2.2% [-6.6, +1.1] | 1.06 | 4/30 | inconclusive | noise |
| config | `config.indexer.x10k` | e0-7 | 111 (30) | 0.4537 [0.3994, 0.5153] | - | 0.4785 | - | 1.06 | - | baseline | - |
| config | `config.indexer.x10k` | e1-8 | 111 (30) | 0.4439 [0.3910, 0.5057] | -2.2% [-7.2, +2.0] | 0.4689 | -2.0% [-6.8, +2.1] | 1.06 | 2/30 | inconclusive | noise |
| config | `config.indexer.x10k` | e1-9 | 111 (30) | 0.4434 [0.3900, 0.5025] | -2.3% [-7.5, +1.1] | 0.4671 | -2.4% [-7.3, +1.0] | 1.06 | 2/30 | inconclusive | noise |
| config | `config.indexer.x10k` | e2-10 | 111 (30) | 0.4466 [0.3932, 0.5063] | -1.6% [-6.2, +1.9] | 0.4739 | -1.0% [-5.5, +2.3] | 1.07 | 2/30 | inconclusive | noise |
| config | `config.indexer.x10k` | e2-11 | 111 (30) | 0.4483 [0.3939, 0.5088] | -1.2% [-2.8, +0.2] | 0.4751 | -0.7% [-2.2, +0.4] | 1.06 | 4/30 | noise | noise |
| config | `config.get-section-deep.x10k` | e0-6 | 111 (30) | 0.3082 [0.2742, 0.3477] | -2.4% [-6.7, +0.8] | 0.6009 | -3.3% [-9.0, +2.5] | 1.13 | 2/30 | inconclusive | noise |
| config | `config.get-section-deep.x10k` | e0-7 | 111 (30) | 0.3158 [0.2808, 0.3536] | - | 0.6216 | - | 1.10 | - | baseline | - |
| config | `config.get-section-deep.x10k` | e1-8 | 111 (30) | 0.3125 [0.2782, 0.3503] | -1.1% [-5.4, +3.2] | 0.6181 | -0.6% [-5.6, +5.9] | 1.12 | 1/30 | inconclusive | noise |
| config | `config.get-section-deep.x10k` | e1-9 | 111 (30) | 0.3106 [0.2770, 0.3488] | -1.6% [-6.0, +2.0] | 0.5375 | -13.5% [-17.7, -7.6] | 1.12 | 2/30 | inconclusive | noise |
| config | `config.get-section-deep.x10k` | e2-10 | 111 (30) | 0.3084 [0.2742, 0.3458] | -2.3% [-6.4, +0.7] | 0.5528 | -11.1% [-17.3, -5.5] | 1.10 | 1/30 | inconclusive | noise |
| config | `config.get-section-deep.x10k` | e2-11 | 111 (30) | 0.3182 [0.2830, 0.3572] | +0.8% [-0.2, +2.1] | 0.5170 | -16.8% [-21.1, -12.8] | 1.09 | 0/30 | noise | noise |
| config | `config.bind-poco50.x1k` | e0-6 | 111 (30) | 54.3638 [48.4683, 61.2891] | +12.8% [+8.2, +17.4] | 54.9061 | +14.1% [+9.5, +18.8] | 1.14 | 29/30 | inconclusive | slower |
| config | `config.bind-poco50.x1k` | e0-7 | 111 (30) | 48.1841 [42.6095, 54.1739] | - | 48.1055 | - | 1.08 | - | baseline | - |
| config | `config.bind-poco50.x1k` | e1-8 | 111 (30) | 46.6227 [41.0718, 52.6310] | -3.2% [-7.9, +0.4] | 46.7425 | -2.8% [-7.6, +1.1] | 1.08 | 2/30 | inconclusive | noise |
| config | `config.bind-poco50.x1k` | e1-9 | 111 (30) | 45.6247 [40.4776, 51.4746] | -5.3% [-9.9, -1.8] | 45.5996 | -5.2% [-9.8, -1.7] | 1.10 | 9/30 | inconclusive | noise |
| config | `config.bind-poco50.x1k` | e2-10 | 111 (30) | 46.4580 [40.8874, 52.4255] | -3.6% [-8.3, -0.3] | 46.6369 | -3.1% [-7.6, +0.2] | 1.10 | 4/30 | inconclusive | noise |
| config | `config.bind-poco50.x1k` | e2-11 | 111 (30) | 113.8757 [100.6099, 128.6089] | +136.3% [+133.9, +138.9] | 113.7520 | +136.5% [+134.0, +139.0] | 1.05 | 30/30 | slower | slower |
| config | `config.reload-json2000.x10` | e0-6 | 111 (30) | 5.7947 [5.1893, 6.5101] | -0.4% [-3.9, +2.9] | 5.8773 | -0.5% [-4.1, +2.7] | 1.40 | 2/30 | noise | noise |
| config | `config.reload-json2000.x10` | e0-7 | 111 (30) | 5.8152 [5.1551, 6.5196] | - | 5.9048 | - | 1.36 | - | baseline | - |
| config | `config.reload-json2000.x10` | e1-8 | 111 (30) | 6.1115 [5.4354, 6.8550] | +5.1% [+0.3, +8.6] | 6.1226 | +3.7% [-1.1, +7.7] | 1.29 | 26/30 | inconclusive | slower |
| config | `config.reload-json2000.x10` | e1-9 | 111 (30) | 5.8137 [5.1736, 6.5257] | -0.0% [-4.6, +3.0] | 5.8655 | -0.7% [-5.1, +2.3] | 1.33 | 2/30 | noise | noise |
| config | `config.reload-json2000.x10` | e2-10 | 111 (30) | 5.9114 [5.2531, 6.6317] | +1.7% [-2.9, +4.8] | 5.9697 | +1.1% [-3.3, +4.1] | 1.32 | 4/30 | noise | noise |
| config | `config.reload-json2000.x10` | e2-11 | 111 (30) | 5.7837 [5.1409, 6.5100] | -0.5% [-1.6, +0.4] | 5.7078 | -3.3% [-5.1, -2.1] | 1.33 | 1/30 | noise | noise |
| di | `di.cold.build100` | e0-6 | 111 (30) | 0.4728 [0.4129, 0.5423] | -1.0% [-7.2, +4.5] | 0.4728 | -1.0% [-7.3, +4.4] | 1.00 | 3/30 | inconclusive | noise |
| di | `di.cold.build100` | e0-7 | 111 (30) | 0.4776 [0.4199, 0.5484] | - | 0.4776 | - | 1.00 | - | baseline | - |
| di | `di.cold.build100` | e1-8 | 111 (30) | 0.4703 [0.4135, 0.5376] | -1.5% [-7.5, +3.0] | 0.4703 | -1.5% [-7.7, +3.0] | 1.00 | 3/30 | inconclusive | noise |
| di | `di.cold.build100` | e1-9 | 111 (30) | 0.4734 [0.4155, 0.5441] | -0.9% [-7.4, +5.1] | 0.4734 | -0.9% [-7.3, +5.2] | 1.00 | 2/30 | inconclusive | noise |
| di | `di.cold.build100` | e2-10 | 111 (30) | 0.4865 [0.4285, 0.5655] | +1.9% [-4.3, +8.6] | 0.4865 | +1.9% [-4.0, +8.4] | 1.00 | 12/30 | inconclusive | noise |
| di | `di.cold.build100` | e2-11 | 111 (30) | 0.5075 [0.4462, 0.5903] | +6.3% [+3.0, +13.0] | 0.5075 | +6.3% [+3.0, +13.0] | 1.00 | 19/30 | inconclusive | slower |
| di | `di.cold.first-resolve-depth5` | e0-6 | 111 (30) | 6.2584 [5.5006, 7.1061] | -1.8% [-7.4, +2.5] | 6.2584 | -1.8% [-7.5, +2.4] | 1.00 | 2/30 | inconclusive | noise |
| di | `di.cold.first-resolve-depth5` | e0-7 | 111 (30) | 6.3708 [5.5880, 7.2392] | - | 6.3708 | - | 1.00 | - | baseline | - |
| di | `di.cold.first-resolve-depth5` | e1-8 | 111 (30) | 7.0018 [6.1575, 8.0300] | +9.9% [+3.5, +15.4] | 7.0018 | +9.9% [+3.4, +15.3] | 1.00 | 28/30 | inconclusive | slower |
| di | `di.cold.first-resolve-depth5` | e1-9 | 111 (30) | 7.0574 [6.2257, 8.0992] | +10.8% [+4.8, +18.1] | 7.0574 | +10.8% [+4.8, +18.1] | 1.00 | 29/30 | inconclusive | slower |
| di | `di.cold.first-resolve-depth5` | e2-10 | 111 (30) | 7.3134 [6.3879, 8.3892] | +14.8% [+8.6, +19.6] | 7.3134 | +14.8% [+8.4, +19.6] | 1.00 | 29/30 | inconclusive | slower |
| di | `di.cold.first-resolve-depth5` | e2-11 | 111 (30) | 8.0927 [7.1063, 9.2240] | +27.0% [+23.3, +30.0] | 8.0927 | +27.0% [+23.3, +29.9] | 1.00 | 30/30 | slower | slower |
| di | `di.build100.x100` | e0-6 | 111 (30) | 0.7001 [0.6304, 0.7818] | -2.7% [-7.2, +1.1] | 0.9863 | +0.2% [-4.8, +5.8] | 1.74 | 7/30 | inconclusive (by mean) | noise |
| di | `di.build100.x100` | e0-7 | 111 (30) | 0.7198 [0.6473, 0.8052] | - | 0.9840 | - | 1.74 | - | baseline | - |
| di | `di.build100.x100` | e1-8 | 111 (30) | 0.7690 [0.6914, 0.8623] | +6.8% [+1.4, +12.1] | 1.0204 | +3.7% [-0.9, +11.6] | 1.67 | 15/30 | inconclusive (by mean) | slower |
| di | `di.build100.x100` | e1-9 | 111 (30) | 0.8620 [0.7660, 0.9616] | +19.8% [+12.9, +23.9] | 1.0540 | +7.1% [+1.4, +13.8] | 1.25 | 20/30 | inconclusive (by mean) | slower |
| di | `di.build100.x100` | e2-10 | 111 (30) | 0.8708 [0.7782, 0.9833] | +21.0% [+14.7, +27.2] | 1.0775 | +9.5% [+3.4, +17.7] | 1.17 | 21/30 | inconclusive (by mean) | slower |
| di | `di.build100.x100` | e2-11 | 111 (30) | 0.9264 [0.8249, 1.0286] | +28.7% [+23.5, +31.9] | 1.1340 | +15.2% [+10.3, +21.3] | 1.29 | 28/30 | inconclusive (by mean) | slower |
| di | `di.build1000.x10` | e0-6 | 111 (30) | 0.5931 [0.5275, 0.6656] | -1.8% [-6.7, +1.7] | 0.8656 | +2.7% [-2.2, +8.0] | 1.78 | 14/30 | inconclusive (by mean) | noise |
| di | `di.build1000.x10` | e0-7 | 111 (30) | 0.6041 [0.5381, 0.6796] | - | 0.8427 | - | 1.72 | - | baseline | - |
| di | `di.build1000.x10` | e1-8 | 111 (30) | 0.6723 [0.6127, 0.7537] | +11.3% [+6.0, +18.0] | 0.9988 | +18.5% [+12.1, +25.0] | 1.99 | 28/30 | inconclusive (by mean) | slower |
| di | `di.build1000.x10` | e1-9 | 111 (30) | 0.5964 [0.5280, 0.6702] | -1.3% [-6.9, +1.8] | 0.6307 | -25.2% [-29.9, -21.6] | 1.04 | 29/30 | inconclusive (by mean) | noise |
| di | `di.build1000.x10` | e2-10 | 111 (30) | 0.6001 [0.5316, 0.6845] | -0.7% [-5.7, +3.9] | 0.6376 | -24.3% [-28.8, -20.0] | 1.04 | 30/30 | faster (by mean) | noise |
| di | `di.build1000.x10` | e2-11 | 111 (30) | 0.6143 [0.5448, 0.6908] | +1.7% [-1.8, +3.5] | 0.6488 | -23.0% [-26.3, -19.3] | 1.04 | 29/30 | inconclusive (by mean) | noise |
| di | `di.validate-on-build100.x100` | e0-6 | 111 (30) | 3.1048 [2.7590, 3.4896] | -2.9% [-7.2, +0.5] | 3.2544 | -4.3% [-8.5, +0.2] | 1.15 | 2/30 | inconclusive | noise |
| di | `di.validate-on-build100.x100` | e0-7 | 111 (30) | 3.1982 [2.8381, 3.5931] | - | 3.4014 | - | 1.13 | - | baseline | - |
| di | `di.validate-on-build100.x100` | e1-8 | 111 (30) | 3.7336 [3.3161, 4.2322] | +16.7% [+11.2, +21.9] | 3.9497 | +16.1% [+11.2, +22.7] | 1.09 | 29/30 | inconclusive | slower |
| di | `di.validate-on-build100.x100` | e1-9 | 111 (30) | 4.9934 [4.4210, 5.6276] | +56.1% [+48.0, +61.3] | 5.1638 | +51.8% [+44.6, +58.5] | 1.05 | 29/30 | inconclusive | slower |
| di | `di.validate-on-build100.x100` | e2-10 | 111 (30) | 5.0952 [4.5080, 5.7989] | +59.3% [+51.8, +67.1] | 5.3314 | +56.7% [+50.3, +64.8] | 1.05 | 29/30 | inconclusive | slower |
| di | `di.validate-on-build100.x100` | e2-11 | 111 (30) | 5.1713 [4.5894, 5.8155] | +61.7% [+58.7, +64.8] | 5.3775 | +58.1% [+54.3, +63.5] | 1.05 | 30/30 | slower | slower |
| di | `di.first-resolve-depth5.x100` | e0-6 | 111 (30) | 0.2542 [0.2260, 0.2858] | -2.1% [-7.0, +1.1] | 0.3425 | -14.2% [-18.4, -8.7] | 1.06 | 1/30 | inconclusive | noise |
| di | `di.first-resolve-depth5.x100` | e0-7 | 111 (30) | 0.2597 [0.2313, 0.2917] | - | 0.3993 | - | 1.05 | - | baseline | - |
| di | `di.first-resolve-depth5.x100` | e1-8 | 111 (30) | 0.4137 [0.3672, 0.4674] | +59.3% [+51.2, +67.4] | 0.6004 | +50.3% [+44.6, +57.5] | 1.05 | 29/30 | inconclusive | slower |
| di | `di.first-resolve-depth5.x100` | e1-9 | 111 (30) | 0.3992 [0.3546, 0.4497] | +53.7% [+45.9, +58.7] | 0.4885 | +22.3% [+17.0, +27.5] | 1.03 | 29/30 | inconclusive | slower |
| di | `di.first-resolve-depth5.x100` | e2-10 | 111 (30) | 0.4070 [0.3614, 0.4643] | +56.7% [+49.4, +65.8] | 0.4981 | +24.7% [+19.2, +31.6] | 1.03 | 29/30 | inconclusive | slower |
| di | `di.first-resolve-depth5.x100` | e2-11 | 111 (30) | 0.4808 [0.4265, 0.5407] | +85.2% [+82.0, +87.9] | 0.5771 | +44.5% [+39.3, +49.9] | 1.03 | 30/30 | slower | slower |
| di | `di.transient.x10k` | e0-6 | 111 (30) | 0.2025 [0.1837, 0.2230] | -2.0% [-7.6, +3.4] | 0.3028 | +9.4% [+0.2, +17.7] | 1.27 | 6/30 | inconclusive | noise |
| di | `di.transient.x10k` | e0-7 | 111 (30) | 0.2066 [0.1874, 0.2280] | - | 0.2766 | - | 1.24 | - | baseline | - |
| di | `di.transient.x10k` | e1-8 | 111 (30) | 0.1861 [0.1661, 0.2095] | -9.9% [-15.1, -4.7] | 0.3025 | +9.3% [+2.2, +17.7] | 1.28 | 21/30 | inconclusive | noise |
| di | `di.transient.x10k` | e1-9 | 111 (30) | 0.1820 [0.1610, 0.2031] | -11.9% [-17.4, -8.1] | 0.2340 | -15.4% [-20.9, -9.3] | 1.18 | 26/30 | inconclusive | faster |
| di | `di.transient.x10k` | e2-10 | 111 (30) | 0.1837 [0.1633, 0.2069] | -11.1% [-16.1, -5.9] | 0.2372 | -14.2% [-20.4, -7.6] | 1.17 | 25/30 | inconclusive | faster |
| di | `di.transient.x10k` | e2-11 | 111 (30) | 0.1873 [0.1664, 0.2083] | -9.3% [-13.5, -6.7] | 0.2380 | -14.0% [-19.3, -7.5] | 1.17 | 23/30 | inconclusive | faster |
| di | `di.scoped.x10k` | e0-6 | 111 (30) | 0.4382 [0.3855, 0.4941] | -1.9% [-7.8, +2.7] | 0.5521 | -5.9% [-12.9, -0.8] | 1.07 | 5/30 | inconclusive | noise |
| di | `di.scoped.x10k` | e0-7 | 111 (30) | 0.4465 [0.3966, 0.5015] | - | 0.5865 | - | 1.06 | - | baseline | - |
| di | `di.scoped.x10k` | e1-8 | 111 (30) | 0.4278 [0.3744, 0.4828] | -4.2% [-10.3, +0.8] | 0.6589 | +12.3% [+6.0, +18.4] | 1.07 | 7/30 | inconclusive | noise |
| di | `di.scoped.x10k` | e1-9 | 111 (30) | 0.4265 [0.3690, 0.4786] | -4.5% [-11.8, -1.0] | 0.6195 | +5.6% [-0.0, +11.5] | 1.06 | 7/30 | inconclusive | noise |
| di | `di.scoped.x10k` | e2-10 | 111 (30) | 0.4248 [0.3750, 0.4821] | -4.9% [-10.6, +1.4] | 0.6189 | +5.5% [-0.8, +13.4] | 1.07 | 10/30 | inconclusive | noise |
| di | `di.scoped.x10k` | e2-11 | 111 (30) | 0.4439 [0.3895, 0.4958] | -0.6% [-4.7, +1.6] | 0.6248 | +6.5% [-0.8, +14.5] | 1.06 | 3/30 | noise | noise |
| di | `di.singleton.x10k` | e0-6 | 111 (30) | 0.1513 [0.1348, 0.1691] | -2.9% [-8.5, +2.5] | 0.1822 | -4.5% [-9.9, -0.1] | 1.28 | 7/30 | inconclusive | noise |
| di | `di.singleton.x10k` | e0-7 | 111 (30) | 0.1558 [0.1390, 0.1738] | - | 0.1908 | - | 1.25 | - | baseline | - |
| di | `di.singleton.x10k` | e1-8 | 111 (30) | 0.1417 [0.1231, 0.1632] | -9.1% [-15.3, -3.6] | 0.1948 | +2.1% [-3.3, +6.9] | 1.28 | 13/30 | inconclusive | noise |
| di | `di.singleton.x10k` | e1-9 | 111 (30) | 0.1375 [0.1199, 0.1560] | -11.7% [-17.5, -7.6] | 0.1862 | -2.4% [-7.6, +0.6] | 1.28 | 23/30 | faster | faster |
| di | `di.singleton.x10k` | e2-10 | 111 (30) | 0.1383 [0.1206, 0.1580] | -11.3% [-16.6, -6.1] | 0.1867 | -2.2% [-8.2, +2.5] | 1.28 | 23/30 | inconclusive | faster |
| di | `di.singleton.x10k` | e2-11 | 111 (30) | 0.1404 [0.1224, 0.1598] | -9.9% [-13.8, -6.7] | 0.1866 | -2.2% [-7.6, +2.6] | 1.26 | 21/30 | faster | faster |
| di | `di.open-generic.x10k` | e0-6 | 111 (30) | 0.2226 [0.2012, 0.2448] | -2.7% [-7.8, +1.4] | 0.3254 | -4.4% [-9.4, -0.4] | 1.24 | 5/30 | inconclusive | noise |
| di | `di.open-generic.x10k` | e0-7 | 111 (30) | 0.2288 [0.2063, 0.2531] | - | 0.3403 | - | 1.21 | - | baseline | - |
| di | `di.open-generic.x10k` | e1-8 | 111 (30) | 0.2093 [0.1861, 0.2373] | -8.5% [-13.2, -3.4] | 0.3192 | -6.2% [-10.6, -0.7] | 1.18 | 18/30 | inconclusive | noise |
| di | `di.open-generic.x10k` | e1-9 | 111 (30) | 0.2087 [0.1844, 0.2341] | -8.8% [-14.1, -4.8] | 0.2672 | -21.5% [-25.1, -18.6] | 1.12 | 16/30 | inconclusive | noise |
| di | `di.open-generic.x10k` | e2-10 | 111 (30) | 0.2117 [0.1875, 0.2388] | -7.5% [-12.7, -2.6] | 0.2701 | -20.6% [-24.9, -17.2] | 1.12 | 16/30 | inconclusive | noise |
| di | `di.open-generic.x10k` | e2-11 | 111 (30) | 0.2138 [0.1891, 0.2389] | -6.5% [-10.0, -4.0] | 0.2697 | -20.8% [-23.9, -17.3] | 1.12 | 15/30 | inconclusive | noise |
| di | `di.enumerable20.x10k` | e0-6 | 111 (30) | 3.2388 [2.8623, 3.6424] | -2.7% [-7.1, +0.3] | 3.3615 | -0.7% [-5.6, +2.8] | 1.06 | 3/30 | inconclusive | noise |
| di | `di.enumerable20.x10k` | e0-7 | 111 (30) | 3.3297 [2.9456, 3.7454] | - | 3.3869 | - | 1.06 | - | baseline | - |
| di | `di.enumerable20.x10k` | e1-8 | 111 (30) | 3.2198 [2.8553, 3.6388] | -3.3% [-7.8, +0.4] | 3.3679 | -0.6% [-5.3, +3.6] | 1.08 | 2/30 | inconclusive | noise |
| di | `di.enumerable20.x10k` | e1-9 | 111 (30) | 3.2121 [2.8361, 3.6307] | -3.5% [-8.5, -0.5] | 3.2844 | -3.0% [-8.3, +0.3] | 1.04 | 4/30 | inconclusive | noise |
| di | `di.enumerable20.x10k` | e2-10 | 111 (30) | 3.2416 [2.8618, 3.6846] | -2.6% [-7.1, +1.4] | 3.3133 | -2.2% [-6.7, +1.8] | 1.04 | 1/30 | inconclusive | noise |
| di | `di.enumerable20.x10k` | e2-11 | 111 (30) | 3.3316 [2.9478, 3.7600] | +0.1% [-1.5, +1.8] | 3.3997 | +0.4% [-2.0, +2.7] | 1.04 | 0/30 | noise | noise |
| di | `di.create-scope-dispose.x10k` | e0-6 | 111 (30) | 0.6084 [0.5527, 0.6720] | -3.5% [-7.2, +0.1] | 0.8191 | -15.1% [-18.8, -11.2] | 1.43 | 28/30 | faster (by mean) | noise |
| di | `di.create-scope-dispose.x10k` | e0-7 | 111 (30) | 0.6305 [0.5713, 0.6921] | - | 0.9647 | - | 1.65 | - | baseline | - |
| di | `di.create-scope-dispose.x10k` | e1-8 | 111 (30) | 0.5937 [0.5277, 0.6676] | -5.8% [-10.1, -1.2] | 0.8577 | -11.1% [-15.2, -6.9] | 1.52 | 23/30 | inconclusive (by mean) | noise |
| di | `di.create-scope-dispose.x10k` | e1-9 | 111 (30) | 0.5657 [0.5029, 0.6313] | -10.3% [-14.9, -6.9] | 0.8213 | -14.9% [-19.1, -12.0] | 1.11 | 29/30 | faster (by mean) | faster |
| di | `di.create-scope-dispose.x10k` | e2-10 | 111 (30) | 0.5764 [0.5106, 0.6448] | -8.6% [-13.2, -4.4] | 0.8241 | -14.6% [-18.9, -10.9] | 1.11 | 29/30 | faster (by mean) | noise |
| di | `di.create-scope-dispose.x10k` | e2-11 | 111 (30) | 0.5830 [0.5204, 0.6498] | -7.5% [-10.2, -5.3] | 0.8160 | -15.4% [-18.6, -11.7] | 1.10 | 29/30 | inconclusive (by mean) | noise |
| ef | `ef.cold.open-connection-create-schema` | e0-6 | 111 (30) | 47.5454 [42.0339, 53.6430] | -3.9% [-8.4, -1.1] | 47.5454 | -3.9% [-8.7, -1.1] | 1.00 | 5/30 | inconclusive | noise |
| ef | `ef.cold.open-connection-create-schema` | e0-7 | 111 (30) | 49.4836 [43.6254, 55.8475] | - | 49.4836 | - | 1.00 | - | baseline | - |
| ef | `ef.cold.open-connection-create-schema` | e1-8 | 111 (30) | 48.4610 [42.8307, 54.9854] | -2.1% [-6.9, +2.3] | 48.4610 | -2.1% [-6.7, +2.3] | 1.00 | 2/30 | inconclusive | noise |
| ef | `ef.cold.open-connection-create-schema` | e1-9 | 111 (30) | 48.5063 [43.0543, 55.0371] | -2.0% [-6.6, +1.8] | 48.5063 | -2.0% [-6.6, +1.8] | 1.00 | 3/30 | inconclusive | noise |
| ef | `ef.cold.open-connection-create-schema` | e2-10 | 111 (30) | 48.9037 [43.1975, 55.4451] | -1.2% [-5.8, +2.6] | 48.9037 | -1.2% [-5.9, +2.7] | 1.00 | 2/30 | inconclusive | noise |
| ef | `ef.cold.first-query` | e0-6 | 111 (30) | 725.5486 [635.6469, 820.0960] | -6.5% [-10.7, -4.9] | 725.5486 | -6.5% [-10.6, -4.8] | 1.00 | 23/30 | inconclusive | faster |
| ef | `ef.cold.first-query` | e0-7 | 111 (30) | 776.2420 [682.2831, 878.0963] | - | 776.2420 | - | 1.00 | - | baseline | - |
| ef | `ef.cold.first-query` | e1-8 | 111 (30) | 826.7756 [725.9266, 941.2836] | +6.5% [+1.3, +10.0] | 826.7756 | +6.5% [+1.4, +10.0] | 1.00 | 28/30 | inconclusive | slower |
| ef | `ef.cold.first-query` | e1-9 | 111 (30) | 860.2113 [754.2247, 974.6663] | +10.8% [+5.2, +13.9] | 860.2113 | +10.8% [+5.0, +13.9] | 1.00 | 28/30 | inconclusive | slower |
| ef | `ef.cold.first-query` | e2-10 | 111 (30) | 881.1335 [772.3307, 998.5811] | +13.5% [+7.4, +17.0] | 881.1335 | +13.5% [+7.3, +17.0] | 1.00 | 29/30 | inconclusive | slower |
| ef | `ef.new-context.x1k` | e0-6 | 111 (30) | 27.1190 [23.9934, 30.7507] | -5.5% [-9.6, -2.5] | 27.4478 | -5.3% [-9.5, -2.0] | 1.04 | 4/30 | inconclusive | noise |
| ef | `ef.new-context.x1k` | e0-7 | 111 (30) | 28.7060 [25.2540, 32.4520] | - | 28.9933 | - | 1.04 | - | baseline | - |
| ef | `ef.new-context.x1k` | e1-8 | 111 (30) | 30.0562 [26.4309, 33.8835] | +4.7% [-0.7, +7.4] | 30.3954 | +4.8% [-0.3, +7.7] | 1.03 | 27/30 | inconclusive | slower |
| ef | `ef.new-context.x1k` | e1-9 | 111 (30) | 34.5572 [30.4109, 39.0286] | +20.4% [+14.7, +24.0] | 34.8010 | +20.0% [+14.4, +23.8] | 1.03 | 29/30 | inconclusive | slower |
| ef | `ef.new-context.x1k` | e2-10 | 111 (30) | 39.0305 [34.3898, 44.1871] | +36.0% [+29.4, +40.2] | 39.6575 | +36.8% [+30.1, +41.3] | 1.05 | 29/30 | inconclusive | slower |
| ef | `ef.pooled-context.x1k` | e0-6 | 111 (30) | 0.0706 [0.0624, 0.0815] | -20.1% [-23.9, -15.5] | 0.1334 | -11.8% [-16.0, -7.9] | 1.07 | 30/30 | faster | faster |
| ef | `ef.pooled-context.x1k` | e0-7 | 111 (30) | 0.0883 [0.0776, 0.0997] | - | 0.1512 | - | 1.07 | - | baseline | - |
| ef | `ef.pooled-context.x1k` | e1-8 | 111 (30) | 0.0985 [0.0866, 0.1107] | +11.5% [+4.5, +16.5] | 0.1589 | +5.0% [-0.3, +9.4] | 1.09 | 26/30 | inconclusive | slower |
| ef | `ef.pooled-context.x1k` | e1-9 | 111 (30) | 0.0954 [0.0845, 0.1078] | +8.0% [+3.0, +12.3] | 0.1515 | +0.2% [-4.8, +3.6] | 1.07 | 25/30 | inconclusive | slower |
| ef | `ef.pooled-context.x1k` | e2-10 | 111 (30) | 0.0961 [0.0851, 0.1080] | +8.7% [+2.8, +13.0] | 0.1533 | +1.3% [-4.0, +4.4] | 1.07 | 27/30 | inconclusive | slower |
| ef | `ef.query-where-orderby-take.x1k` | e0-6 | 111 (30) | 168.9394 [149.3461, 191.4846] | -2.2% [-6.5, +0.9] | 169.8562 | -2.0% [-6.4, +1.4] | 1.01 | 1/30 | inconclusive | noise |
| ef | `ef.query-where-orderby-take.x1k` | e0-7 | 111 (30) | 172.8017 [152.4602, 194.8521] | - | 173.2703 | - | 1.01 | - | baseline | - |
| ef | `ef.query-where-orderby-take.x1k` | e1-8 | 111 (30) | 169.8712 [149.6397, 191.9812] | -1.7% [-6.8, +1.0] | 170.5289 | -1.6% [-6.5, +1.2] | 1.01 | 1/30 | inconclusive | noise |
| ef | `ef.query-where-orderby-take.x1k` | e1-9 | 111 (30) | 170.7270 [150.0000, 192.9660] | -1.2% [-6.0, +1.6] | 171.2046 | -1.2% [-6.0, +1.9] | 1.01 | 1/30 | inconclusive | noise |
| ef | `ef.query-where-orderby-take.x1k` | e2-10 | 111 (30) | 171.2751 [150.7876, 193.3908] | -0.9% [-5.8, +2.1] | 172.0080 | -0.7% [-5.6, +2.5] | 1.01 | 1/30 | inconclusive | noise |
| ef | `ef.save-changes-1000-entities.x1` | e0-6 | 111 (30) | 21.7642 [19.2968, 24.5723] | +2.3% [-2.4, +5.3] | 22.6986 | +2.0% [-2.7, +5.1] | 1.07 | 12/30 | inconclusive | noise |
| ef | `ef.save-changes-1000-entities.x1` | e0-7 | 111 (30) | 21.2738 [18.8110, 24.0253] | - | 22.2641 | - | 1.08 | - | baseline | - |
| ef | `ef.save-changes-1000-entities.x1` | e1-8 | 111 (30) | 20.6057 [18.1239, 23.2845] | -3.1% [-8.2, -0.4] | 21.4263 | -3.8% [-8.4, -1.0] | 1.06 | 1/30 | inconclusive | noise |
| ef | `ef.save-changes-1000-entities.x1` | e1-9 | 111 (30) | 20.0263 [17.6630, 22.7060] | -5.9% [-10.6, -2.6] | 20.8296 | -6.4% [-11.0, -3.2] | 1.05 | 9/30 | inconclusive | noise |
| ef | `ef.save-changes-1000-entities.x1` | e2-10 | 111 (30) | 21.1042 [18.5835, 23.8628] | -0.8% [-5.7, +2.1] | 21.8922 | -1.7% [-6.4, +1.3] | 1.07 | 1/30 | inconclusive | noise |
| ef | `ef.detect-changes-10k-tracked.x10` | e0-6 | 111 (30) | 52.9038 [47.3252, 59.4285] | +8.6% [+4.3, +12.1] | 53.5033 | +8.7% [+4.3, +12.5] | 1.05 | 29/30 | inconclusive | slower |
| ef | `ef.detect-changes-10k-tracked.x10` | e0-7 | 111 (30) | 48.7291 [43.2250, 54.6348] | - | 49.2004 | - | 1.05 | - | baseline | - |
| ef | `ef.detect-changes-10k-tracked.x10` | e1-8 | 111 (30) | 34.5913 [30.6406, 38.6332] | -29.0% [-32.4, -27.0] | 35.0399 | -28.8% [-32.0, -26.7] | 1.04 | 30/30 | faster | faster |
| ef | `ef.detect-changes-10k-tracked.x10` | e1-9 | 111 (30) | 28.2026 [25.0176, 31.6445] | -42.1% [-44.7, -40.3] | 28.3987 | -42.3% [-44.9, -40.4] | 1.03 | 30/30 | faster | faster |
| ef | `ef.detect-changes-10k-tracked.x10` | e2-10 | 111 (30) | 33.5874 [29.7646, 37.6836] | -31.1% [-34.4, -28.8] | 33.8701 | -31.2% [-34.4, -28.7] | 1.05 | 30/30 | faster | faster |
| ef | `ef.to-query-string.x1k` | e0-6 | 111 (30) | 8.9701 [7.9133, 10.1850] | -1.0% [-5.6, +2.4] | 9.0379 | -1.2% [-6.0, +2.4] | 1.04 | 2/30 | inconclusive | noise |
| ef | `ef.to-query-string.x1k` | e0-7 | 111 (30) | 9.0635 [8.0414, 10.1610] | - | 9.1467 | - | 1.04 | - | baseline | - |
| ef | `ef.to-query-string.x1k` | e1-8 | 111 (30) | 8.8992 [7.8591, 10.0120] | -1.8% [-6.8, +1.1] | 8.9801 | -1.8% [-6.7, +1.2] | 1.03 | 2/30 | inconclusive | noise |
| ef | `ef.to-query-string.x1k` | e1-9 | 111 (30) | 10.9543 [9.6910, 12.3673] | +20.9% [+15.3, +24.9] | 11.0297 | +20.6% [+14.8, +24.7] | 1.03 | 29/30 | inconclusive | slower |
| ef | `ef.to-query-string.x1k` | e2-10 | 111 (30) | 11.0981 [9.8265, 12.4778] | +22.4% [+16.5, +26.3] | 11.1897 | +22.3% [+16.2, +26.3] | 1.04 | 29/30 | inconclusive | slower |
| logging | `logging.cold.factory-create` | e0-6 | 111 (30) | 36.9658 [32.7892, 42.2903] | -5.4% [-10.0, -1.2] | 36.9658 | -5.4% [-10.1, -1.2] | 1.00 | 4/30 | inconclusive | noise |
| logging | `logging.cold.factory-create` | e0-7 | 111 (30) | 39.0556 [34.2473, 44.4012] | - | 39.0556 | - | 1.00 | - | baseline | - |
| logging | `logging.cold.factory-create` | e1-8 | 111 (30) | 43.3834 [38.2766, 49.5895] | +11.1% [+5.2, +15.7] | 43.3834 | +11.1% [+5.4, +15.8] | 1.00 | 29/30 | inconclusive | slower |
| logging | `logging.cold.factory-create` | e1-9 | 111 (30) | 43.0962 [37.8472, 48.9133] | +10.3% [+4.6, +14.2] | 43.0962 | +10.3% [+4.4, +14.1] | 1.00 | 27/30 | inconclusive | slower |
| logging | `logging.cold.factory-create` | e2-10 | 111 (30) | 44.6336 [39.2133, 50.7633] | +14.3% [+9.1, +18.1] | 44.6336 | +14.3% [+9.0, +18.3] | 1.00 | 29/30 | inconclusive | slower |
| logging | `logging.cold.factory-create` | e2-11 | 111 (30) | 47.4539 [41.6674, 53.7520] | +21.5% [+18.2, +23.7] | 47.4539 | +21.5% [+18.2, +23.6] | 1.00 | 30/30 | slower | slower |
| logging | `logging.cold.first-log` | e0-6 | 111 (30) | 6.4452 [5.7009, 7.3703] | -2.9% [-7.8, +0.9] | 6.4452 | -2.9% [-8.0, +0.9] | 1.00 | 3/30 | inconclusive | noise |
| logging | `logging.cold.first-log` | e0-7 | 111 (30) | 6.6406 [5.8564, 7.5514] | - | 6.6406 | - | 1.00 | - | baseline | - |
| logging | `logging.cold.first-log` | e1-8 | 111 (30) | 7.0728 [6.2016, 8.0675] | +6.5% [+0.4, +10.5] | 7.0728 | +6.5% [+0.4, +10.5] | 1.00 | 27/30 | inconclusive | slower |
| logging | `logging.cold.first-log` | e1-9 | 111 (30) | 7.5882 [6.6963, 8.6054] | +14.3% [+8.3, +18.5] | 7.5882 | +14.3% [+8.2, +18.5] | 1.00 | 29/30 | inconclusive | slower |
| logging | `logging.cold.first-log` | e2-10 | 111 (30) | 8.1434 [7.1743, 9.2891] | +22.6% [+17.0, +27.7] | 8.1434 | +22.6% [+17.0, +27.8] | 1.00 | 29/30 | inconclusive | slower |
| logging | `logging.cold.first-log` | e2-11 | 111 (30) | 8.3058 [7.2771, 9.3859] | +25.1% [+21.3, +27.1] | 8.3058 | +25.1% [+21.2, +26.9] | 1.00 | 30/30 | slower | slower |
| logging | `logging.factory-create.x100` | e0-6 | 111 (30) | 1.7993 [1.6270, 1.9943] | -5.4% [-9.3, -2.1] | 2.1594 | -7.3% [-11.0, -3.8] | 1.16 | 15/30 | inconclusive | noise |
| logging | `logging.factory-create.x100` | e0-7 | 111 (30) | 1.9015 [1.7153, 2.1047] | - | 2.3282 | - | 1.24 | - | baseline | - |
| logging | `logging.factory-create.x100` | e1-8 | 111 (30) | 2.7490 [2.4777, 3.0624] | +44.6% [+37.1, +52.8] | 3.6809 | +58.1% [+50.1, +64.4] | 1.17 | 29/30 | inconclusive | slower |
| logging | `logging.factory-create.x100` | e1-9 | 111 (30) | 2.7361 [2.4475, 3.0227] | +43.9% [+36.9, +48.9] | 3.6534 | +56.9% [+49.5, +62.7] | 1.14 | 29/30 | inconclusive | slower |
| logging | `logging.factory-create.x100` | e2-10 | 111 (30) | 2.7134 [2.4245, 3.0164] | +42.7% [+35.3, +49.2] | 3.5066 | +50.6% [+42.9, +56.1] | 1.11 | 29/30 | inconclusive | slower |
| logging | `logging.factory-create.x100` | e2-11 | 111 (30) | 2.8466 [2.5529, 3.1522] | +49.7% [+44.7, +53.6] | 3.6344 | +56.1% [+50.3, +61.6] | 1.12 | 30/30 | slower | slower |
| logging | `logging.log-information-3args-enabled.x10k` | e0-6 | 111 (30) | 3.0455 [2.7002, 3.4182] | +0.6% [-4.7, +3.9] | 3.5001 | -0.3% [-5.6, +4.8] | 1.07 | 3/30 | noise | noise |
| logging | `logging.log-information-3args-enabled.x10k` | e0-7 | 111 (30) | 3.0285 [2.7134, 3.4032] | - | 3.5095 | - | 1.07 | - | baseline | - |
| logging | `logging.log-information-3args-enabled.x10k` | e1-8 | 111 (30) | 2.7105 [2.4206, 3.0428] | -10.5% [-15.8, -5.2] | 3.8833 | +10.7% [+3.1, +15.5] | 1.14 | 26/30 | inconclusive | faster |
| logging | `logging.log-information-3args-enabled.x10k` | e1-9 | 111 (30) | 2.6004 [2.3288, 2.9325] | -14.1% [-18.2, -9.6] | 3.7314 | +6.3% [-0.7, +13.3] | 1.13 | 29/30 | mixed | faster |
| logging | `logging.log-information-3args-enabled.x10k` | e2-10 | 111 (30) | 2.6663 [2.3727, 3.0043] | -12.0% [-16.9, -7.3] | 3.6431 | +3.8% [-3.1, +9.5] | 1.11 | 28/30 | inconclusive | faster |
| logging | `logging.log-information-3args-enabled.x10k` | e2-11 | 111 (30) | 2.6887 [2.4003, 3.0159] | -11.2% [-14.3, -8.9] | 3.6677 | +4.5% [-0.1, +10.1] | 1.12 | 28/30 | inconclusive | faster |
| logging | `logging.log-debug-3args-disabled.x10k` | e0-6 | 111 (30) | 0.4792 [0.4348, 0.5280] | +8.9% [+2.4, +15.1] | 0.5465 | +2.7% [-1.9, +7.1] | 1.29 | 21/30 | inconclusive | noise |
| logging | `logging.log-debug-3args-disabled.x10k` | e0-7 | 111 (30) | 0.4399 [0.4003, 0.4832] | - | 0.5319 | - | 1.32 | - | baseline | - |
| logging | `logging.log-debug-3args-disabled.x10k` | e1-8 | 111 (30) | 0.4283 [0.3857, 0.4729] | -2.6% [-8.6, +3.3] | 0.5737 | +7.9% [+2.0, +12.6] | 1.37 | 6/30 | inconclusive | noise |
| logging | `logging.log-debug-3args-disabled.x10k` | e1-9 | 111 (30) | 0.4170 [0.3791, 0.4644] | -5.2% [-10.0, +0.4] | 0.5663 | +6.5% [+0.5, +11.6] | 1.36 | 10/30 | inconclusive | noise |
| logging | `logging.log-debug-3args-disabled.x10k` | e2-10 | 111 (30) | 0.4434 [0.3985, 0.4942] | +0.8% [-5.1, +6.8] | 0.5638 | +6.0% [+0.6, +10.4] | 1.30 | 12/30 | inconclusive | noise |
| logging | `logging.log-debug-3args-disabled.x10k` | e2-11 | 111 (30) | 0.4561 [0.4104, 0.5007] | +3.7% [-2.6, +8.0] | 0.5729 | +7.7% [+4.3, +12.4] | 1.38 | 13/30 | inconclusive | slower |
| logging | `logging.logger-message-define.x10k` | e0-6 | 111 (30) | 2.9203 [2.5909, 3.2962] | -1.9% [-6.1, +1.6] | 3.3907 | -1.3% [-7.2, +6.0] | 1.06 | 1/30 | inconclusive | noise |
| logging | `logging.logger-message-define.x10k` | e0-7 | 111 (30) | 2.9764 [2.6336, 3.3373] | - | 3.4368 | - | 1.06 | - | baseline | - |
| logging | `logging.logger-message-define.x10k` | e1-8 | 111 (30) | 1.8648 [1.6530, 2.0912] | -37.3% [-40.3, -35.4] | 2.8591 | -16.8% [-23.5, -10.9] | 1.10 | 30/30 | faster | faster |
| logging | `logging.logger-message-define.x10k` | e1-9 | 111 (30) | 1.8599 [1.6495, 2.0919] | -37.5% [-40.3, -35.6] | 2.8275 | -17.7% [-24.0, -11.1] | 1.10 | 30/30 | faster | faster |
| logging | `logging.logger-message-define.x10k` | e2-10 | 111 (30) | 1.8829 [1.6573, 2.1413] | -36.7% [-39.6, -33.4] | 2.6295 | -23.5% [-28.3, -16.7] | 1.08 | 30/30 | faster | faster |
| logging | `logging.logger-message-define.x10k` | e2-11 | 111 (30) | 1.9129 [1.7016, 2.1439] | -35.7% [-36.5, -34.7] | 2.7447 | -20.1% [-25.1, -15.4] | 1.11 | 30/30 | faster | faster |
| logging | `logging.begin-scope.x10k` | e0-6 | 111 (30) | 0.6839 [0.6213, 0.7564] | +7.9% [+3.8, +13.1] | 0.7624 | +5.4% [+0.9, +9.2] | 1.28 | 19/30 | inconclusive | noise |
| logging | `logging.begin-scope.x10k` | e0-7 | 111 (30) | 0.6339 [0.5717, 0.6991] | - | 0.7231 | - | 1.22 | - | baseline | - |
| logging | `logging.begin-scope.x10k` | e1-8 | 111 (30) | 0.6130 [0.5558, 0.6819] | -3.3% [-6.9, +1.0] | 0.7636 | +5.6% [+0.1, +9.5] | 1.29 | 4/30 | inconclusive | noise |
| logging | `logging.begin-scope.x10k` | e1-9 | 111 (30) | 0.6078 [0.5490, 0.6764] | -4.1% [-7.8, -0.1] | 0.7584 | +4.9% [-0.5, +8.8] | 1.30 | 10/30 | inconclusive | noise |
| logging | `logging.begin-scope.x10k` | e2-10 | 111 (30) | 0.6454 [0.5771, 0.7182] | +1.8% [-2.9, +6.6] | 0.9525 | +31.7% [+22.2, +39.3] | 1.26 | 9/30 | inconclusive | noise |
| logging | `logging.begin-scope.x10k` | e2-11 | 111 (30) | 0.6563 [0.5912, 0.7217] | +3.5% [+0.6, +6.7] | 0.9159 | +26.7% [+19.6, +37.0] | 1.33 | 8/30 | inconclusive | noise |
| options | `options.cold.first-value` | e0-6 | 111 (30) | 28.4376 [25.0456, 32.5450] | +13.6% [+7.8, +17.3] | 28.4376 | +13.6% [+7.9, +17.3] | 1.00 | 29/30 | inconclusive | slower |
| options | `options.cold.first-value` | e0-7 | 111 (30) | 25.0243 [22.0101, 28.5246] | - | 25.0243 | - | 1.00 | - | baseline | - |
| options | `options.cold.first-value` | e1-8 | 111 (30) | 27.8584 [24.5222, 31.9512] | +11.3% [+5.5, +19.4] | 27.8584 | +11.3% [+5.5, +19.3] | 1.00 | 29/30 | inconclusive | slower |
| options | `options.cold.first-value` | e1-9 | 111 (30) | 27.7044 [24.4027, 31.6017] | +10.7% [+4.6, +14.8] | 27.7044 | +10.7% [+4.6, +14.8] | 1.00 | 29/30 | inconclusive | slower |
| options | `options.cold.first-value` | e2-10 | 111 (30) | 29.1411 [25.6079, 32.9668] | +16.5% [+11.1, +20.3] | 29.1411 | +16.5% [+11.2, +20.3] | 1.00 | 29/30 | inconclusive | slower |
| options | `options.cold.first-value` | e2-11 | 111 (30) | 31.7032 [27.8343, 36.0938] | +26.7% [+24.8, +29.1] | 31.7032 | +26.7% [+24.8, +29.1] | 1.00 | 30/30 | slower | slower |
| options | `options.ioptions-value.x10k` | e0-6 | 111 (30) | 0.0625 [0.0557, 0.0709] | -2.2% [-8.0, +2.2] | 0.0732 | -2.3% [-7.6, +1.5] | 1.06 | 7/30 | inconclusive | noise |
| options | `options.ioptions-value.x10k` | e0-7 | 111 (30) | 0.0639 [0.0570, 0.0719] | - | 0.0749 | - | 1.06 | - | baseline | - |
| options | `options.ioptions-value.x10k` | e1-8 | 111 (30) | 0.0627 [0.0556, 0.0702] | -2.0% [-7.9, +1.9] | 0.0729 | -2.6% [-8.0, +0.8] | 1.10 | 8/30 | inconclusive | noise |
| options | `options.ioptions-value.x10k` | e1-9 | 111 (30) | 0.0631 [0.0556, 0.0709] | -1.4% [-7.4, +2.7] | 0.0733 | -2.1% [-7.6, +1.7] | 1.07 | 8/30 | inconclusive | noise |
| options | `options.ioptions-value.x10k` | e2-10 | 111 (30) | 0.0631 [0.0561, 0.0707] | -1.3% [-6.0, +2.5] | 0.0736 | -1.7% [-6.0, +1.8] | 1.06 | 6/30 | inconclusive | noise |
| options | `options.ioptions-value.x10k` | e2-11 | 111 (30) | 0.0636 [0.0558, 0.0718] | -0.6% [-4.3, +1.9] | 0.0738 | -1.4% [-4.2, +0.6] | 1.07 | 9/30 | noise | noise |
| options | `options.monitor-current-value.x10k` | e0-6 | 111 (30) | 0.3088 [0.2746, 0.3459] | +149.6% [+135.9, +160.9] | 0.3544 | +140.7% [+128.6, +151.9] | 1.15 | 30/30 | slower | slower |
| options | `options.monitor-current-value.x10k` | e0-7 | 111 (30) | 0.1238 [0.1072, 0.1418] | - | 0.1473 | - | 1.18 | - | baseline | - |
| options | `options.monitor-current-value.x10k` | e1-8 | 111 (30) | 0.1108 [0.0939, 0.1283] | -10.5% [-16.2, -7.1] | 0.1327 | -9.9% [-14.7, -6.4] | 1.18 | 20/30 | faster | faster |
| options | `options.monitor-current-value.x10k` | e1-9 | 111 (30) | 0.1103 [0.0942, 0.1281] | -10.9% [-16.6, -7.1] | 0.1324 | -10.1% [-15.2, -6.6] | 1.17 | 19/30 | inconclusive | faster |
| options | `options.monitor-current-value.x10k` | e2-10 | 111 (30) | 0.1095 [0.0940, 0.1270] | -11.5% [-16.1, -7.2] | 0.1322 | -10.3% [-14.7, -6.6] | 1.19 | 26/30 | faster | faster |
| options | `options.monitor-current-value.x10k` | e2-11 | 111 (30) | 0.1117 [0.0951, 0.1302] | -9.7% [-12.6, -7.1] | 0.1349 | -8.4% [-10.6, -6.2] | 1.17 | 21/30 | faster | faster |
| options | `options.snapshot-new-scope.x1k` | e0-6 | 111 (30) | 44.5602 [39.6658, 50.1283] | +8.9% [+4.2, +12.9] | 44.8994 | +9.0% [+4.3, +12.8] | 1.01 | 27/30 | inconclusive | slower |
| options | `options.snapshot-new-scope.x1k` | e0-7 | 111 (30) | 40.9345 [36.1009, 45.8955] | - | 41.1864 | - | 1.02 | - | baseline | - |
| options | `options.snapshot-new-scope.x1k` | e1-8 | 111 (30) | 38.6923 [34.4034, 43.3479] | -5.5% [-9.5, -2.4] | 38.9162 | -5.5% [-9.6, -2.4] | 1.01 | 11/30 | inconclusive | noise |
| options | `options.snapshot-new-scope.x1k` | e1-9 | 111 (30) | 38.2449 [33.9301, 43.0633] | -6.6% [-10.6, -3.2] | 38.4853 | -6.6% [-10.4, -3.2] | 1.01 | 18/30 | inconclusive | faster |
| options | `options.snapshot-new-scope.x1k` | e2-10 | 111 (30) | 39.1348 [34.7169, 43.9589] | -4.4% [-8.1, -1.0] | 39.3761 | -4.4% [-8.0, -1.1] | 1.01 | 7/30 | inconclusive | noise |
| options | `options.snapshot-new-scope.x1k` | e2-11 | 111 (30) | 106.6437 [94.2714, 120.1412] | +160.5% [+157.3, +164.5] | 107.0756 | +160.0% [+156.3, +163.8] | 1.01 | 30/30 | slower | slower |
| options | `options.validate-data-annotations.x1k` | e0-6 | 111 (30) | 36.6339 [32.6699, 41.2843] | +15.7% [+10.6, +19.8] | 36.8460 | +15.5% [+10.4, +19.5] | 1.02 | 29/30 | inconclusive | slower |
| options | `options.validate-data-annotations.x1k` | e0-7 | 111 (30) | 31.6661 [28.1061, 35.5196] | - | 31.8944 | - | 1.02 | - | baseline | - |
| options | `options.validate-data-annotations.x1k` | e1-8 | 111 (30) | 33.1636 [29.3998, 37.2195] | +4.7% [-0.2, +7.8] | 33.3729 | +4.6% [-0.0, +7.9] | 1.02 | 22/30 | inconclusive | slower |
| options | `options.validate-data-annotations.x1k` | e1-9 | 111 (30) | 32.3721 [28.6011, 36.4595] | +2.2% [-2.8, +5.6] | 32.5631 | +2.1% [-2.9, +5.6] | 1.02 | 14/30 | inconclusive | noise |
| options | `options.validate-data-annotations.x1k` | e2-10 | 111 (30) | 32.7321 [28.9947, 36.6987] | +3.4% [-1.3, +7.0] | 33.0000 | +3.5% [-1.1, +7.0] | 1.02 | 15/30 | inconclusive | noise |
| options | `options.validate-data-annotations.x1k` | e2-11 | 111 (30) | 39.6991 [35.0865, 44.6888] | +25.4% [+23.3, +27.8] | 39.9761 | +25.3% [+23.0, +27.7] | 1.01 | 30/30 | slower | slower |
| trackb-aspnetcore | `aspnetcore.cold.start-kestrel` | b-net10 | 111 (30) | 193.1003 [173.9648, 230.1266] | - | 193.1003 | - | 1.00 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.cold.start-kestrel` | b-net11 | 111 (30) | 87.8236 [77.9907, 99.3543] | -54.5% [-60.1, -53.2] | 87.8236 | -54.5% [-60.2, -53.2] | 1.00 | 30/30 | faster | faster |
| trackb-aspnetcore | `aspnetcore.cold.first-request` | b-net10 | 111 (30) | 1446.2024 [1271.4752, 1642.5732] | - | 1446.2024 | - | 1.00 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.cold.first-request` | b-net11 | 111 (30) | 1513.4827 [1327.9122, 1708.9986] | +4.7% [+2.8, +5.8] | 1513.4827 | +4.7% [+2.8, +5.8] | 1.00 | 12/30 | inconclusive | slower |
| trackb-aspnetcore | `aspnetcore.hello.batch400` | b-net10 | 111 (30) | 100.5022 [100.4738, 100.5294] | - | 100.5125 | - | 1.01 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.hello.batch400` | b-net11 | 111 (30) | 100.5594 [100.5312, 100.5883] | +0.1% [+0.0, +0.1] | 100.5654 | +0.1% [+0.0, +0.1] | 1.01 | 0/30 | noise | noise |
| trackb-aspnetcore | `aspnetcore.json.batch400` | b-net10 | 111 (30) | 100.5031 [100.4739, 100.5294] | - | 100.5125 | - | 1.01 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.json.batch400` | b-net11 | 111 (30) | 100.5649 [100.5323, 100.5950] | +0.1% [+0.0, +0.1] | 100.5760 | +0.1% [+0.0, +0.1] | 1.01 | 0/30 | noise | noise |
| trackb-aspnetcore | `aspnetcore.routes500.batch400` | b-net10 | 111 (30) | 100.5027 [100.4784, 100.5350] | - | 100.5184 | - | 1.01 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.routes500.batch400` | b-net11 | 111 (30) | 100.5708 [100.5433, 100.5984] | +0.1% [+0.0, +0.1] | 100.5776 | +0.1% [+0.0, +0.1] | 1.01 | 0/30 | noise | noise |
| trackb-aspnetcore | `aspnetcore.hello.request` | b-net10 | 111 (30) | 0.0642 [0.0559, 0.0741] | - | 0.0769 | - | 1.68 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.hello.request` | b-net11 | 111 (30) | 0.1392 [0.1233, 0.1563] | +117.0% [+104.7, +126.7] | 0.1423 | +85.1% [+68.0, +93.0] | 2.72 | 30/30 | slower (by mean) | slower |
| trackb-aspnetcore | `aspnetcore.json.request` | b-net10 | 111 (30) | 0.0661 [0.0576, 0.0759] | - | 0.0790 | - | 1.67 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.json.request` | b-net11 | 111 (30) | 0.1410 [0.1252, 0.1581] | +113.3% [+101.6, +122.2] | 0.1452 | +83.9% [+67.6, +91.5] | 2.67 | 30/30 | slower (by mean) | slower |
| trackb-aspnetcore | `aspnetcore.routes500.request` | b-net10 | 111 (30) | 0.0687 [0.0604, 0.0787] | - | 0.0817 | - | 1.65 | - | baseline | - |
| trackb-aspnetcore | `aspnetcore.routes500.request` | b-net11 | 111 (30) | 0.1445 [0.1279, 0.1618] | +110.3% [+99.9, +118.2] | 0.1491 | +82.4% [+64.7, +89.7] | 2.49 | 30/30 | slower (by mean) | slower |
| trackb-channels | `channels.cold.first-pipeline` | b-net10 | 111 (30) | 12.0070 [10.6396, 13.5498] | - | 12.0070 | - | 1.00 | - | baseline | - |
| trackb-channels | `channels.cold.first-pipeline` | b-net11 | 111 (30) | 12.0960 [10.6392, 13.6445] | +0.7% [-3.2, +2.9] | 12.0960 | +0.7% [-3.3, +3.0] | 1.00 | 2/30 | noise | noise |
| trackb-channels | `channels.bounded1024-1p4c.x100k-items` | b-net10 | 111 (30) | 29.6946 [25.4009, 34.9360] | - | 30.0756 | - | 1.22 | - | baseline | - |
| trackb-channels | `channels.bounded1024-1p4c.x100k-items` | b-net11 | 111 (30) | 27.6204 [24.1548, 31.4969] | -7.0% [-10.7, -4.4] | 28.5339 | -5.1% [-9.0, -1.5] | 1.22 | 20/30 | inconclusive | faster |
| trackb-ef | `ef.cold.open-connection-create-schema` | b-net10 | 111 (30) | 44.2043 [39.1362, 50.0990] | - | 44.2043 | - | 1.00 | - | baseline | - |
| trackb-ef | `ef.cold.open-connection-create-schema` | b-net11 | 111 (30) | 36.9168 [32.6795, 41.8097] | -16.5% [-19.0, -13.0] | 36.9168 | -16.5% [-18.9, -12.9] | 1.00 | 30/30 | faster | faster |
| trackb-ef | `ef.cold.first-query` | b-net10 | 111 (30) | 717.3411 [632.3797, 812.2771] | - | 717.3411 | - | 1.00 | - | baseline | - |
| trackb-ef | `ef.cold.first-query` | b-net11 | 111 (30) | 778.2468 [686.6125, 882.5847] | +8.5% [+6.0, +10.7] | 778.2468 | +8.5% [+5.9, +10.7] | 1.00 | 29/30 | slower | slower |
| trackb-ef | `ef.new-context.x1k` | b-net10 | 111 (30) | 39.0917 [34.4583, 44.1204] | - | 39.6531 | - | 1.05 | - | baseline | - |
| trackb-ef | `ef.new-context.x1k` | b-net11 | 111 (30) | 40.0955 [35.1130, 45.4821] | +2.6% [+1.1, +4.1] | 40.8014 | +2.9% [+0.9, +4.4] | 1.06 | 8/30 | noise | noise |
| trackb-ef | `ef.pooled-context.x1k` | b-net10 | 111 (30) | 0.0950 [0.0836, 0.1073] | - | 0.1491 | - | 1.09 | - | baseline | - |
| trackb-ef | `ef.pooled-context.x1k` | b-net11 | 111 (30) | 0.0931 [0.0820, 0.1057] | -2.0% [-3.2, -0.4] | 0.1503 | +0.8% [-0.5, +2.2] | 1.07 | 5/30 | noise | noise |
| trackb-ef | `ef.query-where-orderby-take.x1k` | b-net10 | 111 (30) | 174.4029 [153.6796, 197.3407] | - | 175.0545 | - | 1.01 | - | baseline | - |
| trackb-ef | `ef.query-where-orderby-take.x1k` | b-net11 | 111 (30) | 176.4477 [155.1700, 199.5955] | +1.2% [+0.2, +1.9] | 177.2611 | +1.3% [-0.3, +2.0] | 1.01 | 1/30 | noise | noise |
| trackb-ef | `ef.save-changes-1000-entities.x1` | b-net10 | 111 (30) | 21.4967 [18.9063, 24.2824] | - | 22.2733 | - | 1.08 | - | baseline | - |
| trackb-ef | `ef.save-changes-1000-entities.x1` | b-net11 | 111 (30) | 21.6005 [19.0978, 24.2654] | +0.5% [-0.6, +1.7] | 22.3523 | +0.4% [-1.4, +1.5] | 1.06 | 2/30 | noise | noise |
| trackb-ef | `ef.detect-changes-10k-tracked.x10` | b-net10 | 111 (30) | 34.0780 [30.1409, 38.2323] | - | 34.3239 | - | 1.05 | - | baseline | - |
| trackb-ef | `ef.detect-changes-10k-tracked.x10` | b-net11 | 111 (30) | 38.0397 [33.7003, 42.8272] | +11.6% [+10.1, +13.9] | 38.3202 | +11.6% [+9.7, +13.9] | 1.05 | 30/30 | slower | slower |
| trackb-ef | `ef.to-query-string.x1k` | b-net10 | 111 (30) | 11.1992 [9.8701, 12.6671] | - | 11.2691 | - | 1.04 | - | baseline | - |
| trackb-ef | `ef.to-query-string.x1k` | b-net11 | 111 (30) | 11.1273 [9.8597, 12.4959] | -0.6% [-2.1, +0.9] | 11.2037 | -0.6% [-2.5, +0.9] | 1.05 | 3/30 | noise | noise |
| trackb-immutable | `immutable.cold.first-build-10k` | b-net10 | 111 (30) | 11.5471 [10.1810, 13.0780] | - | 11.5471 | - | 1.00 | - | baseline | - |
| trackb-immutable | `immutable.cold.first-build-10k` | b-net11 | 111 (30) | 11.3216 [9.9933, 12.8628] | -2.0% [-4.4, +0.6] | 11.3216 | -2.0% [-4.3, +0.6] | 1.00 | 2/30 | noise | noise |
| trackb-immutable | `immutable.dictionary-build-10k.x10` | b-net10 | 111 (30) | 38.5393 [33.8856, 43.5321] | - | 38.6748 | - | 1.01 | - | baseline | - |
| trackb-immutable | `immutable.dictionary-build-10k.x10` | b-net11 | 111 (30) | 37.8250 [33.3452, 42.7024] | -1.9% [-3.2, -0.9] | 37.9639 | -1.8% [-4.0, -0.9] | 1.01 | 1/30 | noise | noise |
| trackb-immutable | `immutable.builder-add-10k.x10` | b-net10 | 111 (30) | 38.7424 [34.0966, 43.6912] | - | 38.9147 | - | 1.01 | - | baseline | - |
| trackb-immutable | `immutable.builder-add-10k.x10` | b-net11 | 111 (30) | 37.5550 [33.0723, 42.3307] | -3.1% [-4.5, -2.2] | 37.7314 | -3.0% [-5.1, -2.2] | 1.01 | 2/30 | noise | noise |
| trackb-immutable | `immutable.dictionary-lookup.x10k` | b-net10 | 111 (30) | 1.5285 [1.3591, 1.7130] | - | 1.5499 | - | 1.04 | - | baseline | - |
| trackb-immutable | `immutable.dictionary-lookup.x10k` | b-net11 | 111 (30) | 1.5472 [1.3734, 1.7305] | +1.2% [-0.2, +2.1] | 1.5649 | +1.0% [-1.1, +1.8] | 1.03 | 0/30 | noise | noise |
| trackb-stj | `stj.cold.first-serialize-reflection` | b-net10 | 111 (30) | 24.9818 [21.9938, 28.2730] | - | 24.9818 | - | 1.00 | - | baseline | - |
| trackb-stj | `stj.cold.first-serialize-reflection` | b-net11 | 111 (30) | 22.8411 [20.2210, 25.7385] | -8.6% [-10.6, -5.4] | 22.8411 | -8.6% [-10.6, -5.5] | 1.00 | 28/30 | inconclusive | faster |
| trackb-stj | `stj.serialize-reflection.x10k` | b-net10 | 111 (30) | 24.3570 [21.4956, 27.3541] | - | 24.4672 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.serialize-reflection.x10k` | b-net11 | 111 (30) | 23.3726 [20.6000, 26.3366] | -4.0% [-5.2, -2.7] | 23.5023 | -3.9% [-5.1, -2.7] | 1.01 | 7/30 | inconclusive | noise |
| trackb-stj | `stj.deserialize-reflection.x10k` | b-net10 | 111 (30) | 38.3371 [34.1772, 42.8801] | - | 38.4901 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.deserialize-reflection.x10k` | b-net11 | 111 (30) | 36.1245 [31.8914, 40.5699] | -5.8% [-7.1, -4.3] | 36.2741 | -5.8% [-7.0, -4.2] | 1.01 | 16/30 | inconclusive | noise |
| trackb-stj | `stj.serialize-sourcegen.x10k` | b-net10 | 111 (30) | 15.6985 [13.8655, 17.6359] | - | 15.7877 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.serialize-sourcegen.x10k` | b-net11 | 111 (30) | 14.9298 [13.1481, 16.7790] | -4.9% [-6.1, -3.7] | 14.9832 | -5.1% [-6.0, -3.8] | 1.01 | 12/30 | inconclusive | noise |
| trackb-stj | `stj.deserialize-sourcegen.x10k` | b-net10 | 111 (30) | 37.1828 [33.0310, 41.6391] | - | 37.2909 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.deserialize-sourcegen.x10k` | b-net11 | 111 (30) | 35.2828 [31.2823, 39.5627] | -5.1% [-6.2, -3.5] | 35.4400 | -5.0% [-6.0, -3.4] | 1.01 | 14/30 | inconclusive | noise |
| trackb-stj | `stj.utf8jsonreader-1mb.x10` | b-net10 | 111 (30) | 26.0097 [23.0592, 29.1405] | - | 26.0594 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.utf8jsonreader-1mb.x10` | b-net11 | 111 (30) | 25.4712 [22.5245, 28.4735] | -2.1% [-3.8, -1.0] | 25.5206 | -2.1% [-3.6, -1.1] | 1.02 | 1/30 | noise | noise |
| trackb-stj | `stj.jsondocument-parse-1mb.x10` | b-net10 | 111 (30) | 41.5559 [36.9197, 46.4012] | - | 41.5975 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.jsondocument-parse-1mb.x10` | b-net11 | 111 (30) | 40.8074 [36.2124, 45.5609] | -1.8% [-3.1, -0.5] | 40.9235 | -1.6% [-2.8, -0.5] | 1.01 | 1/30 | noise | noise |
| trackb-stj | `stj.jsonnode-parse-read.x10k` | b-net10 | 111 (30) | 34.5804 [30.6974, 38.7086] | - | 34.7540 | - | 1.01 | - | baseline | - |
| trackb-stj | `stj.jsonnode-parse-read.x10k` | b-net11 | 111 (30) | 33.2447 [29.4221, 37.1830] | -3.9% [-5.2, -2.3] | 33.4689 | -3.7% [-5.0, -2.2] | 1.01 | 7/30 | inconclusive | noise |

# Statistics

Unit of observation: one process (its median batch time). 95% bootstrap CI of the median with 10,000 resamples; two-sided Mann-Whitney U against the baseline. |change| < 5% is reported as `noise` regardless of p; `inconclusive` means the CI of the change crosses 0 or p >= 0.05.

| scenario | scope | point | n | median ms [95% CI] | change vs baseline [95% CI] | MW p | verdict |
|---|---|---|---:|---|---|---:|---|
| cache | `cache.cold.create-and-first-set` | e0-7 | 2 | 2.6650 [2.5918, 2.7382] | - | - | baseline |
| cache | `cache.cold.create-and-first-set` | e2-10 | 2 | 2.8717 [2.8072, 2.9362] | +7.8% [+2.5, +13.3] | 0.333 | inconclusive |
| cache | `cache.set.x10k` | e0-7 | 2 | 2.3593 [2.2938, 2.4248] | - | - | baseline |
| cache | `cache.set.x10k` | e2-10 | 2 | 2.0152 [1.9988, 2.0316] | -14.6% [-17.6, -11.4] | 0.333 | inconclusive |
| cache | `cache.try-get-hit.x10k` | e0-7 | 2 | 0.5530 [0.5161, 0.5898] | - | - | baseline |
| cache | `cache.try-get-hit.x10k` | e2-10 | 2 | 0.5079 [0.4424, 0.5734] | -8.1% [-25.0, +11.1] | 0.667 | inconclusive |
| cache | `cache.try-get-miss.x10k` | e0-7 | 2 | 0.4342 [0.4260, 0.4424] | - | - | baseline |
| cache | `cache.try-get-miss.x10k` | e2-10 | 2 | 0.3809 [0.3604, 0.4014] | -12.3% [-18.5, -5.8] | 0.333 | inconclusive |
| cache | `cache.get-or-create.x10k` | e0-7 | 2 | 2.8180 [2.4904, 3.1457] | - | - | baseline |
| cache | `cache.get-or-create.x10k` | e2-10 | 2 | 2.3921 [2.3593, 2.4248] | -15.1% [-25.0, -2.6] | 0.333 | inconclusive |
| cache | `cache.size-limit-compact-half.x1` | e0-7 | 2 | 2.4084 [1.7367, 3.0802] | - | - | baseline |
| cache | `cache.size-limit-compact-half.x1` | e2-10 | 2 | 4.1288 [4.0632, 4.1943] | +71.4% [+31.9, +141.5] | 0.333 | inconclusive |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e0-7 | 2 | 0.7537 [0.7373, 0.7700] | - | - | baseline |
| cache | `cache.parallel8-hit-miss.per-thread-x10k` | e2-10 | 2 | 0.7946 [0.7373, 0.8520] | +5.4% [-4.3, +15.6] | 1 | inconclusive |
| config | `config.cold.build-json2000` | e0-7 | 2 | 7.1128 [6.9649, 7.2607] | - | - | baseline |
| config | `config.cold.build-json2000` | e2-10 | 2 | 7.2934 [7.1199, 7.4669] | +2.5% [-1.9, +7.2] | 0.667 | noise |
| config | `config.cold.bind-poco50` | e0-7 | 2 | 10.0999 [9.8322, 10.3676] | - | - | baseline |
| config | `config.cold.bind-poco50` | e2-10 | 2 | 10.4737 [10.0323, 10.9151] | +3.7% [-3.2, +11.0] | 0.667 | noise |
| config | `config.build-json2000.x10` | e0-7 | 2 | 11.0100 [10.4858, 11.5343] | - | - | baseline |
| config | `config.build-json2000.x10` | e2-10 | 2 | 11.1411 [11.0100, 11.2722] | +1.2% [-4.5, +7.5] | 1 | noise |
| config | `config.indexer.x10k` | e0-7 | 2 | 0.3113 [0.3031, 0.3195] | - | - | baseline |
| config | `config.indexer.x10k` | e2-10 | 2 | 0.3277 [0.3195, 0.3359] | +5.3% [+0.0, +10.8] | 0.414 | inconclusive |
| config | `config.get-section-deep.x10k` | e0-7 | 2 | 1.4459 [0.3359, 2.5559] | - | - | baseline |
| config | `config.get-section-deep.x10k` | e2-10 | 2 | 2.6870 [2.5559, 2.8180] | +85.8% [+0.0, +739.0] | 0.414 | inconclusive |
| config | `config.bind-poco50.x1k` | e0-7 | 2 | 31.1951 [30.9330, 31.4573] | - | - | baseline |
| config | `config.bind-poco50.x1k` | e2-10 | 2 | 29.6223 [28.3116, 30.9330] | -5.0% [-10.0, +0.0] | 0.414 | inconclusive |
| config | `config.reload-json2000.x10` | e0-7 | 2 | 9.8304 [9.6993, 9.9615] | - | - | baseline |
| config | `config.reload-json2000.x10` | e2-10 | 2 | 10.3547 [9.9615, 10.7479] | +5.3% [+0.0, +10.8] | 0.414 | inconclusive |
| di | `di.cold.build100` | e0-7 | 2 | 0.2821 [0.2820, 0.2822] | - | - | baseline |
| di | `di.cold.build100` | e2-10 | 2 | 0.2884 [0.2875, 0.2893] | +2.2% [+1.9, +2.6] | 0.333 | noise |
| di | `di.cold.first-resolve-depth5` | e0-7 | 2 | 3.7902 [3.7316, 3.8488] | - | - | baseline |
| di | `di.cold.first-resolve-depth5` | e2-10 | 2 | 4.3559 [4.3087, 4.4032] | +14.9% [+11.9, +18.0] | 0.333 | inconclusive |
| di | `di.build100.x100` | e0-7 | 2 | 1.0076 [0.9994, 1.0158] | - | - | baseline |
| di | `di.build100.x100` | e2-10 | 2 | 2.2118 [2.0644, 2.3593] | +119.5% [+103.2, +136.1] | 0.333 | inconclusive |
| di | `di.build1000.x10` | e0-7 | 2 | 0.7455 [0.7373, 0.7537] | - | - | baseline |
| di | `di.build1000.x10` | e2-10 | 2 | 1.9825 [1.8022, 2.1627] | +165.9% [+139.1, +193.3] | 0.333 | inconclusive |
| di | `di.validate-on-build100.x100` | e0-7 | 2 | 7.2745 [7.2090, 7.3400] | - | - | baseline |
| di | `di.validate-on-build100.x100` | e2-10 | 2 | 12.5829 [12.0586, 13.1072] | +73.0% [+64.3, +81.8] | 0.333 | inconclusive |
| di | `di.first-resolve-depth5.x100` | e0-7 | 2 | 0.5489 [0.5407, 0.5571] | - | - | baseline |
| di | `di.first-resolve-depth5.x100` | e2-10 | 2 | 1.1305 [1.1141, 1.1469] | +106.0% [+100.0, +112.1] | 0.333 | inconclusive |
| di | `di.transient.x10k` | e0-7 | 2 | 0.2683 [0.1843, 0.3523] | - | - | baseline |
| di | `di.transient.x10k` | e2-10 | 2 | 0.5530 [0.5161, 0.5898] | +106.1% [+46.5, +220.0] | 0.333 | inconclusive |
| di | `di.scoped.x10k` | e0-7 | 2 | 1.0568 [0.9011, 1.2124] | - | - | baseline |
| di | `di.scoped.x10k` | e2-10 | 2 | 1.7859 [1.6384, 1.9333] | +69.0% [+35.1, +114.5] | 0.333 | inconclusive |
| di | `di.singleton.x10k` | e0-7 | 2 | 0.2273 [0.1434, 0.3113] | - | - | baseline |
| di | `di.singleton.x10k` | e2-10 | 2 | 0.4915 [0.4669, 0.5161] | +116.2% [+50.0, +260.0] | 0.333 | inconclusive |
| di | `di.open-generic.x10k` | e0-7 | 2 | 0.2970 [0.1925, 0.4014] | - | - | baseline |
| di | `di.open-generic.x10k` | e2-10 | 2 | 0.5734 [0.5407, 0.6062] | +93.1% [+34.7, +214.9] | 0.333 | inconclusive |
| di | `di.enumerable20.x10k` | e0-7 | 2 | 2.2610 [2.1627, 2.3593] | - | - | baseline |
| di | `di.enumerable20.x10k` | e2-10 | 2 | 2.7197 [2.6870, 2.7525] | +20.3% [+13.9, +27.3] | 0.333 | inconclusive |
| di | `di.create-scope-dispose.x10k` | e0-7 | 2 | 0.8438 [0.7373, 0.9503] | - | - | baseline |
| di | `di.create-scope-dispose.x10k` | e2-10 | 2 | 1.5729 [1.5401, 1.6056] | +86.4% [+62.1, +117.8] | 0.333 | inconclusive |
| ef | `ef.cold.open-connection-create-schema` | e0-7 | 2 | 29.5198 [29.2513, 29.7884] | - | - | baseline |
| ef | `ef.cold.open-connection-create-schema` | e2-10 | 2 | 29.7836 [29.5652, 30.0020] | +0.9% [-0.7, +2.6] | 0.667 | noise |
| ef | `ef.cold.first-query` | e0-7 | 2 | 465.0069 [461.7326, 468.2813] | - | - | baseline |
| ef | `ef.cold.first-query` | e2-10 | 2 | 529.5236 [525.2377, 533.8094] | +13.9% [+12.2, +15.6] | 0.333 | inconclusive |
| ef | `ef.new-context.x1k` | e0-7 | 2 | 18.0879 [17.8258, 18.3501] | - | - | baseline |
| ef | `ef.new-context.x1k` | e2-10 | 2 | 24.9037 [24.6415, 25.1658] | +37.7% [+34.3, +41.2] | 0.333 | inconclusive |
| ef | `ef.pooled-context.x1k` | e0-7 | 2 | 0.1116 [0.1106, 0.1126] | - | - | baseline |
| ef | `ef.pooled-context.x1k` | e2-10 | 2 | 0.1106 [0.1085, 0.1126] | -0.9% [-3.6, +1.9] | 1 | noise |
| ef | `ef.query-where-orderby-take.x1k` | e0-7 | 2 | 103.8090 [102.7604, 104.8576] | - | - | baseline |
| ef | `ef.query-where-orderby-take.x1k` | e2-10 | 2 | 108.0033 [106.9548, 109.0519] | +4.0% [+2.0, +6.1] | 0.333 | noise |
| ef | `ef.save-changes-1000-entities.x1` | e0-7 | 2 | 13.6315 [13.1072, 14.1558] | - | - | baseline |
| ef | `ef.save-changes-1000-entities.x1` | e2-10 | 2 | 13.7626 [13.6315, 13.8936] | +1.0% [-3.7, +6.0] | 1 | noise |
| ef | `ef.detect-changes-10k-tracked.x10` | e0-7 | 2 | 30.9330 [29.3601, 32.5059] | - | - | baseline |
| ef | `ef.detect-changes-10k-tracked.x10` | e2-10 | 2 | 22.8065 [22.5444, 23.0687] | -26.3% [-30.6, -21.4] | 0.333 | inconclusive |
| ef | `ef.to-query-string.x1k` | e0-7 | 2 | 5.7016 [5.6361, 5.7672] | - | - | baseline |
| ef | `ef.to-query-string.x1k` | e2-10 | 2 | 7.4711 [7.4711, 7.4711] | +31.0% [+29.5, +32.6] | 0.221 | inconclusive |
| logging | `logging.cold.factory-create` | e0-7 | 2 | 22.8954 [22.5746, 23.2161] | - | - | baseline |
| logging | `logging.cold.factory-create` | e2-10 | 2 | 26.2364 [26.0282, 26.4445] | +14.6% [+12.1, +17.1] | 0.333 | inconclusive |
| logging | `logging.cold.first-log` | e0-7 | 2 | 3.9052 [3.8701, 3.9403] | - | - | baseline |
| logging | `logging.cold.first-log` | e2-10 | 2 | 4.9614 [4.7850, 5.1377] | +27.0% [+21.4, +32.8] | 0.333 | inconclusive |
| logging | `logging.factory-create.x100` | e0-7 | 2 | 3.2768 [3.2113, 3.3423] | - | - | baseline |
| logging | `logging.factory-create.x100` | e2-10 | 2 | 5.0463 [4.7186, 5.3740] | +54.0% [+41.2, +67.3] | 0.333 | inconclusive |
| logging | `logging.log-information-3args-enabled.x10k` | e0-7 | 2 | 3.2113 [3.1457, 3.2768] | - | - | baseline |
| logging | `logging.log-information-3args-enabled.x10k` | e2-10 | 2 | 4.2926 [4.1288, 4.4564] | +33.7% [+26.0, +41.7] | 0.333 | inconclusive |
| logging | `logging.log-debug-3args-disabled.x10k` | e0-7 | 2 | 0.5571 [0.5407, 0.5734] | - | - | baseline |
| logging | `logging.log-debug-3args-disabled.x10k` | e2-10 | 2 | 0.5816 [0.5734, 0.5898] | +4.4% [+0.0, +9.1] | 0.414 | noise |
| logging | `logging.logger-message-define.x10k` | e0-7 | 2 | 3.4406 [3.4079, 3.4734] | - | - | baseline |
| logging | `logging.logger-message-define.x10k` | e2-10 | 2 | 2.8508 [2.6870, 3.0147] | -17.1% [-22.6, -11.5] | 0.333 | inconclusive |
| logging | `logging.begin-scope.x10k` | e0-7 | 2 | 0.6636 [0.6390, 0.6881] | - | - | baseline |
| logging | `logging.begin-scope.x10k` | e2-10 | 2 | 1.6384 [1.5729, 1.7039] | +146.9% [+128.6, +166.7] | 0.333 | inconclusive |
| options | `options.cold.first-value` | e0-7 | 2 | 15.0443 [14.7784, 15.3103] | - | - | baseline |
| options | `options.cold.first-value` | e2-10 | 2 | 17.4924 [17.3381, 17.6467] | +16.3% [+13.2, +19.4] | 0.333 | inconclusive |
| options | `options.ioptions-value.x10k` | e0-7 | 2 | 0.0594 [0.0573, 0.0614] | - | - | baseline |
| options | `options.ioptions-value.x10k` | e2-10 | 2 | 0.0573 [0.0543, 0.0604] | -3.4% [-11.7, +5.4] | 0.667 | noise |
| options | `options.monitor-current-value.x10k` | e0-7 | 2 | 0.1321 [0.1290, 0.1352] | - | - | baseline |
| options | `options.monitor-current-value.x10k` | e2-10 | 2 | 0.1096 [0.1044, 0.1147] | -17.1% [-22.7, -11.1] | 0.333 | inconclusive |
| options | `options.snapshot-new-scope.x1k` | e0-7 | 2 | 25.4280 [25.1658, 25.6901] | - | - | baseline |
| options | `options.snapshot-new-scope.x1k` | e2-10 | 2 | 24.3794 [23.5930, 25.1658] | -4.1% [-8.2, +0.0] | 0.414 | noise |
| options | `options.validate-data-annotations.x1k` | e0-7 | 2 | 19.9229 [19.3987, 20.4472] | - | - | baseline |
| options | `options.validate-data-annotations.x1k` | e2-10 | 2 | 20.4472 [19.9229, 20.9715] | +2.6% [-2.6, +8.1] | 0.667 | noise |
| trackb-aspnetcore | `aspnetcore.cold.start-kestrel` | b-net10 | 2 | 104.4521 [99.7923, 109.1120] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.cold.start-kestrel` | b-net11 | 2 | 59.3957 [53.0026, 65.7887] | -43.1% [-51.4, -34.1] | 0.333 | inconclusive |
| trackb-aspnetcore | `aspnetcore.cold.first-request` | b-net10 | 2 | 845.1599 [840.4598, 849.8600] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.cold.first-request` | b-net11 | 2 | 897.9061 [880.9186, 914.8935] | +6.2% [+3.7, +8.9] | 0.333 | inconclusive |
| trackb-aspnetcore | `aspnetcore.hello.batch400` | b-net10 | 2 | 100.6633 [100.6633, 100.6633] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.hello.batch400` | b-net11 | 2 | 101.7119 [100.6633, 102.7604] | +1.0% [+0.0, +2.1] | 0.617 | noise |
| trackb-aspnetcore | `aspnetcore.json.batch400` | b-net10 | 2 | 100.6633 [100.6633, 100.6633] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.json.batch400` | b-net11 | 2 | 100.6633 [100.6633, 100.6633] | +0.0% [+0.0, +0.0] | - | noise |
| trackb-aspnetcore | `aspnetcore.routes500.batch400` | b-net10 | 2 | 100.6633 [100.6633, 100.6633] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.routes500.batch400` | b-net11 | 2 | 100.6633 [100.6633, 100.6633] | +0.0% [+0.0, +0.0] | - | noise |
| trackb-aspnetcore | `aspnetcore.hello.request` | b-net10 | 2 | 0.0748 [0.0717, 0.0778] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.hello.request` | b-net11 | 2 | 0.1352 [0.1352, 0.1352] | +80.8% [+73.7, +88.6] | 0.221 | inconclusive |
| trackb-aspnetcore | `aspnetcore.json.request` | b-net10 | 2 | 0.0778 [0.0737, 0.0819] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.json.request` | b-net11 | 2 | 0.1393 [0.1352, 0.1434] | +78.9% [+65.0, +94.4] | 0.333 | inconclusive |
| trackb-aspnetcore | `aspnetcore.routes500.request` | b-net10 | 2 | 0.0788 [0.0737, 0.0840] | - | - | baseline |
| trackb-aspnetcore | `aspnetcore.routes500.request` | b-net11 | 2 | 0.1454 [0.1434, 0.1475] | +84.4% [+70.7, +100.0] | 0.333 | inconclusive |
| trackb-channels | `channels.cold.first-pipeline` | b-net10 | 2 | 7.5459 [7.0689, 8.0228] | - | - | baseline |
| trackb-channels | `channels.cold.first-pipeline` | b-net11 | 2 | 7.0709 [7.0465, 7.0953] | -6.3% [-12.2, +0.4] | 0.667 | inconclusive |
| trackb-channels | `channels.bounded1024-1p4c.x100k-items` | b-net10 | 2 | 22.2822 [19.3987, 25.1658] | - | - | baseline |
| trackb-channels | `channels.bounded1024-1p4c.x100k-items` | b-net11 | 2 | 32.7680 [31.9816, 33.5544] | +47.1% [+27.1, +73.0] | 0.333 | inconclusive |
| trackb-ef | `ef.cold.open-connection-create-schema` | b-net10 | 2 | 27.9682 [26.7920, 29.1444] | - | - | baseline |
| trackb-ef | `ef.cold.open-connection-create-schema` | b-net11 | 2 | 23.2581 [21.1724, 25.3438] | -16.8% [-27.4, -5.4] | 0.333 | inconclusive |
| trackb-ef | `ef.cold.first-query` | b-net10 | 2 | 425.2355 [423.2347, 427.2363] | - | - | baseline |
| trackb-ef | `ef.cold.first-query` | b-net11 | 2 | 453.7668 [448.9185, 458.6151] | +6.7% [+5.1, +8.4] | 0.333 | inconclusive |
| trackb-ef | `ef.new-context.x1k` | b-net10 | 2 | 25.1658 [25.1658, 25.1658] | - | - | baseline |
| trackb-ef | `ef.new-context.x1k` | b-net11 | 2 | 25.4280 [24.6415, 26.2144] | +1.0% [-2.1, +4.2] | 1 | noise |
| trackb-ef | `ef.pooled-context.x1k` | b-net10 | 2 | 0.1147 [0.1147, 0.1147] | - | - | baseline |
| trackb-ef | `ef.pooled-context.x1k` | b-net11 | 2 | 0.1116 [0.1085, 0.1147] | -2.7% [-5.4, +0.0] | 0.617 | noise |
| trackb-ef | `ef.query-where-orderby-take.x1k` | b-net10 | 2 | 109.0519 [109.0519, 109.0519] | - | - | baseline |
| trackb-ef | `ef.query-where-orderby-take.x1k` | b-net11 | 2 | 108.0033 [104.8576, 111.1491] | -1.0% [-3.8, +1.9] | 1 | noise |
| trackb-ef | `ef.save-changes-1000-entities.x1` | b-net10 | 2 | 14.6801 [14.4179, 14.9422] | - | - | baseline |
| trackb-ef | `ef.save-changes-1000-entities.x1` | b-net11 | 2 | 21.6269 [13.8936, 29.3601] | +47.3% [-7.0, +103.6] | 1 | inconclusive |
| trackb-ef | `ef.detect-changes-10k-tracked.x10` | b-net10 | 2 | 22.8065 [22.5444, 23.0687] | - | - | baseline |
| trackb-ef | `ef.detect-changes-10k-tracked.x10` | b-net11 | 2 | 25.4280 [23.5930, 27.2630] | +11.5% [+2.3, +20.9] | 0.333 | inconclusive |
| trackb-ef | `ef.to-query-string.x1k` | b-net10 | 2 | 7.1434 [7.0779, 7.2090] | - | - | baseline |
| trackb-ef | `ef.to-query-string.x1k` | b-net11 | 2 | 7.6022 [7.0779, 8.1265] | +6.4% [-1.8, +14.8] | 1 | inconclusive |
| trackb-immutable | `immutable.cold.first-build-10k` | b-net10 | 2 | 7.3653 [6.7519, 7.9786] | - | - | baseline |
| trackb-immutable | `immutable.cold.first-build-10k` | b-net11 | 2 | 6.6607 [6.6397, 6.6817] | -9.6% [-16.8, -1.0] | 0.333 | inconclusive |
| trackb-immutable | `immutable.dictionary-build-10k.x10` | b-net10 | 2 | 24.6415 [23.0687, 26.2144] | - | - | baseline |
| trackb-immutable | `immutable.dictionary-build-10k.x10` | b-net11 | 2 | 22.8065 [22.5444, 23.0687] | -7.4% [-14.0, +0.0] | 0.414 | inconclusive |
| trackb-immutable | `immutable.builder-add-10k.x10` | b-net10 | 2 | 24.6415 [23.0687, 26.2144] | - | - | baseline |
| trackb-immutable | `immutable.builder-add-10k.x10` | b-net11 | 2 | 22.5444 [22.0201, 23.0687] | -8.5% [-16.0, +0.0] | 0.414 | inconclusive |
| trackb-immutable | `immutable.dictionary-lookup.x10k` | b-net10 | 2 | 1.0158 [0.9830, 1.0486] | - | - | baseline |
| trackb-immutable | `immutable.dictionary-lookup.x10k` | b-net11 | 2 | 1.0240 [1.0158, 1.0322] | +0.8% [-3.1, +5.0] | 1 | noise |
| trackb-stj | `stj.cold.first-serialize-reflection` | b-net10 | 2 | 15.5722 [14.5575, 16.5868] | - | - | baseline |
| trackb-stj | `stj.cold.first-serialize-reflection` | b-net11 | 2 | 13.8767 [13.2532, 14.5001] | -10.9% [-20.1, -0.4] | 0.333 | inconclusive |
| trackb-stj | `stj.serialize-reflection.x10k` | b-net10 | 2 | 14.6801 [13.8936, 15.4665] | - | - | baseline |
| trackb-stj | `stj.serialize-reflection.x10k` | b-net11 | 2 | 14.1484 [13.6168, 14.6801] | -3.6% [-12.0, +5.7] | 0.667 | noise |
| trackb-stj | `stj.deserialize-reflection.x10k` | b-net10 | 2 | 23.0687 [22.5444, 23.5930] | - | - | baseline |
| trackb-stj | `stj.deserialize-reflection.x10k` | b-net11 | 2 | 22.0201 [20.9715, 23.0687] | -4.5% [-11.1, +2.3] | 0.667 | noise |
| trackb-stj | `stj.serialize-sourcegen.x10k` | b-net10 | 2 | 9.4372 [9.1750, 9.6993] | - | - | baseline |
| trackb-stj | `stj.serialize-sourcegen.x10k` | b-net11 | 2 | 9.0440 [8.6508, 9.4372] | -4.2% [-10.8, +2.9] | 0.667 | noise |
| trackb-stj | `stj.deserialize-sourcegen.x10k` | b-net10 | 2 | 22.5444 [22.0201, 23.0687] | - | - | baseline |
| trackb-stj | `stj.deserialize-sourcegen.x10k` | b-net11 | 2 | 21.9234 [20.7782, 23.0687] | -2.8% [-9.9, +4.8] | 1 | noise |
| trackb-stj | `stj.utf8jsonreader-1mb.x10` | b-net10 | 2 | 15.4665 [15.2044, 15.7286] | - | - | baseline |
| trackb-stj | `stj.utf8jsonreader-1mb.x10` | b-net11 | 2 | 15.7286 [14.9422, 16.5151] | +1.7% [-5.0, +8.6] | 1 | noise |
| trackb-stj | `stj.jsondocument-parse-1mb.x10` | b-net10 | 2 | 24.9037 [24.6415, 25.1658] | - | - | baseline |
| trackb-stj | `stj.jsondocument-parse-1mb.x10` | b-net11 | 2 | 25.1658 [24.1172, 26.2144] | +1.1% [-4.2, +6.4] | 1 | noise |
| trackb-stj | `stj.jsonnode-parse-read.x10k` | b-net10 | 2 | 21.2337 [20.4472, 22.0201] | - | - | baseline |
| trackb-stj | `stj.jsonnode-parse-read.x10k` | b-net11 | 2 | 20.4472 [19.3987, 21.4958] | -3.7% [-11.9, +5.1] | 0.667 | noise |

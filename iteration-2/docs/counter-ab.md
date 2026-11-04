# Counter interval A/B

Every interval is compared with interval 0 ms (session counters off), and the candidate default of 1000 ms with the current default of 20 ms, on the same points and scopes. Per scope: median change of per-process medians, bootstrap interval, Mann-Whitney p with Benjamini-Hochberg correction across the scopes of one interval, verdict by the rules of `stats.py` (noise band ±5%). Allocation verdicts compare bytes per batch exactly.

| interval | reference | scopes | noise | faster | slower | inconclusive | allocations same | allocations differ |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 20 | 0 | 92 | 52 | 0 | 0 | 40 | 90 | 2 |
| 1000 | 0 | 92 | 56 | 0 | 0 | 36 | 89 | 3 |
| 1000 | 20 | 92 | 64 | 0 | 0 | 28 | 89 | 3 |

## Conclusion

No scope at any interval has a `faster` or `slower` verdict against its reference, so the data do not show that the counter listener changes measured batch times beyond the noise band. Each cell has only 5 processes, so the `inconclusive` rows are lack of power, not evidence of equality. The default stays at 20 ms, which keeps iteration 2 comparable with iteration 1; a longer interval is not shown to be better.

## Scopes outside the noise band

| interval | reference | scenario | point | scope | change | 95% CI | adjusted p | verdict |
|---:|---:|---|---|---|---:|---|---:|---|
| 20 | 0 | di | e0-7 | `di.cold.build100` | -39.5% | [-43.0, -37.1] | 0.104 | inconclusive |
| 1000 | 0 | di | e0-7 | `di.cold.build100` | -38.9% | [-42.4, -34.8] | 0.0913 | inconclusive |
| 1000 | 0 | di | e2-10 | `di.cold.build100` | -36.2% | [-45.7, -32.6] | 0.0913 | inconclusive |
| 20 | 0 | di | e2-10 | `di.cold.build100` | -35.9% | [-45.3, -32.3] | 0.104 | inconclusive |
| 1000 | 0 | cache | e0-7 | `cache.set.x10k` | +25.8% | [+5.6, +105.5] | 0.225 | inconclusive |
| 20 | 0 | cache | e0-7 | `cache.set.x10k` | +20.9% | [-0.7, +131.3] | 0.319 | inconclusive |
| 1000 | 20 | cache | e0-7 | `cache.get-or-create.x10k` | +12.4% | [-1.0, +15.6] | 0.967 | inconclusive |
| 1000 | 0 | cache | e0-7 | `cache.get-or-create.x10k` | +11.0% | [-2.3, +15.7] | 0.325 | inconclusive |
| 1000 | 0 | cache | e2-10 | `cache.cold.create-and-first-set` | -7.8% | [-9.9, -2.3] | 0.0913 | inconclusive |
| 20 | 0 | cache | e2-10 | `cache.cold.create-and-first-set` | -7.6% | [-9.3, -5.2] | 0.104 | inconclusive |
| 1000 | 0 | cache | e2-10 | `cache.set.x10k` | -7.2% | [-19.7, +7.5] | 0.895 | inconclusive |
| 20 | 0 | config | e0-7 | `config.get-section-deep.x10k` | -6.9% | [-10.0, -4.1] | 0.104 | inconclusive |
| 1000 | 20 | cache | e2-10 | `cache.set.x10k` | -6.5% | [-10.2, +12.1] | 1 | inconclusive |
| 20 | 0 | cache | e0-7 | `cache.cold.create-and-first-set` | -6.3% | [-19.4, -4.7] | 0.104 | inconclusive |
| 1000 | 0 | di | e2-10 | `di.singleton.x10k` | -6.1% | [-10.0, -4.0] | 0.0913 | inconclusive |
| 1000 | 20 | options | e2-10 | `options.ioptions-value.x10k` | -6.0% | [-11.5, +6.6] | 1 | inconclusive |
| 1000 | 0 | options | e2-10 | `options.ioptions-value.x10k` | -6.0% | [-11.6, +6.4] | 0.813 | inconclusive |
| 20 | 0 | di | e0-7 | `di.open-generic.x10k` | +5.9% | [+0.1, +10.4] | 0.266 | inconclusive |
| 1000 | 0 | options | e0-7 | `options.ioptions-value.x10k` | -5.8% | [-11.7, +0.0] | 0.42 | inconclusive |
| 20 | 0 | cache | e0-7 | `cache.try-get-hit.x10k` | +5.7% | [-2.0, +9.7] | 0.639 | inconclusive |
| 1000 | 20 | options | e0-7 | `options.ioptions-value.x10k` | -5.6% | [-11.4, +0.2] | 0.967 | inconclusive |
| 1000 | 0 | cache | e0-7 | `cache.cold.create-and-first-set` | -5.6% | [-18.8, -3.1] | 0.0913 | inconclusive |
| 1000 | 0 | cache | e0-7 | `cache.try-get-hit.x10k` | +5.4% | [-2.1, +9.8] | 0.325 | inconclusive |
| 1000 | 0 | di | e0-7 | `di.open-generic.x10k` | +5.4% | [-1.7, +8.9] | 0.42 | inconclusive |
| 1000 | 0 | config | e0-7 | `config.get-section-deep.x10k` | -5.4% | [-8.7, +0.3] | 0.325 | inconclusive |
| 1000 | 0 | logging | e0-7 | `logging.begin-scope.x10k` | -4.6% | [-7.6, -0.0] | 0.225 | inconclusive |
| 1000 | 0 | config | e2-10 | `config.cold.build-json2000` | -4.6% | [-6.9, -2.9] | 0.0913 | inconclusive |
| 20 | 0 | di | e2-10 | `di.singleton.x10k` | -4.4% | [-13.9, -0.3] | 0.266 | inconclusive |
| 1000 | 20 | cache | e0-7 | `cache.set.x10k` | +4.1% | [-45.6, +70.0] | 1 | inconclusive |
| 1000 | 20 | di | e0-7 | `di.singleton.x10k` | -3.8% | [-5.5, +9.2] | 0.967 | inconclusive |
| 1000 | 0 | config | e0-7 | `config.bind-poco50.x1k` | -3.7% | [-15.1, +3.4] | 0.911 | inconclusive |
| 1000 | 0 | logging | e2-10 | `logging.logger-message-define.x10k` | +3.6% | [-0.4, +6.1] | 0.325 | inconclusive |
| 20 | 0 | di | e2-10 | `di.open-generic.x10k` | +3.6% | [-2.8, +7.3] | 0.647 | inconclusive |
| 20 | 0 | di | e0-7 | `di.transient.x10k` | +3.5% | [-1.4, +7.3] | 0.398 | inconclusive |
| 20 | 0 | config | e2-10 | `config.cold.build-json2000` | -3.5% | [-5.9, +0.5] | 0.319 | inconclusive |
| 20 | 0 | config | e0-7 | `config.bind-poco50.x1k` | -3.4% | [-14.9, +2.1] | 0.79 | inconclusive |
| 1000 | 0 | cache | e0-7 | `cache.parallel8-hit-miss.per-thread-x10k` | +3.2% | [+0.2, +6.9] | 0.225 | inconclusive |
| 1000 | 20 | options | e2-10 | `options.monitor-current-value.x10k` | -3.1% | [-9.5, +3.3] | 0.967 | inconclusive |
| 1000 | 0 | options | e0-7 | `options.monitor-current-value.x10k` | +3.0% | [-0.7, +9.9] | 0.325 | inconclusive |
| 20 | 0 | di | e0-7 | `di.scoped.x10k` | -3.0% | [-13.9, +2.0] | 0.639 | inconclusive |
| 20 | 0 | cache | e0-7 | `cache.parallel8-hit-miss.per-thread-x10k` | +3.0% | [-0.0, +12.0] | 0.319 | inconclusive |
| 1000 | 20 | options | e2-10 | `options.validate-data-annotations.x1k` | +2.6% | [-5.6, +6.0] | 0.967 | inconclusive |
| 1000 | 0 | di | e0-7 | `di.singleton.x10k` | -2.6% | [-5.1, +4.2] | 0.911 | inconclusive |
| 20 | 0 | options | e0-7 | `options.validate-data-annotations.x1k` | +2.6% | [-3.8, +5.1] | 0.895 | inconclusive |
| 1000 | 0 | options | e0-7 | `options.validate-data-annotations.x1k` | +2.6% | [-4.1, +5.0] | 0.895 | inconclusive |
| 1000 | 0 | logging | e0-7 | `logging.log-debug-3args-disabled.x10k` | -2.6% | [-5.1, +1.6] | 0.593 | inconclusive |
| 20 | 0 | di | e2-10 | `di.create-scope-dispose.x10k` | -2.5% | [-6.5, +1.8] | 0.398 | inconclusive |
| 20 | 0 | logging | e0-7 | `logging.begin-scope.x10k` | -2.4% | [-7.3, +2.2] | 0.534 | inconclusive |
| 1000 | 20 | options | e0-7 | `options.monitor-current-value.x10k` | +2.4% | [-1.3, +6.3] | 0.967 | inconclusive |
| 1000 | 0 | logging | e2-10 | `logging.log-information-3args-enabled.x10k` | -2.3% | [-6.2, +2.8] | 0.499 | inconclusive |
| 20 | 0 | options | e2-10 | `options.validate-data-annotations.x1k` | -2.1% | [-9.7, +4.0] | 0.639 | inconclusive |
| 20 | 0 | logging | e2-10 | `logging.cold.first-log` | +2.0% | [-0.3, +7.0] | 0.398 | inconclusive |
| 1000 | 0 | config | e0-7 | `config.indexer.x10k` | -2.0% | [-9.4, +2.3] | 0.499 | inconclusive |
| 20 | 0 | di | e0-7 | `di.cold.first-resolve-depth5` | +1.9% | [-7.2, +3.3] | 0.895 | inconclusive |
| 1000 | 0 | di | e0-7 | `di.cold.first-resolve-depth5` | +1.8% | [-7.3, +3.9] | 0.813 | inconclusive |
| 1000 | 0 | di | e2-10 | `di.create-scope-dispose.x10k` | -1.8% | [-5.8, +2.4] | 0.73 | inconclusive |
| 1000 | 0 | di | e2-10 | `di.open-generic.x10k` | +1.8% | [-4.5, +5.2] | 0.593 | inconclusive |
| 1000 | 0 | config | e2-10 | `config.bind-poco50.x1k` | +1.8% | [-1.5, +18.3] | 0.911 | inconclusive |
| 1000 | 20 | di | e0-7 | `di.scoped.x10k` | +1.7% | [-13.8, +14.4] | 1 | inconclusive |
| 20 | 0 | options | e2-10 | `options.snapshot-new-scope.x1k` | -1.7% | [-6.8, +3.4] | 0.79 | inconclusive |
| 1000 | 20 | config | e0-7 | `config.get-section-deep.x10k` | +1.7% | [-2.9, +7.8] | 0.967 | inconclusive |
| 20 | 0 | di | e2-10 | `di.transient.x10k` | +1.7% | [-1.2, +6.5] | 0.944 | inconclusive |
| 1000 | 20 | di | e2-10 | `di.singleton.x10k` | -1.7% | [-6.2, +9.2] | 0.967 | inconclusive |
| 20 | 0 | cache | e0-7 | `cache.size-limit-compact-half.x1` | -1.7% | [-18.4, +5.4] | 0.895 | inconclusive |
| 1000 | 20 | logging | e2-10 | `logging.log-information-3args-enabled.x10k` | -1.4% | [-7.2, +3.6] | 0.967 | inconclusive |
| 1000 | 0 | options | e2-10 | `options.snapshot-new-scope.x1k` | -1.4% | [-7.8, +2.9] | 0.593 | inconclusive |
| 1000 | 0 | cache | e0-7 | `cache.size-limit-compact-half.x1` | -1.3% | [-18.1, +18.9] | 0.911 | inconclusive |
| 1000 | 0 | di | e0-7 | `di.scoped.x10k` | -1.3% | [-16.4, +1.2] | 0.593 | inconclusive |
| 20 | 0 | di | e0-7 | `di.singleton.x10k` | +1.3% | [-10.8, +7.0] | 0.79 | inconclusive |
| 20 | 0 | cache | e0-7 | `cache.get-or-create.x10k` | -1.3% | [-4.3, +8.7] | 1 | inconclusive |
| 20 | 0 | ef | e2-10 | `ef.pooled-context.x1k` | +1.3% | [-1.9, +6.7] | 0.639 | inconclusive |
| 1000 | 0 | di | e2-10 | `di.transient.x10k` | +1.2% | [-1.2, +8.3] | 0.73 | inconclusive |
| 1000 | 20 | config | e0-7 | `config.indexer.x10k` | -1.2% | [-6.1, +2.5] | 0.967 | inconclusive |
| 1000 | 20 | config | e2-10 | `config.cold.build-json2000` | -1.1% | [-5.0, -0.2] | 0.73 | inconclusive |
| 20 | 0 | cache | e2-10 | `cache.get-or-create.x10k` | +1.1% | [-16.5, +11.5] | 0.944 | inconclusive |
| 1000 | 20 | di | e0-7 | `di.cold.build100` | +1.0% | [-3.2, +7.7] | 0.967 | inconclusive |
| 1000 | 20 | config | e2-10 | `config.bind-poco50.x1k` | +0.9% | [-23.2, +17.3] | 0.967 | inconclusive |
| 1000 | 0 | options | e2-10 | `options.monitor-current-value.x10k` | -0.9% | [-7.4, +0.2] | 0.42 | inconclusive |
| 20 | 0 | config | e0-7 | `config.indexer.x10k` | -0.9% | [-8.4, +4.4] | 0.895 | inconclusive |
| 20 | 0 | config | e2-10 | `config.bind-poco50.x1k` | +0.8% | [-2.2, +32.4] | 0.944 | inconclusive |
| 1000 | 20 | ef | e0-7 | `ef.cold.open-connection-create-schema` | -0.8% | [-6.1, +2.1] | 0.967 | inconclusive |
| 20 | 0 | logging | e2-10 | `logging.log-information-3args-enabled.x10k` | -0.8% | [-4.8, +5.3] | 0.944 | inconclusive |
| 1000 | 0 | config | e2-10 | `config.cold.bind-poco50` | -0.8% | [-4.9, +5.7] | 0.895 | inconclusive |
| 20 | 0 | logging | e2-10 | `logging.cold.factory-create` | +0.8% | [-1.9, +5.9] | 1 | inconclusive |
| 20 | 0 | cache | e2-10 | `cache.set.x10k` | -0.8% | [-14.1, +14.7] | 0.895 | inconclusive |
| 1000 | 0 | cache | e2-10 | `cache.get-or-create.x10k` | +0.7% | [-25.9, +11.1] | 0.911 | inconclusive |
| 1000 | 20 | config | e2-10 | `config.cold.bind-poco50` | -0.7% | [-4.2, +5.8] | 0.967 | inconclusive |
| 20 | 0 | options | e0-7 | `options.monitor-current-value.x10k` | +0.6% | [-3.1, +7.3] | 0.79 | inconclusive |
| 20 | 0 | di | e2-10 | `di.build100.x100` | +0.5% | [-1.1, +5.9] | 0.647 | inconclusive |
| 20 | 0 | config | e0-7 | `config.cold.bind-poco50` | -0.5% | [-1.6, +7.0] | 0.944 | inconclusive |
| 1000 | 20 | di | e0-7 | `di.open-generic.x10k` | -0.5% | [-7.1, +5.2] | 0.967 | inconclusive |
| 1000 | 20 | di | e2-10 | `di.transient.x10k` | -0.5% | [-5.6, +8.5] | 0.967 | inconclusive |
| 1000 | 0 | options | e2-10 | `options.validate-data-annotations.x1k` | +0.4% | [-8.8, +4.0] | 0.813 | inconclusive |
| 20 | 0 | di | e2-10 | `di.cold.first-resolve-depth5` | +0.4% | [-2.3, +11.2] | 1 | inconclusive |
| 1000 | 20 | logging | e2-10 | `logging.cold.factory-create` | -0.4% | [-5.2, +1.2] | 0.967 | inconclusive |
| 1000 | 20 | di | e2-10 | `di.cold.first-resolve-depth5` | -0.4% | [-10.0, +4.4] | 0.967 | inconclusive |
| 1000 | 20 | cache | e2-10 | `cache.get-or-create.x10k` | -0.4% | [-26.7, +20.6] | 0.967 | inconclusive |
| 1000 | 20 | cache | e0-7 | `cache.size-limit-compact-half.x1` | +0.3% | [-6.4, +20.9] | 1 | inconclusive |
| 20 | 0 | ef | e0-7 | `ef.cold.open-connection-create-schema` | +0.3% | [-1.1, +5.9] | 0.647 | inconclusive |
| 1000 | 20 | cache | e2-10 | `cache.cold.create-and-first-set` | -0.2% | [-3.5, +5.8] | 0.967 | inconclusive |
| 20 | 0 | options | e0-7 | `options.ioptions-value.x10k` | -0.2% | [-6.2, +6.4] | 0.398 | inconclusive |
| 1000 | 20 | cache | e0-7 | `cache.parallel8-hit-miss.per-thread-x10k` | +0.2% | [-7.9, +3.8] | 0.967 | inconclusive |
| 1000 | 20 | config | e0-7 | `config.cold.bind-poco50` | +0.1% | [-7.0, +1.7] | 0.967 | inconclusive |
| 20 | 0 | options | e2-10 | `options.ioptions-value.x10k` | +0.0% | [-6.1, +6.2] | 0.944 | inconclusive |

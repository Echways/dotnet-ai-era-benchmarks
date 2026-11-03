# Microsoft's .NET libraries before and after AI

Did Microsoft's code get worse with AI? This repository measures the part of that question that can be measured: performance of .NET library hot paths across releases, with git history showing who changed what.

| iteration | scope | status |
| --- | --- | --- |
| [iteration 1](iteration-1/README.md) | DI, configuration, options, logging, caching and EF Core on six versions from 6.0 to 11.0 RC3, plus a Track B comparison of net10.0 against net11.0 | complete |
| [iteration 2](iteration-2/README.md) | a wider range of studied code | planned |

Each iteration is self-contained: its own solution, scripts, results and documentation. Run commands from inside the iteration directory.

## License

[MIT](LICENSE)

# Domain order measuring tool (P5-1, P5-1b)

Re-measures the domain order of the maintainability refactor
([MaintainabilityRefactorDomainOrder.md](../../../SWLOR.Game.Server/Readmes/MaintainabilityRefactorDomainOrder.md)) after
each Phase 5 item and in P5-15. The guard in `SWLOR.Architecture.Tests` keeps the baselines (`DomainCycles.txt`,
`UpwardEdges.txt`, `DeferredResolution.txt`, `SinkWork.txt`); this tool adds what the guard does not: the classes of each
reference, the planned type moves, the pairs at each step, the order-cost search and the fix and item of every upward edge.

## Run

```sh
dotnet build SWLOR.Game.Server/SWLOR.Game.Server.csproj -c Release -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
bash tools/Refactor/DomainOrder/run_all.sh C:/Temp/domain-order
```

`run_all.sh <out> [repository root] [configuration]` takes about a minute and needs `python` on the path. Write `<out>`
outside the repository: `refs.tsv` is about 100 MB.

| Step | Writes | What |
|---|---|---|
| `EdgeExtract` (C#) | `refs.tsv`, `types.tsv` | compiles the server from source and binds every name as the compiler does: one line per source member, referenced type and kind of use, and the shape of every type |
| `py/guard_figures.py` | `guard.json` | today's pairs by band and upward references, counted as the guard counts them; the totals must equal `DebtReport.md` |
| `py/order_cost.py` | `moves.json` | the type moves for the order in `DomainMap.json`, its cost and the cheapest order the dynamic program finds |
| `py/pair_steps.py` | `stages.json` | the pairs at each step of section 4 ("Pairs at each step") |
| `py/upward.py` | `upedges.json` | every upward edge left after the moves, with its fix and item |
| `py/appendix_c.py` | `appendixC.md` | Appendix C of the guide |

The scripts read `DOMAIN_ORDER_OUT` (set by `run_all.sh`), `DOMAIN_ORDER_REPO` and `DOMAIN_ORDER_MAP` (default: the
repository's `SWLOR.Architecture.Tests/DomainMap.json`, whose `order` is measured). `classify.py`, `sim.py`, `simc.py` and
`moves.py` are the shared rules: the reference classes (vocabulary, definition, service call, other), the order nodes and the
move rule. The moves, families and fix rules encode the approved P5-1 plan; once an item lands, its moves find nothing left to
move.

## Keep in step with the guard

`EdgeExtract` and `SWLOR.Architecture.Tests/Debt/SourceReferenceScanner.cs` record the same references, and
`guard_figures.py` and `Debt/DomainCycleScanner.cs` apply the same rules. Change both together, and check that
`guard_figures.py` prints the totals of `DomainCycles.txt` and `UpwardEdges.txt` in `Baseline/Phase5/DebtReport.md`.

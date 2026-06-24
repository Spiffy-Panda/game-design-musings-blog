# Dead Reckoning

*Minimalist taken at its word: no simulation at all — a deterministic, seed-driven deck of discrete events, ticking toward an overrun you cannot outrun.*

> The war isn't modeled. It's a clock. Each haul is a hand. You don't simulate the catastrophe — you draw cards until it reaches you.

## The spark

The other two readings hear "minimalist interface, maximalist simulation." A living market that finds its own prices; a front line that flows across the map like a fluid. Both go deep into the math. Both are *right*, and both quietly smuggle a whole engine in behind the small UI.

This reading hears the word literally. **Minimalist means minimal — including the simulation.** No market solver. No field integrator. No agents, no equilibrium, no continuous anything. The entire game is a **deterministic interpreter walking a tree of hand-authored events**, seeded once at the start of the run. Closer to *FTL* or *Inscryption* or a good Choose-Your-Own-Doom than to *Stellaris*. A tightly authored board game in space, where the board is a deck and the dice are already cast the moment you press start.

The provocation, stated flat so you can argue with it: **the richest version of this whole musing might need the least simulation technology and the most content tooling.** You don't buy depth with a physics engine. You buy it with authored situations and the combinations they fall into. The war never appears as a number you can push. It appears as a clock you can hear, and a series of impossible little choices about what fits in the hold.

## Core loop, ironed out

A run is a sequence of **hands**. You take a contract, resolve a short chain of discrete events, bank what survives, and the clock ticks. Then again. Then the overrun.

Here are the load-bearing decisions, committed.

**DEAD.1 — The minimalist floor is genuinely tiny: 5 systems, 4 goods, ~12 cards in play at once.** Five named nodes on a static line — not a map you navigate, a *string of beads* you move along. Four goods, exactly the canon ones: `ore`, `fuel`, `munitions`, `rations`. The active deck for any single run is around a dozen cards drawn from a much larger authored pool. This is the lowest floor of the three readings *by construction* — a market needs enough goods and venues to find prices; a fluid front needs enough map to flow across. A deck needs only enough cards to keep combinations fresh, and you control that by authoring, not by widening the sim. Small surface, deep pool.

**DEAD.2 — The war is a pressure clock, not a world.** One integer: `pressure`, climbing every tick. There is no battle, no fleet, no territory you can inspect. As `pressure` crosses authored thresholds, the deck *escalates* — calm contracts retire, a "front collapsing" tier shuffles in, lanes start arriving pre-mined. You never see the war. You feel it in the cards getting meaner. The overrun is the clock running out, full stop.

**DEAD.3 — A hand is: pick a contract, resolve 3–5 discrete event beats, settle.** You draw a few contract cards (a route, a cargo ask, a payout). Choose one. It unfurls into a short chain of *event beats*, each a small node with two or three legible choices and a deterministic outcome. A lane is mined — burn fuel to detour, or risk the straight run. A depot is overrun before you arrive — dump the cargo cheap or carry it to the next bead. A refugee ship hails you. **Your hold fits munitions or rations, not both.** Three to five beats, then the hand settles: margin banked, prestige accrued, clock advanced. Few choices, each one heavy.

**DEAD.4 — Prices are a small deterministic table the clock perturbs, not a market.** A flat lookup: a base price per good per node. Each tick, the seeded interpreter applies *authored swings* keyed to `pressure` and to which events fired — `munitions` spikes at the bead nearest the front; `rations` craters where a depot just fell. No supply-and-demand search, no agents reacting. The swings are scripted, but seeded and combinatorial, so you *learn to read them* like weather without there being any weather system underneath. A table and a rulebook, not an economy.

**DEAD.5 — A run is measured in clock ticks, and it is short: roughly 12–20 hands until overrun.** Not jumps, not minutes, not front-distance — **ticks.** One hand, one (or a few) ticks. Twenty minutes, give or take. Short enough that the inevitable ending lands as a *clean punctuation mark* rather than an exhausting grind, and short enough to invite the run right back.

**DEAD.6 — Prestige edits the deck, never the clock.** This is the firm line. Prestige buys **content, not parameters.** The canon trio re-reads cleanly: *starting capital* becomes a richer opening hand; *production-station locations* becomes **which contract-givers exist on your line** — you are literally choosing which cards can appear; *ship & route upgrades* become new event-resolution options (a bigger hold unlocks the "carry both" beat that was greyed out; better range unlocks detour choices). You are editing the deck between runs. You never touch `pressure`. The war does not get easier — your *hand* gets richer.

**DEAD.7 — The campaign win is a meta-counter, and prestige touches the war ONLY through it.** Strong hands deposit into a persistent `supply-line-held` counter across runs. Fill it and the front finally holds — eXterminate happens off-screen, exactly as canon promises. Crucially this is **indirect and discrete**: a counter ticking up over many loops, never a field you deform or a price you push. You out-logistic the loss curve by playing enough good hands, not by buying down the threat. The war stays sovereign and unreachable; your only verb is *throughput*, summed.

## What's "simulated" (and what isn't)

Almost nothing is simulated. What looks like simulation is a **deterministic interpreter over authored content**, and the distinction is the entire architecture.

The machine holds a small, fully-serializable state: `seed`, current `pressure`, the drawn deck and discard, cargo in hold, cash, the prestige-unlocked content set, and an append-only `input log` of every choice you've made. That's the whole world. There is no continuous variable anywhere — no position integrated over time, no price converging toward a clearing point. State changes only in discrete jumps, only when you act or the clock ticks.

Every stochastic call routes through **one seeded PRNG**. Card draws, which authored swing fires, which event variant you get — all of it pulls from the same deterministic stream. Same seed plus same inputs equals byte-identical run, every time. A "tick" is a pure function: take state, advance `pressure`, escalate the deck if a threshold crossed, resolve the chosen beat against the table, return new state. No solver iterates. No field relaxes. The interpreter just *reads the next authored node and applies it*.

Determinism isn't a nicety here — it's the feature that pays for everything. **Shareable seeds:** "try seed 4471, the rations run that strands you at bead 3." **A daily run:** everyone in the world plays the same seed, same deck, same overrun, and compares how much they moved. **Free replay and ghosts:** because save *is* seed-plus-input-log, you can replay any run exactly, watch it back, diff two attempts beat for beat. You get all of that for nothing, precisely *because* you refused to simulate.

## Technology & architecture

Names, and positions taken.

**Engine: Godot 4, GDScript.** This is a turn-based, event-driven UI game — a deck, a few panels, an animated clock. It is not remotely physics- or compute-bound. Godot's scene/node model maps cleanly onto a card-and-panel UI, and content-as-Resource (below) is a first-class fit. Reach for the boring, productive tool.

**PRNG: an explicit, seedable, splittable generator — PCG32.** *Not* the language default and *never* the global RNG. PCG32 is tiny, fast, well-distributed, and trivially serialized to a couple of integers. "Splittable" matters: derive independent sub-streams per concern (one for draws, one for swing selection, one for event variants) so adding a new content type later doesn't reshuffle every existing seed. In Godot you'd wrap `RandomNumberGenerator` with a fixed `seed`/`state`, or vendor a ~40-line PCG implementation if you want bit-identical streams across platforms — and you do, for the daily run.

**Core: a finite state machine driving a content interpreter.** The FSM is small and explicit — `DRAW_CONTRACTS → RESOLVE_BEAT → SETTLE → TICK → (loop | OVERRUN)`. The interpreter is the heart: it reads the next content node, presents choices, applies the deterministic outcome, mutates state. Every authored event is data the interpreter walks, never bespoke code. One walker, thousands of nodes.

**Content-as-data: Godot Resources (`.tres`), authored against a strict schema.** Each card, event beat, contract, swing rule, and pressure threshold is a typed Resource. `.tres` is human-diffable, loads natively, and gets you a typed authoring surface in the editor for free — and an unused field fails at load, not at runtime. Author in `.tres`; if balancing wants spreadsheet-speed bulk edits, keep a JSON or CSV source of truth and bake it to `.tres` with a small build step. The **authoring/balancing pipeline is the real product here** — a content linter (every threshold reachable, no dead branch, no good with no buyer), a seed sweeper that auto-plays N seeds and reports overrun-tick distributions and choice frequencies, and a "deck preview" that lists what can appear at each pressure tier. You are building a content tool that happens to ship a game attached.

**Save = seed + input log. That's the whole file.** A run is `(seed, [choices])`. Bytes, not a state snapshot — you replay the log through the deterministic interpreter to reconstruct any moment. The daily run is one shared seed. A shared run is a short string a friend can paste. This is only possible because nothing is simulated.

**No physics engine. No economy solver. On purpose.** There is no continuous space to integrate, so there is no rigid-body, no collision, no movement system — beads on a line, not ships in a void. There is no emergent market to converge, so there is no agent loop, no auctioneer, no price search — a lookup table the clock perturbs. Every minute *not* spent tuning a solver's stability is a minute spent writing the next event chain. That is the trade, made deliberately.

**Prototype first:** the single-hand vertical slice. One seed, one contract, a 4-beat chain ending in the munitions-or-rations squeeze, prices from a hardcoded table, and the clock ticking once at the end. No prestige, no meta-counter, ~15 hand-written cards. If that one hand feels heavy — if the squeeze stings — the design is real and everything else is volume. If it feels flat, no amount of engine will save it.

**Key risks, named honestly:**

- **Content volume is the whole cost.** Depth lives entirely in authored cards and their combinations. Thin content reads as a thin game, and there is no simulation to paper over the gaps. This is the bet, and it is a *content* bet.
- **Repetition / déjà vu.** A dozen active cards per run will recur. Mitigation is combinatorial design — beats that recombine and read differently against different cargo and pressure — plus tight per-run draw variety. Authorable, but only if you watch for it.
- **Balance is hand-tuned, not emergent.** No equilibrium settles the numbers for you; every swing and threshold is a value someone chose. The seed sweeper is not optional — it's how you keep the overrun landing in the 12–20 hand window and catch the dominant degenerate line before a player does.

## What this buys, and what it costs

The honest ledger against the siblings.

```
                     Dead Reckoning      Economic (market)    Spatial (fluid front)
  simulation tech    almost none         agent-based solver   field integrator
  cost center        authoring content   tuning the sim       tuning the sim
  cheapest to        ----- build -----   ----                 ----
  most expensive to  ----- author ----   ----                 ----
  determinism        free, total         hard (agent chaos)   hard (numeric drift)
  daily / shared run trivial             awkward              awkward
  depth comes from   combinations        emergent prices      emergent geography
  fails if           content runs thin   sim feels gamey      front feels mushy
```

**What it buys:** the cheapest thing to *engineer* and the most legible thing to *play*. Determinism, seeds, daily runs, and replay come free. The minimalist floor is the lowest of the three — five beads, four goods, a dozen cards — because a deck scales by authoring, not by widening a model. And the texture is fully art-directed: every close call, every moral squeeze, every cruel reveal is a thing a writer *chose*, not an accident the math coughed up.

**What it costs:** it is the most expensive to *author*, and the depth ceiling is exactly the size of the content library. The two simulation readings get emergence for free once the engine runs — a market surprises its own designer; a front finds routes nobody drew. This one only surprises you in combinations someone already wrote down. There is no engine to hide behind. If the writing is thin, the game is thin, and the only fix is more writing.

That is the wager of taking *minimalist* at its word: spend on words, not on math. Build the smallest possible machine, and pour everything you saved into the deck it walks.

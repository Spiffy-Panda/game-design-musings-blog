# The Invisible Hand

*You are not trading inside an economy. You are racing an equilibrium that wants to erase you.*

> The war isn't the enemy. The market is. The front line is just the hungriest buyer in it.

## The spark

The other two readings of this game treat price as set dressing — a number a designer painted onto a system so you'd have a reason to fly there. Throw that out. Here, **nobody places the prices.** There is no price curve in a spreadsheet, no "this system pays well for munitions" flag. There is only a population of economic agents — things that *make* goods, things that *eat* goods, and other haulers who want the same margin you do — and prices are what fall out when they all try to clear at once.

Buy ore in a quiet system and you nudge its price up; you were a buyer, and buyers are bullish. Dump that ore into a shortage at the front and you nudge it down — and you deny that exact margin to a rival who was three jumps out, banking on it. **Shortage is not a location. It's a transient disequilibrium**, a gap between what the front is screaming for and what production can deliver, and it stays open only until the rest of the market closes it. You don't get paid for being somewhere. You get paid for being *early* — for finding a gap and filling it before the invisible hand does.

The front, in this framing, is gorgeously simple: it's the **largest, most ravenous, and most mobile consumer on the board.** It doesn't "advance" on a scripted timer. It advances because it is eating its supply faster than anyone can deliver it, and a sink that big drags every price gradient toward itself. You are a single porthole onto a living market. The interface is four prices and a few lanes. The thing behind the glass is an economy that is actively, indifferently trying to take your lunch.

## Core loop, ironed out

Concretely, here is what a run is, and the decisions are made, not deferred.

**`HAND.1` — The board is six systems and four goods.** That's the minimalist floor, and it's deliberately tiny. Six markets, fully connected by lanes of varying length, trading `ore`, `fuel`, `munitions`, `rations`. Four goods is enough for genuine substitution pressure (the front will take rations *or* munitions, but munitions clear at a steeper premium when a push is on) without becoming a commodities desk. Six markets is enough for a price gradient to *exist* and for a rival to beat you to one node. Fewer than ~5 systems and arbitrage collapses into a single see-saw; more than ~8 and the player can no longer hold the whole market in their head, which kills the legibility that makes it minimalist.

**`HAND.2` — The clock is the front's consumption, not a turn counter.** A run is not measured in jumps or minutes. It's measured in **how far the front has eaten.** Every tick the front consumes goods; unmet demand pushes the line forward; the line forward cuts lanes and swallows the system behind it. So the clock is literally the integral of the shortage you *failed* to cover. Supply it well and the line crawls. Let a shortage fester and it lurches, and a market you were counting on next jump is suddenly behind enemy lines. The doomsday timer is endogenous — you are not racing a clock, you are racing the consequences of your own coverage.

**`HAND.3` — You move prices, and the market remembers.** Every transaction applies a real impulse to local price via a depth/elasticity rule (below). A thin market lurches when you trade into it; a deep one barely notices. This means **your own success is self-correcting** — milk one fat route and you flatten the very gradient you were living on. The game pushes you to keep moving, keep finding the *next* gap, which is exactly the trader's-eye fantasy.

**`HAND.4` — Rivals are agents, and some of them are you.** AI haulers run the same arbitrage search you do, and crucially, the **ghosts of your past runs are seeded back in as rival agents.** Last loop's route is now a competitor front-running you on the corridor you taught it. This is how the population stays adversarial without a difficulty knob — you are increasingly competing against your own best ideas.

**`HAND.5` — Prestige edits the agent population, not your stats.** Being overrun banks prestige, and prestige doesn't (mostly) buy bigger numbers — it **seeds the simulation.** "Production-station locations" means you literally instantiate a new producer agent on the map for next run, permanently reshaping where supply originates and therefore every downstream gradient. Starting capital and ship upgrades (hold, range, turnaround) exist, but they're the *small* tree. The big tree is cartographic: you are slowly redrawing the economy you'll be born into.

**`HAND.6` — Prestige never touches the war directly. Only the economy.** You cannot buy "the front holds." You can only buy producers, position, and reach. The campaign win is therefore an **equilibrium victory**: across enough loops, your seeded producers plus your throughput shift the long-run clearing point until production meets the front's consumption faster than attrition destroys it. The line stops because *the market finally clears in the front's favor* — and you're the one who bent it there, one manifest at a time. The war is downstream of the economy, always. You never get to reach past the ledger and touch it.

## What's being simulated

Three agent classes, each holding minimal state:

- **Producers** (mines, refineries, factories): a good type, an output rate, a capacity ceiling, and a current inventory. They emit on their own cadence regardless of you.
- **Consumers** (the front, civilian populations, depots): a demand rate per good and a current backlog. The front's demand rate is large and *grows* with the unmet-backlog integral — hunger compounds.
- **Haulers** (rival AIs + your run-ghosts): capital, a hold, a position, and a greedy arbitrage policy — buy where (price + projected impact) is low, sell where it's high, net of lane cost.

**Each system is a market with a local price per good.** State per market-good is just `(inventory, last_price)`. The update rule each tick is a lightweight **local clearing**, not a global Walrasian solve: producers add to inventory, consumers draw from it, agents (you included) post their buys and sells, and price adjusts by a **constant-elasticity rule on net order flow** — `Δprice ∝ (net_demand / market_depth)`. Excess demand lifts price; a glut drops it; the proportionality is the elasticity, and `market_depth` is what makes your single-ship impact feel small in a deep market and brutal in a thin one. It's a damped excess-demand adjustment — *tâtonnement that never reaches the fixed point*, because producers and the front keep moving it. That permanent not-quite-cleared state **is** the gameplay.

**Time is fixed-timestep ticks**, several per in-game "hour," decoupled from frame rate. Everything — production, consumption, price adjustment, rival decisions — happens on the tick. That's non-negotiable for the next part.

**Determinism is the whole ballgame for a roguelite.** A run is a seed. All stochastic draws (production jitter, rival spawn, event timing) come from a single seeded PRNG, advanced only on ticks, in a fixed agent-iteration order. Same seed plus same player inputs ⇒ the same market, byte for byte. This is what makes a "lost" run reproducible, shareable, and — critically — replayable as a ghost.

## Technology & architecture

Take a position. Here's the stack I'd actually reach for.

**Agent-based simulation, not an order book.** A full limit-order-book exchange per system is the wrong tool — it's maximalist in the *interface* (depth, spreads, fills) when our whole bet is a minimalist surface over a maximalist model. We want emergent prices from agent behavior, not a microstructure trading sim. So: **agent-based modelling with a per-market constant-elasticity excess-demand clearing rule** (the damped-tâtonnement update above). It's cheap, it's legible, and it produces exactly the "gap opens, gap closes" texture the loop needs.

**Data-oriented, not an actor model.** With six systems the agent count is small — dozens of producers/consumers, tens of rivals, maybe a few hundred entities at the deep end. That's trivial per tick; the constraint isn't throughput, it's **determinism**. Actor-model concurrency (Akka-style, or async tasks) reintroduces nondeterministic scheduling — the one thing we cannot have. So run it **single-threaded over flat, struct-of-arrays component buffers**: a lightweight ECS (Godot fits via a hand-rolled SoA layer, or Bevy/`hecs` if this were Rust), iterated in fixed order. Data-oriented design here isn't a performance flex; it's how you guarantee the same seed yields the same market. Parallelism, if ever needed, goes wide over *independent markets within a tick*, never across the agent loop.

**Past runs persist as a market snapshot plus an event log — both.** Two jobs, two tools. (1) To *re-seed* the world, serialize an end-of-run **snapshot**: producer placements and the equilibrium the market settled near. That's the cartographic prestige state. (2) To run a **ghost rival**, record that run's input/decision stream as a compact **event log** and replay it against the new deterministic sim. This is event sourcing in miniature — the snapshot is the materialized view, the log is the ghost. We get reproducible ripples from old runs without storing whole histories forever.

**What to prototype first:** the single-market clearing cell. One system, one good, a producer, a consumer, and a player who can buy/sell — and tune the elasticity-and-depth rule until trading visibly moves the price and the price visibly heals. If that one cell doesn't *feel* like a market reacting to you, nothing scales up. Build it headless, drive it from a seeded script, assert byte-identical reruns from day one.

**Key risks, named:**

- **Legibility collapse.** An emergent system can produce prices the player can't explain, which reads as random, not alive. Mitigation: keep depth/elasticity stable and surface a tiny "why" — a price-trend arrow and a coarse supply/demand tilt per good. The model is maximalist; the *readout* must stay minimalist.
- **Equilibrium runaway / degenerate strategies.** A constant-elasticity rule can oscillate or let one route dominate forever. Mitigation: damping on the adjustment, plus `HAND.3`'s self-correction and `HAND.4`'s ghosts actively arbitraging your favorite gaps shut.
- **Determinism rot.** One unseeded `rand()`, one iteration over a hash map, one float that depends on thread timing, and ghosts and shareable seeds die. Mitigation: a single seeded PRNG, fixed iteration order, integer or fixed-point money, and a reruns-must-match test as a permanent CI gate.

## What this buys, and what it costs

What it buys: a logistics game where the antagonist is genuinely the economy, and *no two runs are scripted* because nothing was scripted to begin with. Shortages you chase are real disequilibria; rivals who beat you there were really running the math; the campaign win is a real macroeconomic outcome you nudged into being. The minimalist surface — four prices, six systems, one ship — sits honestly on top of a model deep enough to surprise its own designer.

What it costs: this is the **hardest of the three approaches to make *feel* good.** Authored content is reliable — a designer hand-tunes a curve and it reads perfectly every time. An emergent market gives that control away. You trade authorial precision for systemic life, and you pay for it in tuning hours, in legibility work, and in the constant discipline of keeping a maximalist simulation reproducible underneath a surface that promised to be small. The bet is that a market that's actually alive is worth more than one that merely looks the part. If you don't believe that, pick a different approach — this one only pays off if you commit to the simulation all the way down.

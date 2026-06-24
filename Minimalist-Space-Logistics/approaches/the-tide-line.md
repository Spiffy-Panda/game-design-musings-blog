# The Tide Line

*The front isn't a timer counting down. It's a fluid sloshing across the map, and your cargo is the only thing pushing back.*

> Delete the empire, keep the supply line — then make the front a physical thing you can see breathe. Every ton you land bends the geometry; every sector you starve caves and drags its neighbors down with it.

## The spark

The other readings of this game treat "the war overruns you" as bookkeeping — a clock, a difficulty curve, a number that ticks up until the run ends. I think that's the most interesting object in the entire design and it deserves to be the **protagonist**.

So: zoom the camera onto the star map and make the front line *the antagonist with a body*. Not "the front advanced 3%." A **simulated boundary** — a pressure field laid over the systems and lanes — where your deliveries are point sources of supply pressure, enemy attrition is a distributed sink, and the line **flows** between them. Land munitions into a contested sector and watch it bulge outward into a salient. Miss a delivery and watch that stretch cave, rupture, and sever the lanes *behind* it — including the one your ship is currently flying down.

The skill stops being "buy low, sell high" and becomes **spatial triage under a moving boundary**: read where the next breakthrough wants to form, decide which sectors you're willing to let die so others hold, and get tonnage to the bulge before physics closes the door. Prices still exist — they're how shortage talks to you — but they are a *readout of the field*, not the game. The field is the game.

Minimalism of *action* (one ship, four goods, a handful of nodes) over maximalism of *consequence* (salients, ruptures, propagating collapse). That tension is the whole pitch, and I'm going to resolve it in favor of the simulation.

## Core loop, ironed out

Here are the decisions, made and load-bearing.

**TIDE.1 — The map is a graph, not a grid.** Systems are nodes; lanes are edges. Pressure is a per-node scalar, flow happens along edges. A grid is seductive (it looks like fluid in screenshots) but a coarse grid of empty sectors is mostly wasted cells, and "a lane got cut" is *legible* on a graph and mushy on a grid. The graph also makes the minimalist floor honest: you can count the nodes.

**TIDE.2 — The minimalist floor is ~12 systems on a roughly linear-with-branches topology.** Fewer than ~8 and a "breakthrough" is just one node flipping — no drama, no flank to lose. The sweet spot is a spine of contested nodes (the front), a couple of rear depots (your sources), and two or three branch lanes that create *flanks* — because a salient is only meaningful if it can be pinched, and a pinch needs an alternate path. Twelve nodes, ~16 lanes. That's small enough to read at a glance and rich enough to rupture interestingly.

**TIDE.3 — Front displacement IS the clock and the score.** Not turns, not jumps, not a wall-clock timer. The run's "time" is the summed position of the front along the spine. The line creeps rearward continuously (attrition wins by default); your deliveries push it forward locally. The run ends when the front overruns your *last reachable depot* — when there's nowhere left to buy. Your score is the integral of tonnage-times-distance you moved before that — *throughput*, weighted by how close to the front you dared deliver. The map is literally the scoreboard.

**TIDE.4 — A delivery is a timed pressure injection, not an instant flip.** Dock at a contested node, unload `munitions`/`fuel`/`rations`, and you deposit a **supply impulse** at that node: a quantity of pressure that then diffuses outward along lanes over the next several timesteps. You don't teleport the line; you prime a pump and the field does the rest. This is what makes positioning matter — deliver to the node *adjacent* to the threatened one and the pressure arrives late and weak.

**TIDE.5 — Collapse propagates along edges, with hysteresis.** When a node's net pressure drops below a rupture threshold, it doesn't just recede — it **dumps enemy pressure into its graph neighbors** (a breakthrough pours through the gap) and *cuts the lanes touching it* for the rest of the run. Neighbors now sit closer to their own thresholds; cascades happen. Hysteresis (rupture threshold lower than the re-stabilize threshold) means a collapsed sector is hard to win back — you feel the line "tear" rather than flicker. This is the failure texture: not a number going red, but a hole opening behind you.

**TIDE.6 — Prestige reshapes geography, never the war's dials.** You never buy "−10% attrition." That would betray the premise. Prestige moves the **depot nodes** (your sources) relative to the front, adds **branch lanes** (new flanks, new ways to reach a salient before it's pinched), and upgrades the ship's **reach** (range = how many lanes deep toward the front you can deliver before getting cut off; hold = impulse size per dock). You don't make the tide weaker. You earn a better shoreline to fight it from.

The loop, on a napkin, same as the original — just with the front made physical:

```
read field  ->  buy at reachable depot  ->  race to the sector about to rupture
   ^                                                    |
   |                                                    v
bank + bend                              dock, inject pressure, watch it diffuse
the prestige  <-  overrun: war crosses  <-  ... or watch the lane behind you cut,
  geography        your last depot           and decide who you let die
```

## What's being simulated

The front is a **scalar pressure field on a graph**, integrated forward at a fixed timestep.

State, per node `i`:
- `p[i]` — net supply pressure (positive = held/pushing, negative = enemy-controlled). This is the field.
- a few constants: attrition sink `a[i]` (how hard the war pulls this node rearward), capacity, and the rupture/restabilize thresholds for TIDE.5.

State, per edge: a conductance (how freely pressure flows along that lane) and an `alive` flag (TIDE.5 cuts set it false).

The update rule is **graph diffusion plus a constant sink plus your impulses** — discrete reaction-diffusion on a network:

```
p_next[i] = p[i]
          + dt * ( SUM_over_live_edges(i,j)  k_ij * (p[j] - p[i])   # diffusion / advection along lanes
                   - a[i]                                            # attrition: the war, always pulling down
                   + s[i] )                                         # your delivered supply impulses, decaying
```

That diffusion term is just the **graph Laplacian** applied to `p` — `p_next = p + dt*(-L·p - a + s)`. Advection (the front having a *direction* it wants to move) falls out by biasing `k_ij` toward the rear, so pressure drains rearward faster than it climbs forward. After each step, run the threshold pass: any node below rupture dumps pressure to neighbors and kills its edges; the topology itself mutates mid-run.

**Time model: fixed timestep, integer-indexed.** The sim ticks at a fixed `dt` regardless of frame rate; rendering interpolates between ticks for smooth visuals. This is non-negotiable for a roguelite — see below.

**Boundary behavior:** the rear depots are *sources* clamped positive (your industry, off-screen, never quite enough); the far enemy edge is clamped negative (the war's reservoir). The interesting dynamics live in the contested middle where the two reservoirs meet — that meeting surface *is* the tide line.

**Determinism & reproducibility — the part most people get wrong.** A roguelite lives or dies on "same seed, same run." Floating-point diffusion summed in arbitrary order is **not** reproducible across machines (FP addition isn't associative; SIMD and compiler reordering shift the last bits, and over thousands of ticks those bits avalanche into a different collapse). Two defenses, and I'd ship the first: **(a) fixed-point integers** for `p` — represent pressure as `i32`/`i64` milli-units, and the entire update is exact integer arithmetic, bit-identical everywhere. Or **(b)** keep floats but enforce a **fixed summation order** (iterate edges in a canonical sorted order, never parallelize the reduction). Fixed-point is the cleaner promise for ~12 nodes; you don't need the speed, you need the determinism. The seed drives only the *initial* `a[i]`, topology variant, and price noise — the dynamics after that are deterministic.

## Technology & architecture

This is the required destination, so here's what I'd actually reach for.

**Engine & compute target.** ~12 nodes and ~16 edges is *nothing* — this runs on a single CPU thread in microseconds per tick. **No GPU, no compute shaders.** The honest engineering call for TIDE.2's scale is a plain CPU sim loop; reaching for compute shaders here would be cargo-cult performance work. (If the dense-grid variant from the rejected TIDE.1 alternative were ever resurrected — thousands of cells — *then* a GPU reaction-diffusion pass via compute shaders earns its keep. It doesn't here.) Godot 4 fits the rest of the repo's orbit and gives you the 2D map rendering, node picking, and tweening for free.

**Data structures.** Store the graph as flat arrays, not an object graph: `p: [i64; N]`, `a: [i64; N]`, a CSR-style edge list (`edge_src`, `edge_dst`, `edge_k`, `edge_alive`) for cache-friendly iteration. The Laplacian update is one pass over the edge arrays accumulating into a scratch `delta[i]`, then a pass over nodes applying `delta`, sink, and decaying impulses. Impulses `s[i]` are a small ring of `(node, magnitude, ticks_remaining)` records.

**The integrator.** Explicit forward Euler, fixed-point. With a small `dt` and diffusion-only dynamics it's stable as long as `dt * sum(k_ij)` stays under the usual `< 1` Laplacian bound per node — easy to guarantee at this scale. No implicit solver, no linear-system inversion; the matrix is tiny but you don't even need it as a matrix. If a fast-diffusing config ever goes unstable, clamp `dt` or split into two sub-steps — far simpler than going implicit.

**Determinism layer.** Fixed-point `i64` milli-pressure as above; a single seeded PRNG (`splitmix64`/`xoshiro`) for initial conditions only; canonical edge ordering baked at map-gen. Record the seed + prestige loadout and a run replays exactly — which doubles as your debugging oracle and your "share this run" feature.

**Rendering the tide.** Draw `p[i]` as a color/height per node and *interpolate the field across lanes* for the visible boundary — a marching-squares-style isoline at `p = 0` traced over the graph gives you the actual visible "tide line" sweeping across the map. The sim is integer and discrete; the *picture* is smooth because you interpolate between ticks and along edges at render time. Cut lanes visibly snap and darken; a rupture flashes pressure bleeding into neighbors.

**What to prototype first.** A non-interactive sim harness, no ship, no art: 12 nodes, the fixed-point Laplacian update, hand-placed attrition, and an ASCII or debug-draw readout of `p` per tick. The single question that validates or kills this entire approach: *does hand-injecting an impulse at one node produce a legible, satisfying, propagating deformation of the boundary — a bulge that holds, a starve that cascades?* If the field just smears into mush or snaps too hard to read, the whole "front as fluid" bet is dead and you fall back to a sibling reading. Prove the dynamics feel like a tide before you build a single trade screen.

**Key risks.**
- *Mush.* Diffusion's natural tendency is to average everything flat — a uniformly gray, unreadable field. Mitigations: strong advection bias, hysteresis (TIDE.5) to make boundaries crisp, and capacity clamps so salients have shape. This is the real risk and the prototype exists to catch it.
- *Legibility vs. emergence.* A system rich enough to surprise can be too opaque to play. The 12-node floor (TIDE.2) is chosen so a human can *hold the whole board in their head*. Resist growing it.
- *Determinism rot.* The instant someone "optimizes" the edge loop with threads or SIMD, replays diverge. Guard it with a CI test: a fixed seed + scripted impulses must produce a known final-state hash. If the hash drifts, the build's broken.
- *Difficulty tuning is now a dynamical-systems problem,* not a curve you author. Balancing means tuning `a[i]`, conductances, and thresholds until the line recedes at the right pace — closer to tuning a physics sim than a spreadsheet. Budget for it.

## What this buys, and what it costs

What it buys: the run-ending defeat stops being an abstraction and becomes the most *visceral* moment in the game — you watch the hole open behind you, you see the salient you bled for cave anyway, you feel the tide. The campaign win (production finally outpacing attrition) renders as the field reaching steady state and then, gloriously, *advancing* — the 4X "eXterminate" shown as the line at last moving the right way. And the skill ceiling is real: load-balancing a deforming boundary with one ship is a genuinely deep spatial puzzle that no amount of price-memorization shortcuts.

What it costs: this is the **least minimalist** of the three readings under the hood. The *inputs* stay tiny — that's preserved, and fiercely — but you've added a continuous simulation with its own stability, determinism, and tuning burden. You're now shipping a physics engine, however small, behind a game whose pitch is "I deleted everything." The honest tension: a sibling approach can ship its discrete-event version in a weekend; this one needs the prototype above to even know if it's *fun* before any of the trading exists. If the dynamics don't sing, there's no game here — only a screensaver. The bet is that the deformation will sing, and that watching the tide line is worth the engine to simulate it.

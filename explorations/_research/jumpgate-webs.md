# Research Notes: Jumpgate Web Topologies in Space Games

*Design research for Minimalist Space Logistics (MSL). Scope: inter-system travel topology,
routing/economy implications, and how a moving war front severs lanes. All summaries are
original design analysis — no copied wiki text. Game names used for reference only.*

---

## Per-Game Topology Notes

### X4: Foundations (and X-series)

The X universe organises space into a two-level hierarchy: **clusters** (multi-sector
local groups connected by super-highways or orbital accelerators) and **the gate network**
(jump gates linking clusters across the whole map). Within a cluster, travel is cheap and
does not cost a jump-count; crossing a gate does. This produces a **two-tier mesh**: a dense
local neighbourhood plus a sparse inter-cluster graph of gates. Gate placement defines
economic regions — production chains that span multiple sectors within a cluster stay
cheap to run, while cross-cluster logistics incur the gate penalty. Critically, the lore
history of X4 begins *after* the entire gate network collapsed and was rebuilt from
scratch by individual races, meaning the current map is a **patchwork**: some connections
are still inactive (and can be opened by player action), and others are controlled by
factions willing to block passage. The interesting design lesson: **gates as economic
seams** — the gate boundary is where trade margins appear, and cutting or opening a gate
is a genuine macro-economic event.

### Freelancer

Freelancer's Sirius Sector spans 49 systems arranged as four major house regions (Liberty,
Bretonia, Kusari, Rheinland) with transit buffer zones between them (the Sigma, Omega, and
Tau system chains). Within each system, **trade lanes** — a chain of small gate buoys
forming a space highway — give fast directed-corridor travel to the main points of interest.
Between systems, **jump gates** connect neighbours. The overall graph is roughly **hub-and-spoke
at the regional scale** (each house has a capital system that concentrates traffic) with
**corridor chains** linking the houses through the buffer zones. Trade lanes inside a system
are effectively one-directional infrastructure — they can be disrupted (the campaign lets
players destroy individual lane buoys, splitting a trade lane mid-route). The interesting
design lesson: **intra-system lanes as destructible bottlenecks** — you don't have to take
the whole system, just break the highway, and the economic and travel cost spikes immediately.

### EVE Online

EVE's New Eden stargate network is the most studied real inter-system graph in gaming.
High-security empire space forms a **loose ring** connecting four racial blocs, with
chokepoint systems that funnel all cross-faction traffic. The canonical example is Niarja,
a single high-sec system that sat on the 9-jump Jita-to-Amarr trade corridor; when
Triglavian invaders severed it in August 2020 (a live world event driven by player choices),
the safe route expanded from 9 to 44 jumps, routing all the way around the ring through two
other factions' space. Low-security space lacks any ring structure; null-security space is a
denser, partially player-defined mesh riddled with **pipe systems** — long single-strand
corridors with only one entry/exit — which become death traps when hostile fleets camp the
entry. The graph is formally undirected (gates go both ways), but in practice certain
connections become socially directed by politics: chokepoints held by one alliance effectively
block passage for enemies. The interesting design lesson: **single-vertex severance has
cascading reroute cost** — a graph that *looks* highly connected can depend on one or two
vertices for most of its throughput, and deleting those vertices produces dramatic route
lengthening, not gentle degradation.

### Stellaris

Stellaris uses a **procedurally generated hyperlane graph** whose density is a start-game
parameter. At low density the galaxy resolves into clusters of 4–8 systems connected to
each other by one or two chokepoint systems; at high density it approaches a full mesh.
Paradox originally shipped three FTL types (hyperlanes, warp drives, wormhole generators)
but removed the latter two in the 1.8 "Heinlein" update. Warp drives and wormhole generators
bypassed the lane graph entirely, making static defences useless and chokepoints
meaningless; the game's territory-control and fortress mechanics only became viable when
hyperlane travel was made universal. Chokepoint systems now support FTL Inhibitor
structures that prevent enemies from jumping out, turning them into killzones. The
interesting design lesson: **topology only matters if you enforce it** — mixed FTL models
let players opt out of the graph, which destroyed the strategic weight of lanes. Committing
to a single constrained model made geography real.

### Mass Effect

The mass relay network is a **strict two-level hierarchy**: primary relays form point-to-point
fixed links between widely separated clusters (each primary relay connects to exactly one
partner relay, thousands of light-years away), while secondary relays handle movement within
a local cluster. The galaxy-level graph of primary relays is effectively a **backbone** — a
set of long-distance spines — with each cluster hanging off the backbone as a **local spoke
group** of secondary-relay systems. There are no loops at the galaxy level; reaching a
different cluster means transiting through the backbone relay for your region. This produces
extreme fragility at the backbone layer: destroying or blocking a relay strands everything
in its cluster. The interesting design lesson: **hierarchical topology concentrates
criticality** — the backbone layer carries the strategic weight; controlling one backbone
relay is equivalent to controlling every system in the cluster it serves.

### Elite Dangerous (Contrast Reference)

Elite Dangerous presents a deliberate contrast: there are no gates and no fixed lane graph.
Any ship can jump to any reachable system within its frame-shift drive range, and the galaxy
is modelled at near-full stellar density (400 billion theoretical systems). Routing is a
continuous geometry problem — find the star-to-star chain within range, not a graph-edge
traversal. The "Neutron Highway" is a community-discovered emergent corridor (neutron stars
that supercharge jump range), not a designed structure. There are no chokepoints because
there is no graph to choke. This eliminates strategic geography: no system is inherently
defensible, no corridor can be severed, and economic differentiation comes from commodity
prices and station distances, not lane control. The interesting design lesson: **without
enforced topology, space collapses to a distance metric** — you get a rich exploration game
but lose the board-game strategic layer that lane graphs provide.

---

## Topology Archetypes for MSL

Five archetypes extracted from the games above, evaluated against MSL's needs: a
14–20 system map, three distance bands (rear/mid/forward), a scripted enemy front that
advances and overwrites systems from one edge.

| Archetype | Shape | Chokepoints | Reroute options | Notes |
|---|---|---|---|---|
| **Ring** | Systems in a loop, each connecting to two neighbours | Every system is a potential cut | After a cut, two long paths remain; front advance severs the near arc | Elegant but too simple; a 14-system ring gives only two path lengths |
| **Hub-and-spoke** | One or two high-connectivity hubs with spokes radiating out | The hub(s) | Hub destruction collapses the map; spoke loss is minor | Works for lore but creates a single point of failure the game revolves around |
| **Mesh** | Every system connects to 2–4 neighbours, no dominant hubs | Rare unless engineered | Many alternate paths; front advance requires many simultaneous losses to choke supply | Forgiving but low tension; rerouting is easy |
| **Backbone + local clusters** | One or two long chains (the backbone); small clusters hanging off each node | Backbone junctions | Losing a backbone node strands its whole cluster | Mass Effect model; scales well; cluster loss feels meaningful |
| **Chokepoint-pipe** | Some corridors that are single-strand chains (pipes) with only one entry/exit | Pipe entry systems | Almost none within the pipe; must retreat to the pipe entrance to reroute | EVE null-sec model; creates lethal corridor tension but rerouting requires a very long detour |

---

## What a Moving Front Does to Each Archetype

The enemy front advances from one edge, overrunning systems band by band. What that means
for each topology:

**Ring:** The front cuts the near arc of the ring first. Two reroute paths of equal length
appear. As the front advances further, both arcs shorten from the same edge; eventually
both are cut and the map is severed. Produces predictable but not very dramatic tension.

**Hub-and-spoke:** If the front advances toward the hub, tension spikes hard near the end
but there is very little tension in the early game when only spoke systems fall. If the hub
is in the rear, falling spokes immediately cut off whole forward sectors — high drama but
possibly too sudden. Hub placement relative to the front matters enormously.

**Mesh:** The front must erase a wide column of systems before supply lines choke. Rerouting
is easy and low-stakes. Tension comes late and mostly from running out of map, not from lane
logic. Not interesting enough for a logistics game.

**Backbone + local clusters:** When the front reaches a backbone node, it simultaneously
severs every system in that node's cluster. The player must decide whether to reroute around
a missing backbone segment (costly) or abandon the cluster's resources entirely. This
produces a series of threshold moments — individual system losses feel minor, then suddenly a
backbone cut makes a whole region vanish. Very well matched to MSL's three-band structure.

**Chokepoint-pipe:** If a pipe runs front-to-rear, the entry chokepoint becomes the
decisive defensive point. Once it falls, the player is inside the pipe with no reroute and
must retreat all the way out. Very high tension in the pipe but almost no strategic agency
during the retreat. Works best as a regional feature (one corridor of the map is a pipe)
rather than the whole topology.

---

## Recommended Starter Topology for MSL

**Archetype:** Backbone + local clusters, with one engineered pipe corridor as a sub-feature.

**Rationale:** The backbone model matches the three-band structure naturally (rear → mid →
forward, one backbone node per band), produces cluster-scale threshold moments when a node
falls, and gives the player meaningful rerouting decisions without making them trivial. The
pipe corridor adds a high-stakes optional route (faster but no reroute if cut).

**Concrete sketch for 14–16 systems, 3 bands:**

```
REAR BAND (4 systems)
  R1 [Shipyard Hub] — R2 — R3
                   \
                   R4 [Backbone junction → MID]

MID BAND (5–6 systems)
  M1 [Backbone junction ← REAR]
      |
     M2 — M3 [Refinery cluster, dead end spoke]
      |
     M4 — M5 [Refinery cluster, dead end spoke]
      |
     M6 [Backbone junction → FWD]

FORWARD BAND (5–6 systems)
  F1 [Backbone junction ← MID]
      |
  F2 — F3 [Forward depot cluster]
      |
  F4 ——————————— F5 [PIPE: single-strand corridor to frontline]
                         (F5 = frontmost depot; falls first)
```

Key properties:
- Losing F5 (the pipe terminus) cuts nothing except F5 itself — expected.
- Losing F4 (the pipe entry/chokepoint) strands F5 immediately and forces rear rerouting.
- Losing F1 (the backbone junction) severs all of FWD band at once — a crisis moment.
- Losing M6 (mid backbone) strands both FWD cluster groups simultaneously.
- The Shipyard Hub (R1) should be the last system standing; if it falls, the game ends.
- M3/M5 spokes as dead ends mean losing them costs resources but never locks out a route.
- The player always has at least two backbone hops between rear and front at game start,
  giving them the space to feel forward pressure building before the first crisis hits.

Adjust lane counts up if 14 systems feels sparse; adding lateral cross-links between
M-band nodes converts the backbone toward mesh and lowers tension — use sparingly.
```

---

*Sources consulted: EVE University Wiki (topology), Steam/Paradox forums (Stellaris FTL
design decisions), community wikis for Freelancer and X4, PC Gamer / Kotaku reporting on
the Niarja severance event, Mass Effect Wikia relay hierarchy, Elite Dangerous community
documentation on jump range and the Neutron Highway. All analysis is original; no extended
verbatim excerpts reproduced.*

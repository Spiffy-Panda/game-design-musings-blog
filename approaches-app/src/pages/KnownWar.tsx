import {
  Page,
  Section,
  Lead,
  Handle,
  DecisionList,
  Callout,
  StatGrid,
  Figure,
  CompareTable,
  Tabs,
  Subheading,
  Tag,
  TagRow,
} from "../components/kit";

export default function KnownWar() {
  return (
    <Page
      accent="rose"
      kicker="HAND lineage · mutation M2"
      mnemonic="M2"
      title="The Known War"
      lede={
        <>
          The enemy doesn&rsquo;t haul, doesn&rsquo;t buy, doesn&rsquo;t bargain &mdash; it eats the map
          on a schedule. Beat it by knowing the schedule.
        </>
      }
      spark={
        <>
          The Hand made the <em>market</em> the antagonist. Here the antagonist is{" "}
          <em>geography on a clock</em>: the war opens from a script, so a veteran reads the map
          like an opening book &mdash; which system falls, when, and which fat contract is really a
          one-way ticket behind the line.
        </>
      }
      backHref="../"
      backLabel="Approaches"
      crumbs={[
        { label: "Panda's Portfolio", href: "https://spiffy-panda.github.io/" },
        { label: "Game Design Musings", href: "../../../../index.html" },
        { label: "Minimalist Space Logistics", href: "../../" },
        { label: "Approaches", href: "../" },
        { label: "The Known War" },
      ]}
    >
      {/* ------------------------------------------------------------------ Spark */}
      <Section title="The spark">
        <Lead>
          The Invisible Hand bet everything on prices nobody placed. Keep the spirit, move the
          target. The thing you&rsquo;re racing isn&rsquo;t an equilibrium &mdash; it&rsquo;s a{" "}
          <strong>front advancing through a fixed map on a largely scripted timeline</strong>. The
          enemy is not another hauler with better routing. It is an <em>asymmetric</em> force that
          doesn&rsquo;t trade at all: it consumes territory, and as it consumes it changes which of
          your lanes still exist.
        </Lead>
        <p>
          That one move &mdash; a <em>known</em> war &mdash; turns the board into a puzzle with a
          right answer you don&rsquo;t have yet. The enemy&rsquo;s opening is set, so System J always
          falls around the same point in the run; the lane to it always becomes a death-trap a
          little before that; the contracts feeding the segment in front of it always pay the most,
          right up until they don&rsquo;t. A first run is blind. A tenth run is a heist you&rsquo;ve
          cased. The skill the game actually trains is <strong>reading topology against a clock you
          have partly memorised</strong>: take the contract that pays because it&rsquo;s near the
          front &mdash; but not the one whose drop-off is about to be overrun. <Handle id="M2.5" />{" "}
          makes that knowledge a thing you build, run over run, on an in-world map that fills itself
          in.
        </p>
        <TagRow>
          <Tag>directed lane graph</Tag>
          <Tag>scripted front</Tag>
          <Tag>front = consumer</Tag>
          <Tag>fog-of-war war plan</Tag>
          <Tag>seed = a war</Tag>
        </TagRow>
      </Section>

      {/* ------------------------------------------------------------------ Core loop */}
      <Section eyebrow="Decisions, not deferrals" title="Core loop, ironed out">
        <p>
          Concretely, here is what a run is. The shared revisions (
          <Handle id="R.1" />&ndash;<Handle id="R.6" />) are folded in, and the two questions this
          mutation owns &mdash; world shape plus enemy asymmetry, and the front-as-consumer &mdash;
          are answered, not waved at.
        </p>
        <DecisionList
          items={[
            {
              handle: "M2.1",
              title: "The board is a directed lane graph, banded by distance from the front.",
              body: (
                <>
                  ~14&ndash;20 star systems, connected by <strong>directed</strong> lanes (a corridor
                  can be safe outbound and lethal inbound). Systems carry a <em>band</em> index:
                  <em> rear</em> (shipyards, raw extraction &mdash; <code>ore</code>,{" "}
                  <code>volatiles</code>, <code>HE</code>, <code>propellant</code>),{" "}
                  <em> midfield</em> (refineries and fabs that turn raw into{" "}
                  <code>alloys</code>, <code>polymers</code>, <code>composite-armor</code>), and{" "}
                  <em> forward</em> (depots feeding the line). Resolving{" "}
                  <strong>_OQ.1</strong>: the graph is the geography, the bands are the supply
                  chain, and the front is a moving cut across it. Not small &mdash; abstract (
                  <Handle id="R.3" />).
                </>
              ),
            },
            {
              handle: "M2.2",
              title: "The enemy is an attrition automaton, not an economy.",
              body: (
                <>
                  It does not buy, post contracts, or compete for margin. It holds a set of{" "}
                  <em>front segments</em>, each with an intensity, and on its own schedule it{" "}
                  <strong>spends intensity to overrun the next system in its plan</strong>. That is
                  the asymmetry that makes <strong>_OQ.1</strong> bite: you cannot out-trade a thing
                  that doesn&rsquo;t trade. You can only out-<em>supply</em> the friendly line it&rsquo;s
                  grinding against &mdash; and route around the systems it&rsquo;s about to take.
                </>
              ),
            },
            {
              handle: "M2.3",
              title: "The front is the clock, and the clock is consumption.",
              body: (
                <>
                  A run is measured in how far the front has eaten, not in turns (inheriting the
                  Hand&rsquo;s endogenous doomsday). Each forward segment burns military goods every
                  tick at a rate set by its intensity; <strong>unmet burn becomes lost ground</strong>.
                  Resolving <strong>_OQ.3</strong>: the front&rsquo;s fighting <em>is</em> its spend &mdash;
                  fed segments hold and the line crawls; starved segments lurch forward and swallow the
                  system (and lanes) behind them.
                </>
              ),
            },
            {
              handle: "M2.4",
              title: "Demand reaches you as contracts, ranked by a danger you must price yourself.",
              body: (
                <>
                  Supply and demand is the friendly faction AI&rsquo;s job (<Handle id="R.4" />),
                  surfaced as Euro-Truck-style <strong>job postings</strong>. The faction reads each
                  forward segment&rsquo;s projected deficit and posts contracts to cover it; payout
                  scales with the deficit and the distance hauled. The catch the faction{" "}
                  <em>won&rsquo;t</em> price for you: a drop-off three jumps deep into a segment about
                  to fall is worth zero if you arrive after it&rsquo;s overrun. Picking is manual at
                  first; route-ranking automation is something you buy.
                </>
              ),
            },
            {
              handle: "M2.5",
              title: "The map is the meta-game: you learn the war diegetically.",
              body: (
                <>
                  The loop is diegetic (<Handle id="R.5" />). The enemy&rsquo;s opening is{" "}
                  <em>set</em>, so what you don&rsquo;t know is simply <em>unobserved</em>, not random.
                  Every overrun you witness writes a keyframe into a persistent{" "}
                  <strong>war-plan map</strong> &mdash; &ldquo;System J falls ~here&rdquo;,
                  &ldquo;this lane goes one-way first&rdquo;. Knowledge, not stats, is the thing
                  prestige-adjacent that carries across runs.
                </>
              ),
            },
            {
              handle: "M2.6",
              title: "You start in one hauler; prestige reshapes the rear, never the enemy.",
              body: (
                <>
                  Start each run in a single ship (<Handle id="R.1" />); buy more mid-run only if
                  wartime shipyards have berths to spare. Being overrun banks prestige, which seeds{" "}
                  <em>where your supply originates</em> &mdash; new rear producers, more shipyard
                  berths, better starting reach &mdash; building your &ldquo;space-FedEx&rdquo; up
                  until your start can fill the yards that build your fleet. Prestige never touches the
                  enemy&rsquo;s plan or its intensity. The war stays sovereign; you just get a better
                  place to fight it from.
                </>
              ),
            },
          ]}
        />
        <Callout kind="key" title="The asymmetry, in one line">
          You are an optimiser moving mass through a graph. The enemy is a <em>schedule</em> that
          deletes the graph from one side. The game is the tension between a route that&rsquo;s
          optimal <em>now</em> and a map that won&rsquo;t exist in ten minutes.
        </Callout>
      </Section>

      {/* ------------------------------------------------------------------ What's simulated */}
      <Section eyebrow="The model" title="What's being simulated">
        <p>
          Three things tick in lockstep: a <strong>graph</strong> that can lose nodes and lanes, a{" "}
          <strong>scripted-yet-reactive enemy advance</strong>, and a <strong>front that
          consumes</strong> and turns its hunger into the contracts you see. None of it is random;
          all of it is a seed.
        </p>

        <Subheading>State</Subheading>
        <p>Minimal, flat, and integer where money or position is involved:</p>
        <ul className="ml-5 list-disc space-y-1.5 text-fog-300 marker:text-[var(--accent)]">
          <li>
            <strong>Systems</strong>: <code>band</code>, <code>status</code> (held / contested /
            overrun), per-good <code>inventory</code>, and the producer or depot role attached.
          </li>
          <li>
            <strong>Lanes</strong>: directed <code>(from, to)</code>, a traversal cost, and a{" "}
            <code>risk</code> that rises as either endpoint goes contested.
          </li>
          <li>
            <strong>Front segments</strong>: a set of contested systems, an <code>intensity</code>{" "}
            scalar, a per-good <code>burn rate</code>, and a pointer into the war plan for{" "}
            <em>what it eats next</em>.
          </li>
          <li>
            <strong>War plan</strong>: the scripted opening &mdash; an ordered list of keyframes,{" "}
            <code>(target system, scheduled fall-tick, prerequisite)</code>, deterministically
            expanded from the seed.
          </li>
        </ul>

        <Subheading>Update (one fixed tick)</Subheading>
        <p>
          Fixed-timestep, several ticks per in-game hour, decoupled from frame rate, single
          iteration order &mdash; non-negotiable, exactly as the Hand demanded. Each tick:
        </p>
        <ol className="ml-5 list-decimal space-y-1.5 text-fog-300 marker:font-mono marker:text-[var(--accent)]">
          <li>Rear and midfield producers add to inventory; the supply chain steps one link.</li>
          <li>
            Each forward segment <strong>burns</strong> military goods at{" "}
            <code>intensity &times; rate</code>; a fed segment&rsquo;s deficit is ~0.
          </li>
          <li>
            <strong>Deficit accumulates</strong> into a segment&rsquo;s pressure when burn outruns
            stock.
          </li>
          <li>
            The enemy advances: when a segment&rsquo;s pressure crosses the next keyframe&rsquo;s
            threshold (or its scheduled tick lands, whichever first), the target system flips to{" "}
            <em>overrun</em>, its lanes are cut or reversed, and the segment slides forward.
          </li>
          <li>
            The faction recomputes projected deficits and <strong>posts/expires contracts</strong>{" "}
            against them; you act between ticks.
          </li>
        </ol>

        <Figure caption="A run, mid-collapse. The front (rose) advances right-to-left along its scripted plan; the dashed keyframe shows where System J is slated to fall. Lane J→F has already gone one-way (▷). The starred contract pays big and is still safe; the skull contract drops into J, which falls in ~2 ticks.">
          <svg viewBox="0 0 720 300" xmlns="http://www.w3.org/2000/svg" className="w-full">
            <defs>
              <marker
                id="arrow"
                viewBox="0 0 10 10"
                refX="9"
                refY="5"
                markerWidth="7"
                markerHeight="7"
                orient="auto-start-reverse"
              >
                <path d="M0,0 L10,5 L0,10 z" fill="var(--accent)" />
              </marker>
              <linearGradient id="frontfade" x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" stopColor="var(--accent)" stopOpacity="0.32" />
                <stop offset="100%" stopColor="var(--accent)" stopOpacity="0" />
              </linearGradient>
            </defs>

            {/* band labels */}
            <text x="80" y="24" textAnchor="middle" className="fill-fog-500" fontSize="11" fontFamily="monospace">REAR</text>
            <text x="300" y="24" textAnchor="middle" className="fill-fog-500" fontSize="11" fontFamily="monospace">MIDFIELD</text>
            <text x="560" y="24" textAnchor="middle" className="fill-fog-500" fontSize="11" fontFamily="monospace">FORWARD</text>

            {/* the advancing front zone */}
            <rect x="600" y="36" width="120" height="220" fill="url(#frontfade)" />
            <line x1="600" y1="36" x2="600" y2="256" stroke="var(--accent)" strokeWidth="2" strokeDasharray="2 5" />
            <text x="610" y="150" className="fill-[var(--accent)]" fontSize="11" fontFamily="monospace">FRONT →</text>

            {/* scheduled-fall keyframe at J */}
            <line x1="500" y1="36" x2="500" y2="256" stroke="var(--accent)" strokeWidth="1.5" strokeDasharray="4 4" opacity="0.6" />
            <text x="504" y="48" className="fill-[var(--accent)]" fontSize="10" fontFamily="monospace" opacity="0.8">J falls ~t+2</text>

            {/* lanes */}
            <g stroke="var(--color-line)" strokeWidth="2" fill="none">
              <line x1="110" y1="120" x2="250" y2="90" />
              <line x1="110" y1="120" x2="250" y2="200" />
              <line x1="290" y1="90" x2="430" y2="120" />
              <line x1="290" y1="200" x2="430" y2="120" />
              <line x1="470" y1="120" x2="560" y2="90" />
            </g>
            {/* one-way reversed lane J -> F */}
            <line x1="540" y1="200" x2="470" y2="140" stroke="var(--accent)" strokeWidth="2.5" markerEnd="url(#arrow)" />
            <text x="492" y="186" className="fill-[var(--accent)]" fontSize="10" fontFamily="monospace">▷ one-way</text>

            {/* nodes */}
            <g>
              {/* Rear */}
              <circle cx="90" cy="120" r="22" className="fill-[var(--color-ink-850)]" stroke="var(--color-line)" strokeWidth="2" />
              <text x="90" y="124" textAnchor="middle" className="fill-fog-200" fontSize="13" fontFamily="monospace">Y</text>
              <text x="90" y="160" textAnchor="middle" className="fill-fog-500" fontSize="9">shipyard</text>

              {/* Midfield top */}
              <circle cx="270" cy="90" r="20" className="fill-[var(--color-ink-850)]" stroke="var(--color-line)" strokeWidth="2" />
              <text x="270" y="94" textAnchor="middle" className="fill-fog-200" fontSize="13" fontFamily="monospace">R</text>
              {/* Midfield bottom */}
              <circle cx="270" cy="200" r="20" className="fill-[var(--color-ink-850)]" stroke="var(--color-line)" strokeWidth="2" />
              <text x="270" y="204" textAnchor="middle" className="fill-fog-200" fontSize="13" fontFamily="monospace">F</text>

              {/* Forward depot D (safe, starred contract target) */}
              <circle cx="450" cy="120" r="20" className="fill-[var(--color-ink-850)]" stroke="var(--accent)" strokeWidth="2" />
              <text x="450" y="124" textAnchor="middle" className="fill-fog-100" fontSize="13" fontFamily="monospace">D</text>
              <text x="450" y="150" textAnchor="middle" className="fill-[var(--accent)]" fontSize="15">★</text>

              {/* Forward J (contested, about to fall, skull contract target) */}
              <circle cx="580" cy="90" r="20" className="fill-[var(--accent-soft)]" stroke="var(--accent)" strokeWidth="2.5" />
              <text x="580" y="94" textAnchor="middle" className="fill-fog-100" fontSize="13" fontFamily="monospace">J</text>
              <text x="580" y="118" textAnchor="middle" className="fill-[var(--accent)]" fontSize="13">☠</text>
            </g>

            {/* legend */}
            <g fontFamily="monospace" fontSize="10">
              <text x="40" y="285" className="fill-fog-500">★ pays + safe</text>
              <text x="190" y="285" className="fill-fog-500">☠ pays most, drop-off falls first</text>
              <text x="520" y="285" className="fill-fog-500">▷ lane reversed by advance</text>
            </g>
          </svg>
        </Figure>

        <StatGrid
          items={[
            { value: "~16", label: "systems / 3 bands" },
            { value: "12", label: "goods (6 + 6)" },
            { value: "1 seed", label: "= one whole war" },
            { value: "10 Hz", label: "sim tick, fixed-step" },
          ]}
        />

        <Subheading>Scripted opening, reactive close</Subheading>
        <p>
          The trick is &ldquo;largely scripted.&rdquo; The <em>opening</em> is a keyframe schedule:
          the first several systems fall in a near-fixed order so the map is{" "}
          <strong>learnable</strong> (<Handle id="M2.5" />). But each keyframe is gated by a
          pressure threshold as well as a tick, so <strong>your coverage perturbs the timing</strong>:
          feed a segment hard and you push its fall later; abandon it and it lurches early. Late in a
          run the script runs out and the front advances purely on accumulated pressure &mdash;
          memorisation gets you through the opening, skill gets you through the endgame.
        </p>

        <Callout kind="note" title="Why it&rsquo;s legible, not random">
          Nothing here rolls dice at runtime. Unknowns are <em>unobserved keyframes</em>, not
          stochastic events. The same seed lays down the same war plan, the same producer placements,
          the same opening order &mdash; so a memorised map stays true, and a &ldquo;lost&rdquo; run
          is exactly reproducible and shareable.
        </Callout>

        <Tabs
          tabs={[
            {
              id: "feed",
              label: "Segment fed",
              content: (
                <p>
                  <strong>Deficit ≈ 0.</strong> Pressure decays toward its floor, the next keyframe&rsquo;s
                  threshold isn&rsquo;t crossed early, and the system holds until at least its scheduled
                  tick. The line crawls; your forward contracts stay reachable. This is you buying
                  time &mdash; never victory.
                </p>
              ),
            },
            {
              id: "starve",
              label: "Segment starved",
              content: (
                <p>
                  <strong>Deficit compounds.</strong> Pressure rises past the keyframe threshold{" "}
                  <em>before</em> the scheduled tick; the target system flips to overrun early, lanes
                  cut or reverse, and any in-flight contract to a now-lost drop-off is forfeit. The
                  front lurches into the next band ahead of book.
                </p>
              ),
            },
            {
              id: "endgame",
              label: "Script exhausted",
              content: (
                <p>
                  <strong>Pure pressure.</strong> Past the scripted opening there are no more
                  keyframes; advance is driven entirely by which segments you&rsquo;ve been able to
                  keep fed. The memorised map no longer predicts &mdash; you&rsquo;re reading live
                  topology. Eventually attrition wins this run, and you bank the loss.
                </p>
              ),
            },
          ]}
        />
      </Section>

      {/* ------------------------------------------------------------------ Tech */}
      <Section eyebrow="What I&rsquo;d actually build" title="Technology & architecture">
        <p>
          Take a position. The Hand reached for an agent-based market; this mutation needs almost
          none of that. It needs a <strong>graph that mutates deterministically</strong>, a{" "}
          <strong>scheduler with a reactive escape hatch</strong>, and a contract generator that
          reads deficits. Here&rsquo;s the stack.
        </p>

        <Subheading>A directed weighted graph as the world</Subheading>
        <p>
          Systems are nodes, lanes are directed edges, both in flat struct-of-arrays buffers (a
          hand-rolled SoA layer in Godot, or <code>petgraph</code>/a CSR adjacency list if this were
          Rust). Overrun is a node-status flip plus an edge mask &mdash; cheap, and trivially
          reversible for replay. Routing for contract-ranking and the faction&rsquo;s deficit-spread
          is plain <strong>Dijkstra / A*</strong> over the live (un-overrun) edge set, with lane{" "}
          <code>risk</code> folded into edge weight. Nothing exotic; the cleverness is in the data,
          not the algorithm.
        </p>

        <Subheading>The enemy: a keyframed schedule + a threshold automaton</Subheading>
        <p>
          The scripted opening is a <strong>timeline of keyframes</strong> expanded from the seed (a
          fixed PCG-style PRNG, advanced only on ticks, in fixed agent order). Each keyframe is{" "}
          <code>(target, scheduled_tick, pressure_threshold)</code>. The reactive layer is a tiny{" "}
          <strong>threshold automaton</strong> per segment: integrate deficit into pressure, fire the
          keyframe when <em>either</em> the tick lands <em>or</em> pressure clears the threshold. This
          is the &ldquo;scripted-yet-reactive&rdquo; engine in one sentence &mdash; closer to a
          tower-defence wave scheduler crossed with a one-dimensional cellular automaton than to
          anything economic. Crucially it is <em>not</em> an agent doing search; that&rsquo;s the
          asymmetry, in code.
        </p>

        <Subheading>The front as a consumer, contracts as its shadow</Subheading>
        <p>
          Each forward segment is a sink with a per-good burn vector scaled by intensity (
          <Handle id="M2.3" />). The faction AI projects each segment&rsquo;s deficit a few ticks out,
          then emits contracts: <code>(good, qty, origin, drop-off, payout, expiry)</code>, payout ∝
          deficit &times; haul distance. The danger isn&rsquo;t encoded in the payout on purpose &mdash;
          it&rsquo;s implicit in how close the drop-off sits to the next keyframe (<Handle id="M2.4" />
          ). Money stays abstract here; M1 owns the treasury and station liquidity, and I&rsquo;m
          staying out of that lane.
        </p>

        <Subheading>Persistence: a snapshot for the rear, a fog map for the player</Subheading>
        <p>
          Two jobs, two artefacts. <strong>(1)</strong> Prestige re-seeds the rear &mdash; serialize
          producer placements and shipyard berths as a small <strong>snapshot</strong> folded into
          the next run&rsquo;s seed expansion (<Handle id="M2.6" />). <strong>(2)</strong> The{" "}
          war-plan map is a <strong>discovery log</strong>: an append-only record of observed
          keyframes (&ldquo;saw J fall at t≈N on seed S-family&rdquo;) that fills in the in-world map
          across runs (<Handle id="M2.5" />). Ghost haulers stay shelved (<Handle id="R.2" />) &mdash;
          no replayed rival streams here.
        </p>

        <Callout kind="warn" title="What to prototype first">
          One band, one segment, headless. A line of ~5 forward systems, a single scripted keyframe
          schedule, a player who can deliver military goods to depots, and the threshold automaton
          wired to deficit. Tune until <em>feeding a segment visibly delays its fall and starving it
          visibly pulls the fall earlier</em>. Assert byte-identical reruns from the same seed on day
          one. If that one segment doesn&rsquo;t feel like a clock you can bargain with by hauling,
          nothing scales up.
        </Callout>

        <Callout kind="warn" title="Risks, named">
          <ul className="ml-4 list-disc space-y-1.5">
            <li>
              <strong>Memorisation &gt; mastery.</strong> If the opening is <em>too</em> rigid the
              game becomes rote recall, not reading. Mitigation: a seed-family that shuffles which
              keyframes are pre-set vs. emergent, and a script that runs out (the endgame tab) so the
              back half is always live.
            </li>
            <li>
              <strong>Determinism rot.</strong> One unseeded <code>rand()</code>, one iteration over a
              hash map, one float that depends on thread timing, and shareable seeds and the discovery
              log both die. Mitigation: single seeded PRNG, fixed iteration order, integer/fixed-point
              money and pressure, a reruns-must-match CI gate.
            </li>
            <li>
              <strong>Topology illegibility.</strong> A directed graph with reversing lanes can become
              unreadable. Mitigation: keep it ~16 nodes / 3 bands (<Handle id="M2.1" />), and surface
              the &ldquo;why&rdquo; minimally &mdash; though <em>how</em> it&rsquo;s drawn is M3&rsquo;s
              lane, not mine.
            </li>
          </ul>
        </Callout>
      </Section>

      {/* ------------------------------------------------------------------ Trade-offs */}
      <Section eyebrow="The trade" title="What this buys, and what it costs">
        <CompareTable
          columns={["You (the hauler)", "The enemy (the front)"]}
          highlight={1}
          rows={[
            { label: "Core verb", cells: ["move mass through a graph", "delete the graph from one edge"] },
            { label: "Driven by", cells: ["contracts + your routing", "a keyframe schedule + pressure"] },
            { label: "Resource", cells: ["hold, reach, capital", "intensity, spent as advance"] },
            { label: "Reacts to you?", cells: ["—", "only via coverage (timing shifts)"] },
            { label: "Can be out-traded?", cells: ["—", "no — it never trades"] },
            { label: "Win condition", cells: ["out-supply attrition over loops", "eat the rear before you can"] },
          ]}
        />
        <p>
          <strong>What it buys:</strong> a logistics game whose antagonist is{" "}
          <em>legible geography under time pressure</em> &mdash; the fantasy of the veteran dispatcher
          who knows this war cold. Because the opening is scripted, mastery is real and teachable; a
          map you&rsquo;ve cased rewards you. Because the enemy is asymmetric, there&rsquo;s no
          dominant &ldquo;just trade better&rdquo; line &mdash; you are always negotiating with a clock
          that doesn&rsquo;t want your money. And because the close is reactive, two players with the
          same memorised opening still diverge on who kept which segment alive.
        </p>
        <p>
          <strong>What it costs:</strong> <em>authoring</em>. Someone designs war plans &mdash; the
          opening orders, the keyframe timings, the prerequisite gates &mdash; per seed-family, and
          tunes them so the map is hard but fair. That&rsquo;s closer to level design than to
          economic tuning; it trades the Hand&rsquo;s &ldquo;nothing was scripted&rdquo; purity for a
          curated, knowable war. You also carry the determinism tax of any roguelite, and the risk
          that a too-fixed script slides from <em>reading</em> into <em>reciting</em>. The bet: a war
          you can <em>learn</em> is worth more than a war that&rsquo;s merely unpredictable.
        </p>
        <Callout kind="key" title="What I&rsquo;d build">
          A directed lane graph with deterministic node/edge mutation, a keyframed-schedule-plus-threshold-automaton
          enemy, and a front-as-sink contract generator &mdash; all single-threaded over flat SoA
          buffers, driven from one seeded PRNG, with a discovery log that turns each lost run into a
          better-read map.
        </Callout>
      </Section>
    </Page>
  );
}

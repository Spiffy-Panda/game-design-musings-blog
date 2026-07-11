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

export default function GlassCockpit() {
  return (
    <Page
      accent="cyan"
      mnemonic="M3"
      kicker="HAND lineage · mutation M3"
      title="The Glass Cockpit"
      lede={
        <>
          The other forks build a war and then ask how to show it. Start at the
          glass instead — the right abstraction isn't decoration, it <em>is</em>{" "}
          the interface to an abstract war.
        </>
      }
      spark={
        <>
          You can't fight the front. You can only read it. So the only thing that
          actually matters is whether one person, glancing at one screen, can read
          a war too big to walk across — and a fleet that keeps growing under them.
        </>
      }
      backHref="../"
      backLabel="Approaches"
      crumbs={[
        { label: "Panda's Portfolio", href: "https://spiffy-panda.github.io/" },
        { label: "Game Design Musings", href: "../../../../index.html" },
        { label: "Minimalist Space Logistics", href: "../../" },
        { label: "Approaches", href: "../" },
        { label: "The Glass Cockpit" },
      ]}
    >
      <Section title="The spark">
        <Lead>
          Every sibling here designs a system and treats the screen as the last
          step — the place the simulation surfaces once it's done. This one inverts
          the order. The screen is the constraint, the model is downstream of it, and
          the design question is brutally concrete: a single player, one porthole,
          must hold an entire theatre of war in their head at a glance.
        </Lead>
        <p>
          That's harder than it sounds, because the brief is hostile to legibility on
          two axes at once. <Handle id="R.3" /> says the front is{" "}
          <em>massive and abstract</em> — not a tidy six-system board you can
          memorize, but a wall of attrition stretching past the edge of the viewport.
          And <Handle id="R.1" /> says your fleet <em>grows</em>: you start as one
          hauler and end as a logistics combine with dozens of hulls in flight. A naive
          renderer turns the first into mush and the second into a spreadsheet. Both
          failures kill the game, because in this game <strong>reading is the
          verb</strong> — you never fire, you observe and supply, so if you can't read
          the board you have nothing to do.
        </p>
        <p>
          So I'm not going to paint a UI over a finished simulation. I'm going to
          design the <em>representational language</em> first — the iconography, the
          abstraction tiers, the motion grammar, the zoom — and treat that language as
          the spec the rest of the game has to satisfy. The thesis is that the visual
          abstraction is the actual game mechanic for an abstract war, and that
          choosing it well is the whole difference between "playable by one person" and
          "RTS micro nobody asked for."
        </p>
      </Section>

      <Section eyebrow="The loop" title="Core loop, ironed out">
        <p>
          The loop is the same overrun-and-bank cadence every fork shares — but read
          through the glass, each beat becomes a question about what the player can{" "}
          <em>see</em>. Decisions, made, not deferred.
        </p>
        <DecisionList
          items={[
            {
              handle: "M3.1",
              title: "The contract board is the primary instrument, not the map.",
              body: (
                <>
                  Per <Handle id="R.4" />, supply and demand is the faction AI's job,
                  surfaced as Euro-Truck-style job postings. So the player's home screen
                  is a <em>manifest</em>, not a battlefield: a ranked list of contracts,
                  each a one-line row — good, origin, destination, payout, and a single
                  hazard glyph. The map exists to <em>answer questions the board
                  raises</em> ("why is this milk run paying triple?"), never to be
                  micromanaged. Picking the best posting is manual at first; automation
                  is a thing you buy, and buying it visibly thins the board down to
                  exceptions.
                </>
              ),
            },
            {
              handle: "M3.2",
              title: "One hauler is a sprite; a fleet is a flow.",
              body: (
                <>
                  This is the load-bearing rule of the whole pitch and it resolves{" "}
                  <Handle id="R.1" />. A single hauler reads as a <em>discrete icon</em>{" "}
                  with a heading and a cargo pip — you can follow it. The moment you own
                  enough hulls that tracking each is hopeless, the representation{" "}
                  <em>changes kind</em>: individual sprites dissolve into a{" "}
                  <strong>flow ribbon</strong> on the lane — width = throughput, color =
                  good, opacity = how reliably the lane is clearing. You stop reading
                  ships and start reading <em>logistics as a fluid</em>. The fleet never
                  becomes a unit roster you babysit.
                </>
              ),
            },
            {
              handle: "M3.3",
              title: "Three abstraction tiers, and altitude is the only camera control.",
              body: (
                <>
                  Zoom isn't free pan-and-scale; it's a discrete ladder —{" "}
                  <strong>Contract → Corridor → Theatre</strong> — and each tier shows a
                  deliberately different <em>kind</em> of thing (detailed below). You
                  don't zoom to see the same picture bigger; you zoom to swap which
                  question the screen is answering. The transition is animated so you
                  never lose your place, but what's <em>rendered</em> genuinely differs
                  per tier. Altitude is the entire navigation model.
                </>
              ),
            },
            {
              handle: "M3.4",
              title: "The enemy is weather, never a unit.",
              body: (
                <>
                  M2 owns the asymmetric enemy and the front's combat math; I just have
                  to <em>render</em> their output. I refuse to draw enemy ships as
                  countable tokens — that invites you to fight them, and you can't.
                  Instead the enemy is a <strong>pressure gradient</strong>: a hazard
                  field, hatched and advancing, with isobars where it's steepening. You
                  read the enemy the way a sailor reads a storm front — direction,
                  intensity, how fast it's closing your lanes — and you route around
                  weather. <Handle id="R.2" /> (ghost haulers) stays shelved; nothing
                  here resurrects them.
                </>
              ),
            },
            {
              handle: "M3.5",
              title: "Discovery is diegetic and it lives on the glass.",
              body: (
                <>
                  Per <Handle id="R.5" />, the enemy's opening plan is fixed, so you
                  learn which systems to refuse contracts to — and that knowledge is
                  rendered, not parked in a menu. A system you've learned is a death-trap
                  gets a <em>persistent annotation</em> burned onto the theatre map; a
                  contract routing through it shows a struck-through hazard glyph on the
                  board. The map is a logbook you're writing by surviving, so prestige's
                  cartographic gains (<Handle id="APR.3" />) show up as ink you've
                  already laid down.
                </>
              ),
            },
            {
              handle: "M3.6",
              title: "What the panel omits is a design decision, not an oversight.",
              body: (
                <>
                  The instrument panel deliberately has <em>no</em> minimap, no unit
                  health bars, no exact prices on the main view, no combat readout. The
                  front's internal combat (M2) and the station ledgers / liquidity (M1)
                  exist in the model, but the cockpit surfaces only their{" "}
                  <em>consequences</em>: a lane closing, a payout spiking, a depot going
                  dark. Omission is how you keep a maximalist war behind a minimalist
                  surface. The glass shows you what to <em>decide</em>, not everything
                  that's <em>true</em>.
                </>
              ),
            },
          ]}
        />
        <Callout kind="key" title="The one rule everything hangs on">
          A representation must change <em>kind</em>, not just scale, as quantity grows.
          One hauler is an icon; a hundred is a current. One system is a node; the
          theatre is a field. Refuse this and you get either a toy that can't depict the
          massive front, or an RTS that drowns one player in micro. <Handle id="M3.2" />{" "}
          and <Handle id="M3.3" /> are the same idea pointed at fleet and at map.
        </Callout>
      </Section>

      <Section eyebrow="The representation" title="What's on the glass">
        <p>
          Here is the representational system end to end — the part this fork actually
          owns (<Handle id="M3.2" />–<Handle id="M3.4" />). It has three moving parts: an{" "}
          <strong>abstraction ladder</strong>, an <strong>icon vocabulary</strong>, and a{" "}
          <strong>motion grammar</strong>. Get these three coherent and the war becomes
          one person's to read.
        </p>

        <Subheading>The abstraction ladder</Subheading>
        <p>
          Three tiers, each answering a different question. You move up the ladder when
          your fleet outgrows the tier below it — the same upgrade that adds hulls also
          pushes your default altitude higher.
        </p>

        <Figure caption="The abstraction ladder — the same lane, rendered at three altitudes. What's drawn changes kind, not just size.">
          <svg viewBox="0 0 720 280" xmlns="http://www.w3.org/2000/svg" className="w-full">
            <defs>
              <marker
                id="arrowCyan"
                markerWidth="8"
                markerHeight="8"
                refX="6"
                refY="3"
                orient="auto"
              >
                <path d="M0,0 L6,3 L0,6 Z" fill="var(--accent)" />
              </marker>
            </defs>

            {/* Tier 1 — Contract */}
            <text x="120" y="24" textAnchor="middle" fill="var(--accent)" fontSize="12" fontFamily="monospace" letterSpacing="1.5">CONTRACT</text>
            <text x="120" y="40" textAnchor="middle" fill="#9aa6b2" fontSize="10">one hauler</text>
            <circle cx="40" cy="150" r="16" fill="none" stroke="#9aa6b2" strokeWidth="1.5" />
            <text x="40" y="183" textAnchor="middle" fill="#6b7681" fontSize="9">origin</text>
            <line x1="58" y1="150" x2="182" y2="150" stroke="#3a444f" strokeWidth="1.5" strokeDasharray="3 4" />
            {/* discrete ship sprite, mid-lane */}
            <g transform="translate(112 150) rotate(0)">
              <path d="M-10,-7 L12,0 L-10,7 L-5,0 Z" fill="var(--accent)" />
              <circle cx="-2" cy="0" r="2.5" fill="#0b0f14" />
            </g>
            <line x1="124" y1="150" x2="178" y2="150" stroke="var(--accent)" strokeWidth="1.5" markerEnd="url(#arrowCyan)" />
            <circle cx="200" cy="150" r="16" fill="none" stroke="#9aa6b2" strokeWidth="1.5" />
            <text x="200" y="183" textAnchor="middle" fill="#6b7681" fontSize="9">dest</text>
            <text x="120" y="222" textAnchor="middle" fill="#6b7681" fontSize="9">follow one ship,</text>
            <text x="120" y="236" textAnchor="middle" fill="#6b7681" fontSize="9">its heading + cargo</text>

            {/* divider */}
            <line x1="248" y1="50" x2="248" y2="250" stroke="#26303a" strokeWidth="1" />

            {/* Tier 2 — Corridor */}
            <text x="360" y="24" textAnchor="middle" fill="var(--accent)" fontSize="12" fontFamily="monospace" letterSpacing="1.5">CORRIDOR</text>
            <text x="360" y="40" textAnchor="middle" fill="#9aa6b2" fontSize="10">a fleet on a route</text>
            <circle cx="290" cy="150" r="13" fill="none" stroke="#9aa6b2" strokeWidth="1.5" />
            <circle cx="430" cy="150" r="13" fill="none" stroke="#9aa6b2" strokeWidth="1.5" />
            {/* flow ribbon — width = throughput */}
            <path d="M303,143 L417,138 L417,162 L303,157 Z" fill="var(--accent)" opacity="0.32" />
            <path d="M303,148 L417,146 L417,154 L303,152 Z" fill="var(--accent)" opacity="0.7" />
            <line x1="312" y1="150" x2="408" y2="150" stroke="var(--accent)" strokeWidth="1" markerEnd="url(#arrowCyan)" opacity="0.9" />
            <text x="360" y="200" textAnchor="middle" fill="#6b7681" fontSize="9">width = throughput</text>
            <text x="360" y="214" textAnchor="middle" fill="#6b7681" fontSize="9">color = good</text>
            <text x="360" y="228" textAnchor="middle" fill="#6b7681" fontSize="9">opacity = reliability</text>

            {/* divider */}
            <line x1="476" y1="50" x2="476" y2="250" stroke="#26303a" strokeWidth="1" />

            {/* Tier 3 — Theatre */}
            <text x="600" y="24" textAnchor="middle" fill="var(--accent)" fontSize="12" fontFamily="monospace" letterSpacing="1.5">THEATRE</text>
            <text x="600" y="40" textAnchor="middle" fill="#9aa6b2" fontSize="10">the whole front</text>
            {/* node cluster */}
            <circle cx="524" cy="140" r="5" fill="#9aa6b2" />
            <circle cx="556" cy="170" r="5" fill="#9aa6b2" />
            <circle cx="548" cy="112" r="5" fill="#9aa6b2" />
            <circle cx="588" cy="150" r="5" fill="#9aa6b2" />
            <line x1="524" y1="140" x2="556" y2="170" stroke="var(--accent)" strokeWidth="2.5" opacity="0.6" />
            <line x1="548" y1="112" x2="588" y2="150" stroke="var(--accent)" strokeWidth="1.5" opacity="0.4" />
            <line x1="524" y1="140" x2="548" y2="112" stroke="var(--accent)" strokeWidth="1" opacity="0.3" />
            {/* hazard weather front */}
            <path d="M636,70 Q660,150 636,236" fill="none" stroke="#fb7185" strokeWidth="2" opacity="0.85" />
            <path d="M652,70 Q678,150 652,236" fill="none" stroke="#fb7185" strokeWidth="1" strokeDasharray="2 5" opacity="0.5" />
            <path d="M636,70 Q660,150 636,236 L700,236 L700,70 Z" fill="#fb7185" opacity="0.1" />
            <text x="668" y="150" textAnchor="middle" fill="#fb7185" fontSize="9" transform="rotate(90 668 150)">front</text>
            <text x="585" y="214" textAnchor="middle" fill="#6b7681" fontSize="9">lanes as flux,</text>
            <text x="585" y="228" textAnchor="middle" fill="#6b7681" fontSize="9">enemy as weather</text>
          </svg>
        </Figure>

        <Tabs
          tabs={[
            {
              id: "contract",
              label: "Contract",
              content: (
                <>
                  <strong>Question answered:</strong> "What is this one job, exactly?" The
                  finest tier. A single hauler is a discrete sprite with a heading vector
                  and a cargo pip colored by good (<Handle id="R.6" />). You see the origin
                  node, the destination, the lane, the live price delta, and the hazard on
                  the route. This is where the player starts the campaign — one ship, fully
                  legible.
                </>
              ),
            },
            {
              id: "corridor",
              label: "Corridor",
              content: (
                <>
                  <strong>Question answered:</strong> "Is this trade lane healthy?"
                  Individual hulls have dissolved into a <em>flow ribbon</em>: width is
                  throughput, color is the dominant good, opacity is how reliably the lane
                  clears. A fraying ribbon means hulls are dying or rerouting. This is the
                  tier you live in once you own a real fleet — you manage <em>flows</em>,
                  not ships.
                </>
              ),
            },
            {
              id: "theatre",
              label: "Theatre",
              content: (
                <>
                  <strong>Question answered:</strong> "Where is the war going?" Systems
                  collapse to a node graph, lanes to flux lines, your whole fleet to the
                  aggregate brightness of the network — and the enemy is rendered as the
                  advancing weather front. No single ship is visible here, by design. You
                  read the macro shape of attrition and decide where to refuse contracts
                  (<Handle id="M3.5" />).
                </>
              ),
            },
          ]}
        />

        <Subheading>The icon vocabulary</Subheading>
        <p>
          Five entity classes, five silhouettes, one rule:{" "}
          <strong>shape carries identity, fill carries allegiance, motion carries
          intent.</strong> A player should be able to name any glyph in a quarter-second
          and never confuse the thing they command with the thing they route around.
        </p>

        <Figure caption="The icon vocabulary — five silhouettes the eye can sort instantly. You own the arrowhead; everything else is information about a war you can't touch.">
          <svg viewBox="0 0 720 200" xmlns="http://www.w3.org/2000/svg" className="w-full">
            {/* Hauler — you */}
            <g transform="translate(72 70)">
              <path d="M-14,-10 L18,0 L-14,10 L-7,0 Z" fill="var(--accent)" />
              <circle cx="-4" cy="0" r="3.5" fill="#0b0f14" />
            </g>
            <text x="72" y="118" textAnchor="middle" fill="#e6edf3" fontSize="12" fontWeight="bold">Hauler</text>
            <text x="72" y="136" textAnchor="middle" fill="#6b7681" fontSize="9">you · solid arrowhead</text>
            <text x="72" y="150" textAnchor="middle" fill="#6b7681" fontSize="9">cargo pip = good</text>

            {/* Station */}
            <g transform="translate(216 70)">
              <circle r="14" fill="none" stroke="#9aa6b2" strokeWidth="2" />
              <circle r="4" fill="#9aa6b2" />
              <line x1="-14" y1="0" x2="-19" y2="0" stroke="#9aa6b2" strokeWidth="2" />
              <line x1="14" y1="0" x2="19" y2="0" stroke="#9aa6b2" strokeWidth="2" />
            </g>
            <text x="216" y="118" textAnchor="middle" fill="#e6edf3" fontSize="12" fontWeight="bold">Station</text>
            <text x="216" y="136" textAnchor="middle" fill="#6b7681" fontSize="9">fixed · ringed node</text>
            <text x="216" y="150" textAnchor="middle" fill="#6b7681" fontSize="9">ring fill = stock</text>

            {/* Warship — friendly escort, neutral grey, blocky */}
            <g transform="translate(360 70)">
              <rect x="-13" y="-9" width="26" height="18" rx="2" fill="none" stroke="#9aa6b2" strokeWidth="2" />
              <rect x="-5" y="-3" width="10" height="6" fill="#9aa6b2" />
            </g>
            <text x="360" y="118" textAnchor="middle" fill="#e6edf3" fontSize="12" fontWeight="bold">Warship</text>
            <text x="360" y="136" textAnchor="middle" fill="#6b7681" fontSize="9">friendly · hollow block</text>
            <text x="360" y="150" textAnchor="middle" fill="#6b7681" fontSize="9">escorts, never yours</text>

            {/* Strike craft — fast, small chevrons (a swarm reads as many) */}
            <g transform="translate(504 70)" fill="#fbbf24">
              <path d="M-16,-6 L-8,0 L-16,6 L-12,0 Z" />
              <path d="M-4,-6 L4,0 L-4,6 L0,0 Z" />
              <path d="M8,-6 L16,0 L8,6 L12,0 Z" />
            </g>
            <text x="504" y="118" textAnchor="middle" fill="#e6edf3" fontSize="12" fontWeight="bold">Strike craft</text>
            <text x="504" y="136" textAnchor="middle" fill="#6b7681" fontSize="9">fast · chevron swarm</text>
            <text x="504" y="150" textAnchor="middle" fill="#6b7681" fontSize="9">density = number</text>

            {/* Enemy — weather, hatched gradient, no silhouette */}
            <g transform="translate(648 70)">
              <path d="M-18,-22 Q4,0 -18,22 L18,22 L18,-22 Z" fill="#fb7185" opacity="0.16" />
              <path d="M-18,-22 Q4,0 -18,22" fill="none" stroke="#fb7185" strokeWidth="2" />
              <path d="M-6,-22 Q16,0 -6,22" fill="none" stroke="#fb7185" strokeWidth="1" strokeDasharray="2 5" opacity="0.6" />
            </g>
            <text x="648" y="118" textAnchor="middle" fill="#e6edf3" fontSize="12" fontWeight="bold">Enemy</text>
            <text x="648" y="136" textAnchor="middle" fill="#6b7681" fontSize="9">weather · no token</text>
            <text x="648" y="150" textAnchor="middle" fill="#6b7681" fontSize="9">a gradient, not a unit</text>

            {/* baseline rule */}
            <text x="360" y="184" textAnchor="middle" fill="#6b7681" fontSize="10" fontStyle="italic">shape = identity · fill = allegiance · motion = intent</text>
          </svg>
        </Figure>

        <Subheading>The motion grammar</Subheading>
        <p>
          Static icons lie about a moving war, so motion carries its own meaning, and
          it's a small, strict vocabulary so it never reads as noise:
        </p>
        <TagRow>
          <Tag>steady glide = on contract</Tag>
          <Tag>pulsing pip = cargo at risk</Tag>
          <Tag>ribbon shimmer = lane clearing well</Tag>
          <Tag>ribbon fray = throughput dropping</Tag>
          <Tag>isobar creep = front advancing</Tag>
          <Tag>node dim = depot starving</Tag>
        </TagRow>
        <p>
          Color is rationed just as hard. Your cyan is yours and yours alone; rose is
          only ever the enemy and the hazard it casts; neutral grey is the friendly war
          machine you supply but don't command; the twelve goods (<Handle id="R.6" />)
          get a fixed palette that never bleeds into the allegiance colors. If a new
          color shows up, it means something — that's the discipline.
        </p>

        <StatGrid
          items={[
            { value: "3", label: "abstraction tiers" },
            { value: "5", label: "entity silhouettes" },
            { value: "12", label: "good colors (R.6)" },
            { value: "1", label: "screen, one player" },
          ]}
        />
      </Section>

      <Section eyebrow="Endpoint" title="Technology & architecture">
        <p>
          A pitch that stops at "use nice icons" hasn't earned anything. Here's the
          stack I'd actually build the glass on, and why representation-first changes the
          engineering, not just the art.
        </p>

        <Subheading>Engine: Godot 4, and the reason is the renderer</Subheading>
        <p>
          The whole game is 2D vector instruments over an abstract model — no 3D
          theatre, no photoreal anything. Godot 4's <strong>2D renderer with
          MultiMesh / canvas-item batching</strong> draws tens of thousands of
          instanced primitives (sprites, ribbons, glyphs) in a handful of draw calls,
          which is exactly the Contract-tier-to-Theatre-tier load profile. Its{" "}
          <strong>2D shaders</strong> give me the flow-ribbon shimmer and the hazard
          isobars as cheap fragment effects rather than per-frame geometry rebuilds. And
          critically, the sim stays the sibling forks' deterministic, fixed-timestep,
          data-oriented core (<Handle id="APR.5" />) — Godot is the{" "}
          <em>view</em>, subscribing to tick snapshots, never the source of truth. The
          model could be ripped out and replaced; the glass wouldn't notice.
        </p>

        <Subheading>The data-to-visual pipeline: a level-of-detail bus</Subheading>
        <p>
          This is the real architecture. The sim emits a flat per-tick snapshot of
          entity state. A <strong>representation layer</strong> sits between sim and
          renderer and does <em>aggregation as LOD</em>: at Contract tier it maps each
          hauler to one sprite; at Corridor tier it bins hulls per lane into a single
          ribbon instance (sum throughput, dominant good, clearance ratio → width,
          color, opacity); at Theatre tier it rolls lanes up into flux edges and the
          fleet into network brightness. <Handle id="M3.2" /> and <Handle id="M3.3" />{" "}
          aren't UI polish — they're <strong>discrete LOD levels</strong>, and "change
          kind, not scale" means each level is a genuinely different aggregation, not a
          mipmap of the same one.
        </p>

        <CompareTable
          columns={["Contract", "Corridor", "Theatre"]}
          rows={[
            {
              label: "Sim entities → visuals",
              cells: ["1 hull → 1 sprite", "N hulls → 1 ribbon", "lanes → flux edges"],
            },
            {
              label: "Aggregation",
              cells: ["none (identity)", "sum / dominant per lane", "roll-up per region"],
            },
            {
              label: "Enemy rendered as",
              cells: ["hazard glyph on route", "pressure on the lane", "advancing weather front"],
            },
            {
              label: "Draw primitive",
              cells: ["instanced sprites", "shader ribbons", "graph + field shader"],
            },
            {
              label: "Player reads",
              cells: ["one job", "lane health", "the shape of the war"],
            },
          ]}
          highlight={1}
        />

        <Subheading>What to prototype first</Subheading>
        <p>
          Build the <strong>tier-transition harness</strong> and nothing else: a single
          lane, fed by a scripted stream of dummy hauler states, that you can zoom
          between Contract, Corridor, and Theatre. Drive it from a seeded replay so the
          same input always produces the same picture. The disqualifying question is
          narrow and answerable in a week: <em>when ten sprites collapse into one ribbon,
          does the player believe the ribbon is those ten ships — or does it feel like
          the ships vanished?</em> If the dissolve doesn't land, <Handle id="M3.2" />{" "}
          fails and the whole representation-first bet is dead. Everything else —
          iconography, the enemy weather, the contract board — is downstream of that one
          transition feeling honest.
        </p>

        <Callout kind="warn" title="Risks, named">
          <ul className="list-disc space-y-2 pl-5">
            <li>
              <strong>The dissolve reads as a vanish.</strong> If sprites→ribbon feels
              like ships disappearing, players distrust the abstraction and the fleet
              tier collapses. Mitigation: a brief morph animation on every tier change,
              plus a "drill in" gesture that re-explodes a ribbon into its hulls on
              demand so the abstraction is always falsifiable.
            </li>
            <li>
              <strong>Glyph soup at Theatre tier.</strong> A massive front (
              <Handle id="R.3" />) can overwhelm even an aggregated view. Mitigation:
              hard caps on simultaneously-rendered glyphs per tier, with overflow rolled
              into density/brightness rather than more icons — quantity becomes a field
              value, never another sprite.
            </li>
            <li>
              <strong>Color-channel overload.</strong> Twelve goods (
              <Handle id="R.6" />) plus three allegiances is a lot of hues; colorblind
              players hit a wall first. Mitigation: allegiance carried by shape and
              motion <em>as well as</em> color, goods distinguished by pip glyph not
              hue alone, and a tested high-contrast palette as the default, not an
              accessibility afterthought.
            </li>
            <li>
              <strong>The model leaking through the glass.</strong> If the renderer ever
              reaches back into sim state to decide what to draw, determinism (
              <Handle id="APR.5" />) rots. Mitigation: a one-way snapshot bus — the
              representation layer may only read the tick snapshot, never mutate the sim,
              enforced as a hard architectural boundary.
            </li>
          </ul>
        </Callout>
      </Section>

      <Section title="What this buys, and what it costs">
        <p>
          What it buys: a war you can run from a single, calm instrument panel. Because
          the representation was designed first, the massive front of <Handle id="R.3" />{" "}
          stays readable instead of smearing, and the growing fleet of <Handle id="R.1" />{" "}
          never degenerates into RTS babysitting — it graduates from sprites to flows the
          moment counting stops being possible. The glass does the thing the fiction
          promised: it lets one person feel the shape of an attrition war they can
          observe but never fight, and it surfaces exactly the decisions (
          <Handle id="M3.6" />) without drowning them in everything that's true.
        </p>
        <p>
          What it costs: this fork buys its legibility with a <strong>second
          system</strong>. The siblings can ship a renderer that's a thin window onto
          their sim; I've committed to a full representation layer with its own LOD
          logic, its own aggregation rules, and its own tuning budget for whether a
          ribbon "feels like" its ships. That's real engineering the others don't pay
          for, and it's <em>art-and-feel</em> work that resists unit tests — you find out
          if the dissolve lands by watching faces, not by asserting equality. The bet is
          that for a war this abstract, the interface isn't downstream of the game — it
          <em>is</em> the game, and worth building first. If you don't believe a war you
          can't touch lives or dies on how it reads, build one of the other forks. This
          one only pays off if you design from the screen all the way back.
        </p>
      </Section>
    </Page>
  );
}

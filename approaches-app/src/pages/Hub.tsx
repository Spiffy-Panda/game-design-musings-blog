import {
  Page,
  Section,
  Lead,
  Handle,
  DecisionList,
  Callout,
  CompareTable,
  Card,
  CardGrid,
  Subheading,
} from "../components/kit";

export default function Hub() {
  return (
    <Page
      accent="violet"
      kicker="Minimalist Space Logistics"
      title="Approaches"
      lede="One settled premise, handed to a fleet of explorers — each told to wander somewhere the others wouldn't. They keep coming back with the same core and a different engine."
      spark={
        <>
          The pitch was never in doubt: one ship, an unwinnable war, loss as the currency.
          The open question was always <em>what do you actually build?</em> So we forked it —
          and then we forked a fork.
        </>
      }
      backHref="../"
      backLabel="Minimalist Space Logistics"
      crumbs={[
        { label: "Panda's Portfolio", href: "https://spiffy-panda.github.io/" },
        { label: "Game Design Musings", href: "../../../index.html" },
        { label: "Minimalist Space Logistics", href: "../" },
        { label: "Approaches" },
      ]}
    >
      <Section title="What this page is">
        <Lead>
          <a href="../" className="text-[var(--accent)] underline-offset-2 hover:underline">
            Minimalist Space Logistics
          </a>{" "}
          settled its fiction early and left the engineering wide open. Rather than answer
          the open questions once, we answer them many times — each as a self-contained pitch
          that irons the loop down to the technology its simulation would actually run on.
        </Lead>
        <p>
          There are two tiers here. The <strong>original three</strong> are independent
          readings of the whole game. The <strong>mutations</strong> take one of those readings —
          <em> The Invisible Hand</em> — and evolve it against a sharper brief, then split again
          on the questions left unanswered.
        </p>
      </Section>

      <Section eyebrow="Tier 1" title="The original three">
        <p>
          Three independent engines for the same fiction. They converge almost completely on
          what the game <em>is</em> and disagree completely on what the computer is doing while
          you play.
        </p>
        <CardGrid>
          <Card
            href="./the-invisible-hand/"
            title="The Invisible Hand"
            accent="amber"
            meta="agent-based economy"
          >
            The market is the antagonist. Prices emerge from producers, consumers, and rival
            haulers; the front is just the hungriest buyer.
          </Card>
          <Card
            href="./the-tide-line/"
            title="The Tide Line"
            accent="cyan"
            meta="pressure field on a graph"
          >
            The front is the antagonist, made physical — a field you bend with cargo, where
            starved sectors rupture and cut the lanes behind you.
          </Card>
          <Card
            href="./dead-reckoning/"
            title="Dead Reckoning"
            accent="rose"
            meta="deterministic content deck"
          >
            The clock is the antagonist. No simulation at all: a seed-driven deck of authored
            events you can hear but never touch.
          </Card>
        </CardGrid>
        <p className="text-sm text-fog-500">
          (These three are the earlier Markdown pitches, kept as-is.)
        </p>
      </Section>

      <Section eyebrow="Tier 2" title="The Invisible Hand — mutations">
        <p>
          Three evolutions of the economic reading. Each inherits <em>The Invisible Hand</em>{" "}
          plus a shared set of revisions, then takes a seed idea and a slice of the open
          questions somewhere the other two won't follow.
        </p>
        <CardGrid>
          <Card
            href="./two-ledgers/"
            mnemonic="M1"
            title="The Two Ledgers"
            accent="amber"
            meta="OQ4 + OQ5 · money as a munition"
          >
            The faction fights on a war budget that can run dry; stations run on local
            liquidity that can bust or be fattened. You profit in the spread.
          </Card>
          <Card
            href="./known-war/"
            mnemonic="M2"
            title="The Known War"
            accent="rose"
            meta="OQ1 + OQ3 · the scripted opening"
          >
            The enemy's opening war plan is set, so topology becomes a riddle: which contracts
            are death-traps, and where the front bleeds materiel.
          </Card>
          <Card
            href="./glass-cockpit/"
            mnemonic="M3"
            title="The Glass Cockpit"
            accent="cyan"
            meta="OQ2 · design from the screen back"
          >
            Start from how you legibly render a massive, abstract front and a growing fleet —
            and let the representation dictate the mechanics.
          </Card>
        </CardGrid>

        <Subheading>What every mutation inherits</Subheading>
        <p>The shared revisions to the original Invisible Hand pitch:</p>
        <DecisionList
          items={[
            {
              handle: "R.1",
              title: "You start each run in a single hauler.",
              body: "Buy more mid-run — if the wartime shipyards have berths to spare.",
            },
            {
              handle: "R.2",
              title: "Ghost haulers are shelved.",
              body: "A good idea parked for now — too hard to implement, balance, and find the fun in at this stage.",
            },
            {
              handle: "R.3",
              title: "Minimalist means abstract, not small.",
              body: "A massive front. Prestige builds your 'space-FedEx' up until your start is good enough to fill the shipyards that build your fleet.",
            },
            {
              handle: "R.4",
              title: "Supply and demand is the friendly faction's job.",
              body: "Euro-Truck-Sim-style job postings from the faction AI. Picking the best is manual at first; automation is something you buy.",
            },
            {
              handle: "R.5",
              title: "The loop is diegetic.",
              body: "The enemy's early war plan is set, so you learn which systems to refuse contracts to. Discoveries get logged in-game for convenience.",
            },
            {
              handle: "R.6",
              title: "Twelve goods: six general, six military.",
              body: "Each category spans raw → intermediate → finished, so the supply chain has depth.",
            },
          ]}
        />
      </Section>

      <Section eyebrow="Consensus" title="Where they converge (the ironed-out core)">
        <p>
          Strip the engines away and the explorers agree on the game. This is the part the
          forks don't move — call it settled.
        </p>
        <DecisionList
          items={[
            {
              handle: "APR.1",
              title: "One ship, four-to-twelve goods, a handful of systems.",
              body: "Small enough to hold the whole board in your head; that legibility is the minimalism.",
            },
            {
              handle: "APR.2",
              title: "The war is sovereign; you only ever influence it indirectly.",
              body: "You move cargo; the war does what the war does. Throughput is the hidden variable, never a lever.",
            },
            {
              handle: "APR.3",
              title: "Prestige buys position and possibility, never a difficulty dial.",
              body: "Producers, geography, decks, reach — but the war's pressure stays untouchable. A better place to fight from, not a weaker enemy.",
            },
            {
              handle: "APR.4",
              title: "Loss is the deposit, and the win is reached sideways.",
              body: "Every run ends in an overrun; the campaign is won only when production finally outpaces attrition across many loops.",
            },
            {
              handle: "APR.5",
              title: "Determinism is non-negotiable.",
              body: "A run is a seed; same seed plus same inputs reproduces it exactly. It shaped every tech choice the pitches make.",
            },
          ]}
        />
      </Section>

      <Section eyebrow="Divergence" title="Where the original three fork">
        <p>
          The whole disagreement is about <em>what gets simulated</em> — and therefore what
          technology it demands.
        </p>
        <CompareTable
          columns={["The Invisible Hand", "The Tide Line", "Dead Reckoning"]}
          rows={[
            {
              label: "Antagonist",
              cells: ["the market", "the map / front", "the clock"],
            },
            {
              label: "Simulated",
              cells: ["an agent-based economy", "a pressure field on a graph", "(almost) nothing — authored content"],
            },
            {
              label: "Headline tech",
              cells: [
                "damped tâtonnement over a data-oriented ECS",
                "graph-Laplacian reaction-diffusion, fixed-point i64",
                "deterministic content interpreter + seeded PCG32",
              ],
            },
            {
              label: "Minimalist floor",
              cells: ["6 systems, 4 goods", "~12 nodes, ~16 lanes", "5 beads, 4 goods, ~12 cards"],
            },
            {
              label: "The run-clock",
              cells: ["uncovered shortage (integral)", "front displacement", "clock ticks (12–20 hands)"],
            },
            {
              label: "Cost center",
              cells: ["tuning the sim", "tuning the sim", "authoring content"],
            },
            {
              label: "Dies if",
              cells: ["prices feel random", "the front smears to mush", "content runs thin"],
            },
          ]}
        />
        <Callout kind="key" title="The one decision that matters next">
          Complexity that <em>emerges</em> (Hand, Tide) versus complexity that's{" "}
          <em>authored</em> (Dead Reckoning). The mutations live on the "emerges" side and
          push it further — toward a living faction economy you ride rather than command.
        </Callout>
      </Section>

      <Section title="How to read this">
        <p>
          Each of the original three named a cheap disqualifying prototype — a one-market
          clearing cell, a 12-node field harness, a single hand. The mutations carry that
          discipline forward: every one of them ends by naming the technology it would run on,
          so the next move isn't to pick a favorite in the abstract — it's to build the
          smallest thing that could prove one wrong. Start anywhere. The fiction is proven;
          we're choosing what it's made of. <Handle id="APR.5" /> says whatever you build, it
          builds from a seed.
        </p>
      </Section>
    </Page>
  );
}

import {
  Page,
  Section,
  Lead,
  Handle,
  Subheading,
  DecisionList,
  Callout,
  StatGrid,
  Figure,
  CompareTable,
  Tabs,
  Tag,
  TagRow,
} from "../components/kit";

export default function TwoLedgers() {
  return (
    <Page
      accent="amber"
      mnemonic="M1"
      kicker="HAND lineage · mutation M1"
      title="The Two Ledgers"
      lede={
        <>
          The war is fought with money as much as materiel — and there are two separate,
          scarce reservoirs of it. You make your living in the gap between them.
        </>
      }
      spark={
        <>
          A shortage isn&apos;t a payday. A shortage that someone can <em>afford</em> is.
          You don&apos;t arbitrage goods cheap-here-dear-there; you arbitrage{" "}
          <em>who can actually pay.</em>
        </>
      }
      backHref="../"
      backLabel="Approaches"
      crumb="Minimalist Space Logistics"
    >
      <Section eyebrow="Mutation M1" title="The spark">
        <Lead>
          The Invisible Hand made the market the antagonist and got one thing exactly right:
          nobody hand-places prices. This mutation keeps that and adds the thing every war
          economy actually runs on and every space-trader game quietly ignores —{" "}
          <strong>money is finite, and it sits in pools that can run dry.</strong>
        </Lead>
        <p>
          In HAND, demand was the only constraint: the front screams, the gradient tilts, you
          fly the goods. But a screaming front with an empty treasury can&apos;t buy a thing.
          So we split the world&apos;s cash into two coupled reservoirs and simulate both as
          first-class state. The friendly faction fights on a <strong>war budget</strong> — a
          treasury that drains as it pays for hauling and refills on its own slow cadence; broke,
          it posts fewer and stingier contracts even as the line caves in. Every station runs on{" "}
          <strong>local liquidity</strong> — a cash balance that a frontier depot can exhaust
          (so it can&apos;t pay you well even mid-shortage) or that your repeated trade can{" "}
          <em>fatten</em> until it&apos;s the richest buyer on the board.
        </p>
        <p>
          Your edge lives in the <strong>spread</strong> between a cash-strapped faction and
          uneven station solvency. Goods still flow on supply and demand — but the manifest only
          pays if the buyer on the other end has coin in the till. You are not racing a price
          gradient. You are racing a <em>solvency</em> gradient, and you are the one current that
          can move money from where it pools to where it&apos;s starving.
        </p>
        <TagRow>
          <Tag>two ledgers</Tag>
          <Tag>faction treasury</Tag>
          <Tag>station liquidity</Tag>
          <Tag>contracts = spending</Tag>
          <Tag>arbitrage the payer</Tag>
        </TagRow>
      </Section>

      <Section eyebrow="The loop" title="Core loop, ironed out">
        <p>
          Decisions made, not deferred. These fold in the shared revisions (a single starting
          hauler, shelved ghosts, an abstract-not-small front, a faction-driven contract board,
          a diegetic loop, the twelve-good canon) and resolve this mutation&apos;s two open
          questions head-on.
        </p>
        <DecisionList
          items={[
            {
              handle: "M1.1",
              title: "Two ledgers, one wallet between them.",
              body: (
                <>
                  Money lives in exactly three kinds of account: the faction <em>treasury</em>{" "}
                  (one), each station&apos;s <em>till</em> (one per station), and{" "}
                  <em>your wallet</em>. A contract is the faction paying you out of the treasury;
                  a market sale is a station paying you out of its till. Every transfer is
                  double-entry — money leaves one account and lands in another, never minted at
                  the point of sale. The HAND free-agent market is replaced by this:{" "}
                  <Handle id="R.4" /> says supply and demand is the faction AI&apos;s job, and a
                  budgeted treasury is precisely how a faction decides what it can ask for.
                </>
              ),
            },
            {
              handle: "M1.2",
              title: "The faction has a treasury, not just a stockpile (resolves OQ.4).",
              body: (
                <>
                  The faction is dual-constrained: it needs materiel <em>moved</em> and it needs{" "}
                  <em>funds</em> to pay for moving it. The treasury drains by every contract bounty
                  it posts and refills at a fixed <strong>war-bond tick</strong> (a flat credit per
                  in-game day, scaled by held territory). When the treasury is fat it floods the
                  board with high-bounty contracts; when it&apos;s thin it posts fewer, leaner jobs
                  and prioritizes military goods over general. A broke faction with a starving front
                  is the cruelest board state in the game — and it&apos;s emergent, never scripted.
                </>
              ),
            },
            {
              handle: "M1.3",
              title: "Stations can go broke or get fat (resolves OQ.5).",
              body: (
                <>
                  Each station holds a cash balance, not just inventory. It <em>earns</em> when it
                  sells its own production to other haulers and to the front; it <em>spends</em>{" "}
                  when it buys goods (from you) to cover local demand. Sell a depot more than its
                  till can cover and its price offer collapses toward zero — shortage without
                  solvency pays nothing. But trade through a station repeatedly and you pump its
                  till: a fattened hub clears at a premium and becomes a reliable payer. Stations
                  are not assumed-liquid backdrops; their solvency is live, player-bendable state.
                </>
              ),
            },
            {
              handle: "M1.4",
              title: "You're paid in the spread, and the spread is a payer-gap.",
              body: (
                <>
                  HAND paid you for being <em>early</em>. Here you&apos;re paid for being the
                  bridge between a rich account and a poor one: buy cheap from a fat station,
                  fulfil a fat-treasury contract, or sell into a depot you&apos;ve fattened
                  yourself. The fantasy is the war-profiteer-with-a-conscience: the front needs you,
                  but only the solvent parts of the war can reward you, and you decide which parts
                  stay solvent.
                </>
              ),
            },
            {
              handle: "M1.5",
              title: "Start in one hauler; buy berths from a faction that can afford them.",
              body: (
                <>
                  <Handle id="R.1" /> — you begin each run in a single ship and buy more only if
                  wartime shipyards have berths to spare. Here that gates on money twice over: the{" "}
                  <em>shipyard</em> needs a free berth (materiel) <em>and</em> the faction treasury
                  has to be flush enough to subsidize a new hull, or you pay full freight from your
                  wallet. Fleet growth is throttled by the same treasury that throttles contracts.
                </>
              ),
            },
            {
              handle: "M1.6",
              title: "Prestige seeds money, not multipliers.",
              body: (
                <>
                  Being overrun banks prestige, and per <Handle id="R.3" /> it builds your
                  space-FedEx up. In this mutation prestige edits the <em>monetary</em> map: a
                  higher starting treasury, a faster war-bond tick, or a seed station that begins
                  pre-fattened. You don&apos;t buy bigger numbers on your ship — you buy a richer,
                  more solvent war to be born into. The campaign win stays the HAND equilibrium
                  (<Handle id="APR.4" />): across loops, production finally outpaces attrition —
                  but now it only counts if the war could <em>pay</em> to keep it flowing.
                </>
              ),
            },
            {
              handle: "M1.7",
              title: "A run is a seed; the diegetic loop is monetary memory.",
              body: (
                <>
                  <Handle id="R.5" /> — the enemy&apos;s opening is set, so you learn which systems
                  to refuse contracts to. The twist: a system about to be overrun is also a station
                  whose till is about to vanish. The in-game log records not just &ldquo;this lane
                  dies on day 6&rdquo; but &ldquo;this depot stops paying on day 5&rdquo; — solvency
                  intel is the new map intel.
                </>
              ),
            },
          ]}
        />
        <Callout kind="key" title="The reconciliation HAND demanded">
          HAND&apos;s &ldquo;living economy&rdquo; feel survives the move to a contract board because
          the board <em>is</em> the economy&apos;s readout. Contracts aren&apos;t a designer&apos;s
          quest list — they are the faction spending a finite treasury against a moving front. When
          the board goes quiet, that&apos;s not content drying up; that&apos;s a war going broke.
        </Callout>
      </Section>

      <Section eyebrow="The model" title="What's being simulated">
        <p>
          Two coupled monetary systems plus a contract generator that spends one of them. The
          ambition is squarely on the <strong>money</strong> — topology and the front&apos;s shape
          belong to sibling mutations; here they&apos;re inputs, and what we simulate is the cash
          that flows over them.
        </p>

        <Subheading>State — three account types, one currency</Subheading>
        <p>
          All money is fixed-point integer credits (no floats — see the tech section). The world
          holds:
        </p>
        <CompareTable
          columns={["Account", "Balance", "Drains on", "Refills on"]}
          rows={[
            {
              label: "Faction treasury",
              cells: [
                "1 global pool",
                "credits",
                "contract bounties paid out",
                "war-bond tick (daily, territory-scaled)",
              ],
            },
            {
              label: "Station till",
              cells: [
                "1 per station",
                "credits",
                "buying goods to cover local demand",
                "selling its production to the front & rivals",
              ],
            },
            {
              label: "Your wallet",
              cells: [
                "the player",
                "credits",
                "buying goods, fuel, berths",
                "contract payouts + market sales",
              ],
            },
          ]}
          highlight={2}
        />
        <p>
          Goods state stays HAND-shaped — per station-good, an{" "}
          <code className="font-mono text-[0.85em] text-fog-200">(inventory, price)</code> pair over
          the twelve-good canon (<Handle id="R.6" />). The mutation bolts a{" "}
          <code className="font-mono text-[0.85em] text-fog-200">till</code> onto every station and a
          single <code className="font-mono text-[0.85em] text-fog-200">treasury</code> onto the
          faction.
        </p>

        <Subheading>Update rule — solvency gates price</Subheading>
        <p>
          Each fixed tick, in fixed agent order: producers add inventory, the front and civilians
          draw it down, and price still adjusts by HAND&apos;s constant-elasticity rule on net order
          flow — <em>but the realized payout is clamped by the payer&apos;s balance.</em> Two new
          mechanics ride on top:
        </p>
        <Tabs
          tabs={[
            {
              id: "solvency",
              label: "Solvency clamp",
              content: (
                <div className="space-y-3">
                  <p>
                    A station&apos;s effective bid for your cargo is{" "}
                    <code className="font-mono text-[0.85em] text-fog-200">
                      bid = min(price · qty, till)
                    </code>
                    . When the till can&apos;t cover the market price, the offered price{" "}
                    <em>sags</em> proportionally toward what it can afford. A 200-credit shortage in
                    a till holding 40 credits pays 40, and the depot is now empty and desperate. This
                    is the core inversion: <strong>demand without solvency is worthless to you,</strong>{" "}
                    and you can see broke buyers coming.
                  </p>
                </div>
              ),
            },
            {
              id: "fatten",
              label: "Fattening",
              content: (
                <div className="space-y-3">
                  <p>
                    A station earns when it on-sells goods to the front (the front pays out of the
                    treasury) and to rival haulers. Route demand <em>through</em> a depot — keep it
                    stocked so it keeps selling — and its till climbs each tick it nets positive.
                    Cross a fatten threshold and the station&apos;s bid ceiling rises: it becomes a{" "}
                    <strong>hub</strong> that clears premium and reliably pays. Fattening is slow,
                    compounding, and reversible — neglect a hub and the front bleeds its till back
                    down.
                  </p>
                </div>
              ),
            },
            {
              id: "board",
              label: "Contract board",
              content: (
                <div className="space-y-3">
                  <p>
                    The board is generated, not authored. Each posting cycle the faction AI scans
                    unmet front demand, ranks gaps by urgency, and posts a contract{" "}
                    <em>only if the treasury can fund the bounty.</em> Bounty ={" "}
                    <code className="font-mono text-[0.85em] text-fog-200">
                      base(good) · urgency · treasury-confidence
                    </code>
                    . A fat treasury posts many fat contracts; a thin one posts a short list of lean,
                    military-first jobs. The board is a live histogram of what the war can afford to
                    want — Euro-Truck-Sim postings whose generosity is a treasury readout.
                  </p>
                </div>
              ),
            },
          ]}
        />

        <Subheading>Determinism — a run is a seed</Subheading>
        <p>
          Same as HAND, harder enforced because money compounds. One seeded PRNG, advanced only on
          ticks, in fixed agent-iteration order. All money is <strong>i64 fixed-point</strong> so
          treasury, tills, and bounties reconcile to the credit with zero float drift — a run is
          byte-reproducible and shareable, and the books always balance.
        </p>

        <Figure caption="Money is conserved: every credit you earn left a treasury or a till. Contracts are the faction spending; the front spends the treasury back into station tills; you arbitrage the gap.">
          <svg viewBox="0 0 720 360" xmlns="http://www.w3.org/2000/svg" className="w-full">
            <defs>
              <marker
                id="m1arrow"
                viewBox="0 0 10 10"
                refX="8"
                refY="5"
                markerWidth="7"
                markerHeight="7"
                orient="auto-start-reverse"
              >
                <path d="M0 0 L10 5 L0 10 z" fill="var(--accent)" />
              </marker>
              <marker
                id="m1arrowdim"
                viewBox="0 0 10 10"
                refX="8"
                refY="5"
                markerWidth="7"
                markerHeight="7"
                orient="auto-start-reverse"
              >
                <path d="M0 0 L10 5 L0 10 z" fill="var(--color-fog-500)" />
              </marker>
            </defs>

            {/* Faction treasury */}
            <rect x="270" y="20" width="180" height="62" rx="10" fill="var(--accent-soft)" stroke="var(--accent)" strokeWidth="1.5" />
            <text x="360" y="46" textAnchor="middle" fill="var(--color-fog-100)" fontSize="15" fontWeight="700">Faction treasury</text>
            <text x="360" y="66" textAnchor="middle" fill="var(--color-fog-400)" fontSize="11" fontFamily="monospace">drains by bounties · refills by war-bond tick</text>

            {/* Contract board */}
            <rect x="40" y="150" width="190" height="62" rx="10" fill="var(--color-ink-850)" stroke="var(--color-line)" strokeWidth="1.5" />
            <text x="135" y="176" textAnchor="middle" fill="var(--color-fog-100)" fontSize="15" fontWeight="700">Contract board</text>
            <text x="135" y="196" textAnchor="middle" fill="var(--color-fog-400)" fontSize="11" fontFamily="monospace">faction spending, gated by funds</text>

            {/* Your wallet */}
            <rect x="265" y="280" width="190" height="62" rx="10" fill="var(--color-ink-850)" stroke="var(--accent)" strokeWidth="1.5" />
            <text x="360" y="306" textAnchor="middle" fill="var(--color-fog-100)" fontSize="15" fontWeight="700">Your wallet</text>
            <text x="360" y="326" textAnchor="middle" fill="var(--color-fog-400)" fontSize="11" fontFamily="monospace">the bridge between pools</text>

            {/* Station tills */}
            <rect x="490" y="150" width="190" height="62" rx="10" fill="var(--color-ink-850)" stroke="var(--color-line)" strokeWidth="1.5" />
            <text x="585" y="176" textAnchor="middle" fill="var(--color-fog-100)" fontSize="15" fontWeight="700">Station tills</text>
            <text x="585" y="196" textAnchor="middle" fill="var(--color-fog-400)" fontSize="11" fontFamily="monospace">go broke · or get fattened</text>

            {/* Treasury -> board (funds postings) */}
            <path d="M300 82 C250 110 190 120 150 150" fill="none" stroke="var(--accent)" strokeWidth="2" markerEnd="url(#m1arrow)" />
            <text x="205" y="118" textAnchor="middle" fill="var(--color-fog-300)" fontSize="11">funds postings</text>

            {/* Board -> wallet (bounty paid) */}
            <path d="M150 212 C190 250 240 268 280 285" fill="none" stroke="var(--accent)" strokeWidth="2" markerEnd="url(#m1arrow)" />
            <text x="188" y="262" textAnchor="middle" fill="var(--color-fog-300)" fontSize="11">bounty</text>

            {/* Wallet -> station (you buy/sell cargo) */}
            <path d="M455 300 C520 285 560 250 580 212" fill="none" stroke="var(--accent)" strokeWidth="2" markerEnd="url(#m1arrow)" markerStart="url(#m1arrow)" />
            <text x="545" y="262" textAnchor="middle" fill="var(--color-fog-300)" fontSize="11">trade (spread)</text>

            {/* Treasury -> station tills (front buys production, pays treasury into tills) */}
            <path d="M420 82 C470 110 540 120 575 150" fill="none" stroke="var(--color-fog-500)" strokeWidth="1.6" strokeDasharray="5 4" markerEnd="url(#m1arrowdim)" />
            <text x="520" y="118" textAnchor="middle" fill="var(--color-fog-500)" fontSize="11">front buys production</text>

            {/* Solvency-clamp note across station */}
            <text x="585" y="232" textAnchor="middle" fill="var(--color-fog-500)" fontSize="10.5" fontStyle="italic">bid = min(price·qty, till)</text>
          </svg>
        </Figure>

        <StatGrid
          items={[
            { value: "i64", label: "fixed-point credits · books balance" },
            { value: "3", label: "account types: treasury · till · wallet" },
            { value: "1 / day", label: "war-bond tick refills the treasury" },
            { value: "0 pay", label: "a shortage an empty till can't cover" },
          ]}
        />
      </Section>

      <Section eyebrow="The build" title="Technology & architecture">
        <p>
          Take a position. The two ledgers are not a UI veneer over HAND&apos;s prices — they&apos;re
          a second simulated system that must reconcile to the credit, every tick, forever. That
          dictates the stack.
        </p>

        <Subheading>Double-entry ledger as the spine</Subheading>
        <p>
          Money moves as <strong>double-entry transactions</strong>: every transfer is an atomic
          (debit, credit) pair over the account set, applied on the tick. This is the boring,
          battle-tested accounting model, and it buys the one invariant the whole mutation rests on
          — <em>conservation.</em> A periodic assertion that{" "}
          <code className="font-mono text-[0.85em] text-fog-200">Σ(all balances) == constant</code>{" "}
          is a permanent CI gate; if it ever fails, money was minted or destroyed and a seed stopped
          being reproducible. No floats anywhere near money: <strong>i64 fixed-point</strong> (credits
          as integers), because float rounding is exactly how double-entry books silently stop
          balancing.
        </p>

        <Subheading>Data-oriented, single-threaded, on the tick</Subheading>
        <p>
          Like HAND: dozens of stations and producers, a handful of contracts in flight — trivial
          per tick, so the constraint is determinism, not throughput. Run it{" "}
          <strong>single-threaded over struct-of-arrays buffers</strong> (a hand-rolled SoA layer in
          Godot, or <code className="font-mono text-[0.85em] text-fog-200">hecs</code>/Bevy if this
          were Rust), iterated in fixed order. Tills and the treasury are just more component arrays.
          The contract generator runs as a system that reads the demand and treasury buffers and
          writes the board buffer — pure function of state, no hidden globals.
        </p>

        <Subheading>The contract generator</Subheading>
        <p>
          A <strong>budget-constrained priority queue</strong>. Each posting cycle: score every
          unmet front-demand gap by urgency, sort, then greedily post bounties down the list until
          the treasury&apos;s <em>spendable</em> slice (a fraction it won&apos;t dip below) is
          exhausted. It&apos;s a knapsack-flavored greedy fill — cheap, legible, and the generosity
          of the whole board falls straight out of one treasury number. Contracts expire on the same
          clock that overruns systems, so a posting&apos;s value decays as its destination nears the
          front.
        </p>

        <Subheading>Prestige persists as a money snapshot</Subheading>
        <p>
          Per HAND&apos;s snapshot model, end-of-run state serializes the <em>monetary</em> map:
          starting treasury, war-bond rate, and any pre-fattened station tills. No event log is
          needed — ghosts are shelved (<Handle id="R.2" />), so the materialized snapshot is the
          whole of persistence. A compact, deterministic re-seed.
        </p>

        <Callout kind="warn" title="What to prototype first">
          The <strong>single solvency cell</strong>: one station with a till, one producer, the
          front as a treasury-funded consumer, and a player who can buy and sell. Wire double-entry
          transactions and the{" "}
          <code className="font-mono text-[0.85em] text-fog-200">bid = min(price·qty, till)</code>{" "}
          clamp, then tune until <em>draining a till visibly kills its price</em> and{" "}
          <em>fattening it visibly raises its ceiling.</em> Run it headless from a seeded script;
          assert byte-identical reruns and the conservation invariant from day one. If pumping and
          starving a single till doesn&apos;t <em>feel</em> like solvency, nothing scales up.
        </Callout>

        <Callout kind="warn" title="Key risks, named">
          <ul className="ml-4 list-disc space-y-2">
            <li>
              <strong>Deflationary spiral.</strong> Tills drain toward zero, the treasury hoards, the
              board goes dead, the player can&apos;t earn, nothing recovers. Mitigation: the war-bond
              tick is a guaranteed money <em>source</em>, the front pays the treasury back into tills,
              and a treasury floor forces a minimum board even when broke. Money must keep
              circulating, not pool and die.
            </li>
            <li>
              <strong>Conservation rot.</strong> One float, one untracked credit, one mint at the
              point of sale, and the books stop balancing and seeds stop reproducing. Mitigation:
              i64 everywhere, double-entry only, and a Σ-balances assertion as a permanent test gate.
            </li>
            <li>
              <strong>Legibility collapse.</strong> &ldquo;Why did this depot pay me half?&rdquo; must
              never read as random. Mitigation: surface the till as a small fuel-gauge per station and
              a one-line bounty rationale on the board — the model is maximalist, the readout stays
              minimalist (HAND&apos;s discipline, applied to money).
            </li>
          </ul>
        </Callout>

        <TagRow>
          <Tag>double-entry ledger</Tag>
          <Tag>i64 fixed-point</Tag>
          <Tag>SoA / ECS</Tag>
          <Tag>single-threaded tick</Tag>
          <Tag>budget-constrained greedy</Tag>
          <Tag>seeded PRNG</Tag>
          <Tag>snapshot re-seed</Tag>
        </TagRow>
      </Section>

      <Section eyebrow="The trade" title="What this buys, and what it costs">
        <p>
          What it buys: the most direct economic descendant of HAND, where the antagonist isn&apos;t
          just an indifferent market but an <em>under-funded</em> one. Shortages you chase are real
          disequilibria <em>and</em> real solvency gaps; a quiet contract board is a war you can
          watch go broke; a hub you built is a till you fattened by hand. The war-profiteer fantasy
          gets teeth: you can keep a frontier solvent or let it starve, and the front&apos;s fate is
          downstream of where you chose to move money.
        </p>
        <CompareTable
          columns={["The Invisible Hand", "The Two Ledgers (M1)"]}
          rows={[
            {
              label: "Antagonist",
              cells: ["an indifferent market", "an under-funded war economy"],
            },
            {
              label: "What you arbitrage",
              cells: ["goods, cheap-here-dear-there", "the payer — who can actually pay"],
            },
            {
              label: "Scarce resource",
              cells: ["supply (goods + position)", "supply and money (two pools)"],
            },
            {
              label: "The contract board",
              cells: ["(free agent market)", "faction spending a finite treasury"],
            },
            {
              label: "New simulated state",
              cells: ["prices over inventory", "+ treasury, + per-station tills"],
            },
            {
              label: "Headline tech",
              cells: [
                "damped tâtonnement over an ECS",
                "double-entry i64 ledgers + budgeted contract generator",
              ],
            },
          ]}
          highlight={1}
        />
        <p>
          What it costs: a <strong>second balancing act</strong> on top of HAND&apos;s already-hard
          emergent-pricing problem. You&apos;re now tuning two coupled feedback loops — prices{" "}
          <em>and</em> liquidity — and a deflationary spiral is a real failure mode that a goods-only
          economy never faces. The conservation discipline is unforgiving: money is the one thing the
          player will notice instantly if it leaks. And there&apos;s a teaching cost — players must
          learn that a fat shortage can be a bad job, which is counter to every trading game they&apos;ve
          touched.
        </p>
        <Callout kind="note" title="The bet">
          A market that can <em>run out of money</em> is more alive than one that can only run out of
          goods — and far closer to how a real war economy strangles itself. The whole mutation is
          that wager. If you don&apos;t buy that two coupled ledgers are worth the second tuning loop,
          take a sibling: this one only pays off if you commit to money as a simulated, two-sided
          system all the way down.
        </Callout>
        <p className="text-sm text-fog-500">
          What I&apos;d actually build first: the single solvency cell — one till, one producer, a
          treasury-funded front, double-entry transactions, the{" "}
          <code className="font-mono text-[0.85em] text-fog-200">min(price·qty, till)</code> clamp —
          headless, seeded, with the Σ-balances assertion green from the first commit.
        </p>
      </Section>
    </Page>
  );
}

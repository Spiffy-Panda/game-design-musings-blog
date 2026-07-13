# PLAN — adventuring-guild-teller (Adventuring Guild Teller)

**Status:** shipped v0 + correction round 1 folded in (2026-07-12). **Open: `AGT.1` / `AGT.5` / `AGT.7`** — mechanics work gated on those.
**Folder:** `../adventuring-guild-teller/` (HTML-first musing; folder == slug). Nav spec: `../adventuring-guild-teller/ADVENTURING-GUILD-TELLER.md` (mnemonics `AGT` pitch claims · `AGR` research risks).

## The brief (Panda, 2026-07-12 — the chat dump this plan exists to serve)

> You are an adventuring guild's teller in charge of registering requests and confirming
> completion. One part procedural job sim (Papers, Please), one part social/management
> (Stardew's non-farming parts), one part townee-fishbowl (Tomodachi Life, Sims).
>
> The main loop and the hook is the procedural day-to-day of a teller validating
> questing — checking collected herbs against descriptions, validating adventurer rank,
> judging what quests are valid based on current availability. Strange-apothecary
> inspired; mostly right-vs-wrong answers so the player can go into a flow.
>
> Second part is hanging out in the guild afterward — where players make **all** the
> decisions: which quest goes on the board, which goes directly to a befriended
> adventurer, other guild management. Also the social section: suggest loadouts,
> introduce people; through this shape what adventuring teams go down and feel ownership
> in the town.
>
> Last part is for creators: create characters' starting attributes, models/paperdolls,
> backstories (Popup Dungeon does this amazingly). Mostly separate from play time; a
> nightly summary tells how your characters fared in the dungeon — not too useful, more
> for flavor.

(Lightly normalized from chat; the pitch page is the faithful structured read-back.)

## Shipped (2026-07-12) — v0 "pitch stage"

- [x] `pitch.html` — the showpiece: received-stamp hero, day strip (Desk → Floor →
      Night → Summary), three pillar cards with governing rules ("At the counter there
      is a right answer" / "Influence, never orders" / "The summary is gossip, not
      telemetry"), the **Separation Principle** callout (discretion evicted from the
      counter — flow at the desk, judgment on the floor), and the read-back:
      claims `AGT.1`–`AGT.12`, each tagged given/read/gap + a "correct me" line.
- [x] `research.html` — desk research by pillar with take/skip verdicts: checking games
      (Papers, Please · Strange Horticulture · Potion Craft · Death and Taxes ·
      Not Tonight · Mind Scanners · No Umbrellas Allowed · Yes, Your Grace), the floor
      (Stardew · Recettear · Potionomics · Majesty · VA-11 Hall-A / Coffee Talk ·
      Darkest Dungeon embark · Dungeon Village · one-liners), the roster (Popup Dungeon ·
      Tomodachi Life · Sims/Wildermyth · RimWorld/FM one-liners), the guild-receptionist
      trope (Guild Girl; Mato Kousaka LN 2021 / CloverWorks anime 2025), synthesis:
      **warm-bureaucracy positioning gap** + risk register `AGR.1`–`AGR.6` (each tied to
      the claim it pressures). Web-spot-checked facts; sources footnoted on-page.
- [x] `index.html` — hub: framing + cards → pitch / research, ghost card → the Morning
      Queue candidate.
- [x] `emblem.svg` + themed landing row (parchment / approval-green / wax-red / brass,
      serif).
- [x] Registered in `MUSING-CONFIG.json`; spec `ADVENTURING-GUILD-TELLER.md`; human
      `README.md`; verbatim-copy `build-musing.py`.

## Correction round 1 (2026-07-12) — rulings, folded into the pitch

- [x] **AGT.2** confirmed (fishbowl-as-substrate framing stands); the full-town ruling
      in that reply filed under AGT.9 (scope match — flagged to Panda in chat).
- [x] **AGT.3** mostly confirmed; expeditions run async and can span multiple days —
      departures/returns not pinned to the morning tick.
- [x] **AGT.4** both: rank gate-checks are routine desk work; rank-up audits are real,
      rarer desk events.
- [x] **AGT.6** confirmed and doubled: Strange Horticulture **and** Strange Antiquities
      (Bad Viking, 2022 / Sept 2025 — web-verified); research entry now covers the pair.
- [x] **AGT.8** mechanism: acceptance = two affinity scores — suggestee→player (teller
      trust) × suggestee→target (the person / the loadout). AGR.2 gains a "Settled" note.
- [x] **AGT.9** ships with a full town; the creator covers adventurers, quest-posters,
      and anyone else; customization always optional beyond anything required.
- [x] **AGT.10** refined: summaries may carry actionable events ("her sword failed to
      penetrate the hide"); every detailed stat lives in in-game bios — the player never
      remembers or writes anything down. AGR.3 reframed: risk moves to bio UX.
- [x] **AGT.11** the floor **never ticks**: no time limits, no decaying affection;
      desk-round + low-effort floor is always a legitimate day. Pillar II rule now
      "Influence, never orders. And no clock, ever."
- [x] **AGT.12** no death: gearless respawn at the dungeon entrance; lost gear persists
      and seeds retrieval quests — failure feeds AGR.1's request generator. AGR.6 settled.

## Next

- [ ] **Correction round 2** — settle the three open claims: AGT.1 (formal authority
      beyond the counter?), AGT.5 (desk stays dilemma-free — the spine), AGT.7 (what's
      in the "other management" bucket). Fold in place as above.
- [x] **The Morning Queue** — playable Godot 4.6 (GDScript-only) desk-shift prototype at
      `../adventuring-guild-teller/morning-queue/`. Specs: `MORNING-QUEUE.md` (architecture),
      `INSPECTION-TOOLS.md` (examine/weigh + binary desk), `CONTENT-BANKS.md` (week-of-content
      + generator). Built across 2026-07-12/13 via sub-agent workflows, each phase
      capture-verified with the in-engine DevHarness. Arc:
      - Scaffold + 16 curated visitors (`data/visitors.json`) + the `references.json` rulebook;
        autoloads Deck/Session; four components + parchment ThemeFactory.
      - **Binary desk** (`Session.STRICT_BINARY = true`, reversible) — hold/conditional cut
        per Panda; this is the mechanical resolution of **AGT.5** (the desk is dilemma-free,
        two stamps). Round-2 pitch wording can still be folded into `pitch.html` separately.
      - **Inspection tools** (The Glass = examine, The Scale = weigh) with decoy readings;
        standing orders became `accept`/`total` **limits** the Scale measures; item_check =
        identity (Glass vs Book) + amount (Scale vs limit).
      - **Localization prep** (`scripts/loc.gd`), **DevHarness** (viewport capture → `.captures/`,
        auto-step; editor-toggleable `enabled`, ships `false`).
      - **Week of content + procedural generator** — banks (`references.json` broadened to 24
        items/20 postings/6 ciphers/10 drops; new `townees.json` + `adventurers.json`
        directories; `generation.json`), the **dues** mechanic (owing townees can't post),
        and `scripts/gen/ShiftGenerator.gd` (`generate_shift(day)`, seeded → 7 reproducible
        days; day 0 = curated tutorial). Self-check: `7 days, 97 visits, 0 problems`.
- [ ] **Next on the prototype** (only-in-chat backlog, recorded here):
      - a **shift-select / day-advance hub** so the week actually plays as a week (increment
        `Deck.day` between shifts; a between-shift summary/floor beat).
      - a **"pay dues" interaction** (the floor side of the dues gate — let an owing townee
        clear arrears so their post is accepted).
      - **richer Glass readings for card/seal/token visitors** (the generalized Glass works,
        but non-herb subjects read thinner than herbs).
      - an **amount-fail visitor** in the curated shift (currently no curated visitor is
        rejected on weight alone — the Scale's teeth show only via the moonwort pass + decoy;
        would need an `amount` axis entry in `visitors.json`'s schema).
- [ ] Mechanics note: the desk's procedural request/proof generator sketch (AGR.1 is the
      hard problem — reference-library growth without rule soup). **Largely superseded** by the
      shipped `ShiftGenerator` + `CONTENT-BANKS.md`; keep for the design-writeup angle.
- [ ] Floor-economy sketch: costs for favoritism (AGR.4) + refusal-reason surfacing
      (AGR.2).
- [ ] Roster sketch: creator scope (paperdoll + statlet + quotable prose, AGR.5) and the
      summary generator's "quotable, barely actionable" dial (AGR.3).

## Open questions (round 2)

- Authority: does the teller hold any formal power beyond the counter, or is it all
  influence? (AGT.1)
- The spine: confirmed that the desk stays dilemma-free — no routine moral squeeze at
  the counter? (AGT.5) — **mechanically settled 2026-07-13**: the prototype ships a binary
  APPROVE/REJECT desk (`STRICT_BINARY = true`), so the counter is objective by construction.
  Still worth folding the ruling into `pitch.html`'s AGT.5 claim in a round-2 pass.
- What's actually in the "other management of the guild" bucket — fees, upgrades,
  staffing, scheduling? (AGT.7)
- New, from the round-1 rulings: how does a *player-authored* character's bio interact
  with the creator's authored backstory — does the sim append to it (scars, gear lost,
  grudges), Wildermyth-style? (feeds the Roster sketch; no handle yet — becomes AGT.13
  if it graduates to a claim)

# Minimalist Space Logistics

*A 4X space game with the empire turned all the way down — until only the supply line is left.*

> You are not the Emperor. You are not even an admiral. You own **one trading ship**
> in a war you did not start and cannot win. Move cargo. Make money. Stay ahead of a
> front line that is always, quietly, collapsing toward you.

## The one-sentence pitch

Take a [4X](https://en.wikipedia.org/wiki/4X) space game — think *Stellaris* — and
delete almost all of it. No diplomacy screen, no fleet micro, no tech tree the size of a
dinner table. What's left is the part nobody usually plays directly: **logistics**. You
are a single hauler keeping an unwinnable war supplied, one run at a time.

## What you actually do

The moment-to-moment game is small on purpose:

- Buy goods where they're cheap — `ore`, `fuel`, `munitions`, `rations`.
- Haul them across a handful of systems to where they're scarce (read: where the
  fighting is).
- Sell into shortage, bank the margin, and decide what to carry next.
- Watch prices move as the front moves — a system that was a safe market last jump is a
  crater this one.

The entire loop fits on a napkin:

```
buy(cheap)  ->  haul(across systems)  ->  sell(into shortage)  ->  bank(margin)  ->  repeat
                                          ... until the front line catches up and the run ends.
```

That's it. No empire to manage. The empire is *off-screen*, and it is losing.

## The twist: you can't win the war, so you don't try

Every run ends the same way. The front line advances, your routes get cut, and
eventually the war **overruns you**. Not "you might lose" — you *will* be overrun. The
question is never *whether* the run ends but *how much you moved before it did*.

This is the whole emotional hook: you are a small, competent person doing an honest job
inside a catastrophe far larger than you. You can be excellent at it and still lose the
ship. That isn't failure — that's the genre.

## Loss is the meta-game (prestige)

Being overrun is the *currency*. Each run converts everything you moved into prestige,
and prestige buys permanent upgrades for the next loop:

1. **Starting capital** — begin a run with more money, so you skip the slow opening.
2. **Production-station locations** — unlock where factories and depots sit on the map,
   reshaping every future trade route.
3. **Ship and route upgrades** — more hold, more range, safer corridors, faster turnaround.

So a "lost" run is never wasted: it is the only way the next one starts stronger. The
roguelite loop *is* the design — the war is the score, the prestige tree is the progress.

## The win condition you reach sideways

Here is the long arc. You can't win the war by trading. But the **military–industrial
complex** can win it — *if it is supplied fast enough to replace what the front loses.*

> Win the campaign by being such a good logistician, across so many loops, that
> production finally outpaces attrition and the front holds.

You never fire a shot. You win by **out-logisticing the loss curve** — keeping the
factories fed until the line stops moving backward. The 4X "eXterminate" happens entirely
off-screen, paid for by your cargo manifests.

## Why this might be interesting

A few design bets worth poking at:

- **Subtraction as the hook.** Most 4X games add systems. This one's pitch is everything
  it *removed* — the fantasy is the courier, not the king.
- **Inevitable loss reframed as progress.** The run-ending defeat isn't a fail state, it's
  the prestige deposit. Losing should feel *productive* instead of punishing.
- **A macro you only influence.** You never command the war, but your throughput is the
  hidden variable that decides it. Small actions, large indirect stakes.

## Open questions (to be filled in)

This is a first sketch — the framework is settled, the design is not. Still open:

- How many systems / goods before it stops being "minimalist"? Where's the floor?
- Is a run measured in time, in jumps, or in how far the front has advanced?
- Does prestige ever let you *touch* the war directly, or is influence always indirect?
- What's the failure texture between runs — close calls, cut routes, a market that turns
  on you mid-haul?

## Approaches to building it

Those open questions don't have one answer, so this musing forks into a small **gallery of
approaches** — each a self-contained pitch that irons the loop down to the technology its
simulation would run on. Three original readings (the market as antagonist, the front as a
fluid, the war as a deck of cards), plus three *mutations* of the first that push the
economic reading further — money as a munition, a scripted-enemy map, a fleet you read at a
glance.

**[Open the approaches gallery →](./approaches/)** — they converge almost entirely on what
the game *is*, and disagree completely on what the computer is *doing* while you play, which
turns out to be the only decision that matters next.

---

*Status: living draft. The pitch is real; the numbers are not. Details land in a later
pass — this page exists to give the musing framework something true to render.*

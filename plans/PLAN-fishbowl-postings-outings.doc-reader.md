# PLAN-fishbowl-postings-outings.doc-reader.md — the `PNO` kickoff prompt (paste into a fresh chat)

**What this is.** The short prompt Panda pastes into a **fresh Opus chat** to start the `PNO` build. It
does no explaining of its own — its whole job is to send the agent into
[`PLAN-fishbowl-postings-outings.handoff.md`](./PLAN-fishbowl-postings-outings.handoff.md) (the operating
brief), make it read the chain **through sub-agents** rather than burning its own context, and stop it at
the gate before it writes a line.

**Expect it to come back and wait.** The build is gated on nine open rulings; a correct agent's first
response is the nine rulings with their recommendations, a drift report, and an `M1` slice plan — not
code. If it starts writing code on turn one, it skipped the gate: stop it.

Everything below the line is the prompt. Paste it verbatim.

---

```
Implement PNO — postings & outings — in the village fish-bowl prototype
(adventuring-guild-teller/fishbowl/).

Your operating brief is plans/PLAN-fishbowl-postings-outings.handoff.md. Read that FIRST and work
to it — it names the rest of the Rule 4 chain, the hard rules, the determinism contract, the nine
traps waiting in the code, the milestone order, and what done means. The spec it implements is
plans/PLAN-fishbowl-postings-outings.md.

Before you write a single line of code:

1. THE GATE. This build is gated on nine open rulings, PNO.D1-PNO.D9. Check the spec's Status line
   and its "The asks" section. If they are still open you are NOT cleared to build — present the
   nine to me with the spec's recommendation for each and wait. Do not guess, and do not "start on
   the safe parts": PNO.D1 (vocabulary) and PNO.D2 (golden town stays posting-free) decide type
   names and test strategy from the first commit. If I reply "adopt the recommendations", that is a
   full ruling — proceed on the recommended option for all nine and record the adoption in the spec
   and DEV-LOG.md.

2. SPEND SUB-AGENTS, NOT YOUR CONTEXT. Follow the handoff's §2 delegation doctrine. Fire the
   drift-check reader it hands you (the spec makes 22 falsifiable claims about the code, verified
   2026-07-16 — re-verify before trusting any of them; a drifted claim can change a milestone's
   approach). Run independent readers in parallel in one message. Keep your own context for the
   engine work, which is the part you must not delegate.

3. REPORT, THEN STOP. Come back with: the gate status (all nine rulings), the drift report, any
   doc/code desync you hit (Rule 3), and your first-slice plan for PNO.M1 — the board only, no
   outings. Then wait for my go.

Two rules travel with you and into EVERY sub-agent prompt you write, verbatim:

RULE 1 — NO INLINE INTERPRETER CALLS. STERN WARNING. No `python -c`, `python3 -c`, `py -c`,
`node -e`, etc. TRIGGER: if `import` (or `require`, `using`, `#include`) appears in a command line
you are about to send to a shell, STOP — create scrap_scripts/<lang>/<NN>_<slug>.<ext> and run that
file instead. Shell one-liners are fine (git status, a single grep, ls | head); escalate to a file
the moment one grows loops, variables, conditionals, or more than a couple of pipes. Every script
anchors to the repo root (Path(__file__).resolve().parents[N] or the language equivalent) — never
assume the invocation directory. Sonnet-tier models have ignored this rule before; do not be one of
them.

ISOLATION RULE (hard, standing). You do NOT read or modify adventuring-guild-teller/morning-queue/**
or plans/PLAN-morning-queue-tiers.md. The fish-bowl shares no code with the desk prototype and was
built without reading it. Scope every glob and grep to adventuring-guild-teller/fishbowl/ or the
named docs. This plan is exactly where the two are most tempted to touch, and PNO.D9 says not now —
if you find yourself wanting to look, that is the rule working.
```

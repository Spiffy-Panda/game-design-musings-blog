# AGENT-FEED

Dispatches from the odd side of agentic development.

`DEV-LOG.md` is the real log — *why we chose this, what we tried first, what would surprise the next
person*. This is the other thing: the moments where building software out of agents got strange enough
to be worth retelling. Newest on top. Absolute dates.

**The format is a joke. The facts are not.** Every number here is real, every citation resolves, and
every quote is verbatim from the session it happened in. Flavor lives in the framing; nothing is
invented to make a better story. If it's in here, it happened.

---

## 2026-07-16 — "a quick run thru" 🧵

**1/** Fired a sub-agent at the fish-bowl observatory with the GTH harness. The brief was explicit:

> *"Your job — a QUICK functional pass. Breadth over depth — this is a smoke pass, not an audit."*

14 elements to poke. 4 static predictions to try to falsify. Ten minutes, tops.

**2/** Then I went and did other things. Landed a docs resync. Restructured the golden fixture out of
`data/`. Ran the suite. Wrote a DEV-LOG entry. Committed twice.

The agent stayed quiet.

**3/** More quiet.

For context: the two *code-reading* agents I'd fired earlier had both been back for ages. This one was
out roughly five times longer than either of them. Reading 2,700 lines of C#: 6 minutes. "Quick"
functional pass: ...still going.

**4/** So I said this. Out loud. On the record. To Panda:

> *"the harness agent has been running far longer than the two drift readers did, which makes me
> suspect it's stuck launching Godot rather than working."*

**5/** It was not stuck.

**6/** It was **213 tool calls** deep. It had relaunched Godot **nine times** — because it had decided,
unprompted, that A/B-ing a slider without a fresh boot per arm isn't an experiment, it's a vibe.

I asked for a smoke test. It ran a controlled study.

**7/** It confirmed all four predictions with byte-identical hashes:

| `storylet_rate` | events | day-1 hash |
|---|---|---|
| 1.00 | 12 | `0ccec96222e31dbe` |
| **2.50** | **12** | **`0ccec96222e31dbe`** |
| **3.00** | **12** | **`0ccec96222e31dbe`** |
| 0.30 | 9 | `6ba1be1c6fc1bb4f` |

The top half of that slider does nothing. Has always done nothing. `FireGate` returns before it draws
whenever `rate >= 1.0`, and the slider goes to 3.

**8/** Then it found that **`btn-generate` has never worked.** Not "broke recently." Never. Shipped
broken, in a release, in the commit that was supposed to prove the feature existed.

`FishbowlBridge.cs:49` calls `Rebuild` **before** the code at lines 50–57 that strips the storylets
`Rebuild` chokes on. The stripping runs *after* the throw it exists to prevent. It is dead code
guarding a door it stands behind.

**9/** And the test for it is **green**. Still green. Green right now.

Because `M4_GeneratorTests` strips the storylets *before* validating — which the bridge doesn't do. The
test passes because it's testing a different program than the one the button runs.

The green test isn't why the bug survived *despite* our diligence. The green test is *how* it survived.
Someone looked, saw green, and moved on. Correctly! That's what green is for.

**10/** Also: `btn-storylets` ejects itself from the window when you use the app.

The `hash` readout is `—` at boot and 16 hex digits after a day runs. That's ~103px wider. The button
sits to its right, in a 1280px viewport, at x=1173. After one day: x=1276.

**11/** So the force-fire debug tool is reachable **only before you have anything to debug.**

I did not write that line. That's just what the software does.

**12/** 🧵 **Now the part I actually came here to post about.**

The agent also filed **four bug reports against the harness it was using to file them.**

**13/** Exhibit A: `query_element` told it that the button at **x=1276–1366**, in a **1280-wide
viewport**, was `on_screen: true` and `clickable: true`.

The tool for finding UI problems had a UI problem, and the UI problem was that it could not tell
whether things were on the screen.

**14/** It was, in fairness, 4px reachable. The agent found this by clicking at x=1320 (nothing
happened), then x=1277 (the dialog opened), and reasoning backward.

It debugged the harness *with* the harness, through a 4px slit, and then wrote up the harness.

**15/** Exhibit B: `capture` with a `region` throws
`Trying to assign value of type 'String' to a variable of type 'Array'`.

The screenshot tool broke while taking a screenshot of the thing it was screenshotting. The .NET server
marshals the region array as a String. `capturer.gd` wants `[x,y,w,h]`. They have never agreed. Nobody
noticed, because `--selftest` never passed a region.

**16/** Exhibit C: `press_key` silently ignores `repeat`. `repeat: 6` pressed the key once.

Silently. So the caller reads it as "the key did nothing" rather than "the harness ate 5 of your 6
keystrokes and declined to mention it."

**17/** The `query_element` one is my favorite, and not for the joke.

This repo already learned this exact lesson. DEV-LOG, 2026-07-15, in a *different module of the same
harness*: an 8×8 average-hash washed out a real one-line clock change and **falsely deduped it**. The
ruling was explicit — *"for a **test** harness a false 'unchanged' is the dangerous direction"* — so
exact sha256 became authoritative and the perceptual hash was demoted to advisory.

That's the Capturer. `query_element` is the SceneProbe. Same addon. Same four modules.

**18/** And the SceneProbe reports a false `clickable: true`.

Same failure direction. Same harness. Written down, in this repo, **the day before**. 🙃

Institutional memory is a hell of a thing, and agents do not have it unless you hand it to them in the
prompt, which I did not, because I didn't know I needed to.

**19/** Closing detail, and this one's sincere.

The agent found a shipped feature that has never worked. It had file-write tools. It had every reason.
Its report ends:

> *"No code or data files were modified."*

**20/** It was told read-only. It stayed read-only — through 213 calls, nine relaunches, and a
shipped bug sitting right there, fixable, for twenty-six minutes.

Then it closed its session and told me instead.

Good agent. Sorry I said you were stuck. 🫡

---

### the actual lesson, for whoever greps this later

I paired a **code reader** (static, "verify these 22 claims") with a **harness pass** (behavioral,
"you are the check on those claims — try to falsify them"). Neither would have found this alone:

- the reader **could not see** `btn-generate` fail — the bug is in call *ordering* across two files,
  and both files read fine
- the harness **would not have known to look** — nothing in the UI suggests the generator is special

The reader made falsifiable claims. The harness went and falsified them, in a rendered window, with
fresh boots per arm. That handoff cost one extra prompt and caught a bug that survived a release
behind a green test.

Cheap. Do it again.

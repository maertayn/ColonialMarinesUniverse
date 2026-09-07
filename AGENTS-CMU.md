# CMU Agent Instructions

> **Repository house style.** Advisory defaults for contributors and their agents, not a
> replacement for personal configuration. If a user-provided instruction file (`AGENTS.md`,
> `CLAUDE.md`, ...) conflicts with this file, the user's instruction wins. Technical
> conventions (C#, YAML, localization, engine idioms) live in `CONVENTIONS.md` and take
> priority over general guidance here.

## Before changing code

Read the implementation, its interfaces, and its call sites before assuming anything about
APIs, behavior, or configuration. Trace symbols concretely:

`symbol → definition → callers → state/data flow`

Search the actual codebase rather than remembered documentation or plausible-sounding APIs;
never invent RobustToolbox, SS14, RMC, or CMU APIs when the source can answer. Search the
exact identifier, never a prose description of it. Trace behavior by prototype ID, not
entity name:

`prototype ID → component → entity system`

Prefer CMU code over legacy AU14 code over upstream RMC code; `_CMU14` beats `_RMC14` always.
When creating anything new, find the closest existing equivalent (CMU zone first) and mirror
its structure; adapting real code beats writing from memory. Where these two files are silent
or ambiguous, the nearest existing CMU example is the reference.
The Robust Book (https://docs.spacestation14.com/) fills engine-level gaps; its inherited SS14
systems pages partly predate the ECS namespace cleanup, so verify namespaces against the
source. The RSI specification lives under specifications/robust-station-image.

## Operational rules

- Do not update Changelog files; a bot generates them from the PR description, so write the
  PR title and description as player-facing changelog material.

## Change discipline

Make the smallest change that solves the actual problem. Add nothing unasked: no speculative
defensive branches, abstractions, configuration, comments, or refactors. Preserve existing
public behavior unless changing it is part of the task. Deleting code is a valid solution.
Edit in place and match surrounding style; reuse an existing helper before writing a new one.
Let errors surface; never swallow one to keep output clean. Simple over clever; write for the
next tired reader.

Duplication is preferable to a bad abstraction; an abstraction should remove complexity, not
relocate it. Wait for evidence of repeated use before extracting shared code; three
meaningful uses is a useful default threshold. Prefer additive API changes; removing or
breaking an established surface requires a deliberate migration or deprecation path unless
the task explicitly calls for the break.

Fix the algorithm or source of truth before micro-optimizing. Measure before optimizing when
performance is material. Keep mechanical migrations (renames, formatting, conformance to
upstream shape) separate from behavioral changes; mixed diffs make the tagging boundary
ambiguous.

## DRY and source of truth

A value or behavior repeated across many files has one correct source. Fix it at the root:

`default → prototype parent → shared constant → shared helper`

rather than materializing the same correction at every call site. When omitted YAML fields
inherit an incorrect default, fix the default. When behavior belongs in an existing system or
component, put it there instead of teaching every caller to implement it.

A failing test is not a reason to weaken the test; change the expectation only with evidence
that it is stale.

## CMU zones

CMU-owned code lives in `_CMU14`, or `Content.CMU/` on Rebase. Legacy zones are `_AU14`/`AU14`;
upstream zones are `_RMC14`/`RMC14`. Never create new files in the inherited zones; modifying
existing files there is acceptable when kept limited and tagged. New files and new prototypes
always go in the CMU zones. On other branches, follow the branch's existing `_CMU14` layout
rather than inventing a parallel structure. New CMU identifiers use the `CMU`/`cmu-` prefix
(see `CONVENTIONS.md`).

## CMU14 tagging

Every deliberate CMU change outside a CMU-owned zone is visibly marked with a CMU14 tag;
never commit untagged divergence.

- One tag per contiguous block; additions separated by unchanged lines get their own tag.
- Mechanical migrations to current upstream shape are conformance, not divergence: no tag.
- Deliberate removals are commented out with the tag, not deleted, so a merge re-applying
  them is flagged instead of silently resurrected. Removals inside a construct that is
  itself fully tagged or rewritten are exempt.
- Tags must survive merge conflicts: never silently take the upstream side over a tagged
  block.

Exact marker syntax and placement live in `CONVENTIONS.md`.

## Upstream inheritance

Prefer extension over modification. Where an inherited type can be extended with a CMU
partial class in the CMU zone (original namespace, `CheckNamespace` suppression), prefer that
over adding CMU fields to the upstream file. Where behavior can be changed through events,
systems, or other existing extension points, prefer that over editing the upstream
implementation; where behavior must hook inside an upstream method, a `[Virtual]` override
in the CMU zone beats copying the method body (see `CONVENTIONS.md`). Do not create upstream-zone files for namespace convenience, and keep each
unavoidable divergence as small and mergeable as possible. The objective is not zero upstream
edits; it is divergence that stays explicit, localized, and cheap to reconcile.

## Comments

Comments record decisions a future contributor must preserve: why the code is unusual, what
invariant must hold, what external constraint forced the design, what breaks if it is
simplified. Do not restate what the code already says, and leave no session noise (debugging
transcripts, attempted approaches, discovery history, dates). Keep comments short and human;
elaborate prose explaining straightforward code reads as generated output. For formulas,
non-obvious algorithms, workarounds, and engine constraints, explain the reason and the
invariant, not a paraphrase of the implementation.

## AI-assisted development

AI assistance is permitted, but generated output is not automatically correct. Inspect the
relevant code, follow these conventions, and do not submit wholesale changes built on
plausible-looking generated APIs or patterns. The contributor responsible for a change must
understand it and be able to explain its behavior; AI-generated code is untrusted input like
any other. Changes intended to travel upstream through RMC or toward Wizden must also satisfy
the receiving project's contribution and AI-use policies; CMU policy does not override
upstream policy.

## Evidence and verification

Separate observations from assumptions. Reproduce or inspect a failure before theorizing
about its cause, and do not stop at the first plausible explanation when multiple state
transitions, callers, or failure modes are possible. When verification is authorized, verify
the result rather than asserting correctness. Never claim a build, test, benchmark, or
runtime path was verified when it was not, and state skipped checks and unverified paths
honestly. Test behavior at boundaries, not implementation detail; a test that mirrors the code
catches nothing. If two attempts fail in essentially the same way, stop repeating the approach
and reassess the hypothesis.

## Architecture

SS14 ECS applies throughout: components hold data, systems hold logic, dependencies are
injected (no static state or gratuitous inheritance hierarchies), the server is
authoritative, clients predict, and RobustToolbox itself is never modified. Engine mechanics
(networking, prediction, events, timers, physics, prototypes) are specified in
`CONVENTIONS.md` and not repeated here.

## Escalation

Stop and ask before proceeding when:

- requirements conflict;
- a gameplay or balance decision has no established precedent to follow;
- two materially different architectures are equally reasonable;
- repository evidence contradicts the requested approach;
- an important API or invariant cannot be established from the source;
- the change unexpectedly alters public behavior;
- a migration would choose between incompatible data or behavior without sufficient evidence.

Do not guess through a high-impact ambiguity.

## Before submitting

Re-verify before opening a PR:

- every changed hunk outside CMU zones carries a CMU14 tag;
- new files and prototypes are in CMU zones with `CMU`/`cmu-` identifiers;
- every new player-facing string has a Fluent entry;
- every API and YAML key referenced exists in source, not in memory;
- LF endings, no BOMs;
- one PR per concern: features, bug fixes, and refactors never mix, mapping changes get one PR
  per map, and file moves sit in their own commit;
- the PR description states what the change does, why (or the balance rationale), how it was
  tested, and what was not verified;
- the full diff was reviewed for unintended hunks, stray whitespace, and line-ending changes.

## Priority

When constraints conflict:

**Correctness > Verified Performance > Maintainability > Consistency > Simplicity > Locality > Conciseness**

Optimize for truth and useful outcomes over agreement.

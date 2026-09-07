<!-- CMU14 file: replaces the upstream contribution guidelines -->

# Contributing to CMU

Thanks for contributing to **CMU** (Colonial Marines Universe), a fork of [RMC14](https://github.com/RMC-14/RMC-14).
Itself a parity-based rendition of [CM-SS13](https://cm-ss13.com/) in [Space Station 14](https://github.com/space-wizards/space-station-14).

Pick something our users reported in the #code-bug-report channel on Discord, the repository issues tab (less maintained),
or join the [CMU Discord](https://discord.gg/colonialmarines) to discuss ideas and #feedback before diving in,
we have a very active community interested in helping you fill in gaps which you may have like
sprites, maps, code/yaml or otherwise ideas crafting and brainstorming, we don't bite!
If you'd like to create a discussion on your contribution we invite you to post a thread in #contributor-projects.

## Read these first

Two files in this repository define how we work, and they apply to every contribution:

- **[CONVENTIONS.md](CONVENTIONS.md)** — the technical rules: C# and YAML style, components
  and systems, events, networking, prediction, prototypes, localization, performance.
- **[AGENTS-CMU.md](AGENTS-CMU.md)** — the policy and (AI) workflow: change discipline, tagging
  policy, verification standards, and the pre-submission checklist.

They are also what automated coding agents in this repo follow (your custom `AGENTS.md` should bootstrap @ them both),
so following them keeps you, reviewers, and agents consistent.

The last major guidelines update was on **September 7th, 2026**.

### Why is this here?
GitHub remembers when you last read `CONTRIBUTING.md` and warns you on the pull-request
form if it has changed since then. Bump this date whenever the guidelines meaningfully
change, so contributors get nudged to re-read them.

## How the fork is layered

CMU inherits code from two upstream layers, and the tree marks which layer owns what:

| Zone | Owner |
| ---- | ----- |
| `_CMU14` (or `Content.CMU/` on the Rebase branch) | CMU: new code and resources go here |
| `_AU14` / `AU14` | Legacy CMU zone: do not extend, deprecated |
| `_RMC14` / `RMC14` and everything else | Upstream (RMC14 and Space Station 14) |

- **New files and new prototypes always go in the CMU zone.** Never create files in the
  upstream or legacy zones; modifying an existing upstream file in place is acceptable when
  the change stays limited.
- Prefer extension over modification: a `partial` class in the CMU zone beats adding fields
  to an upstream file, existing events and systems beat editing upstream implementations,
  and a `[Virtual]` method override (`base.` plus CMU behavior) beats copying an upstream
  method body. Keep unavoidable upstream divergence small, explicit, and cheap to reconcile.

## The CMU14 tag

Every deliberate CMU change **outside** the CMU zones is marked with a `CMU14` tag so merges
against upstream surface our divergence instead of silently erasing it. If you change an
upstream or legacy file without a tag, the **PR will be sent back**!

Tweaks tag the changed line inline; new constructs get a kind marker on top:

```csharp
falloff = radius / strength; // CMU14: guard divide-by-zero on zero strength
```
```csharp
// CMU14 method
public void ApplyBurstDistortion(EntityUid uid, float radius)
```

- One tag per contiguous block of changed lines; separated additions get their own tag.
- **Removing code is a no-go.** Deliberate removals are commented out with the tag, never
  deleted, so an upstream downmerge that re-applies them is flagged in the diff instead of
  silently resurrecting them.
- Mechanical conformance to current upstream shape is not divergence and gets no tag.

The exact marker syntax and placement rules live in
[CONVENTIONS.md](CONVENTIONS.md), the policy behind them in [AGENTS-CMU.md](AGENTS-CMU.md).

## Naming

New CMU prototype IDs are PascalCase with a `CMU` prefix (`CMUColonyBudget`); new
localization IDs are kebab-case with a `cmu-` prefix (`cmu-colony-budget-insufficient`).
Do not retrofit prefixes onto inherited Wizden or RMC identifiers.

## House style essentials

The short list that catches most first PRs. Full detail in [CONVENTIONS.md](CONVENTIONS.md):

- **C#**: expression-bodied members with `=>` on their own line; boolean chains wrapped with
  leading `&&`/`||`; no braces around single-statement `if`/`else`; `sealed` by default;
  data definitions `partial` with a parameterless constructor; bare `[DataField]` unless the
  YAML key differs from the field name.
- **YAML**: list items at the key's indent (`components:` and `- type:` flush); prototype key
  order `type > abstract > parent > id > name > suffix > description > components`; one blank
  line between prototypes, none inside a `components:` list.
- **Localization**: every player-facing string goes through Fluent under
  `Resources/Locale/en-US/`. Downstreams translate everything, and hardcoded text
  causes issues. Reuse an existing file when entries fit; a line may not start with `[`.
- **LF line endings, no BOMs.**
- **Changelogs**: a bot generates them from the `:cl:` block in your PR description. Never
  edit `Resources/Changelog/` yourself; write the PR entry as player-facing (non-technical) text.

## Building and testing locally

- Build: `dotnet build` at the repository root.
- Run the server: `runserver.sh` (or `runserver.bat`), the client: `runclient.sh` /
  `runclient.bat`.
- Validate YAML against the component definitions instead of trusting remembered field names:
  `dotnet run --project Content.YAMLLinter`.

## Pull requests

- **One PR per concern.** Features, bug fixes, and refactors never mix; mapping changes get
  one PR per map; file moves sit in their own commit.
- Fill in the PR template: what the change does, why (or the balance rationale), the
  technical summary, a test plan, and media for anything visible in game.
- State honestly how the PR was tested and what was not verified.

Before submitting, re-check the diff:

- every changed hunk outside the CMU zones carries a CMU14 tag, removals commented out;
- new files and prototypes are in CMU zones with `CMU`/`cmu-` identifiers;
- every new player-facing string has a Fluent entry;
- every API and YAML key referenced exists in the source, not in memory;
- LF endings, no BOMs, no stray whitespace or unintended hunks.

Balance and gameplay decisions without an established precedent should be discussed on
Discord or in an issue first. Don't guess through a high-impact ambiguity.

## AI-assisted contributions

AI assistance is welcome, but you own what you submit. You must understand the change and be
able to explain its behavior; PRs built on plausible-looking APIs or patterns that don't
exist in this codebase will be rejected. Changes intended to continue upstream through RMC14
or toward Wizden must also satisfy those projects' contribution and AI-use (non-use) policies.

## License

By submitting code you confirm you own it or may license it to us; see the
[README](README.md) for the license breakdown (MIT for code predating May 2026, AGPL-3.0 for
code after, CC-BY-SA-3.0 for most assets).

# Clacky foundation provenance

PointPilot's Python/PyQt foundation was imported from the following source
without its Git history:

| Item | Value |
|---|---|
| PointPilot archive source | `main` at `631b64ebd4fe557593263b8e92d24396368c6a6c` |
| Archive tag | `pointpilot-wpf-archive` (annotated; peeled SHA `631b64ebd4fe557593263b8e92d24396368c6a6c`) |
| Clacky repository | https://github.com/Raynan00/clacky |
| Imported Clacky commit | `e239089a4eb9daf7ac62d0f5c38e92fa53648499` |
| Imported release | `v0.2.0` |
| Import method | `git archive` of the pinned commit into the orphan `rewrite/clacky-core` branch |

The initial root commit, `chore: import pinned Clacky foundation`, is an
unaltered snapshot of that exact source tree. It deliberately does not include
Clacky's Git history.

The imported foundation is MIT licensed by Raynan Wuyep. Its vendored shell
also carries the MIT notice for Shashank Singh / Clicky for Windows. PointPilot
retains both notices in `THIRD_PARTY_NOTICES.md`. Subsequent PointPilot-authored
code is identified by its commits and is not presented as Clacky-authored code.


# Stop Jaywalking

Makes jaywalking expensive so pedestrians funnel to marked zebra crossings instead of cutting across roads anywhere, in **Cities: Skylines II**.

**Paradox Mods:** https://mods.paradoxplaza.com/mods/150664

## What it does
The game already treats an unmarked crossing as "unsafe" (a mild pathfind penalty vs a real crosswalk), but it's a nudge, not a deterrent — so cims still jaywalk. This mod multiplies that unsafe-crossing cost by a factor you choose, turning the nudge into a real deterrent. Marked zebra crossings are left untouched and stay cheap, so pedestrians funnel to them.

**Safe by design:** the pedestrian pathfinder never fails on cost, so a cim with no reachable marked crossing still crosses (just very reluctantly) — nobody is ever stranded at a curb.

## Options (Options → Mods → Stop Jaywalking)
- Enable / disable
- Jaywalk cost multiplier (1–30)
- Re-apply interval (in-game hours) — the game can reset the cost after big road edits, so the mod periodically re-asserts it

## Under the hood (for the curious / security-minded)
- **Pure ECS — no Harmony patches.** It multiplies `PathfindPedestrianData.m_UnsafeCrosswalkCost` on the pedestrian pathfind prefab (caching the original so it never compounds), and restores vanilla when disabled. It never adds or removes crossing lanes.
- **No network access at all** — nothing leaves your machine.
- **Filesystem:** writes only its own settings file and a log (`StopJaywalking.Mod.log`). Nothing else.
- **Dependencies:** none beyond the base game.

Full source is here; the compiled DLL decompiles cleanly if you'd like to verify it matches.

## Build from source
Requires the official CS2 modding toolchain. `dotnet build -c Release` compiles and deploys to your local Mods folder.

## License
[MIT](LICENSE).

---

*Made with [Claude Code](https://claude.com/claude-code), Anthropic's agentic coding tool.*

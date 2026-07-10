using System.Collections.Generic;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ZebraCrossings
{
    // Funnels pedestrians to marked zebra crossings by making jaywalking expensive.
    //
    // MECHANISM (verified vs live source): the game already tags every crossing at a node WITHOUT a marked-crosswalk
    // composition as PedestrianLaneFlags.Unsafe (LaneSystem.CreateNodePedestrianLane ~8604-8610). The pathfinder
    // charges m_UnsafeCrosswalkCost (~100) for those vs m_CrosswalkCost (~5) for a zebra — a deterrent, not a wall,
    // so cims still take shortcuts. We multiply PathfindPedestrianData.m_UnsafeCrosswalkCost (on the pedestrian
    // PathfindPrefab entity) by a user factor, leaving m_CrosswalkCost untouched: jaywalking gets expensive, zebras
    // stay cheap. SAFE — the pedestrian pathfinder never fails on cost, so a cim with no reachable marked crossing
    // still crosses (very reluctantly): no stranding.
    //
    // IDEMPOTENT: cache each prefab's ORIGINAL unsafe cost the first time we see it (prefab data reloads to vanilla
    // each session, so first sight == vanilla) and always set target = original * multiplier — re-asserting never
    // compounds. Disabled or multiplier 1 => restore vanilla. Re-asserted on the user interval because a road regen /
    // prefab reload can restore the vanilla cost.
    public partial class ZebraCrossingSystem : GameSystemBase
    {
        // 1 in-game day = 262144 sim frames = 24 in-game hours.
        private const int kFramesPerGameHour = 262144 / 24; // ~10922

        private SimulationSystem m_Sim;
        private EntityQuery m_CostQuery;   // pedestrian PathfindPrefab entity(ies) holding PathfindPedestrianData
        private EntityQuery m_LaneQuery;   // all pedestrian lanes, for the census
        private readonly Dictionary<Entity, float4> m_OrigUnsafe = new Dictionary<Entity, float4>();
        private uint m_LastReassert;
        private uint m_LastLog;
        private float m_AppliedMult = -1f; // last multiplier we wrote (so a setting change re-applies immediately)

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Sim = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_CostQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<PathfindPedestrianData>() },
                Options = EntityQueryOptions.IncludePrefab, // the cost data lives on a prefab entity
            });
            m_LaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Game.Net.PedestrianLane>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Game.Tools.Temp>() },
            });
        }

        // Coarse cadence; the actual re-assert + census are gated on their own frame counters below.
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 2048;

        protected override void OnUpdate()
        {
            Setting s = Mod.ActiveSetting;
            if (s == null)
                return;

            uint frame = m_Sim.frameIndex;
            float mult = s.Enabled ? math.max(1f, s.CostMultiplier) : 1f; // disabled => restore vanilla (x1)
            int intervalFrames = math.max(1, s.ReassertIntervalHours) * kFramesPerGameHour;

            // Re-assert on the user interval, or immediately when the multiplier / enabled toggle changed.
            if (mult != m_AppliedMult || frame - m_LastReassert >= (uint)intervalFrames)
            {
                ApplyUnsafeCost(mult);
                m_AppliedMult = mult;
                m_LastReassert = frame;
            }

            // Census heartbeat — marked zebra vs jaywalk crossings, and whether our cost is applied.
            if (frame - m_LastLog >= 16384)
            {
                m_LastLog = frame;
                LogCensus(s, mult);
            }
        }

        // Set every pedestrian-pathfind prefab's unsafe-crosswalk cost to original * mult (mult == 1 restores vanilla).
        private void ApplyUnsafeCost(float mult)
        {
            NativeArray<Entity> ents = m_CostQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < ents.Length; i++)
            {
                Entity e = ents[i];
                PathfindPedestrianData d = EntityManager.GetComponentData<PathfindPedestrianData>(e);
                if (!m_OrigUnsafe.TryGetValue(e, out float4 orig))
                {
                    orig = d.m_UnsafeCrosswalkCost.m_Value; // first sight this session == vanilla
                    m_OrigUnsafe[e] = orig;
                }
                float4 target = orig * mult;
                if (!d.m_UnsafeCrosswalkCost.m_Value.Equals(target))
                {
                    d.m_UnsafeCrosswalkCost.m_Value = target;
                    EntityManager.SetComponentData(e, d);
                }
            }
            ents.Dispose();
        }

        private void LogCensus(Setting s, float mult)
        {
            NativeArray<Game.Net.PedestrianLane> lanes = m_LaneQuery.ToComponentDataArray<Game.Net.PedestrianLane>(Allocator.Temp);
            int zebra = 0, jaywalk = 0, nonCrossing = 0;
            for (int i = 0; i < lanes.Length; i++)
            {
                PedestrianLaneFlags f = lanes[i].m_Flags;
                if ((f & PedestrianLaneFlags.Crosswalk) == 0) { nonCrossing++; continue; }
                if ((f & PedestrianLaneFlags.Unsafe) != 0) jaywalk++;
                else zebra++;
            }
            lanes.Dispose();
            Mod.log.Info($"[SelfTest] zebraCrossings: enabled={s.Enabled} multiplier={mult}x intervalHrs={s.ReassertIntervalHours} costPrefabs={m_OrigUnsafe.Count} markedZebras={zebra} jaywalkCrossings={jaywalk} sidewalks+paths={nonCrossing}");
        }
    }
}

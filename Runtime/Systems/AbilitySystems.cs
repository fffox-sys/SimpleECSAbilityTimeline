#if HAS_ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace SECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AbilityStateMachineSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AbilityDBSingleton>();
        }

        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var db = SystemAPI.GetSingleton<AbilityDBSingleton>();

            var ensure = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (loadout, e) in SystemAPI.Query<DynamicBuffer<AbilityLoadout>>().WithEntityAccess().WithNone<AbilityCooldownEntry>())
                ensure.AddBuffer<AbilityCooldownEntry>(e);
            foreach (var (loadout, e) in SystemAPI.Query<DynamicBuffer<AbilityLoadout>>().WithEntityAccess().WithNone<AbilityCastRequest>())
                ensure.AddBuffer<AbilityCastRequest>(e);
            foreach (var (loadout, e) in SystemAPI.Query<DynamicBuffer<AbilityLoadout>>().WithEntityAccess().WithNone<AbilityCueEvent>())
                ensure.AddBuffer<AbilityCueEvent>(e);

            foreach (var (buf, ent) in SystemAPI.Query<DynamicBuffer<AbilityCooldownEntry>>().WithEntityAccess())
            {
                var b = SystemAPI.GetBuffer<AbilityCooldownEntry>(ent);
                for (int i = 0; i < b.Length; i++)
                {
                    var entry = b[i];
                    entry.Remaining = math.max(0, entry.Remaining - dt);
                    b[i] = entry;
                }
            }

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (reqBuf, loadout, e) in SystemAPI.Query<DynamicBuffer<AbilityCastRequest>, DynamicBuffer<AbilityLoadout>>().WithEntityAccess().WithNone<ActiveAbility>())
            {
                if (reqBuf.Length == 0) continue;
                var req = reqBuf[0];
                int abilityId = req.AbilityIdHash;
                if (abilityId == 0 && req.Slot >= 0)
                {
                    for (int i = 0; i < loadout.Length; i++)
                        if (loadout[i].Slot == req.Slot) { abilityId = loadout[i].AbilityIdHash; break; }
                }
                if (abilityId == 0) { reqBuf.Clear(); continue; }

                var cdBuf = SystemAPI.GetBuffer<AbilityCooldownEntry>(e);
                bool onCd = false;
                for (int i = 0; i < cdBuf.Length; i++)
                    if (cdBuf[i].AbilityIdHash == abilityId && cdBuf[i].Remaining > 0) { onCd = true; break; }
                if (onCd) { reqBuf.Clear(); continue; }

                ref var defs = ref db.Blob.Value.Abilities;
                int defIndex = -1;
                for (int i = 0; i < defs.Length; i++) if (defs[i].AbilityIdHash == abilityId) { defIndex = i; break; }
                if (defIndex < 0) { reqBuf.Clear(); continue; }

                ecb.AddComponent(e, new ActiveAbility { AbilityIdHash = abilityId, PhaseIndex = 0, TimeInPhase = 0, Elapsed = 0 });
                reqBuf.Clear();
            }

            foreach (var (active, cueBuf, e) in SystemAPI.Query<RefRW<ActiveAbility>, DynamicBuffer<AbilityCueEvent>>().WithEntityAccess())
            {
                var a = active.ValueRO;
                ref var defs = ref db.Blob.Value.Abilities;
                int defIndex = -1;
                for (int i = 0; i < defs.Length; i++) if (defs[i].AbilityIdHash == a.AbilityIdHash) { defIndex = i; break; }
                if (defIndex < 0)
                {
                    ecb.RemoveComponent<ActiveAbility>(e);
                    continue;
                }
                ref var def = ref defs[defIndex];

                float prev = a.TimeInPhase;
                a.TimeInPhase += dt;
                a.Elapsed += dt;

                if (a.PhaseIndex < 0 || a.PhaseIndex >= def.Phases.Length)
                {
                    WriteCooldown(ref ecb, e, def.AbilityIdHash, def.Cooldown);
                    ecb.RemoveComponent<ActiveAbility>(e);
                    active.ValueRW = a;
                    continue;
                }

                ref var phase = ref def.Phases[a.PhaseIndex];
                for (int t = 0; t < phase.Tracks.Length; t++)
                {
                    ref var tr = ref phase.Tracks[t];
                    for (int k = 0; k < tr.Keys.Length; k++)
                    {
                        var key = tr.Keys[k];
                        if (key.Time > prev && key.Time <= a.TimeInPhase)
                        {
                            cueBuf.Add(new AbilityCueEvent
                            {
                                AbilityIdHash = a.AbilityIdHash,
                                Type = key.Type,
                                Time = a.Elapsed,
                            });
                        }
                    }
                }

                if (a.TimeInPhase >= phase.Duration)
                {
                    a.PhaseIndex++;
                    a.TimeInPhase = 0;
                    if (a.PhaseIndex >= def.Phases.Length)
                    {
                        WriteCooldown(ref ecb, e, def.AbilityIdHash, def.Cooldown);
                        ecb.RemoveComponent<ActiveAbility>(e);
                    }
                }

                active.ValueRW = a;
            }
        }

        private static void WriteCooldown(ref EntityCommandBuffer ecb, Entity e, int abilityIdHash, float cooldown)
        {
            if (cooldown <= 0) return;
            ecb.AppendToBuffer(e, new AbilityCooldownEntry { AbilityIdHash = abilityIdHash, Remaining = cooldown });
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityStateMachineSystem))]
    public partial struct AbilityCueDispatchSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var em = state.EntityManager;

            foreach (var (cueBuf, e) in SystemAPI.Query<DynamicBuffer<AbilityCueEvent>>().WithEntityAccess())
            {
                if (cueBuf.Length == 0) continue;

                EnsureRequestBuffers(ref ecb, e, em);

                for (int i = 0; i < cueBuf.Length; i++)
                {
                    var cue = cueBuf[i];
                    DispatchCue(ref ecb, e, cue, em);
                }
                cueBuf.Clear();
            }
        }

        private static void EnsureRequestBuffers(ref EntityCommandBuffer ecb, Entity e, EntityManager em)
        {
            if (!em.HasBuffer<HitboxActivateRequest>(e))
                ecb.AddBuffer<HitboxActivateRequest>(e);
            if (!em.HasBuffer<VFXSpawnRequest>(e))
                ecb.AddBuffer<VFXSpawnRequest>(e);
            if (!em.HasBuffer<SFXPlayRequest>(e))
                ecb.AddBuffer<SFXPlayRequest>(e);
            if (!em.HasBuffer<AnimationParamRequest>(e))
                ecb.AddBuffer<AnimationParamRequest>(e);
            if (!em.HasBuffer<CameraImpulseRequest>(e))
                ecb.AddBuffer<CameraImpulseRequest>(e);
            if (!em.HasBuffer<TimeScaleRequest>(e))
                ecb.AddBuffer<TimeScaleRequest>(e);
            if (!em.HasBuffer<ScriptHookRequest>(e))
                ecb.AddBuffer<ScriptHookRequest>(e);
            if (!em.HasBuffer<ClipActivateRequest>(e))
                ecb.AddBuffer<ClipActivateRequest>(e);
        }

        private static void DispatchCue(ref EntityCommandBuffer ecb, Entity source, AbilityCueEvent cue, EntityManager em)
        {
            float3 position = float3.zero;
            if (em.HasComponent<Position>(source))
                position = em.GetComponentData<Position>(source).Value;

            quaternion rotation = quaternion.identity;
            if (em.HasComponent<Heading>(source))
            {
                var heading = em.GetComponentData<Heading>(source).Value;
                if (math.lengthsq(heading) > 1e-6f)
                    rotation = quaternion.LookRotationSafe(heading, new float3(0, 1, 0));
            }

            switch (cue.Type)
            {
                case AbilityKeyType.Hitbox:
                    ecb.AppendToBuffer(source, new HitboxActivateRequest 
                    { 
                        HitboxIdHash = cue.Param0, 
                        ParamF = cue.ParamF, 
                        TargetSlot = cue.TargetSlot 
                    });
                    break;

                case AbilityKeyType.VFX:
                    ecb.AppendToBuffer(source, new VFXSpawnRequest
                    {
                        VFXIdHash = cue.Param0,
                        Position = position,
                        Rotation = rotation,
                        Scale = cue.ParamF.x > 0 ? cue.ParamF.x : 1.0f,
                        Flags = 0,
                        AttachTarget = source
                    });
                    break;

                case AbilityKeyType.SFX:
                    ecb.AppendToBuffer(source, new SFXPlayRequest
                    {
                        SFXIdHash = cue.Param0,
                        Position = position,
                        Volume = cue.ParamF.x > 0 ? cue.ParamF.x : 1.0f,
                        Pitch = 1.0f,
                        Flags = 0,
                        AttachTarget = source
                    });
                    break;

                case AbilityKeyType.AnimationEvent:
                    ecb.AppendToBuffer(source, new AnimationParamRequest
                    {
                        ParamHash = cue.Param0,
                        Type = (byte)AnimParamType.Trigger,
                        FloatValue = cue.ParamF.x,
                        BoolValue = 1,
                        TriggerValue = 1
                    });
                    break;

                case AbilityKeyType.CameraShake:
                    ecb.AppendToBuffer(source, new CameraImpulseRequest
                    {
                        Direction = new float3(0, 0, 1),
                        Magnitude = cue.ParamF.x > 0 ? cue.ParamF.x : 1.0f,
                        Duration = 0.2f,
                        ProfileId = (byte)cue.Param0
                    });
                    break;

                case AbilityKeyType.Signal:
                    ecb.AppendToBuffer(source, new ScriptHookRequest
                    {
                        HookIdHash = cue.Param0,
                        Param0 = cue.Param0,
                        ParamF = cue.ParamF.x,
                        ContextEntity = source
                    });
                    break;

                case AbilityKeyType.Footstep:
                    ecb.AppendToBuffer(source, new SFXPlayRequest
                    {
                        SFXIdHash = cue.Param0 != 0 ? cue.Param0 : "Footstep".GetHashCode(),
                        Position = position,
                        Volume = 0.6f,
                        Pitch = 1.0f + (cue.ParamF.y * 0.2f - 0.1f),
                        Flags = 0,
                        AttachTarget = Entity.Null
                    });
                    break;

                case AbilityKeyType.Custom:
                    ecb.AppendToBuffer(source, new ScriptHookRequest
                    {
                        HookIdHash = cue.Param0,
                        Param0 = cue.Param0,
                        ParamF = cue.ParamF.x,
                        ContextEntity = source
                    });
                    break;
            }
        }
    }
}
#endif

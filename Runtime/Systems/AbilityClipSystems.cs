#if HAS_ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SECS
{
    /// <summary>
    /// Clip激活系统 - 处理ClipActivateRequest，创建Clip临时实体
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct ClipActivationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (reqBuf, ownerEntity) in SystemAPI.Query<DynamicBuffer<ClipActivateRequest>>()
                         .WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                    var clipEntity = ecb.CreateEntity();
                    ecb.AddComponent(clipEntity, new ActiveClip
                    {
                        ClipIdHash = req.ClipIdHash,
                        ClipType = req.ClipType,
                        OwnerEntity = req.OwnerEntity,
                        Elapsed = 0f,
                        Duration = req.Duration,
                        Flags = 0
                    });

                    ecb.AddComponent(clipEntity, new Position { Value = req.StartPosition });

                    if (math.lengthsq(req.EndPosition - req.StartPosition) > 1e-6f)
                    {
                    }

                    ecb.AddBuffer<ClipCueEvent>(clipEntity);

                    switch (req.ClipType)
                    {
                        case AbilityClipType.Animation:
                            break;

                        case AbilityClipType.VFX:
                            break;

                        case AbilityClipType.SFX:
                            break;

                        case AbilityClipType.Custom:
                            break;
                    }
                }

                reqBuf.Clear();
            }
        }
    }

    /// <summary>
    /// Clip状态机系统 - 推进ActiveClip的时间，触发Clip内的Key
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClipActivationSystem))]
    public partial struct ClipStateMachineSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (clipRW, cueB, clipEntity) in SystemAPI
                         .Query<RefRW<ActiveClip>, DynamicBuffer<ClipCueEvent>>()
                         .WithEntityAccess())
            {
                var clip = clipRW.ValueRO;
                if ((clip.Flags & 1) != 0)
                    continue;

                float prevElapsed = clip.Elapsed;
                clip.Elapsed += dt;


                if (clip.Elapsed >= clip.Duration)
                {
                    clip.Flags |= 2;
                    ecb.AddComponent<ClipPendingDestroyTag>(clipEntity);
                }

                clipRW.ValueRW = clip;
            }
        }
    }

    /// <summary>
    /// Clip Cue分发系统 - 处理Clip内的Key触发事件
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClipStateMachineSystem))]
    public partial struct ClipCueDispatchSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var em = state.EntityManager;

            foreach (var (clip, cueB, clipEntity) in SystemAPI
                         .Query<RefRO<ActiveClip>, DynamicBuffer<ClipCueEvent>>()
                         .WithEntityAccess())
            {
                if (cueB.Length == 0) continue;

                var ownerEntity = clip.ValueRO.OwnerEntity;
                if (!em.Exists(ownerEntity)) continue;

                EnsureRequestBuffers(ref ecb, ownerEntity, em);

                for (int i = 0; i < cueB.Length; i++)
                {
                    var cue = cueB[i];
                    var abilityCue = new AbilityCueEvent
                    {
                        AbilityIdHash = clip.ValueRO.ClipIdHash,
                        Type = cue.Type,
                        Time = cue.Time,
                        Param0 = cue.Param0,
                        ParamF = new float4(cue.ParamF, 0, 0, 0),
                        TargetSlot = 0
                    };

                    DispatchClipCue(ref ecb, ownerEntity, abilityCue, em);
                }

                cueB.Clear();
            }
        }

        private static void EnsureRequestBuffers(ref EntityCommandBuffer ecb, Entity e, EntityManager em)
        {
            if (!em.HasBuffer<VFXSpawnRequest>(e))
                ecb.AddBuffer<VFXSpawnRequest>(e);
            if (!em.HasBuffer<SFXPlayRequest>(e))
                ecb.AddBuffer<SFXPlayRequest>(e);
            if (!em.HasBuffer<AnimationParamRequest>(e))
                ecb.AddBuffer<AnimationParamRequest>(e);
            if (!em.HasBuffer<CameraImpulseRequest>(e))
                ecb.AddBuffer<CameraImpulseRequest>(e);
        }

        private static void DispatchClipCue(ref EntityCommandBuffer ecb, Entity source, AbilityCueEvent cue, EntityManager em)
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
            }
        }
    }

    /// <summary>
    /// Clip清理系统 - 销毁完成的Clip实体
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClipCueDispatchSystem))]
    public partial struct ClipCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (tag, clipEntity) in SystemAPI.Query<ClipPendingDestroyTag>()
                         .WithEntityAccess())
            {
                ecb.DestroyEntity(clipEntity);
            }
        }
    }
}
#endif

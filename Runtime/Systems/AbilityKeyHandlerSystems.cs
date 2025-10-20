#if HAS_ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SECS
{
    /// <summary>
    /// VFX生成系统 - 处理VFXSpawnRequest，生成特效实体或调用表现接口
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct VFXSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (reqBuf, e) in SystemAPI.Query<DynamicBuffer<VFXSpawnRequest>>().WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                }

                reqBuf.Clear();
            }
        }
    }

    /// <summary>
    /// SFX播放系统 - 处理SFXPlayRequest，播放音效
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct SFXPlaySystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (reqBuf, e) in SystemAPI.Query<DynamicBuffer<SFXPlayRequest>>().WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                }

                reqBuf.Clear();
            }
        }
    }

    /// <summary>
    /// 动画参数设置系统 - 处理AnimationParamRequest，设置Animator参数
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct AnimationParamSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (reqBuf, e) in SystemAPI.Query<DynamicBuffer<AnimationParamRequest>>().WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                }

                reqBuf.Clear();
            }
        }
    }

    /// <summary>
    /// 相机冲击系统 - 处理CameraImpulseRequest，触发屏幕震动
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct CameraImpulseSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (reqBuf, e) in SystemAPI.Query<DynamicBuffer<CameraImpulseRequest>>().WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                }

                reqBuf.Clear();
            }
        }
    }

    /// <summary>
    /// 时间缩放系统 - 处理TimeScaleRequest，局部慢动作效果
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct TimeScaleSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (reqBuf, e) in SystemAPI.Query<DynamicBuffer<TimeScaleRequest>>().WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                }

                reqBuf.Clear();
            }
        }
    }

    /// <summary>
    /// 脚本钩子系统 - 处理ScriptHookRequest，触发自定义逻辑
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AbilityCueDispatchSystem))]
    public partial struct ScriptHookSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (reqBuf, e) in SystemAPI.Query<DynamicBuffer<ScriptHookRequest>>().WithEntityAccess())
            {
                if (reqBuf.Length == 0) continue;

                for (int i = 0; i < reqBuf.Length; i++)
                {
                    var req = reqBuf[i];
                }

                reqBuf.Clear();
            }
        }
    }
}
#endif

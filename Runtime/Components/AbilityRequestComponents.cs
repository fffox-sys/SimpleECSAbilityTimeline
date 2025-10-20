#if HAS_ENTITIES
using Unity.Entities;
using Unity.Mathematics;

namespace SECS
{
    /// <summary>
    /// 各种Ability Key/Clip触发请求组件
    /// 这些Request由AbilityCueDispatchSystem生成，由各自的Handler System处理
    /// </summary>


    /// <summary>
    /// VFX生成请求 - 在指定位置生成特�?
    /// </summary>
    public struct VFXSpawnRequest : IBufferElementData
    {
        public int VFXIdHash;
        public float3 Position;
        public quaternion Rotation;
        public float Scale;
        public byte Flags;
        public Entity AttachTarget;
    }

    /// <summary>
    /// SFX播放请求 - 播放音效
    /// </summary>
    public struct SFXPlayRequest : IBufferElementData
    {
        public int SFXIdHash;
        public float3 Position;
        public float Volume;
        public float Pitch;
        public byte Flags;
        public Entity AttachTarget;
    }


    /// <summary>
    /// 动画参数设置请求 - 设置Animator参数
    /// </summary>
    public struct AnimationParamRequest : IBufferElementData
    {
        public int ParamHash;
        public byte Type;
        public float FloatValue;
        public byte BoolValue;
        public byte TriggerValue;
    }

    /// <summary>
    /// 相机冲击请求 - 屏幕震动/冲击
    /// </summary>
    public struct CameraImpulseRequest : IBufferElementData
    {
        public float3 Direction;
        public float Magnitude;
        public float Duration;
        public byte ProfileId;
    }

    /// <summary>
    /// 时间缩放请求 - 局部慢动作/子弹时间
    /// </summary>
    public struct TimeScaleRequest : IBufferElementData
    {
        public float TimeScale;
        public float Duration;
        public byte Scope;
        public Entity TargetEntity;
        public float3 AreaCenter;
        public float AreaRadius;
    }

    public enum TimeScaleScope : byte
    {
        Global = 0,
        Entity = 1,
        Area = 2
    }

    /// <summary>
    /// 脚本钩子请求 - 触发自定义逻辑
    /// </summary>
    public struct ScriptHookRequest : IBufferElementData
    {
        public int HookIdHash;
        public int Param0;
        public float ParamF;
        public Entity ContextEntity;
    }


    /// <summary>
    /// Clip激活请�?- 启动一个持续性Clip
    /// </summary>
    public struct ClipActivateRequest : IBufferElementData
    {
        public int ClipIdHash;
        public AbilityClipType ClipType;
        public float Duration;
        public Entity OwnerEntity;
        public float3 StartPosition;
        public float3 EndPosition;
    }

    /// <summary>
    /// 当前激活的Clip状�?- 挂在Clip临时实体�?
    /// </summary>
    public struct ActiveClip : IComponentData
    {
        public int ClipIdHash;
        public AbilityClipType ClipType;
        public Entity OwnerEntity;
        public float Elapsed;
        public float Duration;
        public byte Flags;
    }



    /// <summary>
    /// Clip内部的Key触发事件 - 在Clip播放过程中触发
    /// </summary>
    public struct ClipCueEvent : IBufferElementData
    {
        public int ClipIdHash;
        public AbilityKeyType Type;
        public float Time;
        public int Param0;
        public float ParamF;
        public Entity TargetEntity;
    }


    /// <summary>
    /// 标记：此实体需要处理Ability请求
    /// </summary>
    public struct AbilityRequestProcessorTag : IComponentData {}

    /// <summary>
    /// 标记：此Clip实体需要清除
    /// </summary>
    public struct ClipPendingDestroyTag : IComponentData {}
}
#endif

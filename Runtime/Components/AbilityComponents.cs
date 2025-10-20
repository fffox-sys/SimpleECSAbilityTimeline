#if HAS_ENTITIES
using Unity.Entities;
using Unity.Mathematics;

namespace SECS
{

    public struct AbilityLoadout : IBufferElementData
    {
        public int Slot;
        public int AbilityIdHash;
    }

    public struct AbilityCastRequest : IBufferElementData
    {
        public int Slot;
        public int AbilityIdHash;
    }

    public struct AbilityCooldownEntry : IBufferElementData
    {
        public int AbilityIdHash;
        public float Remaining;
    }

    public struct ActiveAbility : IComponentData
    {
        public int AbilityIdHash;
        public int PhaseIndex;
        public float TimeInPhase;
        public float Elapsed;
    }

    public struct AbilityCueEvent : IBufferElementData
    {
        public int AbilityIdHash;
        public AbilityKeyType Type;
        public float Time;
        public int Param0;
        public float4 ParamF;
        public int TargetSlot;
    }

    public struct HitboxActivateRequest : IBufferElementData
    {
        public int HitboxIdHash;
        public float4 ParamF;
        public int TargetSlot;
    }
}
#endif

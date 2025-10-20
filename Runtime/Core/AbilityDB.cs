#if HAS_ENTITIES
using Unity.Entities;
using Unity.Mathematics;

namespace SECS
{
    public enum AbilityKeyType : byte
    {
        Hitbox = 0,
        Signal = 1,
        AnimationEvent = 2,
        CameraShake = 3,
        Footstep = 4,
        VFX = 5,
        SFX = 6,
        Custom = 7
    }

    public enum AbilityTrackType : byte
    {
        Hitbox = 0,
        Animation = 1,
        VFX = 2,
        SFX = 3,
        Camera = 4,
        Script = 5,
        Custom = 6
    }

    public enum HitboxShapeType : byte
    {
        Sphere = 0,
        Cone = 1,
        Capsule = 2
    }

    public struct HitboxKeyData
    {
        public HitboxShapeType Shape;
        public float Radius;
        public float Angle;
        public float Height;
        public float Damage;
        public float ActiveDuration;
        public byte TeamMask;
        public float3 Offset;
        public bool UseHeading;
    }

    public struct VFXKeyData
    {
        public int PrefabGuidHash;
        public float3 Offset;
        public float3 Rotation;
        public float Scale;
        public bool AttachToTarget;
    }

    public struct SFXKeyData
    {
        public int ClipGuidHash;
        public float Volume;
        public float Pitch;
        public bool Loop;
    }

    public struct AbilityKeyDef
    {
        public AbilityKeyType Type;
        public float Time;
        public int EventNameHash;
        public int StringParamHash;
        public float4 ParamF;
        public int IntParam;
        public HitboxKeyData HitboxData;
        public VFXKeyData VFXData;
        public SFXKeyData SFXData;
    }

    public enum AbilityClipType : byte
    {
        Animation = 0,
        SFX = 1,
        VFX = 2,
        Custom = 3
    }

    public struct AbilityClipDef
    {
        public AbilityClipType Type;
        public float Start;
        public float Duration;
        public int AnimationClipGuidHash;
        public int AudioClipGuidHash;
        public int VFXPrefabGuidHash;
        public float4 ParamF;
    }

    public struct AbilityTrackDef
    {
        public AbilityTrackType Type;
        public BlobArray<AbilityKeyDef> Keys;
        public BlobArray<AbilityClipDef> Clips;
    }

    public struct AbilityPhaseDef
    {
        public float Duration;
        public BlobArray<AbilityTrackDef> Tracks;
    }

    public struct AbilityDef
    {
        public int AbilityIdHash;
        public float Cooldown;
        public BlobArray<AbilityPhaseDef> Phases;
    }

    public struct AbilityDBBlob
    {
        public BlobArray<AbilityDef> Abilities;
    }

    public struct AbilityDBSingleton : IComponentData
    {
        public BlobAssetReference<AbilityDBBlob> Blob;
    }
}
#endif

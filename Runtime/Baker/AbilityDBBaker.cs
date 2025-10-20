#if HAS_ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using SECS;
using SECS.Configs;

namespace SECS.Bakers
{
    public class AbilityDBAuthoring : MonoBehaviour
    {
        public AbilityConfigSO Config;
    }

    public class AbilityDBBaker : Baker<AbilityDBAuthoring>
    {
        public override void Bake(AbilityDBAuthoring authoring)
        {
            if (authoring.Config == null)
                return;

            DependsOn(authoring.Config);

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SECS.AbilityDBBlob>();

            var abilities = authoring.Config.Abilities ?? new System.Collections.Generic.List<AbilityConfigSO.AbilityDef>();
            var abilityArray = builder.Allocate(ref root.Abilities, abilities.Count);

            for (int i = 0; i < abilities.Count; i++)
            {
                var srcAbility = abilities[i];
                ref var dstAbility = ref abilityArray[i];
                dstAbility.AbilityIdHash = string.IsNullOrEmpty(srcAbility.AbilityId) ? 0 : srcAbility.AbilityId.GetHashCode();
                dstAbility.Cooldown = Mathf.Max(0f, srcAbility.Cooldown);

                var phases = srcAbility.Phases ?? new System.Collections.Generic.List<AbilityConfigSO.Phase>();
                var phaseArray = builder.Allocate(ref dstAbility.Phases, phases.Count);
                for (int p = 0; p < phases.Count; p++)
                {
                    var srcPhase = phases[p];
                    ref var dstPhase = ref phaseArray[p];
                    dstPhase.Duration = Mathf.Max(0f, srcPhase.Duration);

                    var tracks = srcPhase.Tracks ?? new System.Collections.Generic.List<AbilityConfigSO.Track>();
                    var trackArray = builder.Allocate(ref dstPhase.Tracks, tracks.Count);
                    for (int t = 0; t < tracks.Count; t++)
                    {
                        var srcTrack = tracks[t];
                        ref var dstTrack = ref trackArray[t];
                        dstTrack.Type = srcTrack.Type switch
                        {
                            AbilityConfigSO.TrackType.Hitbox => AbilityTrackType.Hitbox,
                            AbilityConfigSO.TrackType.Animation => AbilityTrackType.Animation,
                            AbilityConfigSO.TrackType.VFX => AbilityTrackType.VFX,
                            AbilityConfigSO.TrackType.SFX => AbilityTrackType.SFX,
                            AbilityConfigSO.TrackType.Camera => AbilityTrackType.Camera,
                            AbilityConfigSO.TrackType.Script => AbilityTrackType.Script,
                            AbilityConfigSO.TrackType.Custom => AbilityTrackType.Custom,
                            _ => AbilityTrackType.Animation
                        };

                        var keys = srcTrack.Keys ?? new System.Collections.Generic.List<AbilityConfigSO.Key>();
                        var keyArray = builder.Allocate(ref dstTrack.Keys, keys.Count);
                        for (int k = 0; k < keys.Count; k++)
                        {
                            var srcKey = keys[k];
                            ref var dstKey = ref keyArray[k];
                            dstKey.Type = srcKey.Type switch
                            {
                                AbilityConfigSO.KeyType.Hitbox => AbilityKeyType.Hitbox,
                                AbilityConfigSO.KeyType.Signal => AbilityKeyType.Signal,
                                AbilityConfigSO.KeyType.AnimationEvent => AbilityKeyType.AnimationEvent,
                                AbilityConfigSO.KeyType.CameraShake => AbilityKeyType.CameraShake,
                                AbilityConfigSO.KeyType.Footstep => AbilityKeyType.Footstep,
                                AbilityConfigSO.KeyType.Custom => AbilityKeyType.Custom,
                                _ => AbilityKeyType.Hitbox
                            };
                            dstKey.Time = Mathf.Max(0f, srcKey.Time);
                            dstKey.EventNameHash = string.IsNullOrEmpty(srcKey.EventName) ? 0 : srcKey.EventName.GetHashCode();
                            dstKey.StringParamHash = string.IsNullOrEmpty(srcKey.StringParam) ? 0 : srcKey.StringParam.GetHashCode();
                            dstKey.IntParam = srcKey.IntParam;
                            var pf = srcKey.ParamF;
                            dstKey.ParamF = new float4(pf.x, pf.y, pf.z, pf.w);
                            if (srcKey.Type == AbilityConfigSO.KeyType.Hitbox)
                            {
                                dstKey.HitboxData = new HitboxKeyData
                                {
                                    Shape = (HitboxShapeType)srcKey.HitboxShape,
                                    Radius = srcKey.HitboxRadius,
                                    Angle = srcKey.HitboxAngle,
                                    Height = srcKey.HitboxHeight,
                                    Damage = srcKey.HitboxDamage,
                                    ActiveDuration = srcKey.HitboxActiveDuration,
                                    TeamMask = srcKey.HitboxTeamMask,
                                    Offset = new float3(srcKey.HitboxOffset.x, srcKey.HitboxOffset.y, srcKey.HitboxOffset.z),
                                    UseHeading = srcKey.HitboxUseHeading
                                };
                            }
                            if (srcKey.VFXPrefab != null && !string.IsNullOrEmpty(srcKey.VFXPrefabGuid))
                            {
                                dstKey.VFXData = new VFXKeyData
                                {
                                    PrefabGuidHash = srcKey.VFXPrefabGuid.GetHashCode(),
                                    Offset = new float3(srcKey.VFXOffset.x, srcKey.VFXOffset.y, srcKey.VFXOffset.z),
                                    Rotation = new float3(srcKey.VFXRotation.x, srcKey.VFXRotation.y, srcKey.VFXRotation.z),
                                    Scale = srcKey.VFXScale,
                                    AttachToTarget = srcKey.VFXAttachToTarget
                                };
                            }
                            if (srcKey.SFXClip != null && !string.IsNullOrEmpty(srcKey.SFXClipGuid))
                            {
                                dstKey.SFXData = new SFXKeyData
                                {
                                    ClipGuidHash = srcKey.SFXClipGuid.GetHashCode(),
                                    Volume = srcKey.SFXVolume,
                                    Pitch = srcKey.SFXPitch,
                                    Loop = srcKey.SFXLoop
                                };
                            }
                        }
                        var clips = srcTrack.Clips ?? new System.Collections.Generic.List<AbilityConfigSO.Clip>();
                        var validClips = new System.Collections.Generic.List<AbilityConfigSO.Clip>();
                        foreach (var clip in clips)
                        {
                            bool hasValidResource = false;
                            switch (clip.Type)
                            {
                                case AbilityConfigSO.ClipType.Animation:
                                    hasValidResource = clip.AnimationClip != null && !string.IsNullOrEmpty(clip.AnimationClipGuid);
                                    break;
                                case AbilityConfigSO.ClipType.SFX:
                                    hasValidResource = clip.AudioClip != null && !string.IsNullOrEmpty(clip.AudioClipGuid);
                                    break;
                                case AbilityConfigSO.ClipType.VFX:
                                    hasValidResource = clip.VFXPrefab != null && !string.IsNullOrEmpty(clip.VFXPrefabGuid);
                                    break;
                                case AbilityConfigSO.ClipType.Custom:
                                    hasValidResource = true;
                                    break;
                            }
                            if (hasValidResource)
                            {
                                validClips.Add(clip);
                            }
                        }
                        var clipArray = builder.Allocate(ref dstTrack.Clips, validClips.Count);
                        for (int c = 0; c < validClips.Count; c++)
                        {
                            var srcClip = validClips[c];
                            ref var dstClip = ref clipArray[c];
                            dstClip.Type = srcClip.Type switch
                            {
                                AbilityConfigSO.ClipType.Animation => AbilityClipType.Animation,
                                AbilityConfigSO.ClipType.SFX => AbilityClipType.SFX,
                                AbilityConfigSO.ClipType.VFX => AbilityClipType.VFX,
                                AbilityConfigSO.ClipType.Custom => AbilityClipType.Custom,
                                _ => AbilityClipType.Custom
                            };
                            dstClip.Start = Mathf.Max(0f, srcClip.Start);
                            dstClip.Duration = Mathf.Max(0f, srcClip.Duration);
                            dstClip.AnimationClipGuidHash = 0;
                            dstClip.AudioClipGuidHash = 0;
                            dstClip.VFXPrefabGuidHash = 0;
                            switch (srcClip.Type)
                            {
                                case AbilityConfigSO.ClipType.Animation:
                                    if (!string.IsNullOrEmpty(srcClip.AnimationClipGuid))
                                    {
                                        dstClip.AnimationClipGuidHash = srcClip.AnimationClipGuid.GetHashCode();
                                    }
                                    break;
                                case AbilityConfigSO.ClipType.SFX:
                                    if (!string.IsNullOrEmpty(srcClip.AudioClipGuid))
                                    {
                                        dstClip.AudioClipGuidHash = srcClip.AudioClipGuid.GetHashCode();
                                    }
                                    break;
                                case AbilityConfigSO.ClipType.VFX:
                                    if (!string.IsNullOrEmpty(srcClip.VFXPrefabGuid))
                                    {
                                        dstClip.VFXPrefabGuidHash = srcClip.VFXPrefabGuid.GetHashCode();
                                    }
                                    break;
                            }
                            var cpf = srcClip.ParamF;
                            dstClip.ParamF = new float4(cpf.x, cpf.y, cpf.z, cpf.w);
                        }
                    }
                }
            }

            var blob = builder.CreateBlobAssetReference<AbilityDBBlob>(Allocator.Persistent);
            AddBlobAsset(ref blob, out var _);
            builder.Dispose();

            var dbEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(dbEntity, new AbilityDBSingleton { Blob = blob });
        }
    }
}
#endif

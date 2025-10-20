using System;
using System.Collections.Generic;
using UnityEngine;

namespace SECS.Configs
{
    [CreateAssetMenu(menuName = "SECS/Configs/Ability Config", fileName = "AbilityConfig")]
    public class AbilityConfigSO : ScriptableObject
    {
        public List<AbilityDef> Abilities = new List<AbilityDef>();

        private void OnValidate()
        {
#if UNITY_EDITOR
            bool dirty = false;
            foreach (var ability in Abilities)
            {
                if (ability?.Phases == null) continue;
                foreach (var phase in ability.Phases)
                {
                    if (phase?.Tracks == null) continue;
                    foreach (var track in phase.Tracks)
                    {
                        if (track.Keys != null)
                        {
                            foreach (var key in track.Keys)
                            {
                                if (key.VFXPrefab != null)
                                {
                                    string newGuid = PrefabGuidUtility.GetGuid(key.VFXPrefab);
                                    if (key.VFXPrefabGuid != newGuid)
                                    {
                                        key.VFXPrefabGuid = newGuid;
                                        dirty = true;
                                    }
                                }
                                else if (!string.IsNullOrEmpty(key.VFXPrefabGuid))
                                {
                                    key.VFXPrefabGuid = null;
                                    dirty = true;
                                }
                                if (key.SFXClip != null)
                                {
                                    string newGuid = PrefabGuidUtility.GetGuid(key.SFXClip);
                                    if (key.SFXClipGuid != newGuid)
                                    {
                                        key.SFXClipGuid = newGuid;
                                        dirty = true;
                                    }
                                }
                                else if (!string.IsNullOrEmpty(key.SFXClipGuid))
                                {
                                    key.SFXClipGuid = null;
                                    dirty = true;
                                }
                            }
                        }
                        if (track.Clips != null)
                        {
                            foreach (var clip in track.Clips)
                            {
                                if (clip.AnimationClip != null)
                                {
                                    string newGuid = PrefabGuidUtility.GetGuid(clip.AnimationClip);
                                    if (clip.AnimationClipGuid != newGuid)
                                    {
                                        clip.AnimationClipGuid = newGuid;
                                        dirty = true;
                                    }
                                }
                                else if (!string.IsNullOrEmpty(clip.AnimationClipGuid))
                                {
                                    clip.AnimationClipGuid = null;
                                    dirty = true;
                                }
                                if (clip.AudioClip != null)
                                {
                                    string newGuid = PrefabGuidUtility.GetGuid(clip.AudioClip);
                                    if (clip.AudioClipGuid != newGuid)
                                    {
                                        clip.AudioClipGuid = newGuid;
                                        dirty = true;
                                    }
                                }
                                else if (!string.IsNullOrEmpty(clip.AudioClipGuid))
                                {
                                    clip.AudioClipGuid = null;
                                    dirty = true;
                                }
                                if (clip.VFXPrefab != null)
                                {
                                    string newGuid = PrefabGuidUtility.GetGuid(clip.VFXPrefab);
                                    if (clip.VFXPrefabGuid != newGuid)
                                    {
                                        clip.VFXPrefabGuid = newGuid;
                                        dirty = true;
                                    }
                                }
                                else if (!string.IsNullOrEmpty(clip.VFXPrefabGuid))
                                {
                                    clip.VFXPrefabGuid = null;
                                    dirty = true;
                                }
                            }
                        }
                    }
                }
            }
            if (dirty)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        [Serializable]
        public class AbilityDef
        {
            public string AbilityId = "Test_Ability";
            public float Cooldown = 1f;
            public List<Phase> Phases = new List<Phase> { new Phase() };
            [NonReorderable]
            public List<Marker> Markers = new List<Marker>();
        }

        [Serializable]
        public class Phase
        {
            public string Name = string.Empty;
            public bool Enabled = true;
            public bool Locked = false;
            public Color Color = Color.white;
            [NonSerialized] public bool IsExpanded = true;

            public float Duration = 1.0f;
            public List<Track> Tracks = new List<Track> { new Track() };
        }

        public enum TrackType
        {
            Hitbox,
            Animation,
            VFX,
            SFX,
            Camera,
            Script,
            Custom
        }

        [Serializable]
        public class Track
        {
            public TrackType Type = TrackType.Animation;
            public string Name = string.Empty;
            public bool Enabled = true;
            public bool Locked = false;
            public Color Color = Color.white;
            public bool Visible = true;
            public bool Muted = false;
            public bool Solo = false;
            [NonSerialized] public bool IsExpanded = true;

            public List<Clip> Clips = new List<Clip>();
            public List<Key> Keys = new List<Key> { new Key() };
            public static Color GetDefaultColor(TrackType type)
            {
                return type switch
                {
                    TrackType.Hitbox => new Color(1f, 0.3f, 0.3f, 1f),
                    TrackType.Animation => new Color(0.4f, 0.8f, 1f, 1f),
                    TrackType.VFX => new Color(1f, 0.7f, 0.3f, 1f),
                    TrackType.SFX => new Color(0.5f, 1f, 0.5f, 1f),
                    TrackType.Camera => new Color(0.8f, 0.4f, 1f, 1f),
                    TrackType.Script => new Color(1f, 0.9f, 0.3f, 1f),
                    TrackType.Custom => new Color(0.7f, 0.7f, 0.7f, 1f),
                    _ => Color.white
                };
            }
        }

        public enum KeyType
        {
            Hitbox,
            Signal,
            AnimationEvent,
            CameraShake,
            Footstep,
            Custom
        }

        [Serializable]
        public class Key
        {
            public KeyType Type = KeyType.Hitbox;
            public float Time = 0.05f;
            public string EventName = "OnKey";
            public string StringParam = "";
            public Vector4 ParamF;
            public int IntParam = 0;
            public Color EditorColor = Color.cyan;
            [Header("Hitbox Settings")]
            public HitboxShapeType HitboxShape = HitboxShapeType.Sphere;
            public float HitboxRadius = 1.0f;
            public float HitboxAngle = 60f;
            public float HitboxHeight = 2.0f;
            public float HitboxDamage = 10f;
            public float HitboxActiveDuration = 0.2f;
            public byte HitboxTeamMask = 1;
            public Vector3 HitboxOffset = Vector3.zero;
            public bool HitboxUseHeading = true;
            [Header("VFX Settings")]
            public GameObject VFXPrefab;
            [HideInInspector] public string VFXPrefabGuid;
            public Vector3 VFXOffset = Vector3.zero;
            public Vector3 VFXRotation = Vector3.zero;
            public float VFXScale = 1.0f;
            public bool VFXAttachToTarget = false;
            [Header("SFX Settings")]
            public AudioClip SFXClip;
            [HideInInspector] public string SFXClipGuid;
            public float SFXVolume = 1.0f;
            public float SFXPitch = 1.0f;
            public bool SFXLoop = false;
            [Header("Other Preview")]
            public float CameraShakeRadius = 5f;
            public Vector3 MarkerPosition = Vector3.zero;
        }
        public enum HitboxShapeType
        {
            Sphere = 0,
            Cone = 1,
            Capsule = 2
        }

        public enum ClipType { Animation, SFX, VFX, Custom }

        [Serializable]
        public class Clip
        {
            public ClipType Type = ClipType.Animation;
            public float Start = 0f;
            public AnimationClip AnimationClip;
            [HideInInspector] public string AnimationClipGuid;
            public AudioClip AudioClip;
            [HideInInspector] public string AudioClipGuid;
            public GameObject VFXPrefab;
            [HideInInspector] public string VFXPrefabGuid;
            public float Duration => CalculateDuration();
            public Vector4 ParamF;
            private float CalculateDuration()
            {
                switch (Type)
                {
                    case ClipType.Animation:
                        return AnimationClip != null ? AnimationClip.length : 0.25f;
                    case ClipType.SFX:
                        return AudioClip != null ? AudioClip.length : 0.25f;
                    case ClipType.VFX:
                        if (VFXPrefab != null)
                        {
                            var ps = VFXPrefab.GetComponent<ParticleSystem>();
                            if (ps != null)
                                return ps.main.duration + ps.main.startLifetime.constantMax;
                        }
                        return 1f;
                    case ClipType.Custom:
                        return ParamF.x > 0 ? ParamF.x : 0.25f;
                    default:
                        return 0.25f;
                }
            }
        }

        [Serializable]
        public class Marker
        {
            public float Time = 0f;
            public string Type = "Signal"; 
            public string Label = string.Empty;
        }

    }
}

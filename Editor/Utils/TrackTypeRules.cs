#if UNITY_EDITOR
using System.Collections.Generic;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
    /// <summary>
    /// Track类型规则 - 定义每种Track可以放置的Clip和Key类型
    /// </summary>
    public static class TrackTypeRules
    {
        /// <summary>
        /// 获取指定Track类型允许的Clip类型
        /// </summary>
        public static HashSet<AbilityConfigSO.ClipType> GetAllowedClipTypes(AbilityConfigSO.TrackType trackType)
        {
            return trackType switch
            {
                AbilityConfigSO.TrackType.Hitbox => new HashSet<AbilityConfigSO.ClipType>
                {
                },
                AbilityConfigSO.TrackType.Animation => new HashSet<AbilityConfigSO.ClipType>
                {
                    AbilityConfigSO.ClipType.Animation
                },
                AbilityConfigSO.TrackType.VFX => new HashSet<AbilityConfigSO.ClipType>
                {
                    AbilityConfigSO.ClipType.VFX,
                    AbilityConfigSO.ClipType.Custom
                },
                AbilityConfigSO.TrackType.SFX => new HashSet<AbilityConfigSO.ClipType>
                {
                    AbilityConfigSO.ClipType.SFX,
                    AbilityConfigSO.ClipType.Custom
                },
                AbilityConfigSO.TrackType.Camera => new HashSet<AbilityConfigSO.ClipType>
                {
                    AbilityConfigSO.ClipType.Custom
                },
                AbilityConfigSO.TrackType.Script => new HashSet<AbilityConfigSO.ClipType>
                {
                    AbilityConfigSO.ClipType.Custom
                },
                AbilityConfigSO.TrackType.Custom => new HashSet<AbilityConfigSO.ClipType>
                {
                    AbilityConfigSO.ClipType.Animation,
                    AbilityConfigSO.ClipType.VFX,
                    AbilityConfigSO.ClipType.SFX,
                    AbilityConfigSO.ClipType.Custom
                },
                _ => new HashSet<AbilityConfigSO.ClipType>()
            };
        }
        /// <summary>
        /// 获取指定Track类型允许的Key类型
        /// </summary>
        public static HashSet<AbilityConfigSO.KeyType> GetAllowedKeyTypes(AbilityConfigSO.TrackType trackType)
        {
            return trackType switch
            {
                AbilityConfigSO.TrackType.Hitbox => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.Hitbox
                },
                AbilityConfigSO.TrackType.Animation => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.AnimationEvent,
                    AbilityConfigSO.KeyType.Signal
                },
                AbilityConfigSO.TrackType.VFX => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.Signal,
                    AbilityConfigSO.KeyType.Custom
                },
                AbilityConfigSO.TrackType.SFX => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.Footstep,
                    AbilityConfigSO.KeyType.Signal,
                    AbilityConfigSO.KeyType.Custom
                },
                AbilityConfigSO.TrackType.Camera => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.CameraShake,
                    AbilityConfigSO.KeyType.Signal
                },
                AbilityConfigSO.TrackType.Script => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.Signal,
                    AbilityConfigSO.KeyType.Custom
                },
                AbilityConfigSO.TrackType.Custom => new HashSet<AbilityConfigSO.KeyType>
                {
                    AbilityConfigSO.KeyType.Hitbox,
                    AbilityConfigSO.KeyType.Signal,
                    AbilityConfigSO.KeyType.AnimationEvent,
                    AbilityConfigSO.KeyType.CameraShake,
                    AbilityConfigSO.KeyType.Footstep,
                    AbilityConfigSO.KeyType.Custom
                },
                _ => new HashSet<AbilityConfigSO.KeyType>()
            };
        }
        /// <summary>
        /// 检查Track是否可以添加指定类型的Clip
        /// </summary>
        public static bool CanAddClip(AbilityConfigSO.TrackType trackType, AbilityConfigSO.ClipType clipType)
        {
            return GetAllowedClipTypes(trackType).Contains(clipType);
        }
        /// <summary>
        /// 检查Track是否可以添加指定类型的Key
        /// </summary>
        public static bool CanAddKey(AbilityConfigSO.TrackType trackType, AbilityConfigSO.KeyType keyType)
        {
            return GetAllowedKeyTypes(trackType).Contains(keyType);
        }
        /// <summary>
        /// 获取Track类型的推荐Key类型（默认创建时使用�?
        /// </summary>
        public static AbilityConfigSO.KeyType GetDefaultKeyType(AbilityConfigSO.TrackType trackType)
        {
            return trackType switch
            {
                AbilityConfigSO.TrackType.Hitbox => AbilityConfigSO.KeyType.Hitbox,
                AbilityConfigSO.TrackType.Animation => AbilityConfigSO.KeyType.AnimationEvent,
                AbilityConfigSO.TrackType.VFX => AbilityConfigSO.KeyType.Signal,
                AbilityConfigSO.TrackType.SFX => AbilityConfigSO.KeyType.Footstep,
                AbilityConfigSO.TrackType.Camera => AbilityConfigSO.KeyType.CameraShake,
                AbilityConfigSO.TrackType.Script => AbilityConfigSO.KeyType.Signal,
                AbilityConfigSO.TrackType.Custom => AbilityConfigSO.KeyType.Custom,
                _ => AbilityConfigSO.KeyType.Signal
            };
        }
        /// <summary>
        /// 获取Track类型的推荐Clip类型
        /// </summary>
        public static AbilityConfigSO.ClipType GetDefaultClipType(AbilityConfigSO.TrackType trackType)
        {
            return trackType switch
            {
                AbilityConfigSO.TrackType.Animation => AbilityConfigSO.ClipType.Animation,
                AbilityConfigSO.TrackType.VFX => AbilityConfigSO.ClipType.VFX,
                AbilityConfigSO.TrackType.SFX => AbilityConfigSO.ClipType.SFX,
                _ => AbilityConfigSO.ClipType.Custom
            };
        }
        /// <summary>
        /// 获取Track类型的限制说明
        /// </summary>
        public static string GetRestrictionInfo(AbilityConfigSO.TrackType trackType)
        {
            var allowedClips = GetAllowedClipTypes(trackType);
            var allowedKeys = GetAllowedKeyTypes(trackType);
            string clipInfo = allowedClips.Count > 0 
                ? $"Clips: {string.Join(", ", allowedClips)}" 
                : "Clips: None allowed";
            string keyInfo = allowedKeys.Count > 0 
                ? $"Keys: {string.Join(", ", allowedKeys)}" 
                : "Keys: None allowed";
            return $"{clipInfo}\n{keyInfo}";
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SECS.Configs
{
    /// <summary>
    /// 预设系统：保存常用的Key组合
    /// </summary>
    [CreateAssetMenu(menuName = "SECS/Configs/Ability Key Preset", fileName = "AbilityKeyPreset")]
    public class AbilityKeyPresetSO : ScriptableObject
    {
        [Serializable]
        public class KeyPreset
        {
            public string Name = "New Preset";
            public List<AbilityConfigSO.Key> Keys = new List<AbilityConfigSO.Key>();
            [TextArea(2, 4)]
            public string Description = string.Empty;
            public float PivotTime = 0f;
        }
        public List<KeyPreset> Presets = new List<KeyPreset>();
    }
}

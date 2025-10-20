using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{

public class InspectorView : IDisposable
{
    private readonly VisualElement _container;
    private readonly TimelineState _state;
    private readonly TimelineController _controller;

    public InspectorView(VisualElement container, TimelineState state, TimelineController controller)
    {
        _container = container;
        _state = state;
        _controller = controller;
    }

    public void Build()
    {
        _container.Clear();
        var title = new Label("属性")
        {
            style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    marginBottom = 6
                }
        };
        _container.Add(title);

        Rebuild();
    }

    private void Rebuild()
    {
        // 清除旧内容（除了标题）
        while (_container.childCount > 1)
        {
            _container.RemoveAt(1);
        }

        if (!_state.Selection.HasSelection)
        {
            var placeholder = new Label("未选择")
            {
                style = { color = new Color(0.5f, 0.5f, 0.5f, 1f), marginTop = 10 }
            };
            _container.Add(placeholder);
            return;
        }

        var ability = _state.Data.CurrentAbility;
        if (ability == null) return;

        switch (_state.Selection.Kind)
        {
            case TimelineState.SelectionKind.Key:
                BuildKeyInspector();
                break;
            case TimelineState.SelectionKind.Clip:
                BuildClipInspector();
                break;
            case TimelineState.SelectionKind.Track:
                BuildTrackInspector();
                break;
            case TimelineState.SelectionKind.Phase:
                BuildPhaseInspector();
                break;
        }
    }

    private void BuildKeyInspector()
    {
        var ability = _state.Data.CurrentAbility;
        int p = _state.Selection.PhaseIndex;
        int t = _state.Selection.TrackIndex;
        int k = _state.Selection.KeyIndex;

        if (p < 0 || p >= ability.Phases.Count) return;
        var phase = ability.Phases[p];
        if (t < 0 || t >= phase.Tracks.Count) return;
        var track = phase.Tracks[t];
        if (k < 0 || k >= track.Keys.Count) return;
        var key = track.Keys[k];

        AddLabel("Key 属性");
        
       
        AddFloatField("时间", key.Time, v => { key.Time = v; SaveConfig(); });
        
       
        AddEnumField("类型", key.Type, v => 
        { 
            key.Type = (AbilityConfigSO.KeyType)v; 
            SaveConfig();
            Rebuild(); 
        });
        
        
        AddTextField("事件名称", key.EventName ?? "", v => { key.EventName = v; SaveConfig(); });
        
        
        AddColorField("编辑器颜色", key.EditorColor, v => { key.EditorColor = v; SaveConfig(); });
        
       
        AddSpace(10);
        var foldout = new Foldout { text = $"{key.Type} 设置", value = true };
        foldout.style.marginTop = 5;
        foldout.style.marginBottom = 5;
        _container.Add(foldout);
        
       
        BuildKeyTypeSpecificProperties(key, foldout);
    }
    
    /// <summary>
    /// 构建类型特定属性
    /// </summary>
    private void BuildKeyTypeSpecificProperties(AbilityConfigSO.Key key, Foldout container)
    {
        switch (key.Type)
        {
            case AbilityConfigSO.KeyType.Hitbox:
                BuildHitboxKeyProperties(key, container);
                break;
                
            case AbilityConfigSO.KeyType.Signal:
            case AbilityConfigSO.KeyType.Custom:
                BuildGenericKeyProperties(key, container);
                break;
                
            case AbilityConfigSO.KeyType.AnimationEvent:
                BuildAnimationEventKeyProperties(key, container);
                break;
                
            case AbilityConfigSO.KeyType.CameraShake:
                BuildCameraShakeKeyProperties(key, container);
                break;
                
            case AbilityConfigSO.KeyType.Footstep:
                BuildFootstepKeyProperties(key, container);
                break;
        }
    }
    
    private void BuildHitboxKeyProperties(AbilityConfigSO.Key key, VisualElement container)
    {
        
        var shapeField = new EnumField("形状", key.HitboxShape);
        shapeField.RegisterValueChangedCallback(evt => 
        { 
            key.HitboxShape = (AbilityConfigSO.HitboxShapeType)evt.newValue; 
            SaveConfig();
            Rebuild(); //
        });
        container.Add(shapeField);
        
        // Radius
        var radiusField = new FloatField("半径") { value = key.HitboxRadius };
        radiusField.isDelayed = true;
        radiusField.RegisterValueChangedCallback(evt => { key.HitboxRadius = Mathf.Max(0.1f, evt.newValue); SaveConfig(); });
        container.Add(radiusField);
        
        // Angle (Cone only)
        if (key.HitboxShape == AbilityConfigSO.HitboxShapeType.Cone)
        {
            var angleField = new FloatField("角度 (度)") { value = key.HitboxAngle };
            angleField.isDelayed = true;
            angleField.RegisterValueChangedCallback(evt => { key.HitboxAngle = Mathf.Clamp(evt.newValue, 1f, 360f); SaveConfig(); });
            container.Add(angleField);
        }
        
        // Height (Capsule only)
        if (key.HitboxShape == AbilityConfigSO.HitboxShapeType.Capsule)
        {
            var heightField = new FloatField("高度") { value = key.HitboxHeight };
            heightField.isDelayed = true;
            heightField.RegisterValueChangedCallback(evt => { key.HitboxHeight = Mathf.Max(0.1f, evt.newValue); SaveConfig(); });
            container.Add(heightField);
        }
        
        // Damage
        var damageField = new FloatField("伤害") { value = key.HitboxDamage };
        damageField.isDelayed = true;
        damageField.RegisterValueChangedCallback(evt => { key.HitboxDamage = Mathf.Max(0f, evt.newValue); SaveConfig(); });
        container.Add(damageField);
        
        // Active Duration
        var durationField = new FloatField("激活持续时间") { value = key.HitboxActiveDuration };
        durationField.isDelayed = true;
        durationField.RegisterValueChangedCallback(evt => { key.HitboxActiveDuration = Mathf.Max(0.01f, evt.newValue); SaveConfig(); });
        container.Add(durationField);
        
        // Team Mask
        var teamMaskField = new IntegerField("队伍掩码 (1=敌人, 2=友军, 3=全部)") { value = key.HitboxTeamMask };
        teamMaskField.isDelayed = true;
        teamMaskField.RegisterValueChangedCallback(evt => { key.HitboxTeamMask = (byte)Mathf.Clamp(evt.newValue, 0, 3); SaveConfig(); });
        container.Add(teamMaskField);
        
        // Offset
        var offsetField = new Vector3Field("偏移") { value = key.HitboxOffset };
        foreach (var child in offsetField.Query<FloatField>().ToList())
        {
            child.isDelayed = true;
        }
        offsetField.RegisterValueChangedCallback(evt => { key.HitboxOffset = evt.newValue; SaveConfig(); });
        container.Add(offsetField);
        
        // Use Heading
        var headingToggle = new Toggle("使用角色朝向") { value = key.HitboxUseHeading };
        headingToggle.RegisterValueChangedCallback(evt => { key.HitboxUseHeading = evt.newValue; SaveConfig(); });
        container.Add(headingToggle);
    }
    
    private void BuildGenericKeyProperties(AbilityConfigSO.Key key, VisualElement container)
    {
        // String Parameter
        var stringField = new TextField("字符串参数") { value = key.StringParam ?? "" };
        stringField.isDelayed = true;
        stringField.RegisterValueChangedCallback(evt => { key.StringParam = evt.newValue; SaveConfig(); });
        container.Add(stringField);
        
        // Int Parameter
        var intField = new IntegerField("整数参数") { value = key.IntParam };
        intField.isDelayed = true;
        intField.RegisterValueChangedCallback(evt => { key.IntParam = evt.newValue; SaveConfig(); });
        container.Add(intField);
        
        // ParamF Vector4
        var paramField = new Vector4Field("参数向量 (F)") { value = key.ParamF };
        foreach (var child in paramField.Query<FloatField>().ToList())
        {
            child.isDelayed = true;
        }
        paramField.RegisterValueChangedCallback(evt => { key.ParamF = evt.newValue; SaveConfig(); });
        container.Add(paramField);
        
        // Marker Position
        var markerPosField = new Vector3Field("标记位置") { value = key.MarkerPosition };
        foreach (var child in markerPosField.Query<FloatField>().ToList())
        {
            child.isDelayed = true;
        }
        markerPosField.RegisterValueChangedCallback(evt => { key.MarkerPosition = evt.newValue; SaveConfig(); });
        container.Add(markerPosField);
    }
    
    private void BuildAnimationEventKeyProperties(AbilityConfigSO.Key key, VisualElement container)
    {
        var label = new Label("函数名称 → 事件名称");
        label.style.unityFontStyleAndWeight = FontStyle.Italic;
        container.Add(label);
        
        var paramField = new TextField("参数") { value = key.StringParam ?? "" };
        paramField.isDelayed = true;
        paramField.RegisterValueChangedCallback(evt => { key.StringParam = evt.newValue; SaveConfig(); });
        container.Add(paramField);
        
        // Int parameter
        var intField = new IntegerField("整数参数") { value = key.IntParam };
        intField.isDelayed = true;
        intField.RegisterValueChangedCallback(evt => { key.IntParam = evt.newValue; SaveConfig(); });
        container.Add(intField);
        
        // Float parameter
        var floatField = new FloatField("浮点参数") { value = key.ParamF.x };
        floatField.isDelayed = true;
        floatField.RegisterValueChangedCallback(evt => 
        { 
            key.ParamF = new Vector4(evt.newValue, key.ParamF.y, key.ParamF.z, key.ParamF.w); 
            SaveConfig(); 
        });
        container.Add(floatField);
    }
    
    private void BuildCameraShakeKeyProperties(AbilityConfigSO.Key key, VisualElement container)
    {
        var radiusField = new FloatField("半径") { value = key.CameraShakeRadius };
        radiusField.isDelayed = true;
        radiusField.RegisterValueChangedCallback(evt => { key.CameraShakeRadius = Mathf.Max(0.1f, evt.newValue); SaveConfig(); });
        container.Add(radiusField);
        
        var intensityField = new FloatField("强度") { value = key.ParamF.x };
        intensityField.isDelayed = true;
        intensityField.RegisterValueChangedCallback(evt => 
        { 
            key.ParamF = new Vector4(evt.newValue, key.ParamF.y, key.ParamF.z, key.ParamF.w); 
            SaveConfig(); 
        });
        container.Add(intensityField);
        
        var durationField = new FloatField("持续时间") { value = key.ParamF.y };
        durationField.isDelayed = true;
        durationField.RegisterValueChangedCallback(evt => 
        { 
            key.ParamF = new Vector4(key.ParamF.x, evt.newValue, key.ParamF.z, key.ParamF.w); 
            SaveConfig(); 
        });
        container.Add(durationField);
    }
    
    private void BuildFootstepKeyProperties(AbilityConfigSO.Key key, VisualElement container)
    {
        var soundIdField = new TextField("音效 ID") { value = key.StringParam ?? "" };
        soundIdField.isDelayed = true;
        soundIdField.RegisterValueChangedCallback(evt => { key.StringParam = evt.newValue; SaveConfig(); });
        container.Add(soundIdField);
        
        var footField = new IntegerField("脚步 (0=左脚, 1=右脚)") { value = key.IntParam };
        footField.isDelayed = true;
        footField.RegisterValueChangedCallback(evt => { key.IntParam = Mathf.Clamp(evt.newValue, 0, 1); SaveConfig(); });
        container.Add(footField);
        
        var volumeField = new FloatField("音量") { value = key.ParamF.x };
        volumeField.isDelayed = true;
        volumeField.RegisterValueChangedCallback(evt => 
        { 
            key.ParamF = new Vector4(Mathf.Clamp01(evt.newValue), key.ParamF.y, key.ParamF.z, key.ParamF.w); 
            SaveConfig(); 
        });
        container.Add(volumeField);
    }

    private void BuildClipInspector()
    {
        var ability = _state.Data.CurrentAbility;
        int p = _state.Selection.PhaseIndex;
        int t = _state.Selection.TrackIndex;
        int c = _state.Selection.ClipIndex;

        if (p < 0 || p >= ability.Phases.Count) return;
        var phase = ability.Phases[p];
        if (t < 0 || t >= phase.Tracks.Count) return;
        var track = phase.Tracks[t];
        if (c < 0 || c >= track.Clips.Count) return;
        var clip = track.Clips[c];

        AddLabel("Clip 属性");
        
        // Clip Type (类型改变时需要Rebuild，因为会影响显示的字段)
        AddEnumField("Clip 类型", clip.Type, v => { clip.Type = (AbilityConfigSO.ClipType)v; SaveConfig(); Rebuild(); });
        
        // Start Time (可编辑，不需要Rebuild)
        AddFloatField("起始时间", clip.Start, v => { clip.Start = v; SaveConfig(); });
        
        // Duration (只读，自动计算)
        var durationLabel = new Label($"持续时间: {clip.Duration:F3}秒 (自动)")
        {
            style =
            {
                marginTop = 5,
                marginBottom = 5,
                color = new Color(0.7f, 0.7f, 0.7f, 1f)
            }
        };
        _container.Add(durationLabel);
        
        // 根据类型显示不同的资源字段
        switch (clip.Type)
        {
            case AbilityConfigSO.ClipType.Animation:
                var animField = new ObjectField("动画 Clip") 
                { 
                    objectType = typeof(AnimationClip),
                    value = clip.AnimationClip
                };
                animField.RegisterValueChangedCallback(evt => 
                { 
                    clip.AnimationClip = evt.newValue as AnimationClip; 
                    SaveConfig(); 
                    Rebuild(); // 刷新Duration显示
                });
                _container.Add(animField);
                break;
                
            case AbilityConfigSO.ClipType.SFX:
                var audioField = new ObjectField("音频 Clip") 
                { 
                    objectType = typeof(AudioClip),
                    value = clip.AudioClip
                };
                audioField.RegisterValueChangedCallback(evt => 
                { 
                    clip.AudioClip = evt.newValue as AudioClip; 
                    SaveConfig(); 
                    Rebuild();
                });
                _container.Add(audioField);
                break;
                
            case AbilityConfigSO.ClipType.VFX:
                var vfxField = new ObjectField("特效预制体") 
                { 
                    objectType = typeof(GameObject),
                    value = clip.VFXPrefab
                };
                vfxField.RegisterValueChangedCallback(evt => 
                { 
                    clip.VFXPrefab = evt.newValue as GameObject; 
                    SaveConfig(); 
                    Rebuild();
                });
                _container.Add(vfxField);
                break;
                
            case AbilityConfigSO.ClipType.Custom:
                AddLabel("自定义 Clip (持续时间在 ParamF.x 中)");
                var customDurationField = new FloatField("自定义持续时间");
                customDurationField.value = clip.ParamF.x;
                customDurationField.isDelayed = true; // 只在失去焦点或按回车时触发
                customDurationField.RegisterValueChangedCallback(evt => 
                { 
                    var v = clip.ParamF;
                    v.x = Mathf.Max(0.01f, evt.newValue);
                    clip.ParamF = v;
                    SaveConfig(); 
                    // Duration改变了，需要Rebuild更新Duration标签
                    Rebuild();
                });
                _container.Add(customDurationField);
                break;
        }
        
        // End Time
        AddLabel($"结束时间: {(clip.Start + clip.Duration):F3}秒");
        
        // 警告：如果Clip超出Phase长度
        var phase2 = ability.Phases[p];
        if (clip.Start + clip.Duration > phase2.Duration)
        {
            var warningLabel = new Label($"Clip超出Phase持续时间！");
            warningLabel.style.color = new Color(1f, 0.5f, 0f, 1f);
            warningLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _container.Add(warningLabel);
        }
        
        // ParamF Vector4 (仅Custom类型显示)
        if (clip.Type == AbilityConfigSO.ClipType.Custom)
        {
            var paramField = new Vector4Field("参数向量 (Y/Z/W)");
            paramField.value = clip.ParamF;
            // Vector4Field的子字段也需要设置isDelayed
            foreach (var child in paramField.Query<FloatField>().ToList())
            {
                child.isDelayed = true;
            }
            paramField.RegisterValueChangedCallback(evt => { clip.ParamF = evt.newValue; SaveConfig(); });
            _container.Add(paramField);
        }
    }

    private void BuildTrackInspector()
    {
        var ability = _state.Data.CurrentAbility;
        int p = _state.Selection.PhaseIndex;
        int t = _state.Selection.TrackIndex;

        if (p < 0 || p >= ability.Phases.Count) return;
        var phase = ability.Phases[p];
        if (t < 0 || t >= phase.Tracks.Count) return;
        var track = phase.Tracks[t];

        AddLabel("Track 属性");
        
        // Track Type（类型枚举）
        AddEnumField("类型", track.Type, v => 
        { 
            track.Type = (AbilityConfigSO.TrackType)v; 
            // 类型变更时自动设置默认颜色（如果当前是白色）
            if (track.Color == Color.white)
            {
                track.Color = AbilityConfigSO.Track.GetDefaultColor(track.Type);
            }
            SaveConfig(); 
        });
        
        AddTextField("Track 名称", track.Name ?? "", v => { track.Name = v; SaveConfig(); });
        AddColorField("颜色", track.Color, v => { track.Color = v; SaveConfig(); });
        
        // 快速按钮：重置为默认颜色
        var resetColorBtn = new Button(() => 
        {
            track.Color = AbilityConfigSO.Track.GetDefaultColor(track.Type);
            SaveConfig();
            Rebuild();
        }) { text = "重置为默认颜色" };
        _container.Add(resetColorBtn);
        
        // 开关
        var enabledToggle = new Toggle("启用") { value = track.Enabled };
        enabledToggle.RegisterValueChangedCallback(evt => { track.Enabled = evt.newValue; SaveConfig(); });
        _container.Add(enabledToggle);
        
        var mutedToggle = new Toggle("静音") { value = track.Muted };
        mutedToggle.RegisterValueChangedCallback(evt => { track.Muted = evt.newValue; SaveConfig(); });
        _container.Add(mutedToggle);
        
        var soloToggle = new Toggle("Solo") { value = track.Solo };
        soloToggle.RegisterValueChangedCallback(evt => { track.Solo = evt.newValue; SaveConfig(); });
        _container.Add(soloToggle);
        
        // 统计信息
        AddLabel($"Keys: {track.Keys.Count}");
        AddLabel($"Clips: {track.Clips.Count}");
        
        // Track类型说明
        AddLabel(GetTrackTypeHelp(track.Type), new Color(0.7f, 0.7f, 0.7f, 1f));
        
        // Track类型限制说明
        AddLabel("允许的类型:", new Color(0.9f, 0.9f, 0.5f, 1f));
        AddLabel(TrackTypeRules.GetRestrictionInfo(track.Type), new Color(0.6f, 0.6f, 0.6f, 1f));
    }
    
    private string GetTrackTypeHelp(AbilityConfigSO.TrackType type)
    {
        return type switch
        {
            AbilityConfigSO.TrackType.Hitbox => "Hitbox Track: 包含用于伤害检测的判定框激活 Key",
            AbilityConfigSO.TrackType.Animation => "Animation Track: 通过 Clip 控制角色动画",
            AbilityConfigSO.TrackType.VFX => "VFX Track: 在指定时间生成视觉特效",
            AbilityConfigSO.TrackType.SFX => "SFX Track: 在 Ability 执行期间播放音效",
            AbilityConfigSO.TrackType.Camera => "Camera Track: 相机震动和冲击特效",
            AbilityConfigSO.TrackType.Script => "Script Track: 自定义脚本回调/钩子",
            AbilityConfigSO.TrackType.Custom => "Custom Track: 用户自定义 Track 行为",
            _ => ""
        };
    }

    private void BuildPhaseInspector()
    {
        var ability = _state.Data.CurrentAbility;
        int p = _state.Selection.PhaseIndex;

        if (p < 0 || p >= ability.Phases.Count) return;
        var phase = ability.Phases[p];

        AddLabel("Phase 属性");
        AddTextField("Phase 名称", phase.Name ?? "", v => { phase.Name = v; SaveConfig(); });
        
        // Phase Duration手动编辑
        AddFloatField("持续时间", phase.Duration, v => { phase.Duration = Mathf.Max(0.01f, v); SaveConfig(); });
        
        AddColorField("颜色", phase.Color, v => { phase.Color = v; SaveConfig(); });
        
        // 开关
        var enabledToggle = new Toggle("启用") { value = phase.Enabled };
        enabledToggle.RegisterValueChangedCallback(evt => { phase.Enabled = evt.newValue; SaveConfig(); });
        _container.Add(enabledToggle);
        
        var lockedToggle = new Toggle("锁定") { value = phase.Locked };
        lockedToggle.RegisterValueChangedCallback(evt => { phase.Locked = evt.newValue; SaveConfig(); });
        _container.Add(lockedToggle);
        
        // 统计信息
        AddLabel($"Tracks: {phase.Tracks.Count}");
        AddLabel($"注意: Phase 按顺序执行");
    }

    private void AddLabel(string text, Color? color = null)
    {
        var label = new Label(text)
        {
            style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 10,
                    marginBottom = 5
                }
        };
        if (color.HasValue)
        {
            label.style.color = color.Value;
            label.style.unityFontStyleAndWeight = FontStyle.Normal;
        }
        _container.Add(label);
    }
    
    private void AddSpace(float height)
    {
        var space = new VisualElement();
        space.style.height = height;
        _container.Add(space);
    }

    private void AddFloatField(string label, float value, Action<float> onChanged)
    {
        var field = new FloatField(label) { value = value };
        field.isDelayed = true; 
        field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
        _container.Add(field);
    }

    private void AddTextField(string label, string value, Action<string> onChanged)
    {
        var field = new TextField(label) { value = value };
        field.isDelayed = true; 
        field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
        _container.Add(field);
    }

    private void AddEnumField(string label, Enum value, Action<Enum> onChanged)
    {
        var field = new EnumField(label, value);
        field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
        _container.Add(field);
    }

    private void AddColorField(string label, Color value, Action<Color> onChanged)
    {
        var field = new ColorField(label) { value = value };
        field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
        _container.Add(field);
    }

    private void SaveConfig()
    {
        if (_state.Data.Config != null)
        {
            Undo.RecordObject(_state.Data.Config, "Modify Property");
            EditorUtility.SetDirty(_state.Data.Config);
            _state.NotifyChanged();
        }
    }

    public void Refresh()
    {
        Rebuild();
    }

    public void Dispose() { }
}
}

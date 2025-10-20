using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{

    public class PreviewController : IDisposable
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private GameObject _previewTarget;
        private Animator _previewAnimator;
        private bool _isPreviewMode;
        private float _currentPreviewTime;
        private List<KeyPreviewData> _activeKeys = new List<KeyPreviewData>();
        private List<ClipPreviewData> _activeClips = new List<ClipPreviewData>();
        private struct TransformState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }
        private Dictionary<Transform, TransformState> _cachedTransforms = new Dictionary<Transform, TransformState>();
        private Dictionary<AnimationClip, AnimationClip> _clipCache = new Dictionary<AnimationClip, AnimationClip>();
        private PreviewClipInstanceManager _clipInstanceManager;
        private struct KeyPreviewData
        {
            public AbilityConfigSO.Key Key;
            public float Time;
        }
        private struct ClipPreviewData
        {
            public AbilityConfigSO.Clip Clip;
            public float StartTime;
            public float LocalTime;
        }
        public GameObject PreviewTarget => _previewTarget;
        public bool IsPreviewMode => _isPreviewMode;
        public PreviewController(TimelineState state, TimelineController controller)
        {
            _state = state;
            _controller = controller;
        }
        /// <summary>
        /// 设置预览目标GameObject
        /// </summary>
        public void SetPreviewTarget(GameObject target)
        {
            if (_isPreviewMode && _previewTarget != target)
            {
                ExitPreviewMode();
            }
            _previewTarget = target;
            if (_previewTarget != null)
            {
                _previewAnimator = _previewTarget.GetComponent<Animator>();
                _clipInstanceManager = new PreviewClipInstanceManager(_previewTarget.transform);
            }
            else
            {
                _previewAnimator = null;
                _clipInstanceManager = null;
            }
        }
        /// <summary>
        /// 进入预览模式
        /// </summary>
        public bool EnterPreviewMode()
        {
            if (_isPreviewMode) return true;
            if (_previewTarget == null)
            {
                Debug.LogWarning("[PreviewController] No preview target set. Please assign a GameObject first.");
                return false;
            }
            AnimationMode.StartAnimationMode();
            _isPreviewMode = true;
            CacheTransformStates(_previewTarget.transform);
            SceneView.duringSceneGui += OnSceneGUI;
            Debug.Log($"[PreviewController] Entered preview mode. Target: {_previewTarget.name}");
            return true;
        }
        /// <summary>
        /// 退出预览模式
        /// </summary>
        public void ExitPreviewMode()
        {
            if (!_isPreviewMode) return;
            SceneView.duringSceneGui -= OnSceneGUI;
            _clipInstanceManager?.ClearAll();
            RestoreTransformStates();
            AnimationMode.StopAnimationMode();
            _isPreviewMode = false;
            _cachedTransforms.Clear();
            _activeKeys.Clear();
            _activeClips.Clear();
            Debug.Log("[PreviewController] Exited preview mode.");
        }
        /// <summary>
        /// 采样动画到指定时间
        /// </summary>
        public void Sample(float time)
        {
            if (!_isPreviewMode)
            {
                Debug.LogWarning("[PreviewController] Cannot sample: not in preview mode.");
                return;
            }
            if (_previewTarget == null)
            {
                Debug.LogWarning("[PreviewController] Cannot sample: preview target is null.");
                return;
            }
            _currentPreviewTime = time;
            _activeKeys.Clear();
            _activeClips.Clear();
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;
            foreach (var phase in ability.Phases)
            {
                if (!phase.Enabled) continue;
                float phaseStartTime = _state.GetPhaseStartOffset(ability.Phases.IndexOf(phase));
                float phaseEndTime = phaseStartTime + phase.Duration;
                if (time < phaseStartTime || time >= phaseEndTime) continue;
                float localTime = time - phaseStartTime;
                foreach (var track in phase.Tracks)
                {
                    if (!track.Enabled || track.Muted) continue;
                    bool hasSolo = phase.Tracks.Exists(t => t.Solo);
                    if (hasSolo && !track.Solo) continue;
                    foreach (var clip in track.Clips)
                    {
                        float clipStart = clip.Start;
                        float clipEnd = clipStart + clip.Duration;
                        if (localTime < clipStart || localTime >= clipEnd) continue;
                        float clipLocalTime = localTime - clipStart;
                        if (clip.Type == AbilityConfigSO.ClipType.Animation && clip.AnimationClip != null)
                        {
                            AnimationMode.BeginSampling();
                            AnimationMode.SampleAnimationClip(_previewTarget, clip.AnimationClip, clipLocalTime);
                            AnimationMode.EndSampling();
                        }
                        _activeClips.Add(new ClipPreviewData
                        {
                            Clip = clip,
                            StartTime = phaseStartTime + clipStart,
                            LocalTime = clipLocalTime
                        });
                    }
                    foreach (var key in track.Keys)
                    {
                        float keyTime = phaseStartTime + key.Time;
                        const float keyDisplayWindow = 0.1f;
                        if (Mathf.Abs(time - keyTime) <= keyDisplayWindow)
                        {
                            _activeKeys.Add(new KeyPreviewData
                            {
                                Key = key,
                                Time = keyTime
                            });
                        }
                    }
                }
            }
            if (_clipInstanceManager != null)
            {
                var clipSampleData = new List<(AbilityConfigSO.Clip, float)>();
                foreach (var clipData in _activeClips)
                {
                    if (clipData.Clip.Type == AbilityConfigSO.ClipType.VFX ||
                        clipData.Clip.Type == AbilityConfigSO.ClipType.SFX)
                    {
                        clipSampleData.Add((clipData.Clip, clipData.LocalTime));
                    }
                }
                _clipInstanceManager.UpdateClips(clipSampleData);
            }
            SceneView.RepaintAll();
        }

        private void CacheTransformStates(Transform root)
        {
            _cachedTransforms[root] = new TransformState
            {
                Position = root.localPosition,
                Rotation = root.localRotation,
                Scale = root.localScale
            };
            for (int i = 0; i < root.childCount; i++)
            {
                CacheTransformStates(root.GetChild(i));
            }
        }

        private void RestoreTransformStates()
        {
            foreach (var kvp in _cachedTransforms)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.localPosition = kvp.Value.Position;
                    kvp.Key.localRotation = kvp.Value.Rotation;
                    kvp.Key.localScale = kvp.Value.Scale;
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isPreviewMode || _previewTarget == null) return;
            foreach (var keyData in _activeKeys)
            {
                DrawKeyGizmo(keyData.Key);
            }
            foreach (var clipData in _activeClips)
            {
                PreviewGizmoDrawer.DrawClip(_previewTarget.transform, clipData.Clip, clipData.LocalTime);
            }
        }

        private void DrawKeyGizmo(AbilityConfigSO.Key key)
        {
            if (key == null) return;
            switch (key.Type)
            {
                case AbilityConfigSO.KeyType.Hitbox:
                    PreviewGizmoDrawer.DrawHitboxKey(_previewTarget.transform, key);
                    break;
                case AbilityConfigSO.KeyType.Signal:
                case AbilityConfigSO.KeyType.Custom:
                case AbilityConfigSO.KeyType.Footstep:
                case AbilityConfigSO.KeyType.AnimationEvent:
                    PreviewGizmoDrawer.DrawMarkerKey(_previewTarget.transform, key);
                    break;
                case AbilityConfigSO.KeyType.CameraShake:
                    PreviewGizmoDrawer.DrawCameraShakeKey(_previewTarget.transform, key);
                    break;
            }
        }
        public void Dispose()
        {
            ExitPreviewMode();
        }
        /// <summary>
        /// 预览Clip实例管理�?- 管理VFX/SFX实例的生命周期和采样
        /// </summary>
        private class PreviewClipInstanceManager
        {
            private Transform _previewTarget;
            private Dictionary<AbilityConfigSO.Clip, ClipInstance> _activeInstances = new Dictionary<AbilityConfigSO.Clip, ClipInstance>();
            public PreviewClipInstanceManager(Transform previewTarget)
            {
                _previewTarget = previewTarget;
            }

            public void UpdateClips(List<(AbilityConfigSO.Clip clip, float localTime)> activeClips)
            {
                var instancesToRemove = new HashSet<AbilityConfigSO.Clip>(_activeInstances.Keys);
                foreach (var (clip, localTime) in activeClips)
                {
                    if (!_activeInstances.TryGetValue(clip, out var instance))
                    {
                        instance = CreateInstance(clip);
                        if (instance != null)
                        {
                            _activeInstances[clip] = instance;
                        }
                    }
                    if (instance != null)
                    {
                        instance.Sample(localTime);
                        instancesToRemove.Remove(clip);
                    }
                }
                foreach (var clip in instancesToRemove)
                {
                    if (_activeInstances.TryGetValue(clip, out var instance))
                    {
                        instance.Destroy();
                        _activeInstances.Remove(clip);
                    }
                }
            }
            public void ClearAll()
            {
                foreach (var instance in _activeInstances.Values)
                {
                    instance.Destroy();
                }
                _activeInstances.Clear();
            }
            private ClipInstance CreateInstance(AbilityConfigSO.Clip clip)
            {
                switch (clip.Type)
                {
                    case AbilityConfigSO.ClipType.VFX:
                        return new VFXClipInstance(_previewTarget, clip);
                    case AbilityConfigSO.ClipType.SFX:
                        return new SFXClipInstance(_previewTarget, clip);
                    default:
                        return null;
                }
            }
            private abstract class ClipInstance
            {
                protected Transform _target;
                protected AbilityConfigSO.Clip _clip;
                protected float _lastSampleTime = -1f;
                protected ClipInstance(Transform target, AbilityConfigSO.Clip clip)
                {
                    _target = target;
                    _clip = clip;
                }
                public abstract void Sample(float localTime);
                public abstract void Destroy();
            }
            private class VFXClipInstance : ClipInstance
            {
                private GameObject _instance;
                private ParticleSystem[] _particleSystems;
                public VFXClipInstance(Transform target, AbilityConfigSO.Clip clip) : base(target, clip)
                {
                    if (clip.VFXPrefab == null) return;
                    _instance = UnityEngine.Object.Instantiate(clip.VFXPrefab, target.position, target.rotation);
                    _instance.transform.SetParent(target);
                    _instance.hideFlags = HideFlags.HideAndDontSave;
                    _particleSystems = _instance.GetComponentsInChildren<ParticleSystem>();
                    foreach (var ps in _particleSystems)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Simulate(0f, true, true);
                    }
                }
                public override void Sample(float localTime)
                {
                    if (_instance == null || _particleSystems == null) return;
                    bool timeJump = Mathf.Abs(localTime - _lastSampleTime) > 0.1f || localTime < _lastSampleTime;
                    if (timeJump)
                    {
                        foreach (var ps in _particleSystems)
                        {
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            ps.Clear(true);
                            ps.Simulate(0f, true, true);
                        }
                    }
                    foreach (var ps in _particleSystems)
                    {
                        ps.Simulate(localTime, true, false, true);
                    }
                    _lastSampleTime = localTime;
                }
                public override void Destroy()
                {
                    if (_instance != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_instance);
                    }
                }
            }
            private class SFXClipInstance : ClipInstance
            {
                private GameObject _audioObject;
                private AudioSource _audioSource;
                private bool _isPlaying = false;
                public SFXClipInstance(Transform target, AbilityConfigSO.Clip clip) : base(target, clip)
                {
                    if (clip.AudioClip == null) return;
                    _audioObject = new GameObject($"SFX_Preview_{clip.AudioClip.name}");
                    _audioObject.transform.SetParent(target);
                    _audioObject.transform.localPosition = Vector3.zero;
                    _audioObject.hideFlags = HideFlags.HideAndDontSave;
                    _audioSource = _audioObject.AddComponent<AudioSource>();
                    _audioSource.clip = clip.AudioClip;
                    _audioSource.loop = false;
                    _audioSource.playOnAwake = false;
                    _audioSource.volume = clip.ParamF.x > 0 ? clip.ParamF.x : 1.0f;
                    _audioSource.pitch = clip.ParamF.y > 0 ? clip.ParamF.y : 1.0f;
                }
                public override void Sample(float localTime)
                {
                    if (_audioSource == null || _audioSource.clip == null) return;
                    float clipDuration = _audioSource.clip.length;
                    bool timeJump = Mathf.Abs(localTime - _lastSampleTime) > 0.1f || localTime < _lastSampleTime;
                    if (timeJump)
                    {
                        _audioSource.Stop();
                        _isPlaying = false;
                    }
                    if (localTime >= 0 && localTime <= clipDuration)
                    {
                        if (!_isPlaying)
                        {
                            _audioSource.time = localTime;
                            _audioSource.Play();
                            _isPlaying = true;
                        }
                        else
                        {
                            if (Mathf.Abs(_audioSource.time - localTime) > 0.05f)
                            {
                                _audioSource.time = Mathf.Clamp(localTime, 0f, clipDuration);
                            }
                        }
                    }
                    else
                    {
                        if (_isPlaying)
                        {
                            _audioSource.Stop();
                            _isPlaying = false;
                        }
                    }
                    _lastSampleTime = localTime;
                }
                public override void Destroy()
                {
                    if (_audioSource != null)
                    {
                        _audioSource.Stop();
                    }
                    if (_audioObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_audioObject);
                    }
                }
            }
        }
    }
}

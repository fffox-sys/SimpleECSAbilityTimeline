#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
    /// <summary>
    /// Timeline 核心控制�?- 协调所有子控制�?
    /// </summary>
    public class TimelineController
    {
        private readonly TimelineState _state;
        private SelectionController _selectionCtrl;
        private PlaybackController _playbackCtrl;
        private EditController _editCtrl;
        private PreviewController _previewCtrl;
        private double _lastEditorTime;

        public TimelineController(TimelineState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SelectionController Selection => _selectionCtrl;
        public PlaybackController Playback => _playbackCtrl;
        public EditController Edit => _editCtrl;
        public PreviewController Preview => _previewCtrl;

        public void Initialize()
        {
            _selectionCtrl = new SelectionController(_state);
            _playbackCtrl = new PlaybackController(_state);
            _editCtrl = new EditController(_state);
            _previewCtrl = new PreviewController(_state, this);

            _lastEditorTime = EditorApplication.timeSinceStartup;
        }

        public void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(now - _lastEditorTime);
            _lastEditorTime = now;

            if (_state.Playback.IsPlaying)
            {
                _playbackCtrl.UpdatePlayback(deltaTime);

                EnsurePlayheadVisible();
            }
            if (_previewCtrl != null && _previewCtrl.IsPreviewMode)
            {
                _previewCtrl.Sample(_state.Playback.Playhead);
            }
        }

        private void EnsurePlayheadVisible()
        {
            float playhead = _state.Playback.Playhead;
            float pps = _state.View.PixelsPerSecond;
            float scrollX = _state.View.ScrollX;
            const float viewportWidth = 800f;
            const float margin = 50f;
            float playheadX = playhead * pps;
            float visibleStart = scrollX;
            float visibleEnd = scrollX + viewportWidth;
            if (playheadX < visibleStart + margin)
            {
                _state.View.ScrollX = Mathf.Max(0f, playheadX - margin);
                _state.NotifyViewChanged();
            }
            else if (playheadX > visibleEnd - margin)
            {
                _state.View.ScrollX = playheadX - viewportWidth + margin;
                _state.NotifyViewChanged();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _selectionCtrl?.Dispose();
            _playbackCtrl?.Dispose();
            _editCtrl?.Dispose();
        }
    }

    public class SelectionController
    {
        private readonly TimelineState _state;

        public SelectionController(TimelineState state)
        {
            _state = state;
        }

        public void SelectKey(int phaseIndex, int trackIndex, int keyIndex)
        {
            _state.Selection.Clear();
            _state.Selection.Kind = TimelineState.SelectionKind.Key;
            _state.Selection.PhaseIndex = phaseIndex;
            _state.Selection.TrackIndex = trackIndex;
            _state.Selection.KeyIndex = keyIndex;
            _state.Selection.MultiSelection.Clear();
            _state.Selection.MultiSelection.Add(new TimelineState.SelItem(phaseIndex, trackIndex, keyIndex));
            _state.NotifySelectionChanged();
        }

        public void SelectClip(int phaseIndex, int trackIndex, int clipIndex)
        {
            _state.Selection.Clear();
            _state.Selection.Kind = TimelineState.SelectionKind.Clip;
            _state.Selection.PhaseIndex = phaseIndex;
            _state.Selection.TrackIndex = trackIndex;
            _state.Selection.ClipIndex = clipIndex;
            _state.NotifySelectionChanged();
        }

        public void SelectPhase(int phaseIndex)
        {
            _state.Selection.Clear();
            _state.Selection.Kind = TimelineState.SelectionKind.Phase;
            _state.Selection.PhaseIndex = phaseIndex;
            _state.NotifySelectionChanged();
        }

        public void SelectTrack(int phaseIndex, int trackIndex)
        {
            _state.Selection.Clear();
            _state.Selection.Kind = TimelineState.SelectionKind.Track;
            _state.Selection.PhaseIndex = phaseIndex;
            _state.Selection.TrackIndex = trackIndex;
            _state.NotifySelectionChanged();
        }

        public void AddToSelection(int phaseIndex, int trackIndex, int keyIndex)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null) return;

            if (_state.Selection.MultiSelection.Count == 0 && _state.Selection.Kind == TimelineState.SelectionKind.Key)
            {
                _state.Selection.MultiSelection.Add(new TimelineState.SelItem(
                    _state.Selection.PhaseIndex, 
                    _state.Selection.TrackIndex, 
                    _state.Selection.KeyIndex));
            }

            var item = new TimelineState.SelItem(phaseIndex, trackIndex, keyIndex);
            _state.Selection.MultiSelection.Add(item);
            _state.Selection.Kind = TimelineState.SelectionKind.Key;
            _state.Selection.PhaseIndex = phaseIndex;
            _state.Selection.TrackIndex = trackIndex;
            _state.Selection.KeyIndex = keyIndex;
            _state.NotifySelectionChanged();
        }

        public void ToggleSelection(int phaseIndex, int trackIndex, int keyIndex)
        {
            var item = new TimelineState.SelItem(phaseIndex, trackIndex, keyIndex);
            if (_state.Selection.MultiSelection.Contains(item))
            {
                _state.Selection.MultiSelection.Remove(item);
            }
            else
            {
                _state.Selection.MultiSelection.Add(item);
            }

            if (_state.Selection.MultiSelection.Count > 0)
            {
                var first = _state.Selection.MultiSelection.GetEnumerator();
                if (first.MoveNext())
                {
                    var it = first.Current;
                    _state.Selection.Kind = TimelineState.SelectionKind.Key;
                    _state.Selection.PhaseIndex = it.P;
                    _state.Selection.TrackIndex = it.T;
                    _state.Selection.KeyIndex = it.K;
                }
            }
            else
            {
                _state.Selection.Clear();
            }

            _state.NotifySelectionChanged();
        }

        public bool IsSelected(int phaseIndex, int trackIndex, int keyIndex)
        {
            if (_state.Selection.Kind == TimelineState.SelectionKind.Key &&
                _state.Selection.PhaseIndex == phaseIndex &&
                _state.Selection.TrackIndex == trackIndex &&
                _state.Selection.KeyIndex == keyIndex)
                return true;

            return _state.Selection.MultiSelection.Contains(new TimelineState.SelItem(phaseIndex, trackIndex, keyIndex));
        }

        public bool IsPrimary(int phaseIndex, int trackIndex, int keyIndex)
        {
            return _state.Selection.Kind == TimelineState.SelectionKind.Key &&
                   _state.Selection.PhaseIndex == phaseIndex &&
                   _state.Selection.TrackIndex == trackIndex &&
                   _state.Selection.KeyIndex == keyIndex;
        }

        public void ClearSelection()
        {
            _state.Selection.Clear();
            _state.NotifySelectionChanged();
        }

        public void Dispose() { }
    }

    public class PlaybackController
    {
        private readonly TimelineState _state;

        public PlaybackController(TimelineState state)
        {
            _state = state;
        }

        public void Initialize()
        {
            _state.Playback.LastEditorTime = EditorApplication.timeSinceStartup;
        }

        public void TogglePlay()
        {
            bool wasPlaying = _state.Playback.IsPlaying;
            _state.Playback.IsPlaying = !_state.Playback.IsPlaying;
            if (wasPlaying && !_state.Playback.IsPlaying)
            {
                float snappedTime = SnapTimeToGrid(_state.Playback.Playhead);
                _state.Playback.Playhead = snappedTime;
                Debug.Log($"[TogglePlay] Paused - Snap playhead: {_state.Playback.Playhead:F4} �?{snappedTime:F4}");
            }
            _state.NotifyPlaybackChanged();
        }

        public void Stop()
        {
            _state.Playback.IsPlaying = false;
            _state.Playback.Playhead = 0f;
            _state.NotifyPlaybackChanged();
        }

        public void SetPlayhead(float time)
        {
            _state.Playback.Playhead = Mathf.Clamp(time, 0f, _state.Data.TotalDuration);
            _state.NotifyPlaybackChanged();
        }

        public void SetPlayheadFromGlobalTime(float globalTime)
        {
            if (_state.View.Snap)
            {
                globalTime = SnapTimeToGrid(globalTime);
            }
            SetPlayhead(globalTime);
        }
        /// <summary>
        /// 将时间吸附到网格刻度
        /// </summary>
        private float SnapTimeToGrid(float time)
        {
            float snapStep = _state.View.FrameMode ? (1f / _state.View.FPS) : _state.View.SnapStep;
            float snappedTime = Mathf.Round(time / snapStep) * snapStep;
            return snappedTime;
        }

        public void UpdatePlayback(float deltaTime)
        {
            _state.Playback.Playhead += deltaTime * _state.Playback.Speed;
            float duration = _state.Data.TotalDuration;

            if (_state.Playback.RangeEnabled)
            {
                _state.EnsureRangeValid();
                float inT = _state.Playback.RangeIn;
                float outT = Mathf.Min(_state.Playback.RangeOut, duration);
                float len = Mathf.Max(0.0001f, outT - inT);

                if (_state.Playback.Playhead > outT)
                {
                    if (_state.Playback.Loop)
                        _state.Playback.Playhead = inT + Mathf.Repeat(_state.Playback.Playhead - inT, len);
                    else
                    {
                        _state.Playback.Playhead = outT;
                        _state.Playback.IsPlaying = false;
                    }
                }
                else if (_state.Playback.Playhead < inT)
                {
                    if (_state.Playback.Loop)
                        _state.Playback.Playhead = inT + Mathf.Repeat(_state.Playback.Playhead - inT, len);
                    else
                    {
                        _state.Playback.Playhead = inT;
                        _state.Playback.IsPlaying = false;
                    }
                }
            }
            else
            {
                if (_state.Playback.Playhead > duration)
                {
                    if (_state.Playback.Loop)
                        _state.Playback.Playhead = Mathf.Repeat(_state.Playback.Playhead, duration);
                    else
                    {
                        _state.Playback.Playhead = duration;
                        _state.Playback.IsPlaying = false;
                    }
                }
            }

            _state.NotifyPlaybackChanged();
        }

        public void Dispose() { }
    }

    public class EditController
    {
        private readonly TimelineState _state;
        private bool _groupDragActive;
        private float _groupDragStartTime;
        private readonly Dictionary<TimelineState.SelItem, float> _groupDragInitialTimes = new Dictionary<TimelineState.SelItem, float>();

        public EditController(TimelineState state)
        {
            _state = state;
        }

        public void StartPanning(Vector2 mousePos)
        {
            _state.Edit.IsPanning = true;
            _state.Edit.LastMousePos = mousePos;
            _state.NotifyEditChanged();
        }

        public void UpdatePanning(Vector2 mousePos, float pixelsPerSecond)
        {
            if (!_state.Edit.IsPanning) return;

            float dx = mousePos.x - _state.Edit.LastMousePos.x;
            _state.View.ScrollX = Mathf.Max(0f, _state.View.ScrollX - dx);
            _state.Edit.LastMousePos = mousePos;
            _state.NotifyViewChanged();
        }

        public void StopPanning()
        {
            _state.Edit.IsPanning = false;
            _state.NotifyEditChanged();
        }

        public void Zoom(float factor, float cursorX, float rulerLeft)
        {
            float oldPps = _state.View.PixelsPerSecond;
            float newPps = Mathf.Clamp(oldPps * factor, 50f, 600f);

            float timeAtCursor = (cursorX - rulerLeft + _state.View.ScrollX) / oldPps;
            float desiredScroll = timeAtCursor * newPps - (cursorX - rulerLeft);

            _state.View.PixelsPerSecond = newPps;
            _state.View.ScrollX = Mathf.Max(0f, desiredScroll);
            _state.NotifyViewChanged();
        }

        public void BeginGroupDrag(float startGlobalTime, AbilityConfigSO config)
        {
            if (_groupDragActive)
            {
                Debug.LogWarning($"[EditController] BeginGroupDrag: Already dragging! Force reset. Old start={_groupDragStartTime:F3}, new start={startGlobalTime:F3}");
                EndGroupDrag();
            }
            var ab = _state.Data.CurrentAbility;
            if (ab == null)
            {
                Debug.LogWarning("[EditController] BeginGroupDrag: No ability!");
                return;
            }

            _groupDragActive = true;
            _groupDragStartTime = startGlobalTime;
            _groupDragInitialTimes.Clear();

            if (_state.Selection.MultiSelection.Count == 0 && _state.Selection.Kind == TimelineState.SelectionKind.Key)
            {
                _state.Selection.MultiSelection.Add(new TimelineState.SelItem(
                    _state.Selection.PhaseIndex,
                    _state.Selection.TrackIndex,
                    _state.Selection.KeyIndex));
            }

            foreach (var it in _state.Selection.MultiSelection)
            {
                if (it.P >= 0 && it.P < ab.Phases.Count)
                {
                    var ph = ab.Phases[it.P];
                    if (it.T >= 0 && it.T < ph.Tracks.Count)
                    {
                        var track = ph.Tracks[it.T];
                        if (it.K >= 0 && it.K < track.Keys.Count)
                        {
                            var key = track.Keys[it.K];
                            _groupDragInitialTimes[it] = key.Time;
                        }
                    }
                }
            }

            Debug.Log($"[EditController] BeginGroupDrag: startTime={startGlobalTime:F3}, keyCount={_groupDragInitialTimes.Count}, multiSelCount={_state.Selection.MultiSelection.Count}");
            Undo.RecordObject(config, "拖动 Key");
        }

        public void UpdateGroupDrag(float curGlobalTime, AbilityConfigSO config)
        {
            if (!_groupDragActive)
            {
                Debug.LogWarning("[EditController] UpdateGroupDrag: Not dragging!");
                return;
            }
            var ab = _state.Data.CurrentAbility;
            if (ab == null)
            {
                Debug.LogWarning("[EditController] UpdateGroupDrag: No ability!");
                return;
            }

            float deltaGlobalTime = curGlobalTime - _groupDragStartTime;
            int updateCount = 0;

            foreach (var kv in _groupDragInitialTimes)
            {
                var it = kv.Key;
                float basePhaseTime = kv.Value;
                if (it.P < 0 || it.P >= ab.Phases.Count) continue;
                var ph = ab.Phases[it.P];
                if (it.T < 0 || it.T >= ph.Tracks.Count) continue;
                var track = ph.Tracks[it.T];
                if (it.K < 0 || it.K >= track.Keys.Count) continue;

                float newPhaseTime = basePhaseTime + deltaGlobalTime;
                float beforeSnap = newPhaseTime;
                if (_state.View.Snap)
                {
                    newPhaseTime = Snap(newPhaseTime);
                }
                newPhaseTime = Mathf.Clamp(newPhaseTime, 0f, Mathf.Max(0f, ph.Duration));
                track.Keys[it.K].Time = newPhaseTime;
                updateCount++;
            }


            EditorUtility.SetDirty(config);
            _state.NotifyDataChanged(); 
        }

        public void EndGroupDrag()
        {
            _groupDragActive = false;
            _groupDragInitialTimes.Clear();
            _state.NotifyChanged();
        }

        public bool IsGroupDragging => _groupDragActive;

        private float WorldXToTime(float worldX) => worldX / Mathf.Max(1f, _state.View.PixelsPerSecond);
        private float Snap(float time)
        {
            if (!_state.View.Snap) return time;
            float step = _state.View.FrameMode ? (1f / _state.View.FPS) : _state.View.SnapStep;
            return Mathf.Round(time / step) * step;
        }

        public void AddPhase(AbilityConfigSO config)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || config == null) return;

            Undo.RecordObject(config, "添加 Phase");
            ab.Phases.Add(new AbilityConfigSO.Phase
            {
                Name = $"Phase {ab.Phases.Count}",
                Color = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.8f, 0.8f, 1f)
            });
            EditorUtility.SetDirty(config);
            _state.NotifyChanged();
        }

        public void DeletePhase(int phaseIndex, AbilityConfigSO config)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || config == null) return;
            if (phaseIndex < 0 || phaseIndex >= ab.Phases.Count) return;

            Undo.RecordObject(config, "删除 Phase");
            ab.Phases.RemoveAt(phaseIndex);
            EditorUtility.SetDirty(config);
            _state.NotifyChanged();
        }

        public void MovePhase(int phaseIndex, int delta, AbilityConfigSO config)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || config == null) return;
            if (phaseIndex < 0 || phaseIndex >= ab.Phases.Count) return;

            int newIndex = phaseIndex + delta;
            if (newIndex < 0 || newIndex >= ab.Phases.Count) return;

            Undo.RecordObject(config, "移动 Phase");
            var phase = ab.Phases[phaseIndex];
            ab.Phases.RemoveAt(phaseIndex);
            ab.Phases.Insert(newIndex, phase);
            EditorUtility.SetDirty(config);
            _state.NotifyChanged();
        }

        public void AddTrackToPhase(int phaseIndex, AbilityConfigSO config)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || config == null) return;
            if (phaseIndex < 0 || phaseIndex >= ab.Phases.Count) return;

            Undo.RecordObject(config, "添加 Track");
            var phase = ab.Phases[phaseIndex];
            phase.Tracks.Add(new AbilityConfigSO.Track
            {
                Name = $"Track {phase.Tracks.Count}",
                Enabled = true
            });
            EditorUtility.SetDirty(config);
            _state.NotifyChanged();
        }

        public void DeleteTrack(int phaseIndex, int trackIndex, AbilityConfigSO config)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || config == null) return;
            if (phaseIndex < 0 || phaseIndex >= ab.Phases.Count) return;

            var phase = ab.Phases[phaseIndex];
            if (trackIndex < 0 || trackIndex >= phase.Tracks.Count) return;

            Undo.RecordObject(config, "删除 Track");
            phase.Tracks.RemoveAt(trackIndex);
            EditorUtility.SetDirty(config);
            _state.NotifyChanged();
        }

        public void MoveTrack(int phaseIndex, int trackIndex, int delta, AbilityConfigSO config)
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || config == null) return;
            if (phaseIndex < 0 || phaseIndex >= ab.Phases.Count) return;

            var phase = ab.Phases[phaseIndex];
            if (trackIndex < 0 || trackIndex >= phase.Tracks.Count) return;

            int newIndex = trackIndex + delta;
            if (newIndex < 0 || newIndex >= phase.Tracks.Count) return;

            Undo.RecordObject(config, "移动 Track");
            var track = phase.Tracks[trackIndex];
            phase.Tracks.RemoveAt(trackIndex);
            phase.Tracks.Insert(newIndex, track);
            EditorUtility.SetDirty(config);
            _state.NotifyChanged();
        }

        public void Dispose() { }
    }
}
#endif

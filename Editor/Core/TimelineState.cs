#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
    /// <summary>
    /// 集中式状态管
    /// </summary>
    public class TimelineState
    {
        public class ViewState
        {
            public float PixelsPerSecond { get; set; } = 200f;
            public float ScrollX { get; set; }
            public float ScrollY { get; set; }
            public bool Snap { get; set; } = true;
            public float SnapStep { get; set; } = 0.01f;
            public bool FrameMode { get; set; }
            public float FPS { get; set; } = 60f;
            public float TrackHeaderWidth { get; set; } = 120f;
        }

        public enum SelectionKind { None, Key, Clip, Track, Phase, Marker }
        public class SelectionState
        {
            public SelectionKind Kind { get; set; } = SelectionKind.None;
            public int PhaseIndex { get; set; } = -1;
            public int TrackIndex { get; set; } = -1;
            public int KeyIndex { get; set; } = -1;
            public int ClipIndex { get; set; } = -1;
            public List<SelItem> MultiSelection { get; } = new();
            public ClipboardData Clipboard { get; set; }
            public bool HasSelection => Kind != SelectionKind.None;
            public bool HasMultiSelection => MultiSelection.Count > 0;
            public void Clear()
            {
                Kind = SelectionKind.None;
                PhaseIndex = TrackIndex = KeyIndex = ClipIndex = -1;
                MultiSelection.Clear();
            }
        }
        public struct SelItem : IEquatable<SelItem>
        {
            public int P, T, K, C;
            public SelItem(int p, int t, int k, int c = -1) { P = p; T = t; K = k; C = c; }
            public bool Equals(SelItem other)
            {
                return P == other.P && T == other.T && K == other.K && C == other.C;
            }
            public override bool Equals(object obj)
            {
                return obj is SelItem other && Equals(other);
            }
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + P;
                    hash = hash * 31 + T;
                    hash = hash * 31 + K;
                    hash = hash * 31 + C;
                    return hash;
                }
            }
            public static bool operator ==(SelItem left, SelItem right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(SelItem left, SelItem right)
            {
                return !left.Equals(right);
            }
        }
        public class ClipboardData
        {
            public List<AbilityConfigSO.Key> Keys { get; set; }
            public float MinTime { get; set; }
        }

        public class PlaybackState
        {
            public bool IsPlaying { get; set; }
            public float Playhead { get; set; }
            public float Speed { get; set; } = 1f;
            public bool Loop { get; set; } = true;
            public bool RangeEnabled { get; set; }
            public float RangeIn { get; set; }
            public float RangeOut { get; set; }
            public GameObject PreviewTarget { get; set; }
            public AnimationClip PreviewClip { get; set; }
            public double LastEditorTime { get; set; }
        }

        public class EditState
        {
            public bool IsPanning { get; set; }
            public Vector2 LastMousePos { get; set; }
            public bool IsScrubbing { get; set; }
            public bool IsDragging { get; set; }
            public int DragKeyIndex { get; set; } = -1;
            public float DragStartWorld { get; set; }
            public Dictionary<SelItem, float> DragOriginalTimes { get; } = new();
            public int DragClipIndex { get; set; } = -1;
            public ClipDragMode ClipDragMode { get; set; } = ClipDragMode.None;
            public float ClipDragStartWorld { get; set; }
            public float ClipStartInit { get; set; }
            public float ClipLengthInit { get; set; }
            public int HoverClipIndex { get; set; } = -1;
            public ClipDragMode HoverClipHandle { get; set; } = ClipDragMode.None;
            public bool IsBoxSelecting { get; set; }
            public Vector2 BoxStart { get; set; }
            public Rect BoxRect { get; set; }
            public bool IsGlobalBoxSelecting { get; set; }
            public Vector2 GlobalBoxStartWorld { get; set; }
            public Rect GlobalBoxWorldRect { get; set; }
            public RangeDragKind RangeDragKind { get; set; } = RangeDragKind.None;
            public float RangeDragUndoIn { get; set; }
            public float RangeDragUndoOut { get; set; }
            public void ClearDragStates()
            {
                IsDragging = false;
                DragKeyIndex = -1;
                DragClipIndex = -1;
                ClipDragMode = ClipDragMode.None;
                IsBoxSelecting = false;
                IsGlobalBoxSelecting = false;
                RangeDragKind = RangeDragKind.None;
            }
        }
        public enum ClipDragMode { None, Move, ResizeL, ResizeR }
        public enum RangeDragKind { None, In, Out }

        public class DataState
        {
            public AbilityConfigSO Config { get; set; }
            public int AbilityIndex { get; set; }
            public int LastFocusPhase { get; set; }
            public int LastFocusTrack { get; set; }
            public AbilityKeyPresetSO PresetAsset { get; set; }
            public AbilityConfigSO.AbilityDef CurrentAbility
            {
                get
                {
                    if (Config == null || Config.Abilities == null || 
                        AbilityIndex < 0 || AbilityIndex >= Config.Abilities.Count)
                        return null;
                    return Config.Abilities[AbilityIndex];
                }
            }
            public float TotalDuration
            {
                get
                {
                    var ab = CurrentAbility;
                    if (ab == null) return 1f;
                    float totalDuration = 0f;
                    foreach (var p in ab.Phases)
                        totalDuration += p.Duration;
                    return Mathf.Max(0.001f, totalDuration);
                }
            }
        }

        public ViewState View { get; } = new();
        public SelectionState Selection { get; } = new();
        public PlaybackState Playback { get; } = new();
        public EditState Edit { get; } = new();
        public DataState Data { get; } = new();

        public event Action OnStateChanged;
        public event Action<ViewState> OnViewChanged;
        public event Action<SelectionState> OnSelectionChanged;
        public event Action<PlaybackState> OnPlaybackChanged;
        public event Action<EditState> OnEditChanged;
        public event Action OnDataChanged;

        public void NotifyChanged()
        {
            OnStateChanged?.Invoke();
        }
        public void NotifyDataChanged()
        {
            OnDataChanged?.Invoke();
        }
        public void NotifyViewChanged()
        {
            OnViewChanged?.Invoke(View);
        }
        public void NotifySelectionChanged()
        {
            OnSelectionChanged?.Invoke(Selection);
        }
        public void NotifyPlaybackChanged()
        {
            OnPlaybackChanged?.Invoke(Playback);
        }
        public void NotifyEditChanged()
        {
            OnEditChanged?.Invoke(Edit);
            NotifyChanged();
        }

        /// <summary>
        /// 计算 Phase 的起始偏移时间（顺序执行，累加前面所有Phase的Duration�?
        /// </summary>
        public float GetPhaseStartOffset(int phaseIndex)
        {
            var ab = Data.CurrentAbility;
            if (ab == null || phaseIndex < 0 || phaseIndex >= ab.Phases.Count)
                return 0f;
            float offset = 0f;
            for (int i = 0; i < phaseIndex; i++)
            {
                offset += ab.Phases[i].Duration;
            }
            return offset;
        }
        /// <summary>
        /// 确保 Range 有效（In <= Out�?
        /// </summary>
        public void EnsureRangeValid()
        {
            float dur = Data.TotalDuration;
            Playback.RangeIn = Mathf.Clamp(Playback.RangeIn, 0f, dur);
            Playback.RangeOut = Mathf.Clamp(Playback.RangeOut, 0f, dur);
            if (Playback.RangeOut < Playback.RangeIn)
            {
                (Playback.RangeIn, Playback.RangeOut) = (Playback.RangeOut, Playback.RangeIn);
            }
        }
        /// <summary>
        /// 确保 Playhead �?Range �?
        /// </summary>
        public void EnsurePlayheadInRange()
        {
            if (!Playback.RangeEnabled) return;
            EnsureRangeValid();
            Playback.Playhead = Mathf.Clamp(
                Playback.Playhead, 
                Playback.RangeIn, 
                Mathf.Min(Playback.RangeOut, Data.TotalDuration)
            );
        }
    }
}
#endif

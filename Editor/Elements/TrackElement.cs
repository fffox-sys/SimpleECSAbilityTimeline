#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
    /// <summary>
    /// Track 头部元素 - 显示Track名称和控制按�?
    /// </summary>
    public class TrackElement : VisualElement
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private readonly int _phaseIndex;
        private readonly int _trackIndex;
        private Label _nameLabel;
        private VisualElement _statusButtonContainer;

        public TrackElement(TimelineState state, TimelineController controller, int phaseIndex, int trackIndex)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _phaseIndex = phaseIndex;
            _trackIndex = trackIndex;

            style.height = 36;
            style.backgroundColor = new Color(0.09f, 0.09f, 0.09f, 1f);
            style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            style.borderBottomWidth = 1;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 8;

            _nameLabel = new Label
            {
                style =
                {
                    color = new Color(0.9f, 0.9f, 0.9f, 1f),
                    fontSize = 11,
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            Add(_nameLabel);

            _statusButtonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginRight = 4
                }
            };
            Add(_statusButtonContainer);

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<ContextClickEvent>(OnContextClick);
            _state.OnStateChanged += UpdateUI;
            UpdateUI();
        }

        private void UpdateUI()
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null || _phaseIndex < 0 || _phaseIndex >= ability.Phases.Count)
            {
                _nameLabel.text = "无效的 Track";
                return;
            }

            var phase = ability.Phases[_phaseIndex];
            if (_trackIndex < 0 || _trackIndex >= phase.Tracks.Count)
            {
                _nameLabel.text = "无效的 Track";
                return;
            }

            var track = phase.Tracks[_trackIndex];
            string trackName = string.IsNullOrEmpty(track.Name) ? $"Track {_trackIndex}" : track.Name;
            _nameLabel.text = $"{trackName} ({track.Keys.Count} keys)";
            bool isSelected = _state.Selection.Kind == TimelineState.SelectionKind.Track &&
                             _state.Selection.PhaseIndex == _phaseIndex &&
                             _state.Selection.TrackIndex == _trackIndex;
            style.backgroundColor = isSelected 
                ? new Color(0.3f, 0.5f, 0.8f, 0.3f)
                : new Color(0.09f, 0.09f, 0.09f, 1f);
            BuildStatusButtons(track);
        }

        private void BuildStatusButtons(AbilityConfigSO.Track track)
        {
            _statusButtonContainer.Clear();

            var muteBtn = CreateStatusButton("M", track.Muted, new Color(1f, 0.3f, 0.3f), () =>
            {
                Undo.RecordObject(_state.Data.Config, "切换mute");
                track.Muted = !track.Muted;
                if (track.Muted && track.Solo)
                    track.Solo = false;
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
            });
            muteBtn.tooltip = track.Muted ? "取消mute Track" : "mute Track";
            _statusButtonContainer.Add(muteBtn);

            var soloBtn = CreateStatusButton("S", track.Solo, new Color(1f, 1f, 0.3f), () =>
            {
                Undo.RecordObject(_state.Data.Config, "切换 Solo");
                track.Solo = !track.Solo;
                if (track.Solo && track.Muted)
                    track.Muted = false;
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
            });
            soloBtn.tooltip = track.Solo ? "取消 Solo Track" : "Solo Track";
            _statusButtonContainer.Add(soloBtn);

            var lockBtn = CreateStatusButton("L", track.Locked, new Color(1f, 0.6f, 0.2f), () =>
            {
                Undo.RecordObject(_state.Data.Config, "切换锁定");
                track.Locked = !track.Locked;
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
            });
            lockBtn.tooltip = track.Locked ? "解锁 Track" : "锁定 Track";
            _statusButtonContainer.Add(lockBtn);
        }

        private Button CreateStatusButton(string text, bool isActive, Color activeColor, Action onClick)
        {
            Color inactiveColor = new Color(0.3f, 0.3f, 0.3f);
            Color currentColor = isActive ? activeColor : inactiveColor;
            var btn = new Button(onClick)
            {
                text = text,
                style =
                {
                    width = 20,
                    height = 20,
                    marginLeft = 2,
                    marginRight = 0,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = currentColor,
                    color = Color.white,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0
                }
            };
            return btn;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                _controller.Selection.SelectTrack(_phaseIndex, _trackIndex);
                evt.StopPropagation();
            }
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            var menu = new GenericMenu();
            var ability = _state.Data.CurrentAbility;
            var phase = ability?.Phases[_phaseIndex];
            var track = phase?.Tracks[_trackIndex];
            if (phase != null && track != null)
            {
                menu.AddItem(new GUIContent("在开头添加 Key"), false, () =>
                {
                    Undo.RecordObject(_state.Data.Config, "添加 Key");
                    track.Keys.Add(new AbilityConfigSO.Key
                    {
                        Time = 0f,
                        Type = AbilityConfigSO.KeyType.Signal
                    });
                    EditorUtility.SetDirty(_state.Data.Config);
                    _state.NotifyChanged();
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("删除 Track"), false, () =>
                {
                    if (EditorUtility.DisplayDialog("删除 Track",
                        $"确定要删除 Track {_trackIndex}?",
                        "删除", "取消"))
                    {
                        _controller.Edit.DeleteTrack(_phaseIndex, _trackIndex, _state.Data.Config);
                    }
                });
                menu.AddSeparator("");
                bool canMoveUp = _trackIndex > 0;
                bool canMoveDown = _trackIndex < phase.Tracks.Count - 1;
                if (canMoveUp)
                    menu.AddItem(new GUIContent("上移"), false, () =>
                    {
                        _controller.Edit.MoveTrack(_phaseIndex, _trackIndex, -1, _state.Data.Config);
                    });
                else
                    menu.AddDisabledItem(new GUIContent("上移"));
                if (canMoveDown)
                    menu.AddItem(new GUIContent("下移"), false, () =>
                    {
                        _controller.Edit.MoveTrack(_phaseIndex, _trackIndex, 1, _state.Data.Config);
                    });
                else
                    menu.AddDisabledItem(new GUIContent("下移"));
            }
            menu.ShowAsContext();
        }

        public void Refresh()
        {
            MarkDirtyRepaint();
        }
    }

    /// <summary>
    /// Track 行元�?- 显示Keys和Clips的时间轴
    /// </summary>
    public class TrackRowElement : VisualElement
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private readonly int _phaseIndex;
        private readonly int _trackIndex;

        private bool _isDragging;
        private int _dragKeyIndex = -1;
        private bool _isDraggingClip;
        private int _dragClipIndex = -1;
        private float _clipDragStartTime;
        private float _clipDragMouseStartX;
        private bool _isBoxSelecting;
        private bool _pendingClick;
        private Vector2 _pendingDownLocal;
        private Vector2 _boxStart;
        private Rect _boxRect;
        private const float ClickToDragThreshold = 3f;
        private readonly int _instanceId;
        private static int _nextInstanceId = 1;

        public TrackRowElement(TimelineState state, TimelineController controller, int phaseIndex, int trackIndex)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _phaseIndex = phaseIndex;
            _trackIndex = trackIndex;
            _instanceId = _nextInstanceId++;

            style.height = 36;
            style.backgroundColor = new Color(0.11f, 0.11f, 0.11f, 1f);
            style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            style.borderBottomWidth = 1;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            RegisterCallback<ContextClickEvent>(OnContextClick);
            _state.OnSelectionChanged += OnSelectionChangedRepaint;
            _state.OnDataChanged += OnDataChangedRepaint;
        }
        ~TrackRowElement()
        {
        }
        private void OnSelectionChangedRepaint(TimelineState.SelectionState _)
        {
            MarkDirtyRepaint();
        }
        private void OnDataChangedRepaint()
        {
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null || _phaseIndex < 0 || _phaseIndex >= ability.Phases.Count)
                return;

            var phase = ability.Phases[_phaseIndex];
            if (_trackIndex < 0 || _trackIndex >= phase.Tracks.Count)
                return;

            var track = phase.Tracks[_trackIndex];
            var rect = contentRect;
            var painter = ctx.painter2D;

            Color trackBaseColor = track.Color;
            if (trackBaseColor == Color.white)
            {
                trackBaseColor = AbilityConfigSO.Track.GetDefaultColor(track.Type);
            }
            Color bgColor = new Color(
                trackBaseColor.r * 0.15f, 
                trackBaseColor.g * 0.15f, 
                trackBaseColor.b * 0.15f, 
                1f
            );
            painter.fillColor = bgColor;
            painter.BeginPath();
            painter.MoveTo(rect.min);
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(rect.max);
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();

            float phaseStartOffset = _state.GetPhaseStartOffset(_phaseIndex);
            float pps = _state.View.PixelsPerSecond;
            float scrollX = _state.View.ScrollX;

            DrawTimelineGrid(ctx, rect, phaseStartOffset, phase.Duration, pps, scrollX);

            foreach (var clip in track.Clips)
            {
                float clipStart = phaseStartOffset + clip.Start;
                float clipEnd = clipStart + clip.Duration;
                float xStart = (clipStart * pps) - scrollX;
                float xEnd = (clipEnd * pps) - scrollX;
                if (xEnd >= 0 && xStart <= rect.width)
                {
                    bool isSelected = (_state.Selection.Kind == TimelineState.SelectionKind.Clip &&
                                      _state.Selection.PhaseIndex == _phaseIndex &&
                                      _state.Selection.TrackIndex == _trackIndex &&
                                      _state.Selection.ClipIndex == track.Clips.IndexOf(clip));
                    float clipY = 8f;
                    float clipHeight = rect.height - 16f;
                    Rect clipRect = new Rect(xStart, clipY, xEnd - xStart, clipHeight);
                    Color clipColor = clip.Type switch
                    {
                        AbilityConfigSO.ClipType.Animation => new Color(0.3f, 0.7f, 0.4f, 0.6f),
                        AbilityConfigSO.ClipType.SFX => new Color(0.9f, 0.6f, 0.3f, 0.6f),
                        AbilityConfigSO.ClipType.VFX => new Color(0.6f, 0.4f, 0.9f, 0.6f),
                        AbilityConfigSO.ClipType.Custom => new Color(0.5f, 0.5f, 0.5f, 0.6f),
                        _ => new Color(0.5f, 0.5f, 0.5f, 0.6f)
                    };
                    if (isSelected)
                    {
                        clipColor = new Color(clipColor.r * 1.5f, clipColor.g * 1.5f, clipColor.b * 1.5f, 0.8f);
                    }
                    painter.fillColor = clipColor;
                    painter.BeginPath();
                    painter.MoveTo(clipRect.min);
                    painter.LineTo(new Vector2(clipRect.xMax, clipRect.yMin));
                    painter.LineTo(clipRect.max);
                    painter.LineTo(new Vector2(clipRect.xMin, clipRect.yMax));
                    painter.ClosePath();
                    painter.Fill();
                    painter.strokeColor = new Color(clipColor.r, clipColor.g, clipColor.b, 1f);
                    painter.lineWidth = isSelected ? 2f : 1f;
                    painter.BeginPath();
                    painter.MoveTo(clipRect.min);
                    painter.LineTo(new Vector2(clipRect.xMax, clipRect.yMin));
                    painter.LineTo(clipRect.max);
                    painter.LineTo(new Vector2(clipRect.xMin, clipRect.yMax));
                    painter.ClosePath();
                    painter.Stroke();
                    if (clipRect.width > 40f)
                    {
                    }
                }
            }

            foreach (var key in track.Keys)
            {
                float globalTime = phaseStartOffset + key.Time;
                float x = (globalTime * pps) - scrollX;

                if (x >= -10 && x <= rect.width + 10)
                {
                    bool isSelected = _controller.Selection.IsSelected(_phaseIndex, _trackIndex, track.Keys.IndexOf(key));
                    Vector2 position = new Vector2(x, rect.center.y);
                    KeyDrawer.DrawKey(painter, key, position, isSelected, 8f);
                }
            }

            DrawStatusBar(ctx, rect, track);

        }

       
        private void DrawStatusBar(MeshGenerationContext ctx, Rect rect, AbilityConfigSO.Track track)
        {
            var painter = ctx.painter2D;
            float barHeight = 3f;
            float barY = rect.yMax - barHeight;

            Color statusColor;
            bool hasStatus = false;

            if (track.Muted)
            {
                statusColor = new Color(1f, 0.3f, 0.3f, 0.9f);
                hasStatus = true;
            }
            else if (track.Solo)
            {
                statusColor = new Color(1f, 1f, 0.3f, 0.9f);
                hasStatus = true;
            }
            else if (track.Locked)
            {
                statusColor = new Color(1f, 0.6f, 0.2f, 0.9f);
                hasStatus = true;
            }
            else
            {
                statusColor = new Color(0.3f, 1f, 0.3f, 0.9f);
                hasStatus = true;
            }

            if (!hasStatus)
                return;

            painter.fillColor = statusColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, barY));
            painter.LineTo(new Vector2(rect.xMax, barY));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;

            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];

            if (track.Locked)
            {
                evt.StopPropagation();
                return;
            }

            int hitKey = HitTestKey(evt.localMousePosition);
            int hitClip = HitTestClip(evt.localMousePosition);

            if (hitKey >= 0)
            {
                bool isAlreadySelected = _controller.Selection.IsSelected(_phaseIndex, _trackIndex, hitKey);
                if (evt.ctrlKey || evt.commandKey)
                {
                    _controller.Selection.ToggleSelection(_phaseIndex, _trackIndex, hitKey);
                }
                else if (evt.shiftKey)
                {
                    _controller.Selection.AddToSelection(_phaseIndex, _trackIndex, hitKey);
                }
                else
                {
                    if (!isAlreadySelected)
                    {
                        _controller.Selection.SelectKey(_phaseIndex, _trackIndex, hitKey);
                    }
                }

                _isDragging = true;
                _dragKeyIndex = hitKey;
                this.CaptureMouse();
                float startGlobalTime = PixelToGlobalTime(evt.localMousePosition.x);
                _controller.Edit.BeginGroupDrag(startGlobalTime, _state.Data.Config);
                evt.StopPropagation();
                return;
            }

            if (evt.clickCount == 2)
            {
                float localTime = PixelToPhaseTime(evt.localMousePosition.x);
                if (_state.View.Snap) localTime = Snap(localTime);
                localTime = Mathf.Clamp(localTime, 0, Mathf.Max(0f, phase.Duration));

                Undo.RecordObject(_state.Data.Config, "添加 Key");
                track.Keys.Add(new AbilityConfigSO.Key 
                { 
                    Time = localTime,
                    Type = AbilityConfigSO.KeyType.Hitbox
                });
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
                evt.StopPropagation();
                return;
            }

            if (hitKey < 0 && hitClip >= 0)
            {
                _controller.Selection.SelectClip(_phaseIndex, _trackIndex, hitClip);
                _isDraggingClip = true;
                _dragClipIndex = hitClip;
                _clipDragStartTime = track.Clips[hitClip].Start;
                _clipDragMouseStartX = evt.localMousePosition.x;
                this.CaptureMouse();
                evt.StopPropagation();
                return;
            }

            if (hitKey < 0 && hitClip < 0)
            {
                _pendingClick = true;
                _pendingDownLocal = evt.localMousePosition;
                this.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging || _isBoxSelecting || _pendingClick)
            {
                Debug.Log($"[TrackRow] OnMouseMove ENTER: _isDragging={_isDragging}, _isBoxSelecting={_isBoxSelecting}, _pendingClick={_pendingClick}");
            }

            if (_isBoxSelecting)
            {
                Vector2 cur = evt.localMousePosition;
                _boxRect = Rect.MinMaxRect(
                    Mathf.Min(_boxStart.x, cur.x), Mathf.Min(_boxStart.y, cur.y),
                    Mathf.Max(_boxStart.x, cur.x), Mathf.Max(_boxStart.y, cur.y));
                MarkDirtyRepaint();
                evt.StopPropagation();
                return;
            }

            if (_pendingClick)
            {
                var cur = evt.localMousePosition;
                float dist = Mathf.Abs(cur.x - _pendingDownLocal.x) + Mathf.Abs(cur.y - _pendingDownLocal.y);
                if (dist > ClickToDragThreshold)
                {
                    Debug.Log($"[TrackRow] PendingClick -> BoxSelect: dist={dist:F1}, capturing mouse");
                    _pendingClick = false;
                    _isBoxSelecting = true;
                    _boxStart = _pendingDownLocal;
                    _boxRect = new Rect(_boxStart, Vector2.zero);
                    this.CaptureMouse();
                    MarkDirtyRepaint();
                    evt.StopPropagation();
                    return;
                }
            }

            if (_isDraggingClip && _dragClipIndex >= 0)
            {
                var ability = _state.Data.CurrentAbility;
                if (ability != null)
                {
                    var phase = ability.Phases[_phaseIndex];
                    var track = phase.Tracks[_trackIndex];
                    if (_dragClipIndex < track.Clips.Count)
                    {
                        var clip = track.Clips[_dragClipIndex];
                        float deltaX = evt.localMousePosition.x - _clipDragMouseStartX;
                        float pps = _state.View.PixelsPerSecond;
                        float deltaTime = deltaX / pps;
                        float newStart = _clipDragStartTime + deltaTime;
                        if (_state.View.Snap)
                        {
                            float snapStep = _state.View.FrameMode ? (1f / _state.View.FPS) : _state.View.SnapStep;
                            newStart = Mathf.Round(newStart / snapStep) * snapStep;
                        }
                        float clipDuration = clip.Duration;
                        newStart = Mathf.Clamp(newStart, 0f, Mathf.Max(0f, phase.Duration - clipDuration));
                        if (Mathf.Abs(clip.Start - newStart) > 0.0001f)
                        {
                            clip.Start = newStart;
                            MarkDirtyRepaint();
                        }
                    }
                }
                evt.StopPropagation();
                return;
            }

            if (_isDragging && _dragKeyIndex >= 0)
            {
                float curGlobalTime = PixelToGlobalTime(evt.localMousePosition.x);
                _controller.Edit.UpdateGroupDrag(curGlobalTime, _state.Data.Config);
                evt.StopPropagation();
                return;
            }
            else if (_isDragging || _dragKeyIndex >= 0)
            {
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (_isDraggingClip && _dragClipIndex >= 0)
            {
                _isDraggingClip = false;
                _dragClipIndex = -1;
                this.ReleaseMouse();
                Undo.RecordObject(_state.Data.Config, "移动 Clip");
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
                evt.StopPropagation();
                return;
            }
            if (_isBoxSelecting)
            {
                _isBoxSelecting = false;
                var rect = _boxRect;
                _boxRect = Rect.zero;
                this.ReleaseMouse();
                MarkDirtyRepaint();

                var hitKeys = BoxHitTest(rect);

                if (!evt.shiftKey && !evt.actionKey)
                {
                    _controller.Selection.ClearSelection();
                    if (hitKeys.Count > 0)
                        _controller.Selection.SelectKey(_phaseIndex, _trackIndex, hitKeys[0]);
                    for (int i = 1; i < hitKeys.Count; i++)
                        _controller.Selection.AddToSelection(_phaseIndex, _trackIndex, hitKeys[i]);
                }
                else if (evt.shiftKey)
                {
                    foreach (var i in hitKeys)
                        _controller.Selection.AddToSelection(_phaseIndex, _trackIndex, i);
                }
                else if (evt.actionKey)
                {
                    foreach (var i in hitKeys)
                        _controller.Selection.ToggleSelection(_phaseIndex, _trackIndex, i);
                }

                evt.StopPropagation();
                return;
            }

            if (_pendingClick && evt.button == 0)
            {
                _pendingClick = false;
                this.ReleaseMouse();
                float globalTime = PixelToGlobalTime(evt.localMousePosition.x);
                _controller.Playback.SetPlayheadFromGlobalTime(globalTime);
                evt.StopPropagation();
                return;
            }

            if (_isDragging)
            {
                Debug.Log($"[TrackRow] OnMouseUp: Ending drag, releasing mouse capture");
                _isDragging = false;
                _dragKeyIndex = -1;
                this.ReleaseMouse();
                _controller.Edit.EndGroupDrag();
                evt.StopPropagation();
                return;
            }
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            int hitKey = HitTestKey(evt.localMousePosition);
            int hitClip = HitTestClip(evt.localMousePosition);
            var menu = new GenericMenu();

            if (hitKey >= 0)
            {
                menu.AddItem(new GUIContent("选择 Key"), false, () =>
                {
                    _controller.Selection.SelectKey(_phaseIndex, _trackIndex, hitKey);
                });
                menu.AddItem(new GUIContent("删除 Key"), false, () =>
                {
                    DeleteKey(hitKey);
                });
                menu.AddItem(new GUIContent("复制 Key"), false, () =>
                {
                    DuplicateKey(hitKey);
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("剪切"), false, () => { /* TODO: Clipboard */ });
                menu.AddItem(new GUIContent("拷贝"), false, () => { /* TODO: Clipboard */ });
            }
            else if (hitClip >= 0)
            {
                menu.AddItem(new GUIContent("选择 Clip"), false, () =>
                {
                    _controller.Selection.SelectClip(_phaseIndex, _trackIndex, hitClip);
                });
                menu.AddItem(new GUIContent("删除 Clip"), false, () =>
                {
                    DeleteClip(hitClip);
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("拷贝 Clip"), false, () => { /* TODO: Clipboard */ });
            }
            else
            {
                menu.AddItem(new GUIContent("在此处添加 Key"), false, () =>
                {
                    AddKeyAtPosition(evt.localMousePosition);
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("添加 Clip/动画 Clip"), false, () =>
                {
                    AddClipAtPosition(evt.localMousePosition, AbilityConfigSO.ClipType.Animation);
                });
                menu.AddItem(new GUIContent("添加 Clip/音效 Clip"), false, () =>
                {
                    AddClipAtPosition(evt.localMousePosition, AbilityConfigSO.ClipType.SFX);
                });
                menu.AddItem(new GUIContent("添加 Clip/特效 Clip"), false, () =>
                {
                    AddClipAtPosition(evt.localMousePosition, AbilityConfigSO.ClipType.VFX);
                });
                menu.AddItem(new GUIContent("添加 Clip/自定义 Clip"), false, () =>
                {
                    AddClipAtPosition(evt.localMousePosition, AbilityConfigSO.ClipType.Custom);
                });
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("粘贴"), false, () => { /* TODO: Clipboard */ });
            }

            menu.ShowAsContext();
        }


        private void DrawTimelineGrid(MeshGenerationContext ctx, Rect rect, float phaseStartOffset, float phaseDuration, float pps, float scrollX)
        {
            var painter = ctx.painter2D;
            float phaseStartX = (phaseStartOffset * pps) - scrollX;
            float phaseEndX = ((phaseStartOffset + phaseDuration) * pps) - scrollX;
            if (phaseEndX < 0 || phaseStartX > rect.width)
                return;
            float majorStep, minorStep;
            if (_state.View.FrameMode)
            {
                float fps = _state.View.FPS;
                majorStep = 1f;
                minorStep = 1f / fps;
            }
            else
            {
                if (pps < 50f)
                {
                    majorStep = 10f;
                    minorStep = 1f;
                }
                else if (pps < 100f)
                {
                    majorStep = 5f;
                    minorStep = 1f;
                }
                else if (pps < 200f)
                {
                    majorStep = 1f;
                    minorStep = 0.1f;
                }
                else if (pps < 400f)
                {
                    majorStep = 0.5f;
                    minorStep = 0.1f;
                }
                else if (pps < 800f)
                {
                    majorStep = 0.1f;
                    minorStep = 0.01f;
                }
                else if (pps < 1600f)
                {
                    majorStep = 0.05f;
                    minorStep = 0.01f;
                }
                else if (pps < 3200f)
                {
                    majorStep = 0.02f;
                    minorStep = 0.01f;
                }
                else
                {
                    majorStep = 0.01f;
                    minorStep = 0f;
                }
            }
            if (minorStep > 0.0001f)
            {
                float phaseStartTime = phaseStartOffset;
                float phaseEndTime = phaseStartOffset + phaseDuration;
                int startMinorIndex = Mathf.FloorToInt(phaseStartTime / minorStep);
                int endMinorIndex = Mathf.CeilToInt(phaseEndTime / minorStep);
                for (int i = startMinorIndex; i <= endMinorIndex; i++)
                {
                    float globalTime = i * minorStep;
                    if (globalTime < phaseStartTime - 0.0001f || globalTime > phaseEndTime + 0.0001f)
                        continue;
                    float x = (globalTime * pps) - scrollX;
                    if (x < -1 || x > rect.width + 1) continue;
                    bool isMajor = Mathf.Abs(globalTime % majorStep) < 0.0001f;
                    if (isMajor) continue;
                    painter.strokeColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
                    painter.lineWidth = 1f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, rect.yMin));
                    painter.LineTo(new Vector2(x, rect.yMax));
                    painter.Stroke();
                }
            }
            {
                float phaseStartTime = phaseStartOffset;
                float phaseEndTime = phaseStartOffset + phaseDuration;
                int startMajorIndex = Mathf.FloorToInt(phaseStartTime / majorStep);
                int endMajorIndex = Mathf.CeilToInt(phaseEndTime / majorStep);
                for (int i = startMajorIndex; i <= endMajorIndex; i++)
                {
                    float globalTime = i * majorStep;
                    if (globalTime < phaseStartTime - 0.0001f || globalTime > phaseEndTime + 0.0001f)
                        continue;
                    float x = (globalTime * pps) - scrollX;
                    if (x < -1 || x > rect.width + 1) continue;
                    painter.strokeColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
                    painter.lineWidth = 1.5f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, rect.yMin));
                    painter.LineTo(new Vector2(x, rect.yMax));
                    painter.Stroke();
                }
            }
        }

        private int HitTestKey(Vector2 localPos)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return -1;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];
            float phaseStartOffset = _state.GetPhaseStartOffset(_phaseIndex);
            float pps = _state.View.PixelsPerSecond;
            float scrollX = _state.View.ScrollX;
            float keyWidth = 8f;

            for (int i = 0; i < track.Keys.Count; i++)
            {
                float globalTime = phaseStartOffset + track.Keys[i].Time;
                float x = (globalTime * pps) - scrollX;

                if (Mathf.Abs(localPos.x - x) <= keyWidth * 0.5f + 2f)
                    return i;
            }

            return -1;
        }

        private int HitTestClip(Vector2 localPos)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return -1;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];
            float phaseStartOffset = _state.GetPhaseStartOffset(_phaseIndex);
            float pps = _state.View.PixelsPerSecond;
            float scrollX = _state.View.ScrollX;
            var rect = contentRect;

            for (int i = 0; i < track.Clips.Count; i++)
            {
                var clip = track.Clips[i];
                float clipStart = phaseStartOffset + clip.Start;
                float clipEnd = clipStart + clip.Duration;
                float xStart = (clipStart * pps) - scrollX;
                float xEnd = (clipEnd * pps) - scrollX;
                float clipY = 8f;
                float clipHeight = rect.height - 16f;
                Rect clipRect = new Rect(xStart, clipY, xEnd - xStart, clipHeight);

                if (clipRect.Contains(localPos))
                    return i;
            }

            return -1;
        }

        private System.Collections.Generic.List<int> BoxHitTest(Rect rect)
        {
            var hitKeys = new System.Collections.Generic.List<int>();
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return hitKeys;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];
            float phaseStartOffset = _state.GetPhaseStartOffset(_phaseIndex);
            float pps = _state.View.PixelsPerSecond;
            float scrollX = _state.View.ScrollX;
            var size = new Vector2(8, 16);

            for (int i = 0; i < track.Keys.Count; i++)
            {
                float globalTime = phaseStartOffset + track.Keys[i].Time;
                float x = (globalTime * pps) - scrollX;
                var center = new Vector2(x, contentRect.center.y);
                var keyRect = new Rect(center - size * 0.5f, size);

                if (keyRect.Overlaps(rect))
                    hitKeys.Add(i);
            }

            return hitKeys;
        }

        private float PixelToPhaseTime(float pixelX)
        {
            float phaseStartOffset = _state.GetPhaseStartOffset(_phaseIndex);
            float scrollX = _state.View.ScrollX;
            float pps = _state.View.PixelsPerSecond;
            float globalTime = (pixelX + scrollX) / pps;
            return globalTime - phaseStartOffset;
        }

        private float PixelToGlobalTime(float pixelX)
        {
            float scrollX = _state.View.ScrollX;
            float pps = _state.View.PixelsPerSecond;
            return (pixelX + scrollX) / pps;
        }

        private float Snap(float time)
        {
            if (!_state.View.Snap) return time;
            float step = _state.View.FrameMode ? (1f / _state.View.FPS) : _state.View.SnapStep;
            return Mathf.Round(time / step) * step;
        }

        private void DeleteKey(int keyIndex)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];

            if (keyIndex >= 0 && keyIndex < track.Keys.Count)
            {
                Undo.RecordObject(_state.Data.Config, "删除 Key");
                track.Keys.RemoveAt(keyIndex);
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
            }
        }

        private void DeleteClip(int clipIndex)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];

            if (clipIndex >= 0 && clipIndex < track.Clips.Count)
            {
                Undo.RecordObject(_state.Data.Config, "删除 Clip");
                track.Clips.RemoveAt(clipIndex);
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
                if (_state.Selection.Kind == TimelineState.SelectionKind.Clip &&
                    _state.Selection.PhaseIndex == _phaseIndex &&
                    _state.Selection.TrackIndex == _trackIndex &&
                    _state.Selection.ClipIndex == clipIndex)
                {
                    _controller.Selection.ClearSelection();
                }
            }
        }

        private void DuplicateKey(int keyIndex)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];

            if (keyIndex >= 0 && keyIndex < track.Keys.Count)
            {
                Undo.RecordObject(_state.Data.Config, "复制 Key");
                var original = track.Keys[keyIndex];
                var duplicate = new AbilityConfigSO.Key
                {
                    Time = original.Time + 0.1f,
                    Type = original.Type,
                    EventName = original.EventName,
                    StringParam = original.StringParam,
                    ParamF = original.ParamF,
                    IntParam = original.IntParam,
                    EditorColor = original.EditorColor
                };
                track.Keys.Add(duplicate);
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
            }
        }

        private void AddKeyAtPosition(Vector2 localPos)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];
            var allowedKeys = TrackTypeRules.GetAllowedKeyTypes(track.Type);
            if (allowedKeys.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "无法添加 Key",
                    $"Track type '{track.Type}' does not allow Keys.\n{TrackTypeRules.GetRestrictionInfo(track.Type)}",
                    "确定"
                );
                return;
            }
            float localTime = PixelToPhaseTime(localPos.x);
            if (_state.View.Snap) localTime = Snap(localTime);
            localTime = Mathf.Clamp(localTime, 0, Mathf.Max(0f, phase.Duration));

            Undo.RecordObject(_state.Data.Config, "添加 Key");
            track.Keys.Add(new AbilityConfigSO.Key
            {
                Time = localTime,
                Type = TrackTypeRules.GetDefaultKeyType(track.Type)
            });
            EditorUtility.SetDirty(_state.Data.Config);
            _state.NotifyChanged();
        }

        private void AddClipAtPosition(Vector2 localPos, AbilityConfigSO.ClipType clipType)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var phase = ability.Phases[_phaseIndex];
            var track = phase.Tracks[_trackIndex];
            if (!TrackTypeRules.CanAddClip(track.Type, clipType))
            {
                EditorUtility.DisplayDialog(
                    "无法添加 Clip",
                    $"Track type '{track.Type}' does not allow '{clipType}' clips.\n{TrackTypeRules.GetRestrictionInfo(track.Type)}",
                    "确定"
                );
                return;
            }
            float localTime = PixelToPhaseTime(localPos.x);
            if (_state.View.Snap) localTime = Snap(localTime);
            localTime = Mathf.Clamp(localTime, 0, Mathf.Max(0f, phase.Duration));

            var tempClip = new AbilityConfigSO.Clip
            {
                Type = clipType,
                Start = localTime
            };
            switch (clipType)
            {
                case AbilityConfigSO.ClipType.Custom:
                    tempClip.ParamF = new Vector4(0.25f, 0, 0, 0);
                    break;
            }
            float clipEnd = tempClip.Start + tempClip.Duration;
            if (clipEnd > phase.Duration)
            {
                Debug.LogWarning($"[TrackRow] Cannot add Clip: would exceed phase duration ({clipEnd:F2}s > {phase.Duration:F2}s)");
                EditorUtility.DisplayDialog(
                    "无法添加 Clip", 
                    $"Clip 超出 phase 持续时间。\nClip 结束时间: {clipEnd:F2}s\nPhase 持续时间: {phase.Duration:F2}s", 
                    "确定");
                return;
            }
            Undo.RecordObject(_state.Data.Config, "添加 Clip");
            track.Clips.Add(tempClip);
            EditorUtility.SetDirty(_state.Data.Config);
            _state.NotifyChanged();
            _controller.Selection.SelectClip(_phaseIndex, _trackIndex, track.Clips.Count - 1);
        }

        public void Refresh()
        {
            MarkDirtyRepaint();
        }
    }
}
#endif

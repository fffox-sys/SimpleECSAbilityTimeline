#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
     
    public class PhaseElement : VisualElement
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private readonly int _phaseIndex;
        private Rect _foldoutRect;
        private Label _nameLabel;

        public PhaseElement(TimelineState state, TimelineController controller, int phaseIndex)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _phaseIndex = phaseIndex;

            style.height = 28;
            style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            style.borderBottomWidth = 1;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 8;

            _nameLabel = new Label
            {
                style =
                {
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    marginLeft = 20,
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            Add(_nameLabel);

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<ContextClickEvent>(OnContextClick);
            _state.OnStateChanged += UpdateLabel;
            _state.OnSelectionChanged += _ => UpdateSelection();
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null || _phaseIndex < 0 || _phaseIndex >= ability.Phases.Count)
            {
                _nameLabel.text = "无效的 Phase";
                return;
            }

            var phase = ability.Phases[_phaseIndex];
            string name = string.IsNullOrEmpty(phase.Name) ? $"Phase {_phaseIndex}" : phase.Name;
            _nameLabel.text = $"{name}  [{phase.Duration:F2}s] ({phase.Tracks.Count} tracks)";
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null || _phaseIndex < 0 || _phaseIndex >= ability.Phases.Count)
                return;

            var phase = ability.Phases[_phaseIndex];
            bool isSelected = _state.Selection.Kind == TimelineState.SelectionKind.Phase 
                           && _state.Selection.PhaseIndex == _phaseIndex;
            if (isSelected)
            {
                style.backgroundColor = new Color(phase.Color.r * 0.4f, phase.Color.g * 0.4f, phase.Color.b * 0.4f, 0.8f);
                style.borderLeftWidth = 3;
                style.borderLeftColor = phase.Color;
            }
            else
            {
                style.backgroundColor = new Color(phase.Color.r * 0.2f, phase.Color.g * 0.2f, phase.Color.b * 0.2f, 0.5f);
                style.borderLeftWidth = 0;
            }
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null || _phaseIndex < 0 || _phaseIndex >= ability.Phases.Count)
                return;

            var phase = ability.Phases[_phaseIndex];
            var rect = contentRect;
            var painter = ctx.painter2D;

            float foldoutSize = 12f;
            float foldoutMargin = 0f;
            _foldoutRect = new Rect(rect.xMin + foldoutMargin, rect.center.y - foldoutSize * 0.5f, foldoutSize, foldoutSize);

            painter.fillColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            painter.BeginPath();
            if (phase.IsExpanded)
            {
                painter.MoveTo(new Vector2(_foldoutRect.xMin, _foldoutRect.yMin + 2));
                painter.LineTo(new Vector2(_foldoutRect.xMax, _foldoutRect.yMin + 2));
                painter.LineTo(new Vector2(_foldoutRect.center.x, _foldoutRect.yMax - 2));
            }
            else
            {
                painter.MoveTo(new Vector2(_foldoutRect.xMin + 2, _foldoutRect.yMin));
                painter.LineTo(new Vector2(_foldoutRect.xMax - 2, _foldoutRect.center.y));
                painter.LineTo(new Vector2(_foldoutRect.xMin + 2, _foldoutRect.yMax));
            }
            painter.ClosePath();
            painter.Fill();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;

            if (_foldoutRect.Contains(evt.localMousePosition))
            {
                ToggleExpanded();
                evt.StopPropagation();
                return;
            }

            _controller.Selection.SelectPhase(_phaseIndex);
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            var menu = new GenericMenu();
            var ability = _state.Data.CurrentAbility;
            var phase = ability?.Phases[_phaseIndex];
            if (phase != null)
            {
                menu.AddItem(new GUIContent(phase.IsExpanded ? "折叠" : "展开"), false, ToggleExpanded);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("添加 Track"), false, () =>
                {
                    _controller.Edit.AddTrackToPhase(_phaseIndex, _state.Data.Config);
                });
                menu.AddItem(new GUIContent("删除 Phase"), false, () =>
                {
                    if (EditorUtility.DisplayDialog("删除 Phase", 
                        $"确定要删除 Phase {_phaseIndex} 吗?", 
                        "删除", "取消"))
                    {
                        _controller.Edit.DeletePhase(_phaseIndex, _state.Data.Config);
                    }
                });
                menu.AddSeparator("");
                bool canMoveUp = _phaseIndex > 0;
                bool canMoveDown = ability != null && _phaseIndex < ability.Phases.Count - 1;
                if (canMoveUp)
                    menu.AddItem(new GUIContent("上移"), false, () =>
                    {
                        _controller.Edit.MovePhase(_phaseIndex, -1, _state.Data.Config);
                    });
                else
                    menu.AddDisabledItem(new GUIContent("上移"));
                if (canMoveDown)
                    menu.AddItem(new GUIContent("下移"), false, () =>
                    {
                        _controller.Edit.MovePhase(_phaseIndex, 1, _state.Data.Config);
                    });
                else
                    menu.AddDisabledItem(new GUIContent("下移"));
            }
            menu.ShowAsContext();
        }

        private void ToggleExpanded()
        {
            var ability = _state.Data.CurrentAbility;
            if (ability != null && _phaseIndex >= 0 && _phaseIndex < ability.Phases.Count)
            {
                var phase = ability.Phases[_phaseIndex];
                phase.IsExpanded = !phase.IsExpanded;
                Undo.RecordObject(_state.Data.Config, "切换 Phase 展开");
                EditorUtility.SetDirty(_state.Data.Config);
                _state.NotifyChanged();
            }
        }

        public void Refresh()
        {
            MarkDirtyRepaint();
        }
    }
}
#endif

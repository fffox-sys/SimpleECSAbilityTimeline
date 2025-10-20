#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
   
    public class LeftToolbarView : IDisposable
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private VisualElement _root;

        public LeftToolbarView(TimelineState state, TimelineController controller)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public VisualElement Build()
        {
            _root = new VisualElement
            {
                style =
                {
                    height = 28,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f),
                    borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                    borderBottomWidth = 1,
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };

            var addPhaseBtn = new Button(() => AddPhase())
            {
                text = "+ Phase",
                style =
                {
                    height = 22,
                    marginRight = 4,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            addPhaseBtn.tooltip = "添加新 Phase";
            _root.Add(addPhaseBtn);

            var addTrackBtn = new Button(() => ShowAddTrackMenu())
            {
                text = "+ Track",
                style =
                {
                    height = 22,
                    marginRight = 4,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            addTrackBtn.tooltip = "添加 Track 到选中的 Phase";
            _root.Add(addTrackBtn);

            var selectionLabel = new Label
            {
                style =
                {
                    color = new Color(0.7f, 0.7f, 0.7f, 1f),
                    fontSize = 10,
                    marginLeft = 8,
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            _root.Add(selectionLabel);

            _state.OnSelectionChanged += _ => UpdateSelectionLabel(selectionLabel);
            UpdateSelectionLabel(selectionLabel);

            return _root;
        }

        private void UpdateSelectionLabel(Label label)
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null)
            {
                label.text = "";
                return;
            }

            if (_state.Selection.Kind == TimelineState.SelectionKind.Phase)
            {
                int phaseIdx = _state.Selection.PhaseIndex;
                if (phaseIdx >= 0 && phaseIdx < ability.Phases.Count)
                {
                    var phase = ability.Phases[phaseIdx];
                    string phaseName = string.IsNullOrEmpty(phase.Name) ? $"Phase {phaseIdx}" : phase.Name;
                    label.text = $"Selected: {phaseName}";
                    label.style.color = phase.Color;
                    return;
                }
            }

            label.text = "未选择 Phase";
            label.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        private void AddPhase()
        {
            if (_state.Data.Config == null) return;

            Undo.RecordObject(_state.Data.Config, "添加 Phase");
            var ability = _state.Data.CurrentAbility;
            if (ability == null) return;

            var newPhase = new AbilityConfigSO.Phase
            {
                Name = $"Phase {ability.Phases.Count}",
                Duration = 1.0f,
                Color = GetNextPhaseColor(ability.Phases.Count),
                IsExpanded = true
            };
            newPhase.Tracks.Add(new AbilityConfigSO.Track { Name = "Track 0" });

            ability.Phases.Add(newPhase);
            EditorUtility.SetDirty(_state.Data.Config);
            _state.NotifyChanged();

            _controller.Selection.SelectPhase(ability.Phases.Count - 1);
        }

        private void ShowAddTrackMenu()
        {
            var ability = _state.Data.CurrentAbility;
            if (ability == null)
            {
                EditorUtility.DisplayDialog("没有 Ability", "请先选择一个 Ability。", "确定");
                return;
            }

            if (_state.Selection.Kind != TimelineState.SelectionKind.Phase)
            {
                EditorUtility.DisplayDialog("未选择 Phase", 
                    "请先点击选择一个 Phase。", "确定");
                return;
            }

            int phaseIdx = _state.Selection.PhaseIndex;
            if (phaseIdx < 0 || phaseIdx >= ability.Phases.Count)
            {
                EditorUtility.DisplayDialog("无效的 Phase", "选中的 Phase 无效。", "确定");
                return;
            }

            var menu = new GenericMenu();

            foreach (AbilityConfigSO.TrackType trackType in Enum.GetValues(typeof(AbilityConfigSO.TrackType)))
            {
                menu.AddItem(new GUIContent(trackType.ToString()), false, () =>
                {
                    AddTrackToSelectedPhase(phaseIdx, trackType);
                });
            }

            menu.ShowAsContext();
        }

        private void AddTrackToSelectedPhase(int phaseIdx, AbilityConfigSO.TrackType trackType)
        {
            if (_state.Data.Config == null) return;

            var ability = _state.Data.CurrentAbility;
            if (ability == null || phaseIdx < 0 || phaseIdx >= ability.Phases.Count)
                return;

            Undo.RecordObject(_state.Data.Config, "添加 Track");

            var phase = ability.Phases[phaseIdx];
            var newTrack = new AbilityConfigSO.Track
            {
                Name = $"{trackType} {phase.Tracks.Count}",
                Type = trackType,
                Color = AbilityConfigSO.Track.GetDefaultColor(trackType)
            };

            phase.Tracks.Add(newTrack);
            EditorUtility.SetDirty(_state.Data.Config);
            _state.NotifyChanged();

            _controller.Selection.SelectTrack(phaseIdx, phase.Tracks.Count - 1);
        }

        private Color GetNextPhaseColor(int index)
        {
            Color[] colors = new[]
            {
                new Color(0.3f, 0.6f, 0.9f, 1f),
                new Color(0.9f, 0.5f, 0.3f, 1f),
                new Color(0.5f, 0.9f, 0.4f, 1f),
                new Color(0.9f, 0.4f, 0.6f, 1f),
                new Color(0.7f, 0.5f, 0.9f, 1f),
                new Color(0.9f, 0.9f, 0.3f, 1f),
                new Color(0.4f, 0.9f, 0.9f, 1f),
            };
            return colors[index % colors.Length];
        }

        public void Refresh()
        {
        }

        public void Dispose()
        {
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
   
    public class ToolbarView : IDisposable
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private Toolbar _toolbar;
        private ToolbarButton _playBtn;
        private ToolbarToggle _snapToggle;
        private ToolbarToggle _loopToggle;
        private DropdownField _abilityDropdown;

        public ToolbarView(TimelineState state, TimelineController controller)
        {
            _state = state;
            _controller = controller;
        }

        public VisualElement Build()
        {
            _toolbar = new Toolbar { style = { flexWrap = Wrap.Wrap } };

            var configField = new ObjectField("Config")
            {
                objectType = typeof(AbilityConfigSO),
                value = _state.Data.Config,
                style = { flexShrink = 0 }
            };
            configField.RegisterValueChangedCallback(evt =>
            {
                _state.Data.Config = (AbilityConfigSO)evt.newValue;
                _state.Data.AbilityIndex = 0;
                _state.Playback.Playhead = 0;
                _state.NotifyChanged();
                Refresh();
            });
            _toolbar.Add(configField);

            _abilityDropdown = new DropdownField("Ability")
            {
                choices = new List<string> { "<None>" },
                value = "<None>"
            };
            _abilityDropdown.style.flexShrink = 0;
            _abilityDropdown.style.minWidth = 200;

            UpdateAbilityDropdown();

            _abilityDropdown.RegisterValueChangedCallback(evt =>
            {
                if (_state.Data.Config != null && _state.Data.Config.Abilities != null)
                {
                    int index = _abilityDropdown.choices.IndexOf(evt.newValue);
                    if (index >= 0)
                    {
                        _state.Data.AbilityIndex = index;
                        _state.Playback.Playhead = 0;
                        _state.NotifyChanged();
                    }
                }
            });
            _toolbar.Add(_abilityDropdown);

            var addPhaseBtn = new ToolbarButton(() =>
            {
                if (_state.Data.Config != null)
                {
                    _controller.Edit.AddPhase(_state.Data.Config);
                }
            })
            {
                text = "+ Phase",
                style = { flexShrink = 0 }
            };
            _toolbar.Add(addPhaseBtn);

            _snapToggle = new ToolbarToggle { text = "Snap", value = _state.View.Snap };
            _snapToggle.RegisterValueChangedCallback(evt =>
            {
                _state.View.Snap = evt.newValue;
                _state.NotifyViewChanged();
            });
            _toolbar.Add(_snapToggle);

            _playBtn = new ToolbarButton(() => _controller.Playback.TogglePlay())
            {
                text = "Play/Pause",
                style = { flexShrink = 0 }
            };
            _toolbar.Add(_playBtn);

            var stopBtn = new ToolbarButton(() => _controller.Playback.Stop())
            {
                text = "Stop",
                style = { flexShrink = 0 }
            };
            _toolbar.Add(stopBtn);

            _loopToggle = new ToolbarToggle { text = "Loop", value = _state.Playback.Loop };
            _loopToggle.RegisterValueChangedCallback(evt =>
            {
                _state.Playback.Loop = evt.newValue;
                _state.NotifyPlaybackChanged();
            });
            _toolbar.Add(_loopToggle);

            var separator = new VisualElement
            {
                style =
            {
                width = 10,
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
                marginLeft = 5,
                marginRight = 5
            }
            };
            _toolbar.Add(separator);

            var previewLabel = new Label("预览:")
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, marginLeft = 5 }
            };
            _toolbar.Add(previewLabel);

            var previewTargetField = new ObjectField
            {
                objectType = typeof(GameObject),
                value = _controller.Preview?.PreviewTarget,
                style = { flexShrink = 0, minWidth = 150 }
            };
            previewTargetField.RegisterValueChangedCallback(evt =>
            {
                _controller.Preview?.SetPreviewTarget(evt.newValue as GameObject);
            });
            _toolbar.Add(previewTargetField);

            var previewToggle = new ToolbarToggle { text = "启用预览" };
            previewToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    if (!_controller.Preview.EnterPreviewMode())
                    {
                        previewToggle.SetValueWithoutNotify(false);
                    }
                }
                else
                {
                    _controller.Preview.ExitPreviewMode();
                }
            });
            _toolbar.Add(previewToggle);

            return _toolbar;
        }

        private void UpdateAbilityDropdown()
        {
            if (_abilityDropdown == null) return;

            if (_state.Data.Config == null || _state.Data.Config.Abilities == null || _state.Data.Config.Abilities.Count == 0)
            {
                _abilityDropdown.choices = new List<string> { "<None>" };
                _abilityDropdown.value = "<None>";
                return;
            }

            _abilityDropdown.choices = new List<string>();
            for (int i = 0; i < _state.Data.Config.Abilities.Count; i++)
            {
                var ability = _state.Data.Config.Abilities[i];
                string name = string.IsNullOrEmpty(ability.AbilityId) ? $"Ability {i}" : ability.AbilityId;
                _abilityDropdown.choices.Add(name);
            }

            if (_state.Data.AbilityIndex >= 0 && _state.Data.AbilityIndex < _abilityDropdown.choices.Count)
            {
                _abilityDropdown.SetValueWithoutNotify(_abilityDropdown.choices[_state.Data.AbilityIndex]);
            }
            else if (_abilityDropdown.choices.Count > 0)
            {
                _abilityDropdown.SetValueWithoutNotify(_abilityDropdown.choices[0]);
                _state.Data.AbilityIndex = 0;
                _state.NotifyChanged();
            }
        }

        public void Refresh()
        {
            UpdateAbilityDropdown();

            if (_playBtn != null)
            {
                _playBtn.style.backgroundColor = _state.Playback.IsPlaying
                    ? new Color(0.2f, 0.8f, 0.2f, 0.35f)
                    : new Color(0, 0, 0, 0);
            }

            _snapToggle?.SetValueWithoutNotify(_state.View.Snap);
            _loopToggle?.SetValueWithoutNotify(_state.Playback.Loop);
        }

        public void Dispose() { }
    }
}


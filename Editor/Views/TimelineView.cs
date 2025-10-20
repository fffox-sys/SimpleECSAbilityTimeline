#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{ 
   
    public class TimelineView : IDisposable
    {
        private readonly VisualElement _root;
        private readonly TimelineState _state;
        private readonly TimelineController _controller;

        private TwoPaneSplitView _splitView;
        private VisualElement _leftPane;
        private VisualElement _rightPane;
        private VisualElement _inspector;

        private ToolbarView _toolbar;
        private LeftToolbarView _leftToolbar;
        private RulerView _ruler;
        private TrackListView _trackList;
        private InspectorView _inspectorView;
        private OverlayView _overlays;
        private ZoomScrollBarView _zoomScrollBar;

        public TimelineView(VisualElement root, TimelineState state, TimelineController controller)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

       
        public void Build()
        {
            _root.Clear();
            _root.style.flexGrow = 1;

            _toolbar = new ToolbarView(_state, _controller);
            _root.Add(_toolbar.Build());

            var mainRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };

            _splitView = new TwoPaneSplitView(0, _state.View.TrackHeaderWidth, TwoPaneSplitViewOrientation.Horizontal)
            {
                style = { flexGrow = 1 }
            };

            _leftPane = new VisualElement
            {
                style =
                {
                    width = _state.View.TrackHeaderWidth,
                    flexDirection = FlexDirection.Column,
                    flexShrink = 0
                }
            };
            _leftToolbar = new LeftToolbarView(_state, _controller);
            _leftPane.Add(_leftToolbar.Build());

            _rightPane = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new Color(0.09f, 0.09f, 0.09f, 1f)
                }
            };

            _ruler = new RulerView(_state, _controller);
            _rightPane.Add(_ruler.Build());

            var trackContainer = new VisualElement
            {
                style = { flexGrow = 1, flexDirection = FlexDirection.Column, position = Position.Relative }
            };
            var trackListContainer = new VisualElement
            {
                style = { flexGrow = 1, overflow = Overflow.Hidden, position = Position.Relative }
            };
            Debug.Log($"[Timeline] Building TrackListView...");
            _trackList = new TrackListView(_leftPane, trackListContainer, _state, _controller);
            _trackList.Build();
            trackContainer.Add(trackListContainer);

            _zoomScrollBar = new ZoomScrollBarView(_state, _controller);
            trackContainer.Add(_zoomScrollBar.Build());
            _rightPane.Add(trackContainer);

            _overlays = new OverlayView(trackListContainer, _state, _controller);
            _overlays.Build();

            _splitView.Add(_leftPane);
            _splitView.Add(_rightPane);
            mainRow.Add(_splitView);

            _inspector = new VisualElement
            {
                style =
                {
                    width = 300,
                    flexShrink = 0,
                    borderLeftColor = new Color(0, 0, 0, 0.4f),
                    borderLeftWidth = 1,
                    paddingLeft = 6,
                    paddingTop = 4
                }
            };
            _inspectorView = new InspectorView(_inspector, _state, _controller);
            _inspectorView.Build();
            mainRow.Add(_inspector);

            _root.Add(mainRow);

            _state.OnStateChanged += Refresh;
            _state.OnViewChanged += OnViewChanged;
            _state.OnSelectionChanged += OnSelectionChanged;
            _state.OnPlaybackChanged += OnPlaybackChanged;

            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            _root.focusable = true;
            _root.tabIndex = 0;
            _root.Focus();
            Debug.Log("[TimelineView] Keyboard event handler registered, root is focusable");

            Refresh();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            Debug.Log($"[TimelineView] OnKeyDown: keyCode={evt.keyCode}, target={evt.target}");
            if (evt.keyCode == KeyCode.Delete)
            {
                Debug.Log($"[TimelineView] Delete key pressed, Selection.Kind={_state.Selection.Kind}, MultiSelection.Count={_state.Selection.MultiSelection.Count}");
                if (_state.Selection.Kind == TimelineState.SelectionKind.Key && _state.Selection.MultiSelection.Count > 0)
                {
                    DeleteSelectedKeys();
                    evt.StopPropagation();
                }
            }
        }

        private void DeleteSelectedKeys()
        {
            var ab = _state.Data.CurrentAbility;
            if (ab == null || _state.Data.Config == null)
            {
                Debug.LogWarning("[TimelineView] DeleteSelectedKeys: No ability or config");
                return;
            }

            var toDelete = new System.Collections.Generic.List<(int p, int t, int k)>();
            foreach (var sel in _state.Selection.MultiSelection)
            {
                toDelete.Add((sel.P, sel.T, sel.K));
            }
            Debug.Log($"[TimelineView] DeleteSelectedKeys: Collected {toDelete.Count} keys to delete");

            toDelete.Sort((a, b) =>
            {
                if (a.p != b.p) return b.p.CompareTo(a.p);
                if (a.t != b.t) return b.t.CompareTo(a.t);
                return b.k.CompareTo(a.k);
            });

            Undo.RecordObject(_state.Data.Config, "删除 Key");
            int deletedCount = 0;
            foreach (var (p, t, k) in toDelete)
            {
                if (p >= 0 && p < ab.Phases.Count)
                {
                    var phase = ab.Phases[p];
                    if (t >= 0 && t < phase.Tracks.Count)
                    {
                        var track = phase.Tracks[t];
                        if (k >= 0 && k < track.Keys.Count)
                        {
                            Debug.Log($"[TimelineView] Deleting key at Phase={p}, Track={t}, Key={k}");
                            track.Keys.RemoveAt(k);
                            deletedCount++;
                        }
                    }
                }
            }

            Debug.Log($"[TimelineView] Deleted {deletedCount} keys");
            EditorUtility.SetDirty(_state.Data.Config);
            _controller.Selection.ClearSelection();
            _state.NotifyChanged();
        }

        /// <summary>
        /// 刷新整个视图
        /// </summary>
        public void Refresh()
        {
            _toolbar?.Refresh();
            _ruler?.Refresh();
            _trackList?.Refresh();
            _overlays?.Refresh();
            _inspectorView?.Refresh();
            _zoomScrollBar?.Refresh();
        }

        private void OnViewChanged(TimelineState.ViewState viewState)
        {
            _ruler?.Refresh();
            _trackList?.Refresh();
            _overlays?.Refresh();
            _zoomScrollBar?.Refresh();
        }

        private void OnSelectionChanged(TimelineState.SelectionState selectionState)
        {
            _inspectorView?.Refresh();
        }

        private void OnPlaybackChanged(TimelineState.PlaybackState playbackState)
        {
            _overlays?.Refresh();
            _ruler?.Refresh();
        }

        public void Dispose()
        {
            _state.OnStateChanged -= Refresh;
            _state.OnViewChanged -= OnViewChanged;
            _state.OnSelectionChanged -= OnSelectionChanged;
            _state.OnPlaybackChanged -= OnPlaybackChanged;

            _toolbar?.Dispose();
            _leftToolbar?.Dispose();
            _ruler?.Dispose();
            _trackList?.Dispose();
            _overlays?.Dispose();
            _inspectorView?.Dispose();
            _zoomScrollBar?.Dispose();
        }
    }




}
#endif

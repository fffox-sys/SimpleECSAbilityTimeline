using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
   /// <summary>
/// Track List 视图 - 轨道列表（Headers + Rows�?
/// </summary>
public class TrackListView : IDisposable
{
    private readonly VisualElement _headerContainer;
    private readonly VisualElement _trackContainer;
    private readonly TimelineState _state;
    private readonly TimelineController _controller;

    private ScrollView _headerScroll;
    private ScrollView _trackScroll;

    public TrackListView(
        VisualElement headerContainer,
        VisualElement trackContainer,
        TimelineState state,
        TimelineController controller)
    {
        _headerContainer = headerContainer;
        _trackContainer = trackContainer;
        _state = state;
        _controller = controller;
    }

    public void Build()
    {
        _headerScroll = new ScrollView(ScrollViewMode.Vertical)
        {
            style = { flexGrow = 1 }
        };
        _headerContainer.Add(_headerScroll);

        _trackScroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
        {
            style = { flexGrow = 1 }
        };
        _trackScroll.RegisterCallback<MouseDownEvent>(OnGlobalMouseDown, TrickleDown.TrickleDown);
        _trackScroll.RegisterCallback<MouseMoveEvent>(OnGlobalMouseMove, TrickleDown.TrickleDown);
        _trackScroll.RegisterCallback<MouseUpEvent>(OnGlobalMouseUp, TrickleDown.TrickleDown);
        _trackContainer.Add(_trackScroll);

        _headerScroll.verticalScroller.valueChanged += v =>
        {
            if (_trackScroll.verticalScroller != null)
                _trackScroll.verticalScroller.value = v;
        };

        _state.OnStateChanged += Rebuild;

        Rebuild();
    }
    private Vector2 _globalBoxStart;
    private void OnGlobalMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0 && evt.target == _trackScroll.contentContainer)
        {
            _globalBoxStart = evt.localMousePosition;
            _state.Edit.IsGlobalBoxSelecting = true;
            _state.Edit.GlobalBoxStartWorld = _globalBoxStart;
            _state.Edit.GlobalBoxWorldRect = new Rect(_globalBoxStart, Vector2.zero);
            _trackScroll.CaptureMouse();
            evt.StopPropagation();
        }
    }
    private void OnGlobalMouseMove(MouseMoveEvent evt)
    {
        if (_state.Edit.IsGlobalBoxSelecting)
        {
            Vector2 current = evt.localMousePosition;
            float xMin = Mathf.Min(_globalBoxStart.x, current.x);
            float yMin = Mathf.Min(_globalBoxStart.y, current.y);
            float width = Mathf.Abs(current.x - _globalBoxStart.x);
            float height = Mathf.Abs(current.y - _globalBoxStart.y);
            _state.Edit.GlobalBoxWorldRect = new Rect(xMin, yMin, width, height);
            _state.NotifyEditChanged();
            evt.StopPropagation();
        }
    }
    private void OnGlobalMouseUp(MouseUpEvent evt)
    {
        if (_state.Edit.IsGlobalBoxSelecting)
        {
            _state.Edit.IsGlobalBoxSelecting = false;
            _trackScroll.ReleaseMouse();
            _state.NotifyEditChanged();
            evt.StopPropagation();
        }
    }

    public void Rebuild()
    {
        _headerScroll.Clear();
        _trackScroll.Clear();

        var ability = _state.Data.CurrentAbility;
            if (ability == null)
            {
                var placeholder = new Label("没有选择的 Ability")
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        fontSize = 14,
                        color = new Color(0.5f, 0.5f, 0.5f, 1f),
                        marginTop = 20
                    }
                };
                _trackScroll.Add(placeholder);
                return;
            }
        for (int p = 0; p < ability.Phases.Count; p++)
        {
            var phase = ability.Phases[p];

            var phaseHeader = new PhaseElement(_state, _controller, p);
            _headerScroll.Add(phaseHeader);

            var phaseRow = new VisualElement 
            { 
                style = { height = 28, backgroundColor = phaseHeader.style.backgroundColor },
                pickingMode = PickingMode.Ignore
            };
            _trackScroll.Add(phaseRow);

            if (phase.IsExpanded)
            {
                for (int t = 0; t < phase.Tracks.Count; t++)
                {
                    var trackHeader = new TrackElement(_state, _controller, p, t);
                    _headerScroll.Add(trackHeader);

                    var trackRow = new TrackRowElement(_state, _controller, p, t);
                    _trackScroll.Add(trackRow);
                }
            }
        }
    }

    public void Refresh()
    {
        var ability = _state.Data.CurrentAbility;
        if (ability == null)
        {
            Rebuild();
            return;
        }
        int expectedCount = 0;
        foreach (var phase in ability.Phases)
        {
            expectedCount++;
            if (phase.IsExpanded)
            {
                expectedCount += phase.Tracks.Count;
            }
        }
        int currentCount = 0;
        foreach (var child in _trackScroll.Children())
        {
            currentCount++;
        }
        if (currentCount != expectedCount)
        {
            Rebuild();
        }
        else
        {
            foreach (var child in _trackScroll.Children())
            {
                child.MarkDirtyRepaint();
            }
        }
    }

    public void Dispose() 
    {
        _state.OnStateChanged -= Rebuild;
    }
} 
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
   
    public class OverlayView : IDisposable
    {
        private readonly VisualElement _parent;
        internal readonly TimelineState _state;
        private readonly TimelineController _controller;

        private VisualElement _playheadOverlay;
        private VisualElement _rangeOverlay;
        private VisualElement _selectionOverlay;

        public OverlayView(VisualElement parent, TimelineState state, TimelineController controller)
        {
            _parent = parent;
            _state = state;
            _controller = controller;
        }

        public void Build()
        {
            _playheadOverlay = CreateOverlay();
            _playheadOverlay.generateVisualContent += OnPlayheadGenerate;
            _parent.Add(_playheadOverlay);

        _rangeOverlay = CreateOverlay();
        _rangeOverlay.generateVisualContent += OnRangeGenerate;
        _parent.Add(_rangeOverlay);

        _selectionOverlay = CreateOverlay();
        _selectionOverlay.generateVisualContent += OnSelectionGenerate;
        _parent.Add(_selectionOverlay);
    }

    private VisualElement CreateOverlay()
    {
        var overlay = new VisualElement { pickingMode = PickingMode.Ignore };
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        return overlay;
    }

    private void OnPlayheadGenerate(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        var rect = _playheadOverlay.contentRect;
        if (rect.width <= 1) return;

        float pps = _state.View.PixelsPerSecond;
        float scrollX = _state.View.ScrollX;
        float playhead = _state.Playback.Playhead;

        float x = (playhead * pps) - scrollX;

        if (x >= -10 && x <= rect.width + 10)
        {
            painter.strokeColor = new Color(1f, 0.2f, 0.2f, 1f);
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0));
            painter.LineTo(new Vector2(x, rect.height));
            painter.Stroke();
        }
    }

    private void OnRangeGenerate(MeshGenerationContext ctx)
    {
        if (!_state.Playback.RangeEnabled) return;

        var painter = ctx.painter2D;
        var rect = _rangeOverlay.contentRect;
        if (rect.width <= 1) return;

        float pps = _state.View.PixelsPerSecond;
        float scrollX = _state.View.ScrollX;

        float rangeIn = _state.Playback.RangeIn;
        float rangeOut = _state.Playback.RangeOut;

        float xIn = (rangeIn * pps) - scrollX;
        float xOut = (rangeOut * pps) - scrollX;

        if (xOut > 0 && xIn < rect.width)
        {
            float x1 = Mathf.Max(0, xIn);
            float x2 = Mathf.Min(rect.width, xOut);

            painter.fillColor = new Color(0.2f, 0.8f, 0.8f, 0.15f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(x1, 0));
            painter.LineTo(new Vector2(x2, 0));
            painter.LineTo(new Vector2(x2, rect.height));
            painter.LineTo(new Vector2(x1, rect.height));
            painter.ClosePath();
            painter.Fill();

            painter.strokeColor = new Color(0.2f, 0.8f, 0.8f, 0.6f);
            painter.lineWidth = 2f;

            if (xIn >= 0 && xIn <= rect.width)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(xIn, 0));
                painter.LineTo(new Vector2(xIn, rect.height));
                painter.Stroke();
            }

            if (xOut >= 0 && xOut <= rect.width)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(xOut, 0));
                painter.LineTo(new Vector2(xOut, rect.height));
                painter.Stroke();
            }
        }
    }

    private void OnSelectionGenerate(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        var rect = _selectionOverlay.contentRect;

        if (_state.Edit.IsGlobalBoxSelecting)
        {
            Rect boxRect = _state.Edit.GlobalBoxWorldRect;
            painter.fillColor = new Color(0.3f, 0.6f, 1f, 0.2f);
            painter.strokeColor = new Color(0.3f, 0.6f, 1f, 0.8f);
            painter.lineWidth = 1f;

            painter.BeginPath();
            painter.MoveTo(new Vector2(boxRect.xMin, boxRect.yMin));
            painter.LineTo(new Vector2(boxRect.xMax, boxRect.yMin));
            painter.LineTo(new Vector2(boxRect.xMax, boxRect.yMax));
            painter.LineTo(new Vector2(boxRect.xMin, boxRect.yMax));
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();
        }
    }

    public void Refresh()
    {
        _playheadOverlay?.MarkDirtyRepaint();
        _rangeOverlay?.MarkDirtyRepaint();
        _selectionOverlay?.MarkDirtyRepaint();
    }

    public void Dispose()
    {
        if (_playheadOverlay != null)
            _playheadOverlay.generateVisualContent -= OnPlayheadGenerate;
        if (_rangeOverlay != null)
            _rangeOverlay.generateVisualContent -= OnRangeGenerate;
        if (_selectionOverlay != null)
            _selectionOverlay.generateVisualContent -= OnSelectionGenerate;
    }
}
}


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
/// Ruler 视图 - 时间标尺
/// </summary>
public class RulerView : IDisposable
{
    private readonly TimelineState _state;
    private readonly TimelineController _controller;
    private VisualElement _container;
    private VisualElement _ruler;
    private bool _isScrubbing;

    public RulerView(TimelineState state, TimelineController controller)
    {
        _state = state;
        _controller = controller;
    }

    public VisualElement Build()
    {
        _container = new VisualElement
        {
            style =
                {
                    height = 28,
                    flexShrink = 0,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f),
                    borderBottomWidth = 0,
                    marginBottom = 0,
                    paddingBottom = 0
                }
        };

        _ruler = new VisualElement 
        { 
            style = 
            { 
                height = 28, 
                flexGrow = 1,
                borderBottomWidth = 0
            } 
        };
        _ruler.generateVisualContent += OnGenerateVisualContent;
        _ruler.RegisterCallback<MouseDownEvent>(OnMouseDown);
        _ruler.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _ruler.RegisterCallback<MouseUpEvent>(OnMouseUp);
        _container.Add(_ruler);

        return _container;
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0)
        {
            _isScrubbing = true;
            SetPlayheadFromPixel(evt.localMousePosition.x);
            evt.StopPropagation();
        }
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (_isScrubbing)
        {
            SetPlayheadFromPixel(evt.localMousePosition.x);
            evt.StopPropagation();
        }
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (evt.button == 0)
        {
            _isScrubbing = false;
            evt.StopPropagation();
        }
    }

    private void SetPlayheadFromPixel(float pixelX)
    {
        float scrollX = _state.View.ScrollX;
        float pps = _state.View.PixelsPerSecond;
        float globalTime = (pixelX + scrollX) / pps;
        _controller.Playback.SetPlayheadFromGlobalTime(globalTime);
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        var rect = _ruler.contentRect;
        if (rect.width <= 1) return;

        painter.fillColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        painter.BeginPath();
        painter.MoveTo(Vector2.zero);
        painter.LineTo(new Vector2(rect.width, 0));
        painter.LineTo(new Vector2(rect.width, rect.height));
        painter.LineTo(new Vector2(0, rect.height));
        painter.ClosePath();
        painter.Fill();

        DrawTimeMarks(painter, rect);
        DrawPlayheadTriangle(painter, rect);
    }
    private void DrawPlayheadTriangle(Painter2D painter, Rect rect)
    {
        float pps = _state.View.PixelsPerSecond;
        float scrollX = _state.View.ScrollX;
        float playhead = _state.Playback.Playhead;
        float x = (playhead * pps) - scrollX;
        if (x >= -10 && x <= rect.width + 10)
        {
            painter.fillColor = new Color(1f, 0.2f, 0.2f, 1f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(x - 6, 0));
            painter.LineTo(new Vector2(x + 6, 0));
            painter.LineTo(new Vector2(x, 10));
            painter.ClosePath();
            painter.Fill();
        }
    }

    private void DrawTimeMarks(Painter2D painter, Rect rect)
    {
        float pps = _state.View.PixelsPerSecond;
        float scrollX = _state.View.ScrollX;
        float duration = _state.Data.TotalDuration;

        float startTime = scrollX / pps;
        float endTime = (scrollX + rect.width) / pps;

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
            int startMinorIndex = Mathf.FloorToInt(startTime / minorStep);
            int endMinorIndex = Mathf.CeilToInt(endTime / minorStep);
            for (int i = startMinorIndex; i <= endMinorIndex; i++)
            {
                float t = i * minorStep;
                if (t >= 0 && t <= duration + 0.0001f)
                {
                    bool isMajor = Mathf.Abs(t % majorStep) < 0.0001f;
                    if (isMajor) continue;
                    float x = (t * pps) - scrollX;
                    painter.strokeColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                    painter.lineWidth = 1f;
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(x, rect.height - 6));
                    painter.LineTo(new Vector2(x, rect.height));
                    painter.Stroke();
                }
            }
        }
        int startMajorIndex = Mathf.FloorToInt(startTime / majorStep);
        int endMajorIndex = Mathf.CeilToInt(endTime / majorStep);
        for (int i = startMajorIndex; i <= endMajorIndex; i++)
        {
            float t = i * majorStep;
            if (t >= 0 && t <= duration + 0.0001f)
            {
                float x = (t * pps) - scrollX;
                painter.strokeColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                painter.lineWidth = 2f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, rect.height - 12));
                painter.LineTo(new Vector2(x, rect.height));
                painter.Stroke();

                string label;
                if (_state.View.FrameMode)
                {
                    label = $"F{Mathf.RoundToInt(t * _state.View.FPS)}";
                }
                else
                {
                    if (majorStep >= 1f)
                        label = $"{t:F1}s";     // 1s以上：显示到0.1s
                    else if (majorStep >= 0.1f)
                        label = $"{t:F1}s";     // 0.1s-1s：显示到0.1s
                    else if (majorStep >= 0.01f)
                        label = $"{t:F2}s";     // 0.01s-0.1s：显示到0.01s
                    else
                        label = $"{t:F3}s";     // 小于0.01s：显示到0.001s
                }

                var textRect = new Rect(x - 30, 2, 60, 14);
                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
                };
                GUI.Label(textRect, label, labelStyle);
            }
        }
    }

    public void Refresh()
    {
        _ruler?.MarkDirtyRepaint();
    }

    public void Dispose() { }
}  
}

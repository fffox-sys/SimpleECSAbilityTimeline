using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SECS.AbilityTimeline.Editor
{
   
    public class ZoomScrollBarView : IDisposable
    {
        private readonly TimelineState _state;
        private readonly TimelineController _controller;
        private VisualElement _container;
        private VisualElement _scrollBar;
        private enum DragMode { None, LeftHandle, RightHandle, Bar }
        private DragMode _dragMode = DragMode.None;
        private Vector2 _dragStartMouse;
        private float _dragStartScrollX;
        private float _dragStartPPS;
        private const float BarHeight = 16f;
        private const float HandleWidth = 8f;
        private const float MinBarWidth = 30f;

        public ZoomScrollBarView(TimelineState state, TimelineController controller)
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
                    height = BarHeight + 4,
                    flexShrink = 0,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f),
                    paddingTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 2,
                    paddingRight = 2
                }
            };

            _scrollBar = new VisualElement
            {
                style =
                {
                    height = BarHeight,
                    flexGrow = 1
                }
            };
            _scrollBar.generateVisualContent += OnGenerateVisualContent;
            _scrollBar.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            _scrollBar.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            _scrollBar.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            _container.Add(_scrollBar);
            return _container;
        }

        private (Rect barRect, Rect leftHandle, Rect rightHandle) CalculateBarGeometry(Rect rect)
        {
            float totalDuration = _state.Data.TotalDuration;
            if (totalDuration <= 0.001f) totalDuration = 1f;
            float pps = _state.View.PixelsPerSecond;
            float scrollX = _state.View.ScrollX;
            float viewportWidth = rect.width;
            float viewportDuration = viewportWidth / pps;
            float viewStart = scrollX / pps;
            float viewEnd = viewStart + viewportDuration;
            float barStartNorm = Mathf.Clamp01(viewStart / totalDuration);
            float barEndNorm = Mathf.Clamp01(viewEnd / totalDuration);
            float barWidthNorm = barEndNorm - barStartNorm;
            float barX = barStartNorm * rect.width;
            float barWidth = Mathf.Max(barWidthNorm * rect.width, MinBarWidth);
            Rect barRect = new Rect(barX, 0, barWidth, rect.height);
            Rect leftHandle = new Rect(barX, 0, HandleWidth, rect.height);
            Rect rightHandle = new Rect(barX + barWidth - HandleWidth, 0, HandleWidth, rect.height);
            return (barRect, leftHandle, rightHandle);
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            var rect = _scrollBar.contentRect;
            if (rect.width <= 1) return;

            painter.fillColor = new Color(0.08f, 0.08f, 0.08f, 1f);
            painter.BeginPath();
            painter.MoveTo(rect.min);
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(rect.max);
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();

            var (barRect, leftHandle, rightHandle) = CalculateBarGeometry(rect);
            barRect.x += rect.x;
            barRect.y += rect.y;
            leftHandle.x += rect.x;
            leftHandle.y += rect.y;
            rightHandle.x += rect.x;
            rightHandle.y += rect.y;
            painter.fillColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            painter.BeginPath();
            painter.MoveTo(barRect.min);
            painter.LineTo(new Vector2(barRect.xMax, barRect.yMin));
            painter.LineTo(barRect.max);
            painter.LineTo(new Vector2(barRect.xMin, barRect.yMax));
            painter.ClosePath();
            painter.Fill();
            painter.fillColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            painter.BeginPath();
            painter.MoveTo(leftHandle.min);
            painter.LineTo(new Vector2(leftHandle.xMax, leftHandle.yMin));
            painter.LineTo(new Vector2(leftHandle.xMax, leftHandle.yMax));
            painter.LineTo(new Vector2(leftHandle.xMin, leftHandle.yMax));
            painter.ClosePath();
            painter.Fill();
            painter.fillColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            painter.BeginPath();
            painter.MoveTo(rightHandle.min);
            painter.LineTo(new Vector2(rightHandle.xMax, rightHandle.yMin));
            painter.LineTo(new Vector2(rightHandle.xMax, rightHandle.yMax));
            painter.LineTo(new Vector2(rightHandle.xMin, rightHandle.yMax));
            painter.ClosePath();
            painter.Fill();
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return;

            var rect = _scrollBar.contentRect;
            var localPos = evt.localMousePosition;
            var (barRect, leftHandle, rightHandle) = CalculateBarGeometry(rect);
            Debug.Log($"[ZoomScrollBar] OnMouseDown: localPos={localPos}, barRect={barRect}, leftHandle={leftHandle}, rightHandle={rightHandle}");
            if (leftHandle.Contains(localPos))
            {
                _dragMode = DragMode.LeftHandle;
            }
            else if (rightHandle.Contains(localPos))
            {
                _dragMode = DragMode.RightHandle;
            }
            else if (barRect.Contains(localPos))
            {
                _dragMode = DragMode.Bar;
            }
            else
            {
                _dragMode = DragMode.None;
                return;
            }
            _dragStartMouse = evt.mousePosition;
            _dragStartScrollX = _state.View.ScrollX;
            _dragStartPPS = _state.View.PixelsPerSecond;
            _scrollBar.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_dragMode == DragMode.None) return;
            var rect = _scrollBar.contentRect;
            float totalDuration = _state.Data.TotalDuration;
            if (totalDuration <= 0.001f) totalDuration = 1f;
            float deltaX = evt.mousePosition.x - _dragStartMouse.x;
            float viewportWidth = rect.width;
            float startViewStart = _dragStartScrollX / _dragStartPPS;
            float startViewDuration = viewportWidth / _dragStartPPS;
            float startViewEnd = startViewStart + startViewDuration;
            if (_dragMode == DragMode.Bar)
            {
                float deltaTime = (deltaX / rect.width) * totalDuration;
                float newViewStart = startViewStart + deltaTime;
                newViewStart = Mathf.Clamp(newViewStart, 0, Mathf.Max(0, totalDuration - startViewDuration));
                _state.View.ScrollX = newViewStart * _dragStartPPS;
                _state.NotifyViewChanged();
            }
            else if (_dragMode == DragMode.LeftHandle)
            {
                float deltaTime = (deltaX / rect.width) * totalDuration;
                float newViewStart = Mathf.Max(0, startViewStart + deltaTime);
                float newViewEnd = startViewEnd;
                float newViewDuration = newViewEnd - newViewStart;
                newViewDuration = Mathf.Clamp(newViewDuration, 0.01f, totalDuration);
                newViewStart = newViewEnd - newViewDuration;
                float newPPS = viewportWidth / newViewDuration;
                newPPS = Mathf.Clamp(newPPS, 50f, 25000f);
                _state.View.PixelsPerSecond = newPPS;
                _state.View.ScrollX = newViewStart * newPPS;
                _state.NotifyViewChanged();
            }
            else if (_dragMode == DragMode.RightHandle)
            {
                float deltaTime = (deltaX / rect.width) * totalDuration;
                float newViewStart = startViewStart;
                float newViewEnd = Mathf.Min(totalDuration, startViewEnd + deltaTime);
                float newViewDuration = newViewEnd - newViewStart;
                newViewDuration = Mathf.Clamp(newViewDuration, 0.01f, totalDuration);
                newViewEnd = newViewStart + newViewDuration;
                float newPPS = viewportWidth / newViewDuration;
                newPPS = Mathf.Clamp(newPPS, 50f, 25000f);
                _state.View.PixelsPerSecond = newPPS;
                _state.View.ScrollX = newViewStart * newPPS;
                _state.NotifyViewChanged();
            }
            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (_dragMode == DragMode.None) return;
            Debug.Log($"[ZoomScrollBar] OnMouseUp: mode was {_dragMode}");
            _dragMode = DragMode.None;
            _scrollBar.ReleaseMouse();
            evt.StopPropagation();
        }

        public void Refresh()
        {
            _scrollBar?.MarkDirtyRepaint();
        }

        public void Dispose()
        {
        }
    }
}

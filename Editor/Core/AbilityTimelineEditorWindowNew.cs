#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SECS.AbilityTimeline.Editor
{
    public class AbilityTimelineEditorWindowNew : EditorWindow
    {
        private TimelineState _state;
        private TimelineController _controller;
        private TimelineView _view;
        private double _lastUpdateTime;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("SECS/AI/Ability Timeline Editor (New)")]
        public static void Open()
        {
            var window = GetWindow<AbilityTimelineEditorWindowNew>();
            window.titleContent = new GUIContent("Ability Timeline (New)");
            window.Show();
        }

        /// <summary>
        /// 窗口启用时初始化
        /// </summary>
        private void OnEnable()
        {
            try
            {
                Debug.Log("[Timeline] Initializing new architecture...");

                _state = new TimelineState();
                Debug.Log("[Timeline] State created");

                _controller = new TimelineController(_state);
                _controller.Initialize();
                Debug.Log("[Timeline] Controller initialized");

                _view = new TimelineView(rootVisualElement, _state, _controller);
                _view.Build();
                Debug.Log("[Timeline] View built");

                _state.OnStateChanged += OnStateChanged;

                EditorApplication.update += OnUpdate;
                _lastUpdateTime = EditorApplication.timeSinceStartup;

                Debug.Log("[Timeline] Initialization complete!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnDisable()
        {
            try
            {
                Debug.Log("[Timeline] Cleaning up...");

                if (_state != null)
                    _state.OnStateChanged -= OnStateChanged;

                EditorApplication.update -= OnUpdate;

                _view?.Dispose();
                _controller?.Dispose();

                Debug.Log("[Timeline] Cleanup complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Cleanup failed: {ex.Message}");
            }
        }

               private void OnUpdate()
        {
            if (_controller == null) return;

            try
            {
                _controller.Update();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] Update error: {ex.Message}");
            }
        }

        private void OnStateChanged()
        {
            try
            {
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Timeline] State change error: {ex.Message}");
            }
        }
    }
}
#endif

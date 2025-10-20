#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
    public class AbilityEditorWindow : EditorWindow
    {
        private AbilityConfigSO _config;
        private Vector2 _scroll;
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;

        [MenuItem("SECS/Ability/Ability Overview")]
        public static void Open()
        {
            var win = GetWindow<AbilityEditorWindow>(false, "Ability 概览");
            win.minSize = new Vector2(400, 300);
            win.Show();
        }
        /// <summary>
        /// 从Timeline编辑器打开（作为弹出窗口）
        /// </summary>
        public static void OpenForConfig(AbilityConfigSO config)
        {
            var win = GetWindow<AbilityEditorWindow>(true, "Ability 概览", true);
            win.minSize = new Vector2(450, 350);
            win._config = config;
            win.ShowUtility();
        }

        private void OnEnable()
        {
            _headerStyle = null; 
        }

        private void OnGUI()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(5, 5, 10, 5)
                };
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(5, 5, 2, 2)
                };
            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _config = (AbilityConfigSO)EditorGUILayout.ObjectField("Config", _config, typeof(AbilityConfigSO), false);
            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                CreateNewConfig();
            }
            EditorGUILayout.EndHorizontal();
            if (_config == null)
            {
                EditorGUILayout.HelpBox("选择或创建一个 AbilityConfig 进行编辑。", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Ability", GUILayout.Height(25)))
            {
                _config.Abilities.Add(new AbilityConfigSO.AbilityDef());
                EditorUtility.SetDirty(_config);
            }
            if (GUILayout.Button("打开 Timeline 编辑器", GUILayout.Height(25)))
            {
                var timelineWindow = EditorWindow.GetWindow<AbilityTimelineEditorWindowNew>();
                timelineWindow.Show();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Abilities", _headerStyle);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _config.Abilities.Count; i++)
            {
                var ability = _config.Abilities[i];
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(30));
                ability.AbilityId = EditorGUILayout.TextField(ability.AbilityId, EditorStyles.boldLabel);
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    Debug.Log($"Open {ability.AbilityId} in Timeline Editor (TODO)");
                }
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    if (EditorUtility.DisplayDialog("删除 Ability",
                        $"是否确定删除 '{ability.AbilityId}'?",
                        "删除", "取消"))
                    {
                        _config.Abilities.RemoveAt(i);
                        EditorUtility.SetDirty(_config);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel++;
                ability.Cooldown = EditorGUILayout.FloatField("Cooldown", ability.Cooldown);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Phase 列表:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{ability.Phases.Count}");
                if (GUILayout.Button("+ Phase", GUILayout.Width(80)))
                {
                    ability.Phases.Add(new AbilityConfigSO.Phase());
                    EditorUtility.SetDirty(_config);
                }
                EditorGUILayout.EndHorizontal();
                int totalTracks = 0;
                int totalKeys = 0;
                int totalClips = 0;
                float totalDuration = 0f;
                foreach (var phase in ability.Phases)
                {
                    totalTracks += phase.Tracks.Count;
                    totalDuration += phase.Duration;
                    foreach (var track in phase.Tracks)
                    {
                        totalKeys += track.Keys.Count;
                        totalClips += track.Clips.Count;
                    }
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Tracks: {totalTracks}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Keys: {totalKeys}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Clips: {totalClips}", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField($"Total Duration: {totalDuration:F2}s", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_config);
            }
        }
        private void CreateNewConfig()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "创建 AbilityConfig",
                "NewAbilityConfig",
                "asset",
                "选择新 AbilityConfig 的位置"
            );
            if (!string.IsNullOrEmpty(path))
            {
                var asset = ScriptableObject.CreateInstance<AbilityConfigSO>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                _config = asset;
            }
        }
    }
}
#endif

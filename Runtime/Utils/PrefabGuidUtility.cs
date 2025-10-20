using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SECS.Configs
{
    /// <summary>
    /// Prefab GUID工具 - 用于自动获取Prefab的唯一GUID
    /// </summary>
    public static class PrefabGuidUtility
    {
        /// <summary>
        /// 获取GameObject Prefab的GUID�?2位字符串�?
        /// </summary>
        public static string GetGuid(GameObject prefab)
        {
            if (prefab == null) return string.Empty;
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return guid;
#else
            Debug.LogWarning("[PrefabGuidUtility] Cannot get GUID at runtime. GUID should be baked.");
            return string.Empty;
#endif
        }
        /// <summary>
        /// 获取任意Unity资源的GUID（支持Prefab/AudioClip/AnimationClip等）
        /// </summary>
        public static string GetGuid(Object asset)
        {
            if (asset == null) return string.Empty;
#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return string.Empty;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            return guid;
#else
            Debug.LogWarning("[PrefabGuidUtility] Cannot get GUID at runtime. GUID should be baked.");
            return string.Empty;
#endif
        }
        /// <summary>
        /// 从GUID加载GameObject Prefab（仅编辑器）
        /// </summary>
        public static GameObject LoadPrefabFromGuid(string guid)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(guid)) return null;
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return null;
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
            Debug.LogWarning("[PrefabGuidUtility] LoadPrefabFromGuid only works in editor.");
            return null;
#endif
        }
        /// <summary>
        /// 验证GUID是否有效
        /// </summary>
        public static bool IsValidGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            if (guid.Length != 32) return false;
            foreach (char c in guid)
            {
                if (!System.Uri.IsHexDigit(c)) return false;
            }
            return true;
        }
        /// <summary>
        /// 计算GUID的Hash（用于运行时快速查找）
        /// </summary>
        public static int GuidToHash(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return 0;
            return guid.GetHashCode();
        }
    }
}

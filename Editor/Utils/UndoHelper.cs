#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SECS.AbilityTimeline.Editor
{
    public static class UndoHelper
    {
        /// <summary>
        /// 记录对象修改
        /// </summary>
        public static void Record(Object obj, string operationName)
        {
            if (obj == null)
            {
                Debug.LogWarning($"[UndoHelper] Record: Object is null for operation '{operationName}'");
                return;
            }

            Undo.RecordObject(obj, operationName);
            Debug.Log($"[UndoHelper] Recorded: {operationName}");
        }

        /// <summary>
        /// 开始Undo 分组（用于批量操作）
        /// </summary>
        /// <returns>分组索引，用于后续的 EndGroup 调用</returns>
        public static int BeginGroup(string groupName)
        {
            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(groupName);
            Debug.Log($"[UndoHelper] Begin Group: {groupName} (index={groupIndex})");
            return groupIndex;
        }

        /// <summary>
        /// 结束 Undo 分组
        /// </summary>
        public static void EndGroup(int groupIndex)
        {
            Undo.CollapseUndoOperations(groupIndex);
            Debug.Log($"[UndoHelper] End Group: index={groupIndex}");
        }

        public static void PerformGroupOperation(Object obj, string operationName, System.Action operation)
        {
            if (obj == null || operation == null)
            {
                Debug.LogWarning($"[UndoHelper] PerformGroupOperation: Object or operation is null for '{operationName}'");
                return;
            }

            int groupIndex = BeginGroup(operationName);

            try
            {
                Undo.RecordObject(obj, operationName);
                operation?.Invoke();
                EditorUtility.SetDirty(obj);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UndoHelper] Error during operation '{operationName}': {ex.Message}");
            }
            finally
            {
                EndGroup(groupIndex);
            }
        }

        /// <summary>
        /// 标记对象为脏
        /// </summary>
        public static void MarkDirty(Object obj)
        {
            if (obj == null) return;
            EditorUtility.SetDirty(obj);
        }

        /// <summary>
        /// 执行 Undo
        /// </summary>
        public static void PerformUndo()
        {
            Undo.PerformUndo();
            Debug.Log("[UndoHelper] Performed Undo");
        }

        /// <summary>
        /// 执行 Redo
        /// </summary>
        public static void PerformRedo()
        {
            Undo.PerformRedo();
            Debug.Log("[UndoHelper] Performed Redo");
        }
    }
}
#endif

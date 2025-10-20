using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{

    public static class PreviewGizmoDrawer
    {
        private static readonly Color HitboxColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        private static readonly Color HitboxWireColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        private static readonly Color VFXColor = new Color(1f, 0.7f, 0.2f, 0.5f);
        private static readonly Color SFXColor = new Color(0.3f, 1f, 0.3f, 0.5f);
        private static readonly Color CameraShakeColor = new Color(0.8f, 0.3f, 1f, 0.3f);
        private static readonly Color MarkerColor = new Color(1f, 1f, 0.3f, 0.7f);
        public static void DrawHitboxKey(Transform transform, AbilityConfigSO.Key key)
        {
            if (transform == null) return;
            Vector3 center = transform.position + transform.TransformDirection(key.HitboxOffset);
            Vector3 forward = key.HitboxUseHeading ? transform.forward : Vector3.forward;
            Handles.color = HitboxWireColor;
            switch (key.HitboxShape)
            {
                case AbilityConfigSO.HitboxShapeType.Sphere:
                    DrawSphere(center, key.HitboxRadius);
                    break;
                case AbilityConfigSO.HitboxShapeType.Cone:
                    DrawCone(center, forward, key.HitboxRadius, key.HitboxAngle);
                    break;
                case AbilityConfigSO.HitboxShapeType.Capsule:
                    DrawCapsule(center, forward, key.HitboxRadius, key.HitboxHeight);
                    break;
            }
            DrawLabel(center, $"Hitbox: {key.EventName}", HitboxWireColor);
        }
        public static void DrawCameraShakeKey(Transform transform, AbilityConfigSO.Key key)
        {
            if (transform == null) return;
            Vector3 position = transform.position;
            Handles.color = CameraShakeColor;
            Handles.DrawWireDisc(position, Vector3.up, key.CameraShakeRadius);
            Handles.DrawWireDisc(position, Vector3.right, key.CameraShakeRadius);
            Handles.DrawWireDisc(position, Vector3.forward, key.CameraShakeRadius);
            DrawLabel(position, $"Camera Shake (R={key.CameraShakeRadius:F1})", CameraShakeColor);
        }
        public static void DrawMarkerKey(Transform transform, AbilityConfigSO.Key key)
        {
            if (transform == null) return;
            Vector3 position = transform.position + key.MarkerPosition;
            Handles.color = MarkerColor;
            float size = 0.25f;
            Vector3 up = position + Vector3.up * size;
            Vector3 down = position + Vector3.down * size;
            Vector3 left = position + Vector3.left * size;
            Vector3 right = position + Vector3.right * size;
            Vector3 forward = position + Vector3.forward * size;
            Vector3 back = position + Vector3.back * size;
            Handles.DrawLine(up, left);
            Handles.DrawLine(left, down);
            Handles.DrawLine(down, right);
            Handles.DrawLine(right, up);
            Handles.DrawLine(up, forward);
            Handles.DrawLine(forward, down);
            Handles.DrawLine(down, back);
            Handles.DrawLine(back, up);
            string label = key.Type switch
            {
                AbilityConfigSO.KeyType.Signal => "Signal",
                AbilityConfigSO.KeyType.Footstep => "Footstep",
                AbilityConfigSO.KeyType.AnimationEvent => "动画事件",
                AbilityConfigSO.KeyType.Custom => "Custom",
                _ => "Marker"
            };
            DrawLabel(position, $"{label}: {key.EventName}", MarkerColor);
        }

        public static void DrawClip(Transform transform, AbilityConfigSO.Clip clip, float clipTime)
        {
            if (transform == null) return;
            Vector3 position = transform.position;
            switch (clip.Type)
            {
                case AbilityConfigSO.ClipType.Animation:
                    Handles.color = new Color(0.4f, 0.8f, 1f, 0.5f);
                    DrawLabel(position + Vector3.up * 1.5f,
                        $"Animation: {clip.AnimationClip?.name ?? "null"} ({clipTime:F2}s)",
                        Handles.color);
                    break;
                case AbilityConfigSO.ClipType.VFX:
                    Handles.color = VFXColor;
                    float progress = clip.Duration > 0 ? clipTime / clip.Duration : 0f;
                    DrawLabel(position + Vector3.up * 1.2f,
                        $"VFX Clip: {clip.VFXPrefab?.name ?? "null"} ({progress * 100:F0}%)",
                        Handles.color);
                    break;
                case AbilityConfigSO.ClipType.SFX:
                    Handles.color = SFXColor;
                    float sfxProgress = clip.Duration > 0 ? clipTime / clip.Duration : 0f;
                    DrawLabel(position + Vector3.up * 0.9f,
                        $"SFX Clip: {clip.AudioClip?.name ?? "null"} ({sfxProgress * 100:F0}%)",
                        Handles.color);
                    break;
            }
        }
        private static void DrawSphere(Vector3 center, float radius)
        {
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
            Handles.color = new Color(HitboxColor.r, HitboxColor.g, HitboxColor.b, 0.1f);
            Handles.DrawSolidDisc(center, Vector3.up, radius);
        }
        private static void DrawCone(Vector3 center, Vector3 forward, float radius, float angleDeg)
        {
            Vector3 normalizedForward = forward.normalized;
            float halfAngle = angleDeg * 0.5f;
            Handles.DrawWireArc(center, Vector3.up, Quaternion.Euler(0, -halfAngle, 0) * normalizedForward, angleDeg, radius);
            Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * normalizedForward * radius;
            Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * normalizedForward * radius;
            Handles.DrawLine(center, center + leftEdge);
            Handles.DrawLine(center, center + rightEdge);
            Handles.DrawWireArc(center, Vector3.up, Quaternion.Euler(0, -halfAngle, 0) * normalizedForward, angleDeg, radius);
        }
        private static void DrawCapsule(Vector3 center, Vector3 forward, float radius, float height)
        {
            Vector3 normalizedForward = forward.normalized;
            float halfHeight = height * 0.5f;
            Vector3 start = center - normalizedForward * halfHeight;
            Vector3 end = center + normalizedForward * halfHeight;
            Handles.DrawWireDisc(start, normalizedForward, radius);
            Handles.DrawWireDisc(end, normalizedForward, radius);
            Vector3 perpendicular = Vector3.Cross(normalizedForward, Vector3.up).normalized;
            if (perpendicular.magnitude < 0.1f)
                perpendicular = Vector3.Cross(normalizedForward, Vector3.right).normalized;
            Vector3 offset = perpendicular * radius;
            Handles.DrawLine(start + offset, end + offset);
            Handles.DrawLine(start - offset, end - offset);
            Handles.DrawLine(start, end);
        }
        private static void DrawLabel(Vector3 position, string text, Color color)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = color },
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
            Handles.Label(position, text, style);
        }
    }
}

using UnityEngine;
using UnityEngine.UIElements;
using SECS.Configs;

namespace SECS.AbilityTimeline.Editor
{
    public static class KeyDrawer
    {
        public static void DrawKey(Painter2D painter, AbilityConfigSO.Key key, Vector2 position, bool isSelected, float size = 8f)
        {
            Color color = GetKeyColor(key, isSelected);
            switch (key.Type)
            {
                case AbilityConfigSO.KeyType.Hitbox:
                    DrawHitboxKey(painter, position, color, isSelected, size);
                    break;
                case AbilityConfigSO.KeyType.Signal:
                    DrawSignalKey(painter, position, color, isSelected, size);
                    break;
                case AbilityConfigSO.KeyType.AnimationEvent:
                    DrawAnimationEventKey(painter, position, color, isSelected, size);
                    break;
                case AbilityConfigSO.KeyType.CameraShake:
                    DrawCameraShakeKey(painter, position, color, isSelected, size);
                    break;
                case AbilityConfigSO.KeyType.Footstep:
                    DrawFootstepKey(painter, position, color, isSelected, size);
                    break;
                case AbilityConfigSO.KeyType.Custom:
                    DrawCustomKey(painter, position, color, isSelected, size);
                    break;
            }
        }

        private static Color GetKeyColor(AbilityConfigSO.Key key, bool isSelected)
        {
            if (isSelected)
            {
                return Color.white;
            }
            if (key.EditorColor != Color.cyan && key.EditorColor.a > 0.1f)
            {
                return key.EditorColor;
            }
            return key.Type switch
            {
                AbilityConfigSO.KeyType.Hitbox => new Color(1f, 0.3f, 0.3f, 1f),
                AbilityConfigSO.KeyType.Signal => new Color(1f, 0.9f, 0.3f, 1f),
                AbilityConfigSO.KeyType.AnimationEvent => new Color(0.4f, 0.8f, 1f, 1f),
                AbilityConfigSO.KeyType.CameraShake => new Color(0.9f, 0.5f, 1f, 1f),
                AbilityConfigSO.KeyType.Footstep => new Color(0.6f, 0.4f, 0.2f, 1f),
                AbilityConfigSO.KeyType.Custom => new Color(0.5f, 0.5f, 0.5f, 1f),
                _ => new Color(0.4f, 0.6f, 0.9f, 1f)
            };
        }

        private static void DrawHitboxKey(Painter2D painter, Vector2 center, Color color, bool isSelected, float size)
        {
            float width = size;
            float height = size * 2f;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - width * 0.5f, center.y - height * 0.5f));
            painter.LineTo(new Vector2(center.x + width * 0.5f, center.y - height * 0.5f));
            painter.LineTo(new Vector2(center.x + width * 0.5f, center.y + height * 0.5f));
            painter.LineTo(new Vector2(center.x - width * 0.5f, center.y + height * 0.5f));
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            painter.lineWidth = isSelected ? 2f : 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - width * 0.5f, center.y - height * 0.5f));
            painter.LineTo(new Vector2(center.x + width * 0.5f, center.y - height * 0.5f));
            painter.LineTo(new Vector2(center.x + width * 0.5f, center.y + height * 0.5f));
            painter.LineTo(new Vector2(center.x - width * 0.5f, center.y + height * 0.5f));
            painter.ClosePath();
            painter.Stroke();
        }

        private static void DrawSignalKey(Painter2D painter, Vector2 center, Color color, bool isSelected, float size)
        {
            Vector2 top = new Vector2(center.x, center.y - size);
            Vector2 right = new Vector2(center.x + size, center.y);
            Vector2 bottom = new Vector2(center.x, center.y + size);
            Vector2 left = new Vector2(center.x - size, center.y);
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            painter.lineWidth = isSelected ? 2f : 1f;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(right);
            painter.LineTo(bottom);
            painter.LineTo(left);
            painter.ClosePath();
            painter.Stroke();
        }

        private static void DrawAnimationEventKey(Painter2D painter, Vector2 center, Color color, bool isSelected, float size)
        {
            Vector2 top = new Vector2(center.x, center.y - size);
            Vector2 bottomLeft = new Vector2(center.x - size * 0.866f, center.y + size * 0.5f);
            Vector2 bottomRight = new Vector2(center.x + size * 0.866f, center.y + size * 0.5f);
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(bottomRight);
            painter.LineTo(bottomLeft);
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            painter.lineWidth = isSelected ? 2f : 1f;
            painter.BeginPath();
            painter.MoveTo(top);
            painter.LineTo(bottomRight);
            painter.LineTo(bottomLeft);
            painter.ClosePath();
            painter.Stroke();
        }
        private static void DrawCameraShakeKey(Painter2D painter, Vector2 center, Color color, bool isSelected, float size)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, size, 0, 360);
            painter.Fill();
            painter.strokeColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            painter.lineWidth = isSelected ? 2f : 1f;
            painter.BeginPath();
            painter.Arc(center, size, 0, 360);
            painter.Stroke();
        }

        private static void DrawFootstepKey(Painter2D painter, Vector2 center, Color color, bool isSelected, float size)
        {
            Vector2[] vertices = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f - 30f) * Mathf.Deg2Rad;
                vertices[i] = new Vector2(
                    center.x + size * Mathf.Cos(angle),
                    center.y + size * Mathf.Sin(angle)
                );
            }
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(vertices[0]);
            for (int i = 1; i < 6; i++)
            {
                painter.LineTo(vertices[i]);
            }
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            painter.lineWidth = isSelected ? 2f : 1f;
            painter.BeginPath();
            painter.MoveTo(vertices[0]);
            for (int i = 1; i < 6; i++)
            {
                painter.LineTo(vertices[i]);
            }
            painter.ClosePath();
            painter.Stroke();
        }

        private static void DrawCustomKey(Painter2D painter, Vector2 center, Color color, bool isSelected, float size)
        {
            Vector2[] vertices = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (i * 36f - 90f) * Mathf.Deg2Rad;
                float radius = (i % 2 == 0) ? size : size * 0.4f;
                vertices[i] = new Vector2(
                    center.x + radius * Mathf.Cos(angle),
                    center.y + radius * Mathf.Sin(angle)
                );
            }
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(vertices[0]);
            for (int i = 1; i < 10; i++)
            {
                painter.LineTo(vertices[i]);
            }
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            painter.lineWidth = isSelected ? 2f : 1f;
            painter.BeginPath();
            painter.MoveTo(vertices[0]);
            for (int i = 1; i < 10; i++)
            {
                painter.LineTo(vertices[i]);
            }
            painter.ClosePath();
            painter.Stroke();
        }

        public static bool HitTest(AbilityConfigSO.Key key, Vector2 center, Vector2 mousePos, float size = 8f)
        {
            float extendedSize = size + 2f;
            return Vector2.Distance(center, mousePos) <= extendedSize;
        }
    }
}

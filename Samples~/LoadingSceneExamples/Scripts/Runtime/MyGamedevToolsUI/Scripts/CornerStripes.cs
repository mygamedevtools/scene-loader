using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGameDevTools.UI
{
    /// <summary>
    /// The three diagonal stripes for the top-right corner of loading screens, cards and banners.
    /// Fills its parent (see .mgt-stripes) and scales with its height. Each stripe is clipped to
    /// the element's bounds, so it always runs off the top and right edges.
    /// </summary>
    [UxmlElement]
    public partial class CornerStripes : VisualElement
    {
        public const string UssClassName = "mgt-stripes";

        static readonly CustomStyleProperty<Color> Stripe1Property = new("--mgt-stripes-1");
        static readonly CustomStyleProperty<Color> Stripe2Property = new("--mgt-stripes-2");
        static readonly CustomStyleProperty<Color> Stripe3Property = new("--mgt-stripes-3");

        // Same geometry as the web templates: a 300x200 design box anchored to the top-right
        // corner, with 16-unit stripes along y = x - c.
        const float DesignWidth = 300f;
        const float DesignHeight = 200f;
        const float HalfThickness = 8f;
        static readonly float[] Offsets = { 172f, 196f, 220f };

        readonly Color[] _colors = { new(0.89f, 0.34f, 0.18f), new(0.95f, 0.65f, 0.07f), new(0.16f, 0.63f, 0.61f) };

        public CornerStripes()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(Stripe1Property, out var stripe1)) _colors[0] = stripe1;
            if (style.TryGetValue(Stripe2Property, out var stripe2)) _colors[1] = stripe2;
            if (style.TryGetValue(Stripe3Property, out var stripe3)) _colors[2] = stripe3;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var scale = rect.height / DesignHeight;
            var left = rect.xMax - DesignWidth * scale;
            // Vertical offset between a 45-degree stripe's centerline and its edges.
            var edge = HalfThickness * Mathf.Sqrt(2f);
            var painter = context.painter2D;

            for (var i = 0; i < Offsets.Length; i++)
            {
                var c = Offsets[i];
                // A band far longer than the box, then clipped to the element.
                var band = new List<Vector2>
                {
                    new(-200f, -200f - c - edge), new(500f, 500f - c - edge),
                    new(500f, 500f - c + edge), new(-200f, -200f - c + edge),
                };
                for (var p = 0; p < band.Count; p++)
                    band[p] = new Vector2(left + band[p].x * scale, rect.y + band[p].y * scale);

                var clipped = ClipToRect(band, rect);
                if (clipped.Count < 3)
                    continue;

                painter.fillColor = _colors[i];
                painter.BeginPath();
                painter.MoveTo(clipped[0]);
                for (var p = 1; p < clipped.Count; p++)
                    painter.LineTo(clipped[p]);
                painter.ClosePath();
                painter.Fill();
            }
        }

        // Sutherland-Hodgman against the four edges of an axis-aligned rectangle.
        static List<Vector2> ClipToRect(List<Vector2> polygon, Rect rect)
        {
            polygon = Clip(polygon, p => p.x >= rect.xMin, (a, b) => Lerp(a, b, (rect.xMin - a.x) / (b.x - a.x)));
            polygon = Clip(polygon, p => p.x <= rect.xMax, (a, b) => Lerp(a, b, (rect.xMax - a.x) / (b.x - a.x)));
            polygon = Clip(polygon, p => p.y >= rect.yMin, (a, b) => Lerp(a, b, (rect.yMin - a.y) / (b.y - a.y)));
            polygon = Clip(polygon, p => p.y <= rect.yMax, (a, b) => Lerp(a, b, (rect.yMax - a.y) / (b.y - a.y)));
            return polygon;
        }

        static List<Vector2> Clip(List<Vector2> input, System.Func<Vector2, bool> inside, System.Func<Vector2, Vector2, Vector2> intersect)
        {
            var output = new List<Vector2>();
            for (var i = 0; i < input.Count; i++)
            {
                var current = input[i];
                var previous = input[(i + input.Count - 1) % input.Count];
                if (inside(current))
                {
                    if (!inside(previous))
                        output.Add(intersect(previous, current));
                    output.Add(current);
                }
                else if (inside(previous))
                {
                    output.Add(intersect(previous, current));
                }
            }
            return output;
        }

        static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
    }
}

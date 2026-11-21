using UnityEngine;
using UnityEngine.UIElements;

namespace MyGameDevTools.UI
{
    /// <summary>
    /// The H1 mark: a hexagon frame with three rounded axes and a center dot, drawn as vectors.
    /// Colors come from --mgt-mark-frame and --mgt-mark-axis-{1,2,3}, which .mgt-mark maps to the
    /// theme's mark colors, so the mark follows .mgt-theme-light and .mgt-theme-dark. Size it
    /// with width and height.
    /// </summary>
    [UxmlElement]
    public partial class BrandMark : VisualElement
    {
        public const string UssClassName = "mgt-mark";

        static readonly CustomStyleProperty<Color> FrameProperty = new("--mgt-mark-frame");
        static readonly CustomStyleProperty<Color> Axis1Property = new("--mgt-mark-axis-1");
        static readonly CustomStyleProperty<Color> Axis2Property = new("--mgt-mark-axis-2");
        static readonly CustomStyleProperty<Color> Axis3Property = new("--mgt-mark-axis-3");

        // The mark on its 100x100 design grid.
        static readonly Vector2[] Hexagon =
        {
            new(50f, 6f), new(88.1f, 28f), new(88.1f, 72f), new(50f, 94f), new(11.9f, 72f), new(11.9f, 28f),
        };
        static readonly Vector2 Center = new(50f, 50f);
        static readonly Vector2[] AxisEnds = { new(50f, 24f), new(27.5f, 63f), new(72.5f, 63f) };
        const float FrameWidth = 7f;
        const float AxisWidth = 12f;
        const float DotRadius = 8f;

        Color _frame = new(0.96f, 0.94f, 0.88f);
        readonly Color[] _axes = { new(0.89f, 0.34f, 0.18f), new(0.95f, 0.65f, 0.07f), new(0.16f, 0.63f, 0.61f) };

        public BrandMark()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var style = evt.customStyle;
            if (style.TryGetValue(FrameProperty, out var frame)) _frame = frame;
            if (style.TryGetValue(Axis1Property, out var axis1)) _axes[0] = axis1;
            if (style.TryGetValue(Axis2Property, out var axis2)) _axes[1] = axis2;
            if (style.TryGetValue(Axis3Property, out var axis3)) _axes[2] = axis3;
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f)
                return;

            var scale = size / 100f;
            var offset = new Vector2(rect.x + (rect.width - size) * 0.5f, rect.y + (rect.height - size) * 0.5f);
            Vector2 At(Vector2 point) => offset + point * scale;

            var painter = context.painter2D;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;

            painter.strokeColor = _frame;
            painter.lineWidth = FrameWidth * scale;
            painter.BeginPath();
            painter.MoveTo(At(Hexagon[0]));
            for (var i = 1; i < Hexagon.Length; i++)
                painter.LineTo(At(Hexagon[i]));
            painter.ClosePath();
            painter.Stroke();

            painter.lineWidth = AxisWidth * scale;
            for (var i = 0; i < AxisEnds.Length; i++)
            {
                painter.strokeColor = _axes[i];
                painter.BeginPath();
                painter.MoveTo(At(Center));
                painter.LineTo(At(AxisEnds[i]));
                painter.Stroke();
            }

            painter.fillColor = _frame;
            painter.BeginPath();
            painter.Arc(At(Center), DotRadius * scale, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }
    }
}

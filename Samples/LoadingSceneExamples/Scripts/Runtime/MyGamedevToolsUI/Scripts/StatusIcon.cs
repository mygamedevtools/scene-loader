using UnityEngine;
using UnityEngine.UIElements;

namespace MyGameDevTools.UI
{
    public enum Status
    {
        Success,
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// A shape-coded status icon (circle-check, circle-i, triangle, octagon-x) drawn in the
    /// matching status text color (mapped by .mgt-status-icon). Shapes differ, so status never relies on color.
    /// </summary>
    [UxmlElement]
    public partial class StatusIcon : VisualElement
    {
        public const string UssClassName = "mgt-status-icon";

        static readonly CustomStyleProperty<Color>[] ColorProperties =
        {
            new("--mgt-icon-success"),
            new("--mgt-icon-info"),
            new("--mgt-icon-warning"),
            new("--mgt-icon-error"),
        };

        readonly Color[] _colors = { Color.green, Color.cyan, Color.yellow, Color.red };
        Status _status = Status.Info;

        [UxmlAttribute]
        public Status Status
        {
            get => _status;
            set
            {
                _status = value;
                MarkDirtyRepaint();
            }
        }

        public StatusIcon() : this(Status.Info) { }

        public StatusIcon(Status status)
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _status = status;
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            for (var i = 0; i < ColorProperties.Length; i++)
            {
                if (evt.customStyle.TryGetValue(ColorProperties[i], out var color))
                    _colors[i] = color;
            }
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var size = Mathf.Min(rect.width, rect.height);
            if (size <= 0f)
                return;

            // Shapes on a 24x24 grid, matching the web icons.
            var scale = size / 24f;
            var offset = new Vector2(rect.x + (rect.width - size) * 0.5f, rect.y + (rect.height - size) * 0.5f);
            Vector2 At(float x, float y) => offset + new Vector2(x, y) * scale;

            var painter = context.painter2D;
            painter.strokeColor = _colors[(int)_status];
            painter.fillColor = _colors[(int)_status];
            painter.lineWidth = 2.2f * scale;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;

            void Line(float x1, float y1, float x2, float y2)
            {
                painter.BeginPath();
                painter.MoveTo(At(x1, y1));
                painter.LineTo(At(x2, y2));
                painter.Stroke();
            }

            void Circle()
            {
                painter.BeginPath();
                painter.Arc(At(12f, 12f), 9f * scale, Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Stroke();
            }

            void Dot(float x, float y)
            {
                painter.BeginPath();
                painter.Arc(At(x, y), 1.4f * scale, Angle.Degrees(0f), Angle.Degrees(360f));
                painter.Fill();
            }

            switch (_status)
            {
                case Status.Success:
                    Circle();
                    painter.BeginPath();
                    painter.MoveTo(At(8f, 12.5f));
                    painter.LineTo(At(10.7f, 15.2f));
                    painter.LineTo(At(16.2f, 9.5f));
                    painter.Stroke();
                    break;
                case Status.Info:
                    Circle();
                    Line(12f, 11f, 12f, 16.5f);
                    Dot(12f, 7.8f);
                    break;
                case Status.Warning:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 3.5f));
                    painter.LineTo(At(21.5f, 20f));
                    painter.LineTo(At(2.5f, 20f));
                    painter.ClosePath();
                    painter.Stroke();
                    Line(12f, 10f, 12f, 14.5f);
                    Dot(12f, 17.4f);
                    break;
                default:
                    painter.BeginPath();
                    painter.MoveTo(At(8.3f, 3f));
                    painter.LineTo(At(15.7f, 3f));
                    painter.LineTo(At(21f, 8.3f));
                    painter.LineTo(At(21f, 15.7f));
                    painter.LineTo(At(15.7f, 21f));
                    painter.LineTo(At(8.3f, 21f));
                    painter.LineTo(At(3f, 15.7f));
                    painter.LineTo(At(3f, 8.3f));
                    painter.ClosePath();
                    painter.Stroke();
                    Line(9.2f, 9.2f, 14.8f, 14.8f);
                    Line(14.8f, 9.2f, 9.2f, 14.8f);
                    break;
            }
        }
    }
}

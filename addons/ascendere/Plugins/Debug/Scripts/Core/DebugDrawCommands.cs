using Godot;

namespace Ascendere.Debug
{
    /// <summary>
    /// Base class for all debug draw commands
    /// </summary>
    public abstract class DebugDrawCommand
    {
        public float Duration;
        public abstract void Execute(CanvasItem canvas, Node node);
    }

    public class DebugDrawLine3D : DebugDrawCommand
    {
        public Vector3 From,
            To;
        public Color Color;
        public float Thickness;

        public DebugDrawLine3D(
            Vector3 from,
            Vector3 to,
            Color color,
            float duration,
            float thickness
        )
        {
            From = from;
            To = to;
            Color = color;
            Duration = duration;
            Thickness = thickness;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null)
                return;

            var from2D = cam.UnprojectPosition(From);
            var to2D = cam.UnprojectPosition(To);
            canvas.DrawLine(from2D, to2D, Color, Thickness);
        }
    }

    public class DebugDrawSphere : DebugDrawCommand
    {
        public Vector3 Position;
        public float Radius;
        public Color Color;

        public DebugDrawSphere(Vector3 pos, float radius, Color color, float duration)
        {
            Position = pos;
            Radius = radius;
            Color = color;
            Duration = duration;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null)
                return;

            var center2D = cam.UnprojectPosition(Position);
            var edge2D = cam.UnprojectPosition(Position + new Vector3(Radius, 0, 0));
            var radius2D = center2D.DistanceTo(edge2D);

            canvas.DrawArc(center2D, radius2D, 0, Mathf.Tau, 32, Color, 2f);

            var edgeY = cam.UnprojectPosition(Position + new Vector3(0, Radius, 0));
            var radiusY = center2D.DistanceTo(edgeY);
            canvas.DrawArc(center2D, radiusY, 0, Mathf.Tau, 32, Color, 1f);
        }
    }

    public class DebugDrawBox : DebugDrawCommand
    {
        public Vector3 Position;
        public Vector3 Size;
        public Color Color;

        public DebugDrawBox(Vector3 pos, Vector3 size, Color color, float duration)
        {
            Position = pos;
            Size = size;
            Color = color;
            Duration = duration;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null)
                return;

            Vector3 hs = Size / 2;
            Vector3[] corners =
            {
                Position + new Vector3(-hs.X, -hs.Y, -hs.Z),
                Position + new Vector3(hs.X, -hs.Y, -hs.Z),
                Position + new Vector3(hs.X, -hs.Y, hs.Z),
                Position + new Vector3(-hs.X, -hs.Y, hs.Z),
                Position + new Vector3(-hs.X, hs.Y, -hs.Z),
                Position + new Vector3(hs.X, hs.Y, -hs.Z),
                Position + new Vector3(hs.X, hs.Y, hs.Z),
                Position + new Vector3(-hs.X, hs.Y, hs.Z),
            };

            int[] edges =
            {
                0,
                1,
                1,
                2,
                2,
                3,
                3,
                0,
                4,
                5,
                5,
                6,
                6,
                7,
                7,
                4,
                0,
                4,
                1,
                5,
                2,
                6,
                3,
                7
            };

            for (int i = 0; i < edges.Length; i += 2)
            {
                var from2D = cam.UnprojectPosition(corners[edges[i]]);
                var to2D = cam.UnprojectPosition(corners[edges[i + 1]]);
                canvas.DrawLine(from2D, to2D, Color, 2f);
            }
        }
    }

    public class DebugDrawArrow : DebugDrawCommand
    {
        public Vector3 From,
            To;
        public Color Color;

        public DebugDrawArrow(Vector3 from, Vector3 to, Color color, float duration)
        {
            From = from;
            To = to;
            Color = color;
            Duration = duration;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null)
                return;

            var from2D = cam.UnprojectPosition(From);
            var to2D = cam.UnprojectPosition(To);
            canvas.DrawLine(from2D, to2D, Color, 3f);

            var dir = (to2D - from2D).Normalized();
            var right = new Vector2(-dir.Y, dir.X);
            var arrowSize = 15f;

            var arrow1 = to2D - dir * arrowSize + right * arrowSize * 0.5f;
            var arrow2 = to2D - dir * arrowSize - right * arrowSize * 0.5f;

            canvas.DrawLine(to2D, arrow1, Color, 3f);
            canvas.DrawLine(to2D, arrow2, Color, 3f);
        }
    }

    public class DebugDrawLabel : DebugDrawCommand
    {
        public Vector3 Position;
        public string Text;
        public Color Color;

        public DebugDrawLabel(Vector3 pos, string text, Color color, float duration)
        {
            Position = pos;
            Text = text;
            Color = color;
            Duration = duration;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null)
                return;

            var pos2D = cam.UnprojectPosition(Position);

            var bgRect = new Rect2(
                pos2D - new Vector2(5, 20),
                new Vector2(Text.Length * 8 + 10, 25)
            );
            canvas.DrawRect(bgRect, new Color(0, 0, 0, 0.7f));

            canvas.DrawString(
                ThemeDB.FallbackFont,
                pos2D,
                Text,
                HorizontalAlignment.Left,
                -1,
                16,
                Color
            );
        }
    }

    public class DebugDrawPath : DebugDrawCommand
    {
        public Vector3[] Points;
        public Color Color;
        public bool Closed;

        public DebugDrawPath(Vector3[] points, Color color, float duration, bool closed)
        {
            Points = points;
            Color = color;
            Duration = duration;
            Closed = closed;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null || Points.Length < 2)
                return;

            for (int i = 0; i < Points.Length - 1; i++)
            {
                var from2D = cam.UnprojectPosition(Points[i]);
                var to2D = cam.UnprojectPosition(Points[i + 1]);
                canvas.DrawLine(from2D, to2D, Color, 2f);
            }

            if (Closed && Points.Length > 2)
            {
                var from2D = cam.UnprojectPosition(Points[Points.Length - 1]);
                var to2D = cam.UnprojectPosition(Points[0]);
                canvas.DrawLine(from2D, to2D, Color, 2f);
            }
        }
    }

    public class DebugDrawRay : DebugDrawCommand
    {
        public Vector3 Origin,
            Direction;
        public float Length;
        public Color Color;

        public DebugDrawRay(
            Vector3 origin,
            Vector3 direction,
            float length,
            Color color,
            float duration
        )
        {
            Origin = origin;
            Direction = direction.Normalized();
            Length = length;
            Color = color;
            Duration = duration;
        }

        public override void Execute(CanvasItem canvas, Node node)
        {
            var cam = node.GetViewport().GetCamera3D();
            if (cam == null)
                return;

            var to = Origin + Direction * Length;
            
            // Check if positions are in front of camera
            var camTransform = cam.GlobalTransform;
            var camForward = -camTransform.Basis.Z;
            var toOrigin = Origin - cam.GlobalPosition;
            var toEnd = to - cam.GlobalPosition;
            
            // Skip if points are behind the camera
            float originDist = toOrigin.Dot(camForward);
            float endDist = toEnd.Dot(camForward);
            
            if (originDist <= 0.01f || endDist <= 0.01f)
                return;
            
            var from2D = cam.UnprojectPosition(Origin);
            var to2D = cam.UnprojectPosition(to);

            canvas.DrawLine(from2D, to2D, Color, 2f);

            var dir = (to2D - from2D).Normalized();
            var right = new Vector2(-dir.Y, dir.X);
            var arrowSize = 10f;

            var arrow1 = to2D - dir * arrowSize + right * arrowSize * 0.5f;
            var arrow2 = to2D - dir * arrowSize - right * arrowSize * 0.5f;

            canvas.DrawLine(to2D, arrow1, Color, 2f);
            canvas.DrawLine(to2D, arrow2, Color, 2f);
        }
    }
}

using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Editors.Animation;

/// <summary>Native 3D rig handles. Math and export stay in NWN coordinates; only projection is screen space.</summary>
public sealed class AnimationRigControl : Control
{
    private AnimationEditorDocumentViewModel? _viewModel;
    private Point? _last;
    private bool _orbit;
    private float _yaw = 0.35f, _pitch = 0.18f, _zoom = 1;
    private Vector3 _focus = new(0, 0, 1);
    private float _height = 2.2f;
    private string? _rigSignature;
    private readonly List<(string Axis, Point Point)> _rings = [];
    private readonly Dictionary<int, Point> _joints = [];
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#171e28"));
    private static readonly IBrush BoneBrush = new SolidColorBrush(Color.Parse("#59cfdf"));
    private static readonly IBrush SelectedBrush = new SolidColorBrush(Color.Parse("#ffcb69"));
    private static readonly IBrush SourceBrush = new SolidColorBrush(Color.Parse("#e66e7d"));
    public AnimationRigControl()
    {
        ClipToBounds = true; Focusable = true; MinHeight = 220;
        DataContextChanged += (_, _) => Attach();
    }
    private void Attach()
    {
        if (_viewModel != null) _viewModel.PoseChanged -= OnPoseChanged;
        _viewModel = DataContext as AnimationEditorDocumentViewModel;
        if (_viewModel != null) { _viewModel.PoseChanged += OnPoseChanged; OnPoseChanged(); }
        InvalidateVisual();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        EndDrag();
        if (_viewModel != null) _viewModel.PoseChanged -= OnPoseChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); Attach(); }
    private void OnPoseChanged()
    {
        var signature = _viewModel?.Project.ModelName + ":" + string.Join(',', _viewModel?.JointNames ?? []);
        if (_rigSignature != signature) { _rigSignature = signature; FrameRig(); }
        InvalidateVisual();
    }
    public void FrameRig()
    {
        if (_viewModel == null) return;
        var world = AnimationRig.World(_viewModel.Project.Joints, _viewModel.Pose);
        var min = world.Select(m => m.Translation).Aggregate(Vector3.Min);
        var max = world.Select(m => m.Translation).Aggregate(Vector3.Max);
        _focus = (min + max) / 2; _height = Math.Max(0.5f, (max - min).Length() * 1.3f); _zoom = 1;
        InvalidateVisual();
    }
    private Vector3 Right => new(MathF.Cos(_yaw), MathF.Sin(_yaw), 0);
    private Vector3 Up => new(MathF.Sin(_yaw) * MathF.Sin(_pitch), -MathF.Cos(_yaw) * MathF.Sin(_pitch), MathF.Cos(_pitch));
    private float Scale => (float)Math.Max(1, Math.Min(Bounds.Width, Bounds.Height)) / _height * _zoom;
    private Point Project(Vector3 point)
    {
        var relative = point - _focus;
        return new(Bounds.Width / 2 + Vector3.Dot(relative, Right) * Scale, Bounds.Height / 2 - Vector3.Dot(relative, Up) * Scale);
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#2c3748")), 1);
        for (var i = -10; i <= 10; i++)
        {
            context.DrawLine(gridPen, Project(new(i * .25f, -2.5f, 0)), Project(new(i * .25f, 2.5f, 0)));
            context.DrawLine(gridPen, Project(new(-2.5f, i * .25f, 0)), Project(new(2.5f, i * .25f, 0)));
        }
        _joints.Clear(); _rings.Clear();
        if (_viewModel == null) return;
        var world = AnimationRig.World(_viewModel.Project.Joints, _viewModel.Pose);
        for (var i = 0; i < world.Length; i++)
        {
            var point = Project(world[i].Translation); _joints[i] = point;
            var parent = _viewModel.Project.Joints[i].Parent;
            if (parent >= 0) context.DrawLine(new Pen(i == _viewModel.SelectedJoint ? SelectedBrush : BoneBrush, 3), Project(world[parent].Translation), point);
            context.DrawEllipse(i == _viewModel.SelectedJoint ? SelectedBrush : BoneBrush, null, point, i == _viewModel.SelectedJoint ? 6 : 3, i == _viewModel.SelectedJoint ? 6 : 3);
        }
        try
        {
            var source = _viewModel.SourcePositions();
            for (var i = 0; i < source.Length; i++)
            {
                var parent = _viewModel.SourceJoints[i].Parent;
                if (parent >= 0) context.DrawLine(new Pen(SourceBrush, 1), Project(source[parent]), Project(source[i]));
            }
        }
        catch (InvalidDataException) { /* The source loader/bake command reports invalid input. */ }
        var selected = world[_viewModel.SelectedJoint].Translation;
        Matrix4x4.Decompose(world[_viewModel.SelectedJoint], out _, out var rotation, out _);
        foreach (var (axis, color) in new[] { ("X", "#f07777"), ("Y", "#75cd8c"), ("Z", "#77aafa") })
        {
            var brush = new SolidColorBrush(Color.Parse(color));
            var pen = new Pen(brush, _viewModel.Axis == axis ? 2.5 : 1);
            Point? previous = null;
            for (var segment = 0; segment <= 48; segment++)
            {
                var angle = MathF.Tau * segment / 48;
                var a = MathF.Cos(angle) * .18f; var b = MathF.Sin(angle) * .18f;
                var local = axis == "X" ? new Vector3(0, a, b) : axis == "Y" ? new(a, 0, b) : new(a, b, 0);
                var point = Project(selected + Vector3.Transform(local, rotation));
                _rings.Add((axis, point));
                if (previous.HasValue) context.DrawLine(pen, previous.Value, point);
                previous = point;
            }
        }
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_viewModel == null || _viewModel.IsBusy) return;
        var point = e.GetCurrentPoint(this);
        _orbit = point.Properties.IsRightButtonPressed;
        if (!_orbit && !point.Properties.IsLeftButtonPressed) return;
        Focus();
        if (!_orbit)
        {
            var ring = _rings.OrderBy(p => Distance(p.Point, point.Position)).FirstOrDefault();
            var joint = _joints.OrderBy(p => Distance(p.Value, point.Position)).FirstOrDefault();
            if (_rings.Count > 0 && Distance(ring.Point, point.Position) < 7 && Distance(joint.Value, point.Position) > 9)
                _viewModel.SelectGizmoAxis(ring.Axis);
            else if (_joints.Count > 0 && Distance(joint.Value, point.Position) < 16) _viewModel.SelectedJoint = joint.Key;
            else return;
            _viewModel.BeginDrag();
        }
        _last = point.Position; e.Pointer.Capture(this); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_last.HasValue) return;
        var point = e.GetPosition(this); var delta = point - _last.Value; _last = point;
        if (_orbit) { _yaw += (float)delta.X * .01f; _pitch = Math.Clamp(_pitch + (float)delta.Y * .01f, -1.4f, 1.4f); }
        else _viewModel?.Drag(Right * (float)delta.X / Scale - Up * (float)delta.Y / Scale, (float)(delta.X - delta.Y) * .012f);
        InvalidateVisual(); e.Handled = true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e) { EndDrag(); e.Pointer.Capture(null); base.OnPointerReleased(e); }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { EndDrag(); base.OnPointerCaptureLost(e); }
    private void EndDrag() { if (_last.HasValue && !_orbit) _viewModel?.EndDrag(); _last = null; }
    private static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        _zoom = Math.Clamp(_zoom * MathF.Pow(1.15f, (float)e.Delta.Y), .1f, 20); InvalidateVisual(); e.Handled = true;
    }
}

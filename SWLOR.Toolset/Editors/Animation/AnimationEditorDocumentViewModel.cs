using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Animation;

public sealed class BoneMappingRow : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public required string Target { get; init; }
    private string? _source;
    public string? Source { get => _source; set => SetProperty(ref _source, value); }
}

/// <summary>A docked native animation authoring document with ordinary shell save/undo/close behavior.</summary>
public sealed partial class AnimationEditorDocumentViewModel : Document, IEditorDocument, IModelPreviewSource
{
    private readonly IEditorPromptService _prompts;
    private readonly OutputLogService _log;
    private readonly string? _repositoryRoot;
    private readonly ModuleMutationLock? _mutationLock;
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Stopwatch _clock = new();
    private string _saved;
    private string? _path;
    private byte[]? _diskBytes;
    private PosedNode[]? _copiedPose;
    private bool _dragging, _dragChanged;
    private MdlModel? _model;
    private GltfAnimationSource? _source;
    private AnimationRetarget? _calibration;
    private string? _calibrationMap;
    private bool _closeApproved, _closePrompt, _closed, _previewVisible;
    private float _playhead, _playStart;
    private int _selectedJoint;
    private int _sourceClip;
    private decimal _rootScale = 1;
    private string _status = "Load an NWN model to begin posing. Left drag edits; right drag orbits; mouse wheel zooms.";

    public AnimationEditorDocumentViewModel(IEditorPromptService prompts, OutputLogService log,
        ResourceIndex? resources = null, string? repositoryRoot = null, ModuleMutationLock? mutationLock = null,
        AnimationProject? initial = null)
    {
        _prompts = prompts; _log = log; ResourceIndex = resources; _repositoryRoot = repositoryRoot; _mutationLock = mutationLock;
        Project = initial?.Clone() ?? new AnimationProject
        {
            Joints = [new("rootdummy", -1, new(Vector3.Zero, Quaternion.Identity, 1))]
        };
        Pose = Project.Sample(0); _saved = Project.Serialize();
        Id = "animation-editor"; Title = "Animation Editor"; CanClose = true;
        _timer.Tick += Tick;
        RebuildRows();
    }

    public AnimationProject Project { get; private set; }
    public PosedNode[] Pose { get; private set; }
    public ResourceIndex? ResourceIndex { get; }
    public AreaScene? PreviewScene { get; private set; }
    public string? PreviewAnimationName => null;
    public bool IsAnimationPlaying => false;
    public bool IsPlaying => _timer.IsEnabled;
    public bool IsBusy { get; private set; }
    public bool IsDirty => _dragging && _dragChanged || Project.Serialize() != _saved;
    public bool CanUndo => !IsBusy && !_dragging && _undo.Count > 0;
    public bool CanRedo => !IsBusy && !_dragging && _redo.Count > 0;
    public ObservableCollection<string> JointNames { get; } = [];
    public ObservableCollection<float> KeyTimes { get; } = [];
    public ObservableCollection<BoneMappingRow> Mappings { get; } = [];
    public ObservableCollection<string> SourceBones { get; } = [];
    public ObservableCollection<string> SourceClips { get; } = [];
    public string RigResource { get; set; } = "a_ba";
    public string TargetPaths { get; set; } = "";
    public string EditMode { get; set; } = "Rotate";
    public IReadOnlyList<string> EditModes { get; } = ["Rotate", "Move", "IK"];
    public string Axis { get; set; } = "Z";
    public IReadOnlyList<string> Axes { get; } = ["X", "Y", "Z"];
    public int SourceClip
    {
        get => _sourceClip;
        set
        {
            var clip = Math.Clamp(value, 0, Math.Max(0, SourceClips.Count - 1));
            if (_sourceClip != clip)
            {
                _calibration = null;
                Status = "Source clip changed. Lock calibration before baking this clip.";
            }
            _sourceClip = clip;
            if (_source != null && CanEdit()) Duration = (decimal)_source.GetPlaybackDuration(_sourceClip);
            OnPropertyChanged(); PoseChanged?.Invoke();
        }
    }
    public int BakeRate { get; set; } = 30;
    public decimal RootScale { get => _rootScale; set { _rootScale = Math.Clamp(value, .001m, 1000m); OnPropertyChanged(); PoseChanged?.Invoke(); } }
    public bool AnchorLimbs { get; set; } = true;
    public decimal PoleX { get; set; }
    public decimal PoleY { get; set; } = 1;
    public decimal PoleZ { get; set; } = 1;
    public string EventName { get; set; } = "cast";
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public string PathDisplay => _path ?? "Unsaved animation project";
    public string AnimationName { get => Project.Name; set => Edit(project => project.Name = value, false); }
    public decimal Duration
    {
        get => (decimal)Project.Duration;
        set => Edit(project =>
        {
            var duration = (float)value;
            var ratio = duration / project.Duration;
            project.Keys = project.Keys.Select(k => k with { Time = Math.Min(duration, k.Time * ratio) }).ToList();
            project.Events = project.Events.Select(e => e with { Time = Math.Min(duration, e.Time * ratio) }).ToList();
            project.Duration = duration;
        });
    }
    public decimal Transition { get => (decimal)Project.Transition; set => Edit(project => project.Transition = (float)value, false); }
    public double Playhead
    {
        get => _playhead;
        set
        {
            if (!double.IsFinite(value)) return;
            EndDrag();
            _playhead = Math.Clamp((float)value, 0, Project.Duration);
            Pose = Project.Sample(_playhead); NotifyPose(); OnPropertyChanged();
        }
    }
    public int SelectedJoint
    {
        get => _selectedJoint;
        set { _selectedJoint = Math.Clamp(value, 0, Project.Joints.Count - 1); OnPropertyChanged(); NotifyPose(); }
    }
    public decimal PositionX { get => (decimal)Pose[SelectedJoint].Position.X; set => SetPosition(0, (float)value); }
    public decimal PositionY { get => (decimal)Pose[SelectedJoint].Position.Y; set => SetPosition(1, (float)value); }
    public decimal PositionZ { get => (decimal)Pose[SelectedJoint].Position.Z; set => SetPosition(2, (float)value); }
    public decimal RotationX { get; set; }
    public decimal RotationY { get; set; }
    public decimal RotationZ { get; set; }
    public decimal JointScale { get => (decimal)Pose[SelectedJoint].Scale; set => SetJoint(Pose[SelectedJoint] with { Scale = (float)value }); }
    public string EventsDisplay => string.Join(" · ", Project.Events.Select(e => $"{e.Time:0.###}s {e.Name}"));
    public event Action<AnimationEditorDocumentViewModel>? Closed;
    public event Action<AnimationEditorDocumentViewModel>? CloseRequested;
    public event Action? PoseChanged;
    public Func<string, string[], Task<string?>>? PickOpenPath { get; set; }
    public Func<string, string, Task<string?>>? PickSavePath { get; set; }

    private bool CanEdit() => !IsBusy && !_closed && _mutationLock?.IsLocked != true;
    private void Edit(Action<AnimationProject> edit, bool invalidateCalibration = true)
    {
        if (!CanEdit() || _dragging) return;
        var before = Project.Serialize();
        try
        {
            var next = Project.Clone(); edit(next); next.Validate();
            if (next.Serialize() == before) return;
            PushHistory(_undo, before); _redo.Clear(); Project = next;
            if (invalidateCalibration) _calibration = null;
            Changed();
        }
        catch (Exception ex) { Status = ex.GetBaseException().Message; }
    }
    private void Replace(AnimationProject project, bool invalidateCalibration = true)
    {
        _ = project.Serialize(); PushHistory(_undo, Project.Serialize()); _redo.Clear(); Project = project;
        if (invalidateCalibration) _calibration = null;
        Changed(rebuildRows: true);
    }
    private static void PushHistory(Stack<string> history, string value)
    {
        history.Push(value);
        // Keep recent authoring history within 32 MiB of UTF-16 text, including dense imported clips.
        long bytes = 0;
        var keep = history.TakeWhile(entry => (bytes += entry.Length * 2L) <= 32 * 1024 * 1024).Take(100).ToArray();
        history.Clear(); foreach (var entry in keep.Reverse()) history.Push(entry);
    }
    private void Changed(bool rebuildRows = false)
    {
        Stop(); _playhead = Math.Clamp(_playhead, 0, Project.Duration); Pose = Project.Sample(_playhead);
        if (rebuildRows && !JointNames.SequenceEqual(Project.Joints.Select(j => j.Name))) RebuildRows();
        KeyTimes.Clear(); foreach (var key in Project.Keys) KeyTimes.Add(key.Time);
        Title = IsDirty ? "Animation Editor *" : "Animation Editor";
        foreach (var property in new[] { nameof(AnimationName), nameof(Duration), nameof(Transition), nameof(Playhead), nameof(IsDirty),
                     nameof(CanUndo), nameof(CanRedo), nameof(PathDisplay), nameof(EventsDisplay) }) OnPropertyChanged(property);
        NotifyPose();
    }
    private void RebuildRows()
    {
        JointNames.Clear(); foreach (var joint in Project.Joints) JointNames.Add(joint.Name);
        Mappings.Clear(); foreach (var joint in Project.Joints) Mappings.Add(new() { Target = joint.Name });
        _selectedJoint = Math.Clamp(_selectedJoint, 0, Project.Joints.Count - 1);
        _calibration = null;
    }
    private void NotifyPose()
    {
        var q = Pose[SelectedJoint].Orientation;
        const double degrees = 180 / Math.PI;
        RotationX = (decimal)(Math.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y)) * degrees);
        RotationY = (decimal)(Math.Asin(Math.Clamp(2 * (q.W * q.Y - q.Z * q.X), -1, 1)) * degrees);
        RotationZ = (decimal)(Math.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Y * q.Y + q.Z * q.Z)) * degrees);
        OnPropertyChanged(nameof(RotationX)); OnPropertyChanged(nameof(RotationY)); OnPropertyChanged(nameof(RotationZ));
        foreach (var property in new[] { nameof(PositionX), nameof(PositionY), nameof(PositionZ), nameof(JointScale) }) OnPropertyChanged(property);
        PoseChanged?.Invoke();
        if (_previewVisible) RefreshPreview();
    }
    private void SetPosition(int axis, float value)
    {
        var position = Pose[SelectedJoint].Position; position[axis] = value;
        SetJoint(Pose[SelectedJoint] with { Position = position });
    }
    private void SetJoint(PosedNode value) => Edit(project =>
    {
        var pose = AnimationRig.SetJoint(project.Joints, Pose, SelectedJoint, value, AnchorLimbs); project.SetKey(_playhead, pose);
    });

    [RelayCommand] private void Rotate()
    {
        const float radians = MathF.PI / 180;
        SetJoint(Pose[SelectedJoint] with { Orientation = Quaternion.Normalize(
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)RotationZ * radians) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)RotationY * radians) *
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)RotationX * radians)) });
    }
    [RelayCommand] private void SetKey() => Edit(p => p.SetKey(_playhead, Pose));
    [RelayCommand] private void RemoveKey() => Edit(p => p.Keys.RemoveAll(k => Math.Abs(k.Time - _playhead) < 0.00001f));
    [RelayCommand] private void CopyPose() { _copiedPose = (PosedNode[])Pose.Clone(); Status = "Pose copied."; }
    [RelayCommand] private void PastePose()
    {
        if (_copiedPose?.Length == Project.Joints.Count) Edit(p => p.SetKey(_playhead, _copiedPose));
    }
    [RelayCommand] private void ResetPose() => Edit(p => p.SetKey(_playhead, p.Joints.Select(j => j.Rest).ToArray()));
    [RelayCommand] private void AddEvent() => Edit(p => p.Events.Add(new(_playhead, EventName)));
    [RelayCommand] private void RemoveEvents() => Edit(p => p.Events.RemoveAll(e => Math.Abs(e.Time - _playhead) < 0.00001f));
    [RelayCommand] private void Play()
    {
        if (!CanEdit()) return;
        EndDrag();
        if (IsPlaying) { Stop(); return; }
        _playStart = _playhead; _clock.Restart(); _timer.Start(); OnPropertyChanged(nameof(IsPlaying));
    }
    public void Stop() { _timer.Stop(); _clock.Stop(); OnPropertyChanged(nameof(IsPlaying)); }
    private void Tick(object? sender, EventArgs e) => Playhead = (_playStart + _clock.Elapsed.TotalSeconds) % Project.Duration;
    public void Undo()
    {
        if (!CanUndo || !CanEdit()) return;
        PushHistory(_redo, Project.Serialize()); Project = AnimationProject.Deserialize(_undo.Pop()); _calibration = null; Changed(rebuildRows: true);
    }
    public void Redo()
    {
        if (!CanRedo || !CanEdit()) return;
        PushHistory(_undo, Project.Serialize()); Project = AnimationProject.Deserialize(_redo.Pop()); _calibration = null; Changed(rebuildRows: true);
    }

    public void BeginDrag()
    {
        if (!CanEdit()) return;
        Stop(); _dragging = true; _dragChanged = false;
        OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanRedo));
    }
    public void SelectGizmoAxis(string axis)
    {
        Axis = axis; EditMode = "Rotate"; OnPropertyChanged(nameof(Axis)); OnPropertyChanged(nameof(EditMode));
    }
    public void Drag(Vector3 worldDelta, float angle)
    {
        if (!_dragging || !CanEdit()) return;
        try
        {
            var next = (PosedNode[])Pose.Clone();
            var world = AnimationRig.World(Project.Joints, next);
            if (EditMode == "IK")
                next = AnimationRig.SolveLimb(Project.Joints, next, SelectedJoint, world[SelectedJoint].Translation + worldDelta,
                    new((float)PoleX, (float)PoleY, (float)PoleZ));
            else if (EditMode == "Move")
            {
                var parent = Project.Joints[SelectedJoint].Parent;
                var delta = worldDelta;
                if (parent >= 0 && Matrix4x4.Invert(world[parent], out var inverse)) delta = Vector3.TransformNormal(delta, inverse);
                next[SelectedJoint] = next[SelectedJoint] with { Position = next[SelectedJoint].Position + delta };
            }
            else
            {
                var axis = Axis == "X" ? Vector3.UnitX : Axis == "Y" ? Vector3.UnitY : Vector3.UnitZ;
                next[SelectedJoint] = next[SelectedJoint] with { Orientation = Quaternion.Normalize(next[SelectedJoint].Orientation * Quaternion.CreateFromAxisAngle(axis, angle)) };
            }
            if (EditMode != "IK") next = AnimationRig.SetJoint(Project.Joints, Pose, SelectedJoint, next[SelectedJoint], AnchorLimbs);
            foreach (var transform in next) AnimationProject.ValidatePose(transform);
            // Dragging updates only the preview. Commit through the validated document edit once
            // the pointer is released, keeping rejected key/size limits out of the live project.
            if (next.SequenceEqual(Pose)) return;
            Pose = next; _dragChanged = true; _calibration = null;
            OnPropertyChanged(nameof(IsDirty)); NotifyPose();
        }
        catch (Exception ex) { Status = ex.GetBaseException().Message; }
    }
    public void EndDrag()
    {
        if (!_dragging) return;
        var pose = (PosedNode[])Pose.Clone(); _dragging = false;
        if (_dragChanged) Edit(project => project.SetKey(_playhead, pose));
        Changed(); // Restore the saved timeline pose if validation rejected the drag.
    }

    [RelayCommand] private async Task LoadRig() => await Run(async () =>
    {
        if (!await ConfirmReplace()) return;
        if (ResourceIndex == null || !ResourceIndex.TryLookup(ResourceIdentity.FromFileName(RigResource + ".mdl"), out var resource))
            throw new InvalidDataException($"Model '{RigResource}' was not found. Open a local MDL using Load rig file.");
        var model = await Task.Run(() => new MdlReader().Parse(resource.GetBytes()));
        LoadModel(model);
        TargetPaths = ResolveInstallTarget(model.Name, resource.Provenance.SourcePath); OnPropertyChanged(nameof(TargetPaths));
    });
    [RelayCommand] private async Task LoadRigFile() => await Run(async () =>
    {
        var path = await OpenPath("Load NWN rig", ["*.mdl"]);
        if (path == null || !await ConfirmReplace()) return;
        var model = await Task.Run(() => new MdlReader().Parse(File.ReadAllBytes(path)));
        LoadModel(model); TargetPaths = ResolveInstallTarget(model.Name, path); OnPropertyChanged(nameof(TargetPaths));
    });
    private string ResolveInstallTarget(string resref, string fallbackPath)
    {
        var source = _repositoryRoot == null ? null : AnimationInstall.FindTargetSource(_repositoryRoot, resref);
        return source ?? (Path.GetExtension(fallbackPath).Equals(".mdl", StringComparison.OrdinalIgnoreCase) && File.Exists(fallbackPath) ? fallbackPath : "");
    }
    [RelayCommand] private async Task AttachPreview() => await Run(async () =>
    {
        var path = await OpenPath("Attach matching preview model", ["*.mdl"]); if (path == null) return;
        var model = await Task.Run(() => new MdlReader().Parse(File.ReadAllBytes(path)));
        var rig = AnimationProject.FromModel(model);
        if (!MatchesPreviewRig(Project, rig))
            throw new InvalidDataException("The preview model must match this project's joint names, hierarchy, and rest transforms.");
        _model = model; RefreshPreview(); Status = "Preview model attached; animation keys are unchanged.";
    });
    private void LoadModel(MdlModel model)
    {
        var project = AnimationProject.FromModel(model);
        _model = model; Project = project; _undo.Clear(); _redo.Clear(); _copiedPose = null; _calibration = null; _path = null; _diskBytes = null;
        TargetPaths = ""; OnPropertyChanged(nameof(TargetPaths));
        _saved = project.Serialize(); _playhead = 0; Changed(rebuildRows: true);
        Status = $"Loaded {model.Name}: {project.Joints.Count} joints. Pose edits create a key at the playhead.";
    }
    [RelayCommand] private async Task OpenProject() => await Run(async () =>
    {
        var path = await OpenPath("Open animation project", ["*.swlanim"]);
        if (path == null || !await ConfirmReplace()) return;
        var bytes = await File.ReadAllBytesAsync(path);
        var project = AnimationProject.Deserialize(Encoding.UTF8.GetString(bytes));
        _model = await ResolvePreviewModel(project);
        Project = project; _path = path; _diskBytes = bytes; _saved = project.Serialize(); _undo.Clear(); _redo.Clear(); _copiedPose = null; _calibration = null;
        Changed(rebuildRows: true); Status = "Animation project opened.";
    });
    private static bool MatchesPreviewRig(AnimationProject project, AnimationProject rig) =>
        project.Joints.Count == rig.Joints.Count && project.Joints.Zip(rig.Joints).All(pair =>
            pair.First.Name.Equals(pair.Second.Name, StringComparison.OrdinalIgnoreCase) && pair.First.Parent == pair.Second.Parent &&
            Vector3.Distance(pair.First.Rest.Position, pair.Second.Rest.Position) < 1e-5f &&
            Math.Abs(pair.First.Rest.Scale - pair.Second.Rest.Scale) < 1e-5f &&
            Math.Abs(Quaternion.Dot(Quaternion.Normalize(pair.First.Rest.Orientation), Quaternion.Normalize(pair.Second.Rest.Orientation))) > .99999f);

    private async Task<MdlModel?> ResolvePreviewModel(AnimationProject project)
    {
        try
        {
            if (ResourceIndex?.TryLookup(ResourceIdentity.FromFileName(project.ModelName + ".mdl"), out var resource) == true)
            {
                var model = await Task.Run(() => new MdlReader().Parse(resource.GetBytes()));
                if (MatchesPreviewRig(project, AnimationProject.FromModel(model))) return model;
                _log.AppendLine("Mounted animation preview skipped: the model does not match the saved rig.");
            }
        }
        catch (Exception ex) { _log.AppendLine("Mounted animation preview skipped: " + ex.GetBaseException().Message); }
        return _model != null && _model.Name.Equals(project.ModelName, StringComparison.OrdinalIgnoreCase) &&
            MatchesPreviewRig(project, AnimationProject.FromModel(_model)) ? _model : null;
    }
    [RelayCommand] private async Task ImportMdl() => await Run(async () =>
    {
        var path = await OpenPath("Import one MDL animation block", ["*.txt", "*.mdl"]);
        if (path == null) return;
        Replace(AnimationMdl.Import(await File.ReadAllTextAsync(path), Project));
        Status = "Animation imported onto the loaded rig.";
    });
    [RelayCommand] private async Task ExportMdl() => await Run(async () =>
    {
        var path = await SavePath("Export MDL animation block", Project.Name + ".txt");
        if (path != null) { await File.WriteAllTextAsync(path, AnimationMdl.Export(Project)); Status = "MDL animation block exported."; }
    });
    [RelayCommand] private async Task SaveProject() => await TrySaveAsync();
    [RelayCommand] private async Task SaveAs() => await Run(async () =>
    {
        var path = await SavePath("Save animation project as", Project.Name + ".swlanim");
        if (path != null) await SaveTo(path, checkExternal: false);
    });
    public async Task<bool> TrySaveAsync()
    {
        if (IsBusy || _closed) return false;
        EndDrag();
        try
        {
            ModuleMutationLock.ThrowIfModuleLocked();
            Stop(); IsBusy = true; OnPropertyChanged(nameof(IsBusy));
            var path = _path ?? await SavePath("Save animation project", Project.Name + ".swlanim");
            return path != null && await SaveTo(path, checkExternal: _path != null);
        }
        catch (Exception ex) { Status = ex.GetBaseException().Message; return false; }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanRedo)); }
    }
    private async Task<bool> SaveTo(string path, bool checkExternal)
    {
        try
        {
            ModuleMutationLock.ThrowIfModuleLocked();
            if (checkExternal && (_diskBytes == null || !File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(_diskBytes)))
            {
                var choice = await _prompts.ConfirmExternalChangeAsync(path);
                if (choice == ExternalChangeChoice.Cancel) return false;
                if (choice == ExternalChangeChoice.Reload)
                {
                    var bytes = await File.ReadAllBytesAsync(path);
                    var reloaded = AnimationProject.Deserialize(Encoding.UTF8.GetString(bytes));
                    _model = await ResolvePreviewModel(reloaded); Project = reloaded;
                    _diskBytes = bytes; _saved = Project.Serialize(); _undo.Clear(); _redo.Clear(); _copiedPose = null; _calibration = null; Changed(rebuildRows: true); return false;
                }
            }
            var serialized = Project.Serialize(); var data = Encoding.UTF8.GetBytes(serialized);
            var acceptedBytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporary, data);
                ModuleMutationLock.ThrowIfModuleLocked();
                if (acceptedBytes == null ? File.Exists(path) : !File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(acceptedBytes))
                    throw new IOException("The project changed while saving. Save again to review the external change.");
                File.Move(temporary, path, overwrite: true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            _path = path; _diskBytes = data; _saved = serialized; Changed(); Status = "Animation project saved."; return true;
        }
        catch (Exception ex) { Status = ex.GetBaseException().Message; return false; }
    }

    [RelayCommand] private async Task LoadSource() => await Run(async () =>
    {
        var path = await OpenPath("Load retarget source", ["*.glb", "*.gltf"]);
        if (path == null) return;
        _source = await Task.Run(() => GltfAnimationSource.Load(path));
        SourceBones.Clear(); SourceBones.Add(""); foreach (var joint in _source.Joints) SourceBones.Add(joint.Name);
        SourceClips.Clear(); foreach (var clip in _source.Animations) SourceClips.Add(clip.Name);
        SourceClip = 0; OnPropertyChanged(nameof(SourceClip)); _calibration = null;
        var resized = Project.Clone();
        var duration = _source.GetPlaybackDuration(0); var ratio = duration / resized.Duration;
        resized.Keys = resized.Keys.Select(k => k with { Time = Math.Min(duration, k.Time * ratio) }).ToList();
        resized.Events = resized.Events.Select(cue => cue with { Time = Math.Min(duration, cue.Time * ratio) }).ToList();
        resized.Duration = duration; Replace(resized);
        Status = "Map NWN joints to source bones, pose a matching frame, then Lock and Bake."; PoseChanged?.Invoke();
    });
    public Vector3[] SourcePositions()
    {
        try { return _source == null ? [] : _source.Sample(SourceClip, _playhead).Select(m => m.Translation).ToArray(); }
        catch (InvalidDataException ex) { Status = ex.Message; return []; }
    }
    public IReadOnlyList<SourceJoint> SourceJoints => _source?.Joints ?? [];
    private string MapSnapshot() => JsonSerializer.Serialize(Mappings.Where(m => !string.IsNullOrEmpty(m.Source)).Select(m => new RetargetBinding(m.Target, m.Source!)));
    [RelayCommand] private void LockCalibration()
    {
        if (!CanEdit()) return;
        _calibration = null; _calibrationMap = null;
        try
        {
            if (_source == null) throw new InvalidDataException("Load a glTF source first.");
            _calibrationMap = MapSnapshot();
            _calibration = new AnimationRetarget(Project, Pose, _source, SourceClip, _playhead,
                JsonSerializer.Deserialize<RetargetBinding[]>(_calibrationMap)!);
            Status = "Calibration locked. Bake transfers the selected source clip onto the NWN timeline.";
        }
        catch (Exception ex) { Status = ex.GetBaseException().Message; }
    }
    [RelayCommand] private async Task Bake() => await Run(async () =>
    {
        if (_source == null || _calibration == null || _calibrationMap != MapSnapshot())
            throw new InvalidDataException("Lock calibration after setting the bone mapping.");
        var baked = await Task.Run(() => _calibration.Bake(_source, SourceClip, BakeRate, (float)RootScale));
        baked.Name = Project.Name; baked.Transition = Project.Transition;
        Replace(baked, false); Status = $"Baked {Project.Keys.Count} keyframes.";
    });
    [RelayCommand] private async Task SaveMapping() => await Run(async () =>
    {
        var path = await SavePath("Save bone mapping", "bone-map.json");
        if (path != null) await File.WriteAllTextAsync(path, MapSnapshot());
    });
    [RelayCommand] private async Task LoadMapping() => await Run(async () =>
    {
        var path = await OpenPath("Load bone mapping", ["*.json"]); if (path == null) return;
        var map = JsonSerializer.Deserialize<RetargetBinding[]>(await File.ReadAllTextAsync(path)) ?? throw new InvalidDataException("Invalid bone mapping.");
        if (map.Any(b => !Mappings.Any(m => m.Target == b.Target) || !SourceBones.Contains(b.Source)) || map.Select(m => m.Target).Distinct().Count() != map.Length)
            throw new InvalidDataException("Mapping does not match the loaded rigs.");
        foreach (var row in Mappings) row.Source = map.FirstOrDefault(m => m.Target == row.Target)?.Source;
        _calibration = null; Status = "Bone mapping loaded. Lock calibration before baking.";
    });
    [RelayCommand] private async Task AddTarget() => await Run(async () =>
    {
        var path = await OpenPath("Add installation target model", ["*.mdl"]);
        if (path != null) { TargetPaths = string.Join(Environment.NewLine, TargetPaths.Split('\n', StringSplitOptions.RemoveEmptyEntries).Append(path)); OnPropertyChanged(nameof(TargetPaths)); }
    });
    [RelayCommand] private async Task Install() => await Run(async () =>
    {
        if (_repositoryRoot == null) throw new InvalidDataException("Open a SWLOR repository workspace before installing animations.");
        var plan = await Task.Run(() => AnimationInstall.Prepare(_repositoryRoot, Project, TargetPaths.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim())));
        var preview = $"Register {Project.Name} as named animation '{plan.AnimationName}'.\n\n" + string.Join("\n", plan.Changes.Select(c =>
            $"{(c.Before == null ? "Create" : "Update")} {Path.GetRelativePath(_repositoryRoot, c.Path)}")) + "\n\n" + plan.CodeExample +
            "\n\nBuild the HAKs and deploy the rebuilt HAKs to server and clients before using this constant.";
        if (!await _prompts.ConfirmDestructiveAsync("Install animation into SWLOR?", preview, "Install animation")) return;
        using var reservation = _mutationLock?.TryBeginResourceWrite();
        if (_mutationLock != null && reservation == null) throw new IOException("Another operation acquired the workspace. Prepare a new installation preview.");
        await Task.Run(plan.Apply);
        var installedProject = plan.Changes.Single(c => c.Path.EndsWith(".swlanim", StringComparison.OrdinalIgnoreCase));
        _path = installedProject.Path; _diskBytes = installedProject.After; _saved = Project.Serialize(); Changed();
        Status = "Installed. Rebuild HAKs and C# before using the new animation. " + plan.CodeExample;
        _log.AppendLine(Status);
        if (ResourceIndex != null)
        {
            try { await ResourceIndex.ReloadHakLayersAsync(ResourceIndex.HakLayers); }
            catch (Exception ex) { Status += " Resource refresh failed: " + ex.GetBaseException().Message; _log.AppendLine(Status); }
        }
    });

    private async Task Run(Func<Task> operation)
    {
        if (!CanEdit()) return;
        EndDrag();
        Stop(); IsBusy = true; OnPropertyChanged(nameof(IsBusy));
        try { await operation(); }
        catch (Exception ex) { Status = ex.GetBaseException().Message; _log.AppendLine("Animation Editor: " + Status); }
        finally { IsBusy = false; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanRedo)); }
    }
    private async Task<bool> ConfirmReplace()
    {
        if (!IsDirty) return true;
        var choice = await _prompts.ConfirmCloseAsync("the current animation");
        if (choice == UnsavedChangesChoice.Discard) return true;
        if (choice != UnsavedChangesChoice.Save) return false;
        var path = _path ?? await SavePath("Save animation project", Project.Name + ".swlanim");
        return path != null && await SaveTo(path, _path != null);
    }
    private Task<string?> OpenPath(string title, string[] patterns) => PickOpenPath?.Invoke(title, patterns) ?? Task.FromResult<string?>(null);
    private Task<string?> SavePath(string title, string name) => PickSavePath?.Invoke(title, name) ?? Task.FromResult<string?>(null);
    public void SetPreviewVisible(bool visible) { _previewVisible = visible; if (visible) RefreshPreview(); }
    public void ReloadGameResources() { if (_previewVisible) RefreshPreview(); }
    private void RefreshPreview()
    {
        if (_model == null) { PreviewScene = null; OnPropertyChanged(nameof(PreviewScene)); return; }
        var pose = Project.Joints.Select((joint, i) => (joint.Name, Pose[i])).ToDictionary(p => p.Name, p => p.Item2, StringComparer.OrdinalIgnoreCase);
        var model = MdlMeshBuilder.Build(_model, [pose]);
        PreviewScene = new()
        {
            Tileset = "", Width = 1, Height = 1, Tiles = [], Diagnostics = new(),
            Instances = [new() { Kind = InstanceMarkerKind.Creature, TemplateResRef = Project.ModelName, Position = new(5, 5, 0), Orientation = new(-1, 0), Model = model }]
        };
        OnPropertyChanged(nameof(PreviewScene));
    }
    internal void ApproveApplicationClose() => _closeApproved = true;
    public override bool OnClose()
    {
        if (IsBusy) { Status = "Wait for the animation operation to finish before closing."; return false; }
        EndDrag();
        if (!_closeApproved && IsDirty)
        {
            if (!_closePrompt) { _closePrompt = true; _ = ConfirmClose(); }
            return false;
        }
        Stop(); if (!_closed) { _closed = true; _timer.Tick -= Tick; Closed?.Invoke(this); }
        return base.OnClose();
    }
    private async Task ConfirmClose()
    {
        try { if (await ConfirmReplace()) { _closeApproved = true; CloseRequested?.Invoke(this); } }
        finally { _closePrompt = false; }
    }
}

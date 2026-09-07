using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.Input;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.Animation;

public sealed record AnimationBodyPart(int Joint, string Name);
public sealed record AnimationStarterMovement(string Name, MdlAnimationPose.SampledAnimation Movement);

public sealed partial class AnimationEditorDocumentViewModel
{
    private bool _isAdvanced, _showMovementChoices, _hasCharacter;
    private int _beginnerStep;
    private AnimationStarterMovement? _selectedMovement;
    public bool IsAdvanced { get => _isAdvanced; set { EndDrag(); Stop(); SetProperty(ref _isAdvanced, value); } }
    public int BeginnerStep { get => _beginnerStep; set => SetProperty(ref _beginnerStep, Math.Clamp(value, 0, 2)); }
    public bool ShowMovementChoices { get => _showMovementChoices; private set => SetProperty(ref _showMovementChoices, value); }
    public bool HasCharacter => _hasCharacter;
    public bool HasModelPreview => _hasPreviewGeometry;
    public bool ShowRigFallback => HasCharacter && !HasModelPreview;
    public bool HasStarterMovements => StarterMovements.Count > 0;
    public bool HasSelectedBodyPart => SelectedBodyPart != null;
    public ObservableCollection<AnimationBodyPart> BodyParts { get; } = [];
    public ObservableCollection<AnimationStarterMovement> StarterMovements { get; } = [];
    public AnimationStarterMovement? SelectedMovement { get => _selectedMovement; set => SetProperty(ref _selectedMovement, value); }
    public AnimationBodyPart? SelectedBodyPart
    {
        get => BodyParts.FirstOrDefault(part => part.Joint == SelectedJoint);
        set { if (value != null && BodyParts.Contains(value)) SelectedJoint = value.Joint; }
    }
    public string CharacterSummary => !HasCharacter ? "Choose a character to begin" : Project.ModelName.ToLowerInvariant() switch
    { "a_ba" => "Male humanoid", "a_fa" => "Female humanoid", _ => Project.ModelName };
    public string PlaybackLabel => IsPlaying ? "Pause" : "Play animation";
    public string PoseSummary
    {
        get
        {
            var index = Project.Keys.FindIndex(key => Math.Abs(key.Time - _playhead) < .00001f);
            return index < 0 ? $"{_playhead:0.##} s · Adjusting a body part adds a pose here" : $"Pose {index + 1} of {Project.Keys.Count} · {_playhead:0.##} s";
        }
    }
    public bool CanRemovePose => Project.Keys.Count > 1 && Project.Keys.Any(key => Math.Abs(key.Time - _playhead) < .00001f);
    public string InstallationTargetSummary => string.IsNullOrWhiteSpace(TargetPaths) ? "Choose the current character below, or select other models in Advanced."
        : "Applies to: " + string.Join(", ", TargetPaths.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(path => Path.GetFileNameWithoutExtension(path.Trim()) switch
        { "a_ba" => "Male humanoid", "a_fa" => "Female humanoid", var name => name }));

    private void RebuildBodyParts()
    {
        BodyParts.Clear();
        foreach (var (label, names) in new (string, string[])[]
        {
            ("Whole body", ["rootdummy"]), ("Hips", ["pelvis_g"]), ("Chest", ["torso_g"]),
            ("Head", ["head_g", "neck_g", "head"]),
            ("Left upper arm", ["lbicep_g", "lshoulder_g"]), ("Left forearm", ["lforearm_g"]), ("Left hand", ["lhand_g"]),
            ("Right upper arm", ["rbicep_g", "rshoulder_g"]), ("Right forearm", ["rforearm_g"]), ("Right hand", ["rhand_g"]),
            ("Left thigh", ["lthigh_g"]), ("Left shin", ["lshin_g"]), ("Left foot", ["lfoot_g"]),
            ("Right thigh", ["rthigh_g"]), ("Right shin", ["rshin_g"]), ("Right foot", ["rfoot_g"])
        })
        {
            var index = names.Select(name => Project.Joints.FindIndex(joint => joint.Name.Equals(name, StringComparison.OrdinalIgnoreCase))).FirstOrDefault(i => i >= 0, -1);
            if (index >= 0) BodyParts.Add(new(index, label));
        }
        if (BodyParts.Count == 0 && HasCharacter) BodyParts.Add(new(0, "Whole character"));
        NotifyGuided();
    }

    private void NotifyGuided()
    {
        foreach (var property in new[] { nameof(HasCharacter), nameof(HasModelPreview), nameof(ShowRigFallback), nameof(CharacterSummary), nameof(SelectedBodyPart),
                     nameof(HasSelectedBodyPart), nameof(PoseSummary), nameof(CanRemovePose), nameof(PlaybackLabel), nameof(InstallationTargetSummary) }) OnPropertyChanged(property);
    }

    private async Task RefreshStarterMovements()
    {
        StarterMovements.Clear(); SelectedMovement = null; ShowMovementChoices = false;
        try
        {
            if (_model != null)
            {
                var model = _model;
                var localFolder = _modelFolder;
                var samples = await Task.Run(() => MdlAnimationPose.SampleCreaturePreviewAnimations(model, name =>
                {
                    AnimationProject.ValidateToken(name, 16);
                    if (ResourceIndex?.TryLookup(ResourceIdentity.FromFileName(name + ".mdl"), out var resource) == true)
                        return new MdlReader().Parse(resource.GetBytes(AnimationProject.MaximumFileBytes));
                    var localPath = localFolder == null ? null : Path.Combine(localFolder, name + ".mdl");
                    if (localPath != null && File.Exists(localPath))
                        return new MdlReader().Parse(AnimationSourceFile.ReadBytes(localPath, AnimationMdl.MaximumFileBytes, "Supermodel"));
                    return null;
                }, framesPerSecond: 20, maxFrames: 240, maxDepth: AnimationInstall.MaximumModelChainDepth));
                foreach (var sample in samples.Where(sample => sample.Length >= 0 && sample.Length <= 12 && sample.Frames.Count > 0))
                {
                    var label = sample.Name.StartsWith("walk", StringComparison.OrdinalIgnoreCase) ? "Walking" :
                        sample.Name.Contains("pause", StringComparison.OrdinalIgnoreCase) ? "Standing / idle" : "Attack";
                    StarterMovements.Add(new(label, sample));
                }
                SelectedMovement = StarterMovements.FirstOrDefault();
            }
        }
        catch (Exception ex) { _log.AppendLine("Starter movements unavailable: " + ex.GetBaseException().Message); }
        OnPropertyChanged(nameof(HasStarterMovements)); NotifyGuided();
    }

    [RelayCommand] private async Task LoadMaleCharacter() { RigResource = "a_ba"; OnPropertyChanged(nameof(RigResource)); await LoadRig(); }
    [RelayCommand] private async Task LoadFemaleCharacter() { RigResource = "a_fa"; OnPropertyChanged(nameof(RigResource)); await LoadRig(); }
    [RelayCommand] private void ChooseMovement() { if (HasCharacter) ShowMovementChoices = true; }
    [RelayCommand] private void BuildFromPoses()
    {
        if (!CanEdit() || !HasCharacter) return;
        EndDrag();
        var next = Project.Clone(); next.Keys.Clear(); next.Events.Clear(); next.Duration = 2;
        var pose = Project.Joints.Select(joint => joint.Rest).ToArray();
        var idle = StarterMovements.FirstOrDefault(movement => movement.Name == "Standing / idle");
        if (idle != null) pose = Project.Joints.Select(joint => idle.Movement.Frames[^1].TryGetValue(joint.Name, out var value) ? value : joint.Rest).ToArray();
        foreach (var time in new[] { 0f, 1f, 2f }) next.SetKey(time, pose);
        try { Replace(next); }
        catch (Exception ex) { Status = "Could not create poses: " + ex.GetBaseException().Message; return; }
        Playhead = 1; SelectedBodyPart = BodyParts.FirstOrDefault(); BeginnerStep = 1;
        Status = "Start, middle, and finish poses are ready. Choose a body part and adjust the middle pose, then press Play.";
    }
    [RelayCommand] private void UseMovement()
    {
        if (!CanEdit() || !HasCharacter || SelectedMovement == null) return;
        EndDrag();
        var movement = SelectedMovement.Movement;
        var next = Project.Clone(); next.Keys.Clear(); next.Events.Clear(); next.Duration = Math.Max(.01f, movement.Length);
        for (var i = 0; i < movement.Frames.Count; i++)
        {
            var time = movement.Frames.Count == 1 ? 0 : next.Duration * i / (movement.Frames.Count - 1);
            next.Keys.Add(new(time, Project.Joints.Select(joint => movement.Frames[i].TryGetValue(joint.Name, out var value) ? value : joint.Rest).ToArray()));
        }
        try { Replace(next); }
        catch (Exception ex) { Status = "Could not copy this movement: " + ex.GetBaseException().Message; return; }
        Playhead = 0; SelectedBodyPart = BodyParts.FirstOrDefault(); BeginnerStep = 1; Play();
        Status = "Movement copied. Pause on a pose, choose a body part, and make small adjustments. Undo restores the previous animation.";
    }
    [RelayCommand] private void GoToPose(string position)
    {
        Stop(); Playhead = position switch { "start" => 0, "middle" => Project.Duration / 2, _ => Project.Duration };
    }
    [RelayCommand] private void PreviousPose()
    { Stop(); Playhead = Project.Keys.LastOrDefault(key => key.Time < _playhead - .00001f)?.Time ?? 0; }
    [RelayCommand] private void NextPose()
    { Stop(); Playhead = Project.Keys.FirstOrDefault(key => key.Time > _playhead + .00001f)?.Time ?? Project.Duration; }
    [RelayCommand] private void AddPoseAfter()
    {
        if (!CanEdit() || !HasCharacter) return;
        EndDrag(); Stop();
        var time = Project.Keys.FirstOrDefault(key => key.Time > _playhead + .00001f)?.Time ?? Project.Duration;
        time = time > _playhead + .00001f ? (_playhead + time) / 2 : _playhead + .5f;
        var pose = Project.Sample(_playhead); var previous = Project;
        Edit(project => { project.Duration = Math.Max(project.Duration, time); project.SetKey(time, pose); });
        if (!ReferenceEquals(previous, Project)) Playhead = time;
    }
    [RelayCommand] private void RemovePose() { if (CanRemovePose) RemoveKey(); }
    [RelayCommand] private void AdjustBodyPart(string adjustment)
    {
        if (!CanEdit() || !HasCharacter || !HasSelectedBodyPart) return;
        EndDrag(); Stop();
        var axis = adjustment.StartsWith("bend", StringComparison.Ordinal) ? Vector3.UnitX :
            adjustment.StartsWith("turn", StringComparison.Ordinal) ? Vector3.UnitZ : Vector3.UnitY;
        var sign = adjustment.EndsWith("-", StringComparison.Ordinal) ? -1 : 1;
        var world = AnimationRig.World(Project.Joints, Pose);
        Matrix4x4.Decompose(world[SelectedJoint], out _, out var rotation, out _);
        var parentRotation = Quaternion.Identity; var parent = Project.Joints[SelectedJoint].Parent;
        if (parent >= 0) Matrix4x4.Decompose(world[parent], out _, out parentRotation, out _);
        SetJoint(Pose[SelectedJoint] with { Orientation = Quaternion.Normalize(Quaternion.Inverse(parentRotation) *
            Quaternion.CreateFromAxisAngle(axis, sign * MathF.PI / 36) * rotation) });
    }
    [RelayCommand] private void ResetBodyPart() { if (HasCharacter && HasSelectedBodyPart) SetJoint(Project.Joints[SelectedJoint].Rest); }
    [RelayCommand] private void UseCurrentCharacterTarget()
    {
        if (!CanEdit() || _repositoryRoot == null) { Status = "Open a SWLOR repository workspace to choose an installation target."; return; }
        TargetPaths = AnimationInstall.FindTargetSource(_repositoryRoot, Project.ModelName) ?? "";
        OnPropertyChanged(nameof(TargetPaths)); NotifyGuided();
        if (string.IsNullOrEmpty(TargetPaths)) Status = "The current character has no editable HAK source here. Choose a target in Advanced.";
    }
}

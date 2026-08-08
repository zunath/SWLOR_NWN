using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    public enum BehaviorChoiceImageCrop
    {
        None,
        NeverwinterPortrait
    }

    /// <summary>
    /// One reusable filter value carried by a visual choice. The shared gallery discovers these
    /// facets rather than knowing which editor or resource type supplied them.
    /// </summary>
    public sealed record BehaviorChoiceFacet(
        string GroupKey,
        string GroupLabel,
        string ValueKey,
        string Display,
        int Order = 0);

    /// <summary>One named value of a Choice row.</summary>
    /// <remarks>
    /// ToString is the display name and nothing else. A combo box falls back to ToString when it has
    /// no item template, and a record default renders the whole object shape instead.
    /// </remarks>
    public sealed class BehaviorChoice
    {
        /// <summary>The stored number for an integer-backed choice.</summary>
        public long Value { get; }

        /// <summary>The stored text for a string-backed choice.</summary>
        public string? StringValue { get; }

        public string Display { get; }

        /// <summary>
        /// Optional stable identifier shown beneath the friendly name. Resource-backed choices use
        /// the same subdued monospace treatment as blueprint ResRefs instead of appending an opaque
        /// ID to the builder-facing name.
        /// </summary>
        public string? Identifier { get; init; }

        /// <summary>
        /// Optional secondary, builder-facing description shown by the shared searchable picker.
        /// Item choices use it for a compact stat line; choices without one retain the existing
        /// single-line presentation.
        /// </summary>
        public string? Summary { get; init; }

        /// <summary>A texture this choice is pictured by: a load screen, a portrait.</summary>
        public string? ImageResRef { get; }

        /// <summary>
        /// Optional presentation crop applied by the shared preview pipeline. NWN portrait textures
        /// contain a reserved strip beneath the picture which the game deliberately does not show.
        /// </summary>
        public BehaviorChoiceImageCrop ImageCrop { get; init; }

        /// <summary>
        /// A model this choice is pictured by, rendered rather than decoded. A waypoint's appearance
        /// names one of these — <c>waypoint.2da</c> gives each row a marker model, and a builder
        /// choosing between coloured flags and letters is choosing between pictures, not names.
        /// </summary>
        public string? ModelResRef { get; }

        /// <summary>Remote reference artwork loaded by the shared preview pipeline.</summary>
        public string? ImageUrl { get; }

        /// <summary>
        /// Optional blueprint type whose ordinary Toolset thumbnail pictures this choice. Item and
        /// creature pickers use the same render queue and cache as the palette instead of inventing
        /// a second preview pipeline.
        /// </summary>
        public ResourceType? BlueprintPreviewType { get; init; }

        /// <summary>The resref sent to <see cref="BlueprintPreviewType"/>'s thumbnail service.</summary>
        public string? BlueprintPreviewResRef { get; init; }

        /// <summary>Whether this choice can ask its editor to play a representative audio cue.</summary>
        public bool CanPreviewAudio { get; }

        /// <summary>
        /// Whether this option stands for "any of them" rather than for one of them — the load
        /// screen row that leaves the choice to the destination area. It has no picture because
        /// there is no one picture it means, which is different from a picture that failed to load,
        /// and a picker that draws pictures has to say which of the two it is showing.
        /// </summary>
        public bool IsAny { get; init; }

        /// <summary>
        /// Optional metadata the shared gallery turns into filter controls. Portraits currently
        /// provide gender, race, and subject facets; another visual picker can add its own groups
        /// without adding editor-specific branches to the gallery.
        /// </summary>
        public IReadOnlyList<BehaviorChoiceFacet> GalleryFacets { get; init; } =
            Array.Empty<BehaviorChoiceFacet>();

        public bool IsStringValue => StringValue != null;

        public BehaviorChoice(
            long value,
            string display,
            string? imageResRef = null,
            string? modelResRef = null,
            string? imageUrl = null,
            bool canPreviewAudio = false)
        {
            Value = value;
            Display = display;
            ImageResRef = imageResRef;
            ModelResRef = modelResRef;
            ImageUrl = imageUrl;
            CanPreviewAudio = canPreviewAudio;
        }

        public BehaviorChoice(
            string value,
            string display,
            string? imageResRef = null,
            string? modelResRef = null,
            string? imageUrl = null,
            bool canPreviewAudio = false)
        {
            StringValue = value ?? throw new ArgumentNullException(nameof(value));
            Display = display;
            ImageResRef = imageResRef;
            ModelResRef = modelResRef;
            ImageUrl = imageUrl;
            CanPreviewAudio = canPreviewAudio;
        }

        public override string ToString() => Display;
    }
}

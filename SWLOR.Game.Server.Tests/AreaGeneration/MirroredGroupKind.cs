using System;
using System.Linq;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Test-side mirror of LayoutGroupStamper.TryClassify's multi-tile (Rows&gt;=2 or Columns&gt;=2)
/// branch, since that method is internal and the test project has no InternalsVisibleTo access.
/// Used ONLY to filter which configured 2x2+ groups are genuine OpenSetPiece candidates for the
/// placement-rate measurement/regression tests -- not a replacement for the production classifier,
/// and deliberately narrower (ignores 1x1 CorridorInsert/CorridorStub/ReliefPiece paths, which never
/// apply to a Rows&gt;=2/Columns&gt;=2 group, and never generalizes DoorwayCrosser via
/// MacroLayoutParameters.DoorSlotCrossers -- callers on a DoorSlotCrossers-declaring profile, e.g.
/// udp2/tbx78, must not rely on this mirror to recognize their renamed door-family edge as
/// "Doorway"). Keep in sync with LayoutGroupStamper.TryClassify if that method's multi-tile rules
/// ever change -- see this class's own mixed/open-member fallthrough below (added alongside
/// production's identical fallthrough) for the current shape of that sync.
/// </summary>
internal enum MirroredGroupKind { Invalid, WallRoom, WallAlcove, OpenSetPiece, CorridorStubChain }

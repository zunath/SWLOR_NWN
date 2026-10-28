"""Appearance Editor part pictures: render every selectable part in Blender and pack them as DDS."""

VIEWS = ("f", "b", "a")  # front, back, three-quarter angle
MAX_RESREF = 16


def preview_resref(key: str, view: str) -> str:
    """Resref of one picture. Mirrored by AppearancePreview.Resref in the game server."""
    if view not in VIEWS:
        raise ValueError(f"Unknown view {view!r}")
    resref = f"a{key}{view}".lower()
    if len(resref) > MAX_RESREF:
        raise ValueError(f"Preview resref too long: {resref}")
    return resref

using System.Numerics;
using System.Text.Json;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.AnimationDrafts;

internal static class PreviewWriter
{
    private static readonly string[] Bones = ["rootdummy", "torso_g", "neck_g", "head_g", "lbicep_g", "lforearm_g", "lhand_g",
        "rbicep_g", "rforearm_g", "rhand_g", "lthigh_g", "lshin_g", "lfoot_g", "rthigh_g", "rshin_g", "rfoot_g"];

    public static object Motion(AnimationProject project, Motion motion)
    {
        var ids = Bones.Select(name => project.Joints.FindIndex(j => j.Name == name)).ToArray();
        var frames = new List<float[][]>();
        var count = (int)Math.Ceiling(project.Duration * 60) + 1;
        for (var frame = 0; frame < count; frame++)
        {
            var world = AnimationRig.World(project.Joints, project.Sample(project.Duration * frame / (count - 1)));
            var points = ids.Select(i => world[i].Translation).ToList();
            // Even simplified accessories must use NWN's equipment dummies, so the preview
            // cannot conceal a misplaced sword or a shield rotated by the forearm.
            var weapon = world[project.Joints.FindIndex(j => j.Name == "rhand")];
            var shield = world[project.Joints.FindIndex(j => j.Name == "lforearm")];
            points.Add(Vector3.Transform(new Vector3(0, .8f, 0), weapon));
            foreach (var (x, z) in new[] { (-.19f, .49f), (.19f, .49f), (.28f, .34f), (.28f, -.34f),
                         (.19f, -.49f), (-.19f, -.49f), (-.28f, -.34f), (-.28f, .34f) })
                points.Add(Vector3.Transform(new Vector3(-.09f, z, x), shield));
            points.Add(Vector3.Transform(new Vector3(0, .21f, -.075f), world[ids[12]]));
            points.Add(Vector3.Transform(new Vector3(0, .21f, -.075f), world[ids[15]]));
            points.Add(Vector3.Transform(new Vector3(0, .14f, .13f), world[ids[3]]));
            points.Add(weapon.Translation);
            frames.Add(points.Select(v => new[] { v.X, v.Y, v.Z }).ToArray());
        }
        return new { motion.Id, motion.Name, motion.Reference, motion.Observation, motion.Interpretation, motion.Shield,
            project.Duration, Beats = motion.Beats.Select(b => new { b.Time, b.Label }), Frames = frames };
    }

    public static string Html(IEnumerable<object> motions) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Preview.html"))
            .Replace("__MOTIONS__", JsonSerializer.Serialize(motions));
}

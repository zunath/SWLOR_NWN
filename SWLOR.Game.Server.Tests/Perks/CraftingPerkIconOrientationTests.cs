using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The crafting perk icons were drawn upside down: a May 2026 icon pass kept the legacy art's upside-down pixels when it moved the
/// files to bottom-left origin, so the Perks window showed them inverted. These hashes pin the reviewed upright pixels, decoded top
/// row first, so a regeneration or origin pass that flips them again fails here. A new crafting perk icon must be checked upright
/// in the game and added.
/// </summary>
public class CraftingPerkIconOrientationTests
{
    private static readonly PerkCategoryType[] CraftingCategories =
    {
        PerkCategoryType.Smithery,
        PerkCategoryType.Engineering,
        PerkCategoryType.Fabrication,
        PerkCategoryType.Agriculture,
        PerkCategoryType.Gathering
    };

    // The space and city revamps' Gathering and Farmer icons (ife_broil, ife_cred_find, ife_garlic_jeste, ife_hasty_touch,
    // ife_morbol_seedl, ife_stars_minin, ife_tomato_king) were checked upright from their decoded pixels, not yet in the game client.
    private static readonly Dictionary<string, string> UprightPixelHashes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ife_alchemist"] = "85a7e3a2f9872b954d4458f62246514224fe51a99ac1e49b8b421aff99065944",
        ["ife_armorer"] = "6a87a2a08c7cb214d325ce8be9871bf7987bc71d2e360999c0850a728d56c101",
        ["ife_blacksmith"] = "99b26b1776b33b21f79c46475882c2ae0913e0b355dc8769306aba874387ccc3",
        ["ife_broil"] = "56d8d21682e3eb22e0f7bce831cddaa2ccee029aadb38d9275e2518e0b73051c",
        ["ife_buildrobot"] = "ecb4703900cce351eed8a6067cb0ffae3ba765bbf418de755ed330571e4bcb5d",
        ["ife_carve_and_sp"] = "919893a3bb46f54e01c0405617e47ed34896919958b4dc89d8a2a56781a88c72",
        ["ife_coolfire"] = "f81240c06f7df9c74b52ff7e4997fce51c0956a2571cf9775f1677f0a7c9c940",
        ["ife_copycat_bulb"] = "4790f0e3a93e49ce2d8cb4e675d6a958fc6303241c215fd0fe9f6d278b9a350e",
        ["ife_crafting_log"] = "d3ef994b94dd986248fea2cc56e4b51d368b0c03fe1e6e9d68f845d5d89f5c5c",
        ["ife_cred_find"] = "0b3886b87cf90962120be3a308d6cf4260774dec521337acc4e6d0bdb53a3cc9",
        ["ife_culinarian"] = "7aaf13a196be13b0c6e9f3de8c9994d1775f9277b09f12c24bf7528f2ecaaa38",
        ["ife_exmachine"] = "ee2f8ceec43305358d9879ddcaf6d634a892d0c6f3145aceba2ea98163ca7342",
        ["ife_firestarter"] = "e6c7b6cb0fa6751c8960e469c22f7154d51c1012cc30b330b647acfb2111cf90",
        ["ife_fish_eyes"] = "f9e6d9d4cf495b4d42190baa452215c7b616cd90c249e18f52a0bb53c7766c51",
        ["ife_fish_whisper"] = "a076a9a9736df9089a9def1fbcd5ad1e34b4ea03548a59875848850c645c59c7",
        ["ife_garlic_jeste"] = "f6960f33904deaf928a4adfdceb243a41ae6dca21e96873de5a3d414e6c8dc09",
        ["ife_gearhead"] = "b968a3a83b1ea572963bde56a7c3dba53b6fc361c8163c88741c5d313d95de39",
        ["ife_hasty_touch"] = "c8dda4a7bbc6fcadd43e11575cd253f77a668763f037ba37a15e75d794365dff",
        ["ife_hook"] = "50aa024a328feb45572d845778a0823d72376c4a29339eb0edce885ba458f19b",
        ["ife_infusion"] = "94b861984c5ae762080b859ec3294fc78a2ee01d2db52e280a6c0a8b4d84b361",
        ["ife_laurel_goobb"] = "592f28e4b3544c2b3569238619228fbaff16d4fc0fca4b6879bfc4422cdca995",
        ["ife_mastercraft"] = "bf66ef01cda73cdf9678926e1627b5255f38a02d5812f1768386481887611fc9",
        ["ife_min_light"] = "d125f93512ca4da32df9a5ee9ea3c91645f9e5577bdfb12fa21aaf55833029b5",
        ["ife_miner"] = "e1937c6ce4b16e0ece22a38b3e7405c89e6c8afe7986a9ce799e21156faad8bf",
        ["ife_morbol_seedl"] = "2769319be610a5bd398f27fd54b9cbdf61c445f75219e037b4c650fe395ef99f",
        ["ife_observe"] = "bee7b2bb2b8a17f9e4cbfacc112f3833fcacacc7da33bd7f1e00f0f49ab5e61f",
        ["ife_preparation"] = "5d6b5163621eb814e4b28f6ba0f34610f9bf9766a7c6de1ba68ffd9f066695d3",
        ["ife_prune"] = "5300de3fdcb7fb3f17f05956ef0137f870568469c666bbcba4a20588cc626c66",
        ["ife_reassemble"] = "97471e9a67af9f70c491a16907a78c19745a8782e539d2420549aea52f23fa6e",
        ["ife_repair"] = "637259ce10e1037c95582204558b830f04e329664900c27c8c146a7c94de0cd4",
        ["ife_rsrch"] = "6275f9e68e32b46520b8cd044045dbfdb506e44e11beb51ea6853869c81dc068",
        ["ife_rsrch_proj"] = "cf5cc4a3b99a554ec17eabbf3e79a065be722749f9f69a82baa32d47dc025241",
        ["ife_sci_net"] = "d3d91b5fc5d05f41578ce5f236b993d3045706d4d2d2d71dd7ef9a172c215eec",
        ["ife_smoke_screen"] = "a116177337dbb06ec2b5e267d29a5be006371a0deeff6d5ac8f8424a81c20894",
        ["ife_snagging"] = "f0de2b569798203f0923515e7faf1557be64382b8aeed3e70fc9173a71f7e085",
        ["ife_spark"] = "d741cb02ad4a87a3324cd6818cbd75ef89926505a3f44823ef56b08f1e643f9c",
        ["ife_stars_minin"] = "addea29d5bda0551805bbd7de1c7e12b026420275be6967da17cb7b6f9ec00c7",
        ["ife_steady_hand"] = "76489ebbe9afe7ec6fbe6041d0b27f5838cd7112b43c7040551b8ada9e7dc958",
        ["ife_steam-powere"] = "3babaa7e28851615e8b1a2529e15f106adbd0d16b0511f9ef80469ca02cd3c41",
        ["ife_tackle_maste"] = "aeccccc5ee83cb4ea79ff1b179e784267acc5c22dcc72ec3f97b522d3d25c557",
        ["ife_tiny_bulb"] = "957e37beaa0ababa35a1fbd2651b0ed6ce0718184ccfc0897c80c8712d225ceb",
        ["ife_tomato_king"] = "ba61e6b39b3b4afd0b1ed37a142f67052356a7cfd52fc23d05f5c70571096b4a",
        ["ife_treas_hunt"] = "a619f416906431674d0268734fd781818f67912f072edf93b6ab679518ff6da0",
        ["ife_treasure_box"] = "0e3ac6d52e42fb3b2d83875a3677f027d5b3c8d795e30e427ffee51bfe256bbf",
    };

    [Test]
    public void EveryCraftingPerkIcon_HasReviewedUprightArt()
    {
        var icons = PassivePerkIconTests.BuildPerksWithout2daLookup()
            .Where(perk => CraftingCategories.Contains(perk.Detail.Category) && !string.IsNullOrWhiteSpace(perk.Detail.IconResref))
            .Select(perk => perk.Detail.IconResref)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        icons.Should().NotBeEmpty();
        icons.Where(icon => !UprightPixelHashes.ContainsKey(icon)).Should()
            .BeEmpty("a crafting perk icon must be checked upright in the game, then have its pixel hash added here");
    }

    [Test]
    public void CraftingPerkIcons_KeepTheirUprightPixels()
    {
        var iconRoot = Path.Combine(PassivePerkIconTests.FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_ability");
        var files = Directory.GetFiles(iconRoot, "*.tga")
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var (icon, expected) in UprightPixelHashes)
        {
            if (!files.TryGetValue(icon, out var path))
            {
                failures.Add($"{icon}: no TGA in {iconRoot}");
                continue;
            }

            var actual = Convert.ToHexString(SHA256.HashData(DecodeTopRowFirst(File.ReadAllBytes(path)))).ToLowerInvariant();
            if (actual != expected)
                failures.Add($"{icon}: its pixels changed (an upside-down copy, or new art that needs an in-game check and a new hash)");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    /// <summary>The pixels of an uncompressed or run-length true-color TGA as stored per pixel, top row first, honoring its origin bit.</summary>
    private static byte[] DecodeTopRowFirst(byte[] tga)
    {
        int idLength = tga[0], colorMapType = tga[1], imageType = tga[2];
        colorMapType.Should().Be(0, "gameplay icons are true-color TGAs");
        int width = BitConverter.ToUInt16(tga, 12), height = BitConverter.ToUInt16(tga, 14), pixelSize = tga[16] / 8;
        var topLeftOrigin = (tga[17] & 0x20) != 0;
        var size = width * height * pixelSize;
        var offset = 18 + idLength;
        byte[] pixels;
        if (imageType == 2)
        {
            pixels = tga.AsSpan(offset, size).ToArray();
        }
        else
        {
            imageType.Should().Be(10, "gameplay icons are uncompressed or run-length true-color TGAs");
            var decoded = new List<byte>(size);
            while (decoded.Count < size)
            {
                var header = tga[offset++];
                var count = (header & 0x7f) + 1;
                if ((header & 0x80) != 0)
                {
                    for (var repeat = 0; repeat < count; repeat++)
                        decoded.AddRange(tga.AsSpan(offset, pixelSize).ToArray());
                    offset += pixelSize;
                }
                else
                {
                    decoded.AddRange(tga.AsSpan(offset, count * pixelSize).ToArray());
                    offset += count * pixelSize;
                }
            }

            pixels = decoded.Take(size).ToArray();
        }

        if (topLeftOrigin)
            return pixels;

        var rowSize = width * pixelSize;
        var flipped = new byte[size];
        for (var row = 0; row < height; row++)
            Array.Copy(pixels, row * rowSize, flipped, (height - 1 - row) * rowSize, rowSize);
        return flipped;
    }
}

using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Representative RGB values sampled from the midtone column of plt_palette.tga.
    /// These keep the custom picker synchronized with the engine's preset palettes.
    /// </summary>
    public static class TintMapPaletteColors
    {
        private static readonly byte[] Skin = Convert.FromBase64String(
            "zp96woxntXpWlWhPe1hFYEU3SDQpMiQczqt7todWfWBGSToqzZB5tWdUeE1ERi4oraObkIN7Zl5aPDg1j6e5b4ydU2VuMTtAwrF8qpBYeWdIRTwqMDM+ISEmDxASAQEBtrd8m5dbcW1IQUAri72caqJ6U3RbMEM1pKSkhYWFYGBgODg4VTsxSCsgVDBTRx9GLjJQHSBAM1BaIThJRF42NVEkXFQ1UEckyMjIAAAA/7UB459igICAAAAA5eXlHBwcmkpSg2dhtYoviINcZJpKbn1nZU2Xbmd9jkiQfmZ/r4lpjYB0TZt7ZX50QFx/YWt5X2c1bG9ZhY6/foKVgHdkgHdke25pbFo83Uc32T4kqCIVcxML/Nd05bI+soApa04ioj5XjzdNaik6VCAtq1tDoEs4hy0mdx8dYIlSWH1IQmIvPVElpqiDjo9gdHY9bG4xU1VCTU47QkAvOjgouJt7podrg15Mb0g5raaGnJJ0eWxVZVdCgnlydGlkV0xIRzo418Wvrpd+h2tMeFo4QkdVO0BOLzRCKC06PleiN02PKTlqIC1UdrS8ZKmsRoeBNHNs0GLHvVO6jDaZcyOEgq7HUomogcO4UKSZjMF9XaNPx76BqJ9Rx6eBqIBRuHNrkExEmmGCczpbx42DkR6IzdLmUFt/5uH4GR4HnKCIMUM6PS9FMTZDUE8kRjEueFo4cmEg");

        private static readonly byte[] Hair = Convert.FromBase64String(
            "w4Yrq3QgkVwVhk0Mw1Yrq0cgkTUVhicM9OS57tmq58+c38GF2bF3uZBZhV5FUzUm/////v7+7OzssLCweoaaU2aCLj5REx8skV3oMgCjnX7QLxdlXX7oADGjfpHQFy9lXbjoAHajfrPQF1BlcNKlE3VDhcGlIlg9e+hdLqMAj9B+LWUX0ehdk6MAw9B+XWUX6K9do1YA0K9+ZUAX6GpdowQA0IV+ZRoX5eXlAAAA/7QA6baHgICAAAAA5eXlHBwcmkpSg2dhtYoviINcZJpKbn1nZU2Xbmd9jkiQfmZ/r4lpjYB0TZt7ZX50QFx/YWt5X2c1bG9ZhY6/foKVgHdkgHdke25pbFo83Uc32T4kqCIVcxML/Nd05bI+soApa04ioj5XjzdNaik6VCAtq1tDoEs4hy0mdx8dYIlSWH1IQmIvPVElpqiDjo9gdHY9bG4xU1VCTU47QkAvOjgouJt7podrg15Mb0g5raaGnJJ0eWxVZVdCgnlydGlkV0xIRzo418Wvrpd+h2tMeFo4QkdVO0BOLzRCKC06PleiN02PKTlqIC1UdrS8ZKmsRoeBNHNs0GLHvVO6jDaZcyOEgq7HUomogcO4UKSZjMF9XaNPx76BqJ9Rx6eBqIBRuHNrkExEmmGCczpbx42DkR6IzdLmUFt/5uH4GR4HnKCIMUM6PS9FMTZDUE8kRjEueFo4cmEg");

        private static readonly byte[] Metal1 = Convert.FromBase64String(
            "zc3NbGxsPDs7AAAAysrKpaWlbm5uRUVF/70d/8QIyI0AjmIA5sV1tZg6gmQeRzEA/4kd/4oIyF8AjkEA5qt1tXs6gk0eRyAA/0EdjhQA5od1RwoApUjUUxtzuIrQKQ05XF7AJylmlJXGFBYzVaTGI1ZqkbjJEis1SNRKHHMbitCLDTkNodRIVXMbt9CKDTkNDABSAAAvbGxsbGxsMzMzNDQ0bGxsbGxszc3NAAAA/70d5qt1gICAAAAA////AAAAmkpSg2dhtYoviINcZJpKbn1nZU2Xbmd9jkiQfmZ/r4lpjYB0TZt7ZX50QFx/YWt5X2c1bG9ZhY6/foKVgHdkgHdke25pbFo83Uc32T4kqCIVcxML/Nd05bI+soApa04ioj5XjzdNaik6VCAtq1tDoEs4hy0mdx8dYIlSWH1IQmIvPVElpqiDjo9gdHY9bG4xU1VCTU47QkAvOjgouJt7podrg15Mb0g5raaGnJJ0eWxVZVdCgnlydGlkV0xIRzo418Wvrpd+h2tMeFo4QkdVO0BOLzRCKC06PleiN02PKTlqIC1UdrS8ZKmsRoeBNHNs0GLHvVO6jDaZcyOEgq7HUomogcO4UKSZjMF9XaNPx76BqJ9Rx6eBqIBRuHNrkExEmmGCczpbx42DkR6IzdLmUFt/5uH4GR4HnKCIMUM6PS9FMTZDUE8kRjEueFo4cmEg");

        private static readonly byte[] Metal2 = Convert.FromBase64String(
            "4+Pjr6+vk5OTcnJy4eHhzc3Nr6+vmJiY7tOS7dWK0ruCtqN96Nu7zcGbsqaLlIt37r2S7b2J0qeBtpZ96NC7zbWbs56LlIR37qCStoV96MK7lHt3yaTcn4is1sPgiH2OrK3VjI2lxsfcgIGMqcnXip6mxtbdfomNpNyliKqHw+DDfY59yNyjn6yI1uDDfY59zMjmra3Uz7ikr5OJsamKhnhjrqajlo2M4+PjcnJy7tOS6NC7gICAgICA////AAAA2IiQvaGb47hdu7aPlsx8pbSepo7YqaK4zIbOt5+427WVv7KmgM6unLWrg5/Cnae1n6d1p6qUsbrrsLTHtq2atq2atKeirZt9/4h8/4Ju8WtexmZe/+en/9Bl47FasJNn4n6X03uRt3aHpnJ/5JR834p30XZwxm5slsCJk7iDh6d0h5tvy82ovb6PrrB3qatumJqHlZaDj418ioh43sGh1LWZwJuJtI1+0cqqyL6gs6aPp5mEt66nr6SfnZKOlIeF7dvF1r+mwKSFuJp4jJGfh4yagIWTfIGOgZrlfpTWeIi5dIGom9nhj9TXfr+5dLOs/Y/08YftznjbvW3Op9Psh73doePYf9PIruOfjtSA49qd0ch66Mii2LCB6KOb0IyE0Zi5uoGh7rSq2WbP3+T4kZzA8Oz/dXpjw8iwf5GIjoCWgYaTmJdsloF+uJp4sqFg");

        private static readonly byte[] Organic = Convert.FromBase64String(
            "7NvGyKiFnnVIjGM006yCwpVtmmVJf0s3xbyRs6d7i3lWcl9Dk4R6hHNrYFBMTj47kZVEhYM3a2AjVkkYwcLDfYKFT1VYJSouWGmZKjhiVaLXI22hU9G5I5mEZM1PNJch2MVUoZAi2JNUoWAi12NXoTEkr0uGdB9Qj0ulWB5pdVuhQyxofYKFJSouKjhiI22hI5mENJchoZAioWAioTEkdB9QWB5pQyxo5eXlAAAA/7QA6baHgICAAAAA////AAAAmkpSg2dhtYoviINcZJpKbn1nZU2Xbmd9jkiQfmZ/r4lpjYB0TZt7ZX50QFx/YWt5X2c1bG9ZhY6/foKVgHdkgHdke25pbFo83Uc32T4kqCIVcxML/Nd05bI+soApa04ioj5XjzdNaik6VCAtq1tDoEs4hy0mdx8dYIlSWH1IQmIvPVElpqiDjo9gdHY9bG4xU1VCTU47QkAvOjgouJt7podrg15Mb0g5raaGnJJ0eWxVZVdCgnlydGlkV0xIRzo418Wvrpd+h2tMeFo4QkdVO0BOLzRCKC06PleiN02PKTlqIC1UdrS8ZKmsRoeBNHNs0GLHvVO6jDaZcyOEgq7HUomogcO4UKSZjMF9XaNPx76BqJ9Rx6eBqIBRuHNrkExEmmGCczpbx42DkR6IzdLmUFt/5uH4GR4HnKCIMUM6PS9FMTZDUE8kRjEueFo4cmEg");

        private static readonly IReadOnlyDictionary<TintMapLayerType, byte[]> Colors =
            new Dictionary<TintMapLayerType, byte[]>
            {
                [TintMapLayerType.Skin] = Skin,
                [TintMapLayerType.Hair] = Hair,
                [TintMapLayerType.Metal1] = Metal1,
                [TintMapLayerType.Metal2] = Metal2,
                [TintMapLayerType.Cloth1] = Organic,
                [TintMapLayerType.Cloth2] = Organic,
                [TintMapLayerType.Leather1] = Organic,
                [TintMapLayerType.Leather2] = Organic,
                [TintMapLayerType.Tattoo1] = Organic,
                [TintMapLayerType.Tattoo2] = Organic
            };

        public static TintMapColor GetColor(TintMapLayerType layer, int colorId)
        {
            if (!Colors.TryGetValue(layer, out var colors))
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown tint map layer.");
            if (colorId < 0 || colorId >= TintMapMaterialRegistry.PaletteColorCount)
                throw new ArgumentOutOfRangeException(nameof(colorId));

            var offset = colorId * 3;
            return new TintMapColor(colors[offset], colors[offset + 1], colors[offset + 2]);
        }

        /// <summary>
        /// Resolves an arbitrary picker color to the closest row in the layer's PLT palette.
        /// The runtime shader accepts palette rows only, so this keeps picker edits on the same
        /// rendering and persistence path as clicking a preset swatch.
        /// </summary>
        public static int GetClosestColorId(TintMapLayerType layer, TintMapColor color)
        {
            if (!Colors.TryGetValue(layer, out var colors))
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unknown tint map layer.");

            var closestColorId = 0;
            var closestDistance = int.MaxValue;
            for (var colorId = 0; colorId < TintMapMaterialRegistry.PaletteColorCount; colorId++)
            {
                var offset = colorId * 3;
                var redDifference = color.Red - colors[offset];
                var greenDifference = color.Green - colors[offset + 1];
                var blueDifference = color.Blue - colors[offset + 2];
                var distance = redDifference * redDifference +
                               greenDifference * greenDifference +
                               blueDifference * blueDifference;
                if (distance >= closestDistance)
                    continue;

                closestColorId = colorId;
                closestDistance = distance;
                if (distance == 0)
                    break;
            }

            return closestColorId;
        }
    }
}

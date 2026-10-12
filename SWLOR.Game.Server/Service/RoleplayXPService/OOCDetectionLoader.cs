using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// Reads the external OOC detection file kept in the server home next to swlor.env.
    /// </summary>
    public static class OOCDetectionLoader
    {
        public const string DefaultFilePath = "/nwn/home/rpxp-ooc-detection.json";

        public static bool TryLoad(string path, out OOCDetectionModel model, out string error)
        {
            model = OOCDetectionModel.Default;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                error = $"'{path}' does not exist";
                return false;
            }

            try
            {
                model = FromFile(JsonConvert.DeserializeObject<OOCDetectionFile>(File.ReadAllText(path)));
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
            {
                error = $"'{path}' could not be read: {exception.Message}";
                return false;
            }
        }

        public static OOCDetectionModel FromFile(OOCDetectionFile file)
        {
            if (file == null)
                return OOCDetectionModel.Default;

            var threshold = file.Threshold ?? OOCDetectionModel.DefaultThreshold;
            if (!double.IsFinite(threshold))
                throw new InvalidDataException("threshold must be a number from 0 to 1");
            if (file.Weights != null && file.Weights.Values.Any(weight => !double.IsFinite(weight)))
                throw new InvalidDataException("weights must be finite numbers");

            // A missing list keeps the built-in terms; an empty list turns term matching off.
            var terms = file.Terms == null
                ? RoleplayMessageClassifier.DefaultOOCTerms
                : file.Terms
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .Select(term => term.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return new OOCDetectionModel(
                terms,
                file.Weights ?? new Dictionary<string, double>(),
                Math.Clamp(threshold, 0.0, 1.0));
        }
    }
}

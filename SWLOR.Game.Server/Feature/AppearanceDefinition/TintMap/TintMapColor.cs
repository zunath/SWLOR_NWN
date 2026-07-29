namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// An authored tint-map color. RGB values are packed into a positive local-int value with a
    /// marker byte so they cannot be confused with the original palette-index encoding.
    /// </summary>
    public readonly record struct TintMapColor(byte Red, byte Green, byte Blue)
    {
        public const int RgbMarker = 0x01000000;
        private const int RgbMask = 0x00FFFFFF;

        public int ToStoredValue()
        {
            return RgbMarker |
                   Red << 16 |
                   Green << 8 |
                   Blue;
        }

        public static bool TryFromStoredValue(int storedValue, out TintMapColor color)
        {
            if ((storedValue & ~RgbMask) != RgbMarker)
            {
                color = default;
                return false;
            }

            color = new TintMapColor(
                (byte)(storedValue >> 16),
                (byte)(storedValue >> 8),
                (byte)storedValue);
            return true;
        }
    }
}

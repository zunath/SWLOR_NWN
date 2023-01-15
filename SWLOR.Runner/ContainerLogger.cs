namespace SWLOR.Runner
{
    internal class ContainerLogger
    {
        public int LineCount { get; set; }
        public ConsoleColor Color { get; }

        private static readonly Random _random = new();
        private static readonly ConsoleColor[] _validColors =
        {
            ConsoleColor.DarkBlue,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkCyan,
            ConsoleColor.DarkMagenta,
            ConsoleColor.DarkYellow,
            ConsoleColor.Gray,
            ConsoleColor.DarkGray,
            ConsoleColor.Blue,
            ConsoleColor.Green,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
            ConsoleColor.Yellow,
            ConsoleColor.White
        };

        public ContainerLogger()
        {
            Color = _validColors[_random.Next(_validColors.Length)];
        }
    }
}

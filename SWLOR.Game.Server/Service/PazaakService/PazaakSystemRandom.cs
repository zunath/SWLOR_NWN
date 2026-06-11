namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakSystemRandom : IPazaakRandom
    {
        private readonly System.Random _random;

        public PazaakSystemRandom()
        {
            _random = new System.Random();
        }

        public PazaakSystemRandom(int seed)
        {
            _random = new System.Random(seed);
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }
    }
}

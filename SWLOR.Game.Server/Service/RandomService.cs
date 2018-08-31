using System;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class RandomService: IRandomService
    {
        private readonly Random _random;

        public RandomService()
        {
            _random = new Random();
        }

        public int Random()
        {
            return _random.Next();
        }

        public int Random(int max)
        {
            return _random.Next(max);
        }

        public int Random(int min, int max)
        {
            return _random.Next(min, max);
        }

        public float RandomFloat()
        {
            return (float)_random.NextDouble();
        }

        public int GetRandomWeightedIndex(int[] weights)
        {
            int totalWeight = 0;
            foreach (int weight in weights)
            {
                totalWeight += weight;
            }

            int randomIndex = -1;
            double random = RandomFloat() * totalWeight;
            for (int i = 0; i < weights.Length; ++i)
            {
                random -= weights[i];
                if (random <= 0.0d)
                {
                    randomIndex = i;
                    break;
                }
            }

            return randomIndex;
        }

    }
}

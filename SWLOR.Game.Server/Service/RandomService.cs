using System;


namespace SWLOR.Game.Server.Service
{
    public static class RandomService
    {
        private static readonly Random _random;

        static RandomService()
        {
            _random = new Random();
        }

        public static int Random()
        {
            return _random.Next();
        }

        public static int Random(int max)
        {
            return _random.Next(max);
        }

        public static int Random(int min, int max)
        {
            return _random.Next(min, max);
        }

        public static float RandomFloat()
        {
            return (float)_random.NextDouble();
        }

        public static float RandomFloat(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        public static int GetRandomWeightedIndex(int[] weights)
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

        private static int RollDice(int numberOfDice, int min, int max)
        {
            if (numberOfDice < 1) numberOfDice = 1;
            if (min < 1) min = 1;
            if (min > max) min = max;

            int result = 0;
            for (int x = 1; x <= numberOfDice; x++)
            {
                result += _random.Next(min, max);
            }

            return result;
        }

        public static int D2(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 2);
        }

        public static int D3(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 3);
        }

        public static int D4(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 4);
        }
        
        public static int D6(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 6);
        }

        public static int D8(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 8);
        }

        public static int D10(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 10);
        }
        
        public static int D12(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 12);
        }
        
        public static int D20(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 20);
        }
        
        public static int D100(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 100);
        }
    }
}

namespace SWLOR.Game.Server.Service
{
    public static class Random
    {
        private static readonly System.Random _random = new();

        /// <summary>
        /// Retrieves the next random integer value.
        /// </summary>
        /// <returns>The next random integer value.</returns>
        public static int Next()
        {
            return _random.Next();
        }

        /// <summary>
        /// Retrieves the next random integer value, not to exceed the max value.
        /// </summary>
        /// <param name="max">The max value to randomly get.</param>
        /// <returns>The next random integer value.</returns>
        public static int Next(int max)
        {
            return _random.Next(max);
        }

        /// <summary>
        /// Retrieves the next random integer value, not to fall outside of the min and max range.
        /// </summary>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The next random integer value</returns>
        public static int Next(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Retrieves the next random float value
        /// </summary>
        /// <returns>The next random float value</returns>
        public static float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Retrieves the next random float value, not to fall outside of the min and max range.
        /// </summary>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>The next random float value</returns>
        public static float NextFloat(float min, float max)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        /// <summary>
        /// Retrieves a random index out of the provided weight values.
        /// </summary>
        /// <param name="weights">The array of weights to randomly select from</param>
        /// <returns>The index of the selected value</returns>
        public static int GetRandomWeightedIndex(int[] weights)
        {
            int totalWeight = 0;
            foreach (int weight in weights)
            {
                totalWeight += weight;
            }

            int randomIndex = -1;
            double random = NextFloat() * totalWeight;
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

        /// <summary>
        /// Rolls a certain number of dice.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll. Minimum is 1</param>
        /// <param name="min">The minimum number on each die. Minimum is 1</param>
        /// <param name="max">The maximum number on each die. Minimum is 1</param>
        /// <returns>A random value selected based on the configured parameters</returns>
        private static int RollDice(int numberOfDice, int min, int max)
        {
            if (numberOfDice < 1) numberOfDice = 1;
            if (min < 1) min = 1;
            if (min > max) min = max;

            int result = 0;
            for (int x = 1; x <= numberOfDice; x++)
            {
                // Random.Next() returns a number between (min) and (max-1).  So add +1 to max
                // to include it in the range. 
                result += _random.Next(min, max + 1);
            }

            return result;
        }

        /// <summary>
        /// Rolls dice with 2 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D2(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 2);
        }

        /// <summary>
        /// Rolls dice with 3 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D3(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 3);
        }

        /// <summary>
        /// Rolls dice with 4 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D4(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 4);
        }

        /// <summary>
        /// Rolls dice with 6 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D6(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 6);
        }

        /// <summary>
        /// Rolls dice with 8 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D8(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 8);
        }

        /// <summary>
        /// Rolls dice with 10 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D10(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 10);
        }

        /// <summary>
        /// Rolls dice with 12 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D12(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 12);
        }

        /// <summary>
        /// Rolls dice with 20 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D20(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 20);
        }

        /// <summary>
        /// Rolls dice with 100 sides.
        /// </summary>
        /// <param name="numberOfDice">The number of dice to roll</param>
        /// <param name="minimum">The minimum value.</param>
        /// <returns>A random value selected base on the parameters.</returns>
        public static int D100(int numberOfDice, int minimum = 1)
        {
            return RollDice(numberOfDice, minimum, 100);
        }
    }
}

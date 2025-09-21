namespace SWLOR.Shared.Abstractions.Contracts;

public interface IRandomService
{
    /// <summary>
    /// Retrieves the next random integer value.
    /// </summary>
    /// <returns>The next random integer value.</returns>
    int Next();

    /// <summary>
    /// Retrieves the next random integer value, not to exceed the max value.
    /// </summary>
    /// <param name="max">The max value to randomly get.</param>
    /// <returns>The next random integer value.</returns>
    int Next(int max);

    /// <summary>
    /// Retrieves the next random integer value, not to fall outside of the min and max range.
    /// </summary>
    /// <param name="min">The minimum value</param>
    /// <param name="max">The maximum value</param>
    /// <returns>The next random integer value</returns>
    int Next(int min, int max);

    /// <summary>
    /// Retrieves the next random float value
    /// </summary>
    /// <returns>The next random float value</returns>
    float NextFloat();

    /// <summary>
    /// Retrieves the next random float value, not to fall outside of the min and max range.
    /// </summary>
    /// <param name="min">The minimum value</param>
    /// <param name="max">The maximum value</param>
    /// <returns>The next random float value</returns>
    float NextFloat(float min, float max);

    /// <summary>
    /// Retrieves a random index out of the provided weight values.
    /// </summary>
    /// <param name="weights">The array of weights to randomly select from</param>
    /// <returns>The index of the selected value</returns>
    int GetRandomWeightedIndex(int[] weights);

    /// <summary>
    /// Rolls dice with 2 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D2(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 3 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D3(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 4 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D4(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 6 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D6(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 8 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D8(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 10 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D10(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 12 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D12(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 20 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D20(int numberOfDice, int minimum = 1);

    /// <summary>
    /// Rolls dice with 100 sides.
    /// </summary>
    /// <param name="numberOfDice">The number of dice to roll</param>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>A random value selected base on the parameters.</returns>
    int D100(int numberOfDice, int minimum = 1);
}
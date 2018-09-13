namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IRandomService
    {
        int D10(int numberOfDice, int min = 1);
        int D100(int numberOfDice, int min = 1);
        int D12(int numberOfDice, int min = 1);
        int D2(int numberOfDice, int min = 1);
        int D20(int numberOfDice, int min = 1);
        int D3(int numberOfDice, int min = 1);
        int D4(int numberOfDice, int min = 1);
        int D6(int numberOfDice, int min = 1);
        int D8(int numberOfDice, int min = 1);
        int GetRandomWeightedIndex(int[] weights);
        int Random();
        int Random(int max);
        int Random(int min, int max);
        float RandomFloat();
        float RandomFloat(float min, float max);
    }
}

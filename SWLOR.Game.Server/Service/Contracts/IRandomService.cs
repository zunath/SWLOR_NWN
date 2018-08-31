namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IRandomService
    {
        int Random();
        int Random(int max);
        int Random(int min, int max);
        float RandomFloat();
        int GetRandomWeightedIndex(int[] weights);
    }
}

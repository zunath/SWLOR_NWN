namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IRandomService
    {
        int Random();
        int Random(int max);
        int Random(int min, int max);
        float RandomFloat();
        float RandomFloat(float min, float max);
        int GetRandomWeightedIndex(int[] weights);
    }
}

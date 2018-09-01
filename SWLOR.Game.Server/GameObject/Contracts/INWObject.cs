using NWN;

namespace SWLOR.Game.Server.GameObject.Contracts
{
    public interface INWObject
    {
        NWArea Area { get; }
        int CurrentHP { get; }
        float Facing { get; set; }
        bool IsPlot { get; set; }
        bool IsValid { get; }
        Location Location { get; set; }
        int MaxHP { get; }
        string Name { get; set; }
        Vector Position { get; }
        string Resref { get; }
        string Tag { get; set; }

        void DestroyAllInventoryItems();

        void DeleteLocalFloat(string name);
        void DeleteLocalInt(string name);
        void DeleteLocalLocation(string name);
        void DeleteLocalObject(string name);
        void DeleteLocalString(string name);
        float GetLocalFloat(string name);
        int GetLocalInt(string name);
        Location GetLocalLocation(string name);
        Object GetLocalObject(string name);
        string GetLocalString(string name);
        void SetLocalFloat(string name, float value);
        void SetLocalInt(string name, int value);
        void SetLocalLocation(string name, Location value);
        void SetLocalObject(string name, Object value);
        void SetLocalString(string name, string value);

        void AssignCommand(ActionDelegate action);
        void DelayCommand(ActionDelegate action, float delay);
    }
}
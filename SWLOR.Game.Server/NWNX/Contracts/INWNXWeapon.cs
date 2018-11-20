namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXWeapon
    {
        void BypassDevastatingCritical();
        void SetDevastatingCriticalEventScript(string sScript);
        void SetEpicWeaponDevastatingCriticalFeat(int nBaseItem, int nFeat);
        void SetEpicWeaponFocusFeat(int nBaseItem, int nFeat);
        void SetEpicWeaponOverwhelmingCriticalFeat(int nBaseItem, int nFeat);
        void SetEpicWeaponSpecializationFeat(int nBaseItem, int nFeat);
        void SetGreaterWeaponFocusFeat(int nBaseItem, int nFeat);
        void SetGreaterWeaponSpecializationFeat(int nBaseItem, int nFeat);
        void SetOption(int nOption, int nVal);
        void SetWeaponFinesseSize(int nBaseItem, int nSize);
        void SetWeaponFocusFeat(int nBaseItem, int nFeat);
        void SetWeaponImprovedCriticalFeat(int nBaseItem, int nFeat);
        void SetWeaponIsMonkWeapon(int nBaseItem);
        void SetWeaponOfChoiceFeat(int nBaseItem, int nFeat);
        void SetWeaponSpecializationFeat(int nBaseItem, int nFeat);
        void SetWeaponUnarmed(int nBaseItem);
    }
}
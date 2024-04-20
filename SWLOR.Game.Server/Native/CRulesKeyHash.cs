namespace SWLOR.Game.Server.Native
{
    public readonly struct CRulesKeyHash
    {
        public ulong Hash { get; }

        public CRulesKeyHash(string key)
        {
            Hash = 14695981039346656037ul;
            for (int i = 0; i < key.Length; i++)
            {
                Hash = (Hash ^ key[i]) * 1099511628211ul;
            }
        }

        public static implicit operator CRulesKeyHash(string key)
        {
            return new CRulesKeyHash(key);
        }

        public static implicit operator ulong(CRulesKeyHash hash)
        {
            return hash.Hash;
        }
    }
}

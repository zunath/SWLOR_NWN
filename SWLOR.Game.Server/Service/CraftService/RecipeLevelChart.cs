using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipeLevelChart
    {
        private readonly Dictionary<int, RecipeLevelDetail> _recipeLevels = new();

        public RecipeLevelChart()
        {
            _recipeLevels[1] = new RecipeLevelDetail(19, 312, 60, 0.0025f);
            _recipeLevels[2] = new RecipeLevelDetail(20, 325, 60, 0.0025f);
            _recipeLevels[3] = new RecipeLevelDetail(20, 339, 60, 0.0025f);
            _recipeLevels[4] = new RecipeLevelDetail(21, 352, 60, 0.0025f);
            _recipeLevels[5] = new RecipeLevelDetail(33, 451, 60, 0.0025f);
            _recipeLevels[6] = new RecipeLevelDetail(36, 474, 60, 0.0025f);
            _recipeLevels[7] = new RecipeLevelDetail(37, 492, 60, 0.0025f);
            _recipeLevels[8] = new RecipeLevelDetail(41, 526, 60, 0.0025f);
            _recipeLevels[9] = new RecipeLevelDetail(42, 545, 60, 0.0025f);
            _recipeLevels[10] = new RecipeLevelDetail(45, 629, 60, 0.0025f);

            _recipeLevels[11] = new RecipeLevelDetail(48, 665, 60, 0.0025f);
            _recipeLevels[12] = new RecipeLevelDetail(53, 702, 60, 0.0025f);
            _recipeLevels[13] = new RecipeLevelDetail(54, 726, 60, 0.0025f);
            _recipeLevels[14] = new RecipeLevelDetail(54, 751, 60, 0.0025f);
            _recipeLevels[15] = new RecipeLevelDetail(55, 807, 70, 0.025f);
            _recipeLevels[16] = new RecipeLevelDetail(63, 866, 70, 0.025f);
            _recipeLevels[17] = new RecipeLevelDetail(66, 898, 70, 0.025f);
            _recipeLevels[18] = new RecipeLevelDetail(67, 939, 70, 0.025f);
            _recipeLevels[19] = new RecipeLevelDetail(68, 982, 70, 0.025f);
            _recipeLevels[20] = new RecipeLevelDetail(74, 1053, 70, 0.025f);

            _recipeLevels[21] = new RecipeLevelDetail(75, 1090, 70, 0.025f);
            _recipeLevels[22] = new RecipeLevelDetail(75, 1122, 70, 0.025f);
            _recipeLevels[23] = new RecipeLevelDetail(79, 1169, 70, 0.025f);
            _recipeLevels[24] = new RecipeLevelDetail(85, 1239, 70, 0.025f);
            _recipeLevels[25] = new RecipeLevelDetail(89, 1296, 70, 0.025f);
            _recipeLevels[26] = new RecipeLevelDetail(90, 1332, 70, 0.025f);
            _recipeLevels[27] = new RecipeLevelDetail(91, 1368, 70, 0.025f);
            _recipeLevels[28] = new RecipeLevelDetail(100,1498, 70, 0.025f);
            _recipeLevels[29] = new RecipeLevelDetail(101,1544, 70, 0.025f);
            _recipeLevels[30] = new RecipeLevelDetail(102, 1584, 70, 0.025f);

            _recipeLevels[31] = new RecipeLevelDetail(106,1670, 80, 0.025f);
            _recipeLevels[32] = new RecipeLevelDetail(110,1697, 80, 0.025f);
            _recipeLevels[33] = new RecipeLevelDetail(111,1757, 80, 0.025f);
            _recipeLevels[34] = new RecipeLevelDetail(115,1811, 80, 0.025f);
            _recipeLevels[35] = new RecipeLevelDetail(123,1853, 80, 0.025f);
            _recipeLevels[36] = new RecipeLevelDetail(124,1882, 80, 0.025f);
            _recipeLevels[37] = new RecipeLevelDetail(128,1905, 80, 0.025f);
            _recipeLevels[38] = new RecipeLevelDetail(129,1961, 80, 0.025f);
            _recipeLevels[39] = new RecipeLevelDetail(137,2026, 80, 0.025f);
            _recipeLevels[40] = new RecipeLevelDetail(138, 2050, 80, 0.02f);

            _recipeLevels[41] = new RecipeLevelDetail(143,2109, 80, 0.02f);
            _recipeLevels[42] = new RecipeLevelDetail(144,2147, 80, 0.02f);
            _recipeLevels[43] = new RecipeLevelDetail(155,2251, 80, 0.02f);
            _recipeLevels[44] = new RecipeLevelDetail(156,2277, 80, 0.02f);
            _recipeLevels[45] = new RecipeLevelDetail(158,2309, 80, 0.02f);
            _recipeLevels[46] = new RecipeLevelDetail(159,2372, 80, 0.02f);
            _recipeLevels[47] = new RecipeLevelDetail(167,2421, 80, 0.02f);
            _recipeLevels[48] = new RecipeLevelDetail(172,2524, 80, 0.02f);
            _recipeLevels[49] = new RecipeLevelDetail(174,2551, 80, 0.02f);
            _recipeLevels[50] = new RecipeLevelDetail(186, 2641, 80, 0.018f);
        }

        public RecipeLevelDetail GetByLevel(int level)
        {
            return _recipeLevels[level];
        }

    }
}

using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class RecipeLevelChart
    {
        private readonly Dictionary<int, RecipeLevelDetail> _recipeLevels = new();

        // Source: https://docs.google.com/document/d/1Da48dDVPB7N4ignxGeo0UeJ_6R0kQRqzLUH-TkpSQRc/edit
        // Numbers taken from: https://docs.google.com/spreadsheets/d/1n8iteSp1Aa4X2_zXxo7j3soxsmik4K1mG6UZiBPBoNU/edit#gid=247478737
        public RecipeLevelChart()
        {
            _recipeLevels[1] = new RecipeLevelDetail(19, 312, 60);
            _recipeLevels[2] = new RecipeLevelDetail(20, 325, 60);
            _recipeLevels[3] = new RecipeLevelDetail(20, 339, 60);
            _recipeLevels[4] = new RecipeLevelDetail(21, 352, 60);
            _recipeLevels[5] = new RecipeLevelDetail(33, 451, 60);
            _recipeLevels[6] = new RecipeLevelDetail(36, 474, 60);
            _recipeLevels[7] = new RecipeLevelDetail(37, 492, 60);
            _recipeLevels[8] = new RecipeLevelDetail(41, 526, 60);
            _recipeLevels[9] = new RecipeLevelDetail(42, 545, 60);
            _recipeLevels[10] = new RecipeLevelDetail(45, 629, 60);

            _recipeLevels[11] = new RecipeLevelDetail(48, 665, 60);
            _recipeLevels[12] = new RecipeLevelDetail(53, 702, 60);
            _recipeLevels[13] = new RecipeLevelDetail(54, 726, 60);
            _recipeLevels[14] = new RecipeLevelDetail(54, 751, 60);
            _recipeLevels[15] = new RecipeLevelDetail(55, 807, 70);
            _recipeLevels[16] = new RecipeLevelDetail(63, 866, 70);
            _recipeLevels[17] = new RecipeLevelDetail(66, 898, 70);
            _recipeLevels[18] = new RecipeLevelDetail(67, 939, 70);
            _recipeLevels[19] = new RecipeLevelDetail(68, 982, 70);
            _recipeLevels[20] = new RecipeLevelDetail(74, 1053, 70);

            _recipeLevels[21] = new RecipeLevelDetail(75, 1090, 70);
            _recipeLevels[22] = new RecipeLevelDetail(75, 1122, 70);
            _recipeLevels[23] = new RecipeLevelDetail(79, 1169, 70);
            _recipeLevels[24] = new RecipeLevelDetail(85, 1239, 70);
            _recipeLevels[25] = new RecipeLevelDetail(89, 1296, 70);
            _recipeLevels[26] = new RecipeLevelDetail(90, 1332, 70);
            _recipeLevels[27] = new RecipeLevelDetail(91, 1368, 70);
            _recipeLevels[28] = new RecipeLevelDetail(100,1498, 70);
            _recipeLevels[29] = new RecipeLevelDetail(101,1544, 70);
            _recipeLevels[30] = new RecipeLevelDetail(102, 1584, 70);

            _recipeLevels[31] = new RecipeLevelDetail(106,1670, 80);
            _recipeLevels[32] = new RecipeLevelDetail(110,1697, 80);
            _recipeLevels[33] = new RecipeLevelDetail(111,1757, 80);
            _recipeLevels[34] = new RecipeLevelDetail(115,1811, 80);
            _recipeLevels[35] = new RecipeLevelDetail(123,1853, 80);
            _recipeLevels[36] = new RecipeLevelDetail(124,1882, 80);
            _recipeLevels[37] = new RecipeLevelDetail(128,1905, 80);
            _recipeLevels[38] = new RecipeLevelDetail(129,1961, 80);
            _recipeLevels[39] = new RecipeLevelDetail(137,2026, 80);
            _recipeLevels[40] = new RecipeLevelDetail(138, 2050, 80);

            _recipeLevels[41] = new RecipeLevelDetail(143,2109, 80);
            _recipeLevels[42] = new RecipeLevelDetail(144,2147, 80);
            _recipeLevels[43] = new RecipeLevelDetail(155,2251, 80);
            _recipeLevels[44] = new RecipeLevelDetail(156,2277, 80);
            _recipeLevels[45] = new RecipeLevelDetail(158,2309, 80);
            _recipeLevels[46] = new RecipeLevelDetail(159,2372, 80);
            _recipeLevels[47] = new RecipeLevelDetail(167,2421, 80);
            _recipeLevels[48] = new RecipeLevelDetail(172,2524, 80);
            _recipeLevels[49] = new RecipeLevelDetail(174,2551, 80);
            _recipeLevels[50] = new RecipeLevelDetail(186, 2641, 80);

            _recipeLevels[51] = new RecipeLevelDetail(339, 3951, 80);
            _recipeLevels[52] = new RecipeLevelDetail(503, 5172, 80);
            _recipeLevels[53] = new RecipeLevelDetail(586, 5783, 80);
            _recipeLevels[54] = new RecipeLevelDetail(641, 6042, 80);
            _recipeLevels[55] = new RecipeLevelDetail(697, 6301, 70);
            _recipeLevels[56] = new RecipeLevelDetail(752, 6561, 70);
            _recipeLevels[57] = new RecipeLevelDetail(808, 6820, 70);
            _recipeLevels[58] = new RecipeLevelDetail(863, 7080, 70);
            _recipeLevels[59] = new RecipeLevelDetail(919, 7339, 70);
            _recipeLevels[60] = new RecipeLevelDetail(956, 7851, 70);

            _recipeLevels[61] = new RecipeLevelDetail(1116, 8377, 80);
            _recipeLevels[62] = new RecipeLevelDetail(1263, 8581, 80);
            _recipeLevels[63] = new RecipeLevelDetail(1476, 9186, 80);
            _recipeLevels[64] = new RecipeLevelDetail(1586, 9657, 80);
            _recipeLevels[65] = new RecipeLevelDetail(1697, 10023, 80);
            _recipeLevels[66] = new RecipeLevelDetail(1808, 10389, 80);
            _recipeLevels[67] = new RecipeLevelDetail(1919, 10755, 80);
            _recipeLevels[68] = new RecipeLevelDetail(2029, 11121, 80);
            _recipeLevels[69] = new RecipeLevelDetail(2140, 11490, 80);
            _recipeLevels[70] = new RecipeLevelDetail(2214, 11736, 80);

            _recipeLevels[71] = new RecipeLevelDetail(3149, 13086, 80);
            _recipeLevels[72] = new RecipeLevelDetail(3248, 13660, 80);
            _recipeLevels[73] = new RecipeLevelDetail(3348, 14062, 80);
            _recipeLevels[74] = new RecipeLevelDetail(3407, 14482, 80);
            _recipeLevels[75] = new RecipeLevelDetail(3467, 14902, 80);
            _recipeLevels[76] = new RecipeLevelDetail(3526, 15322, 80);
            _recipeLevels[77] = new RecipeLevelDetail(3586, 15742, 80);
            _recipeLevels[78] = new RecipeLevelDetail(3645, 16162, 80);
            _recipeLevels[79] = new RecipeLevelDetail(3705, 16582, 80);
            _recipeLevels[80] = new RecipeLevelDetail(3943, 18262, 80);
        }

        public RecipeLevelDetail GetByLevel(int level)
        {
            return _recipeLevels[level];
        }

    }
}

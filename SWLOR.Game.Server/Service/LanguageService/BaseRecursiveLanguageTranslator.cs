using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Service.LanguageService
{
    public abstract class BaseRecursiveLanguageTranslator : ITranslator
    {
        public BaseRecursiveLanguageTranslator()
        {
            updateDictionaries();
        }
        public bool UseCache = true;
        public bool UseGeneralTranslations=true;
        public void ClearCache()
        {
            //20221129 Hans: When using a dicitonary might be better?
            if (_translationCache != null)
            {
                _translationCache = null;
                lastTranslation = null;
            }
                //_translationCache.Clear();
        }

        //20221129 Hans: When using a dicitonary might be better?
        //private Dictionary<string, string[,]> _translationCache;
        private string[,] _translationCache = null;
        private string lastTranslation = null;

        private Dictionary<string, string>[] _translationsArrayedByLength;
        private Dictionary<string, string>[] _translationsArrayedByLengthAccessor
        {
            get
            {
                if (_translationsArrayedByLength != null)
                    return _translationsArrayedByLength;
                else
                {
                    updateDictionaries();
                    return _translationsArrayedByLength;
                }
            }
        }

        private void updateDictionaries()
        {
            Dictionary<string, string>[] dictionariesBylengthArrayed = null;
            Dictionary<int, Dictionary<string, string>> dictionariesBylength = new Dictionary<int, Dictionary<string, string>>();


            using (var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(string.Format("{0}.LanguageSpecific.csv", this.GetType().Namespace)))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    var res = line.Split(';');

                    if (res[2] == "startcon")
                        continue;



                    foreach (var combi in GetCodeCombinations(prepareStringForTranslation(res[1]), prepareStringForTranslation(res[0]), res[2], res[3]))
                    {
                        //language specific translations
                        if (!dictionariesBylength.ContainsKey(combi.Key.Length))
                            dictionariesBylength.Add(combi.Key.Length, new Dictionary<string, string>() { [combi.Key.ToLower()] = combi.Value.ToLower() });
                        else
                        {
                            dictionariesBylength[combi.Key.Length].Add(combi.Key.ToLower(), combi.Value.ToLower());
                        }
                    }

                    if (res[6] == "1")
                    {
                        foreach (var item in GetCodeCombinations(prepareStringForTranslation(res[0]), res[4], res[5]))
                        {
                            if (!dictionariesBylength.ContainsKey(item.Length))
                                dictionariesBylength.Add(item.Length, new Dictionary<string, string>() { [item.ToLower()] = item.ToLower() });
                            else
                            {
                                if (!dictionariesBylength[item.Length].ContainsKey(item.ToLower()))
                                    dictionariesBylength[item.Length].Add(item.ToLower(), item.ToLower());
                            }
                        }
                    }

                }
            }

            if (UseGeneralTranslations)
            {
                using (var stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("SWLOR.Game.Server.Service.LanguageService.GeneralTranslations.csv"))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {

                        var res = line.Split(';');


                        //general translations i.e. monikers are added for words which dont have their specific translation in-language (like viscara -> viscara)
                        if (!dictionariesBylength.ContainsKey(res[0].Length))
                            dictionariesBylength.Add(res[0].Length, new Dictionary<string, string>() { [res[0].ToLower()] = res[0].ToLower() });
                        else
                        {
                            Dictionary<string, string> dictatlength = dictionariesBylength[res[0].Length];
                            if (!dictatlength.ContainsKey(res[0].ToLower()))
                                dictatlength.Add(res[0].ToLower(), res[0].ToLower());

                            //for general to overwrite the specific translation
                            //else
                            //    dictatlength[res[0]] = res[0];
                        }
                    }
                    // do something with the CSV
                }
            }

            dictionariesBylengthArrayed = new Dictionary<string, string>[dictionariesBylength.Keys.Max()];
            foreach (var item in dictionariesBylength)
            {
                dictionariesBylengthArrayed[item.Key - 1] = item.Value;
            }

            _translationsArrayedByLength = dictionariesBylengthArrayed;
        }
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            if (UseCache)
                return translateFromCache(message, englishChance, out partiallyScrambled);

            else
            {
                partiallyScrambled = "";
                //return _translationsArrayedByLengthAccessor.Length.ToString();
                var sb = new StringBuilder();

                //Fake the end of the former sentence, to better recognize the start of the new sentence
                sb.Append(". ");
                sb.Append(message);
                //Add " at the end to account for missing punctuation
                sb.Append("\"");

                string result = sanitizeStringAfterTranslation(translateRecursive(prepareStringForTranslation(sb.ToString()), englishChance, out partiallyScrambled));
                partiallyScrambled = sanitizeStringAfterTranslation(partiallyScrambled);
                partiallyScrambled = partiallyScrambled.Substring(2, partiallyScrambled.Length - 3);
                return result.Substring(2, result.Length - 3);
            }
        }

        private string translateFromCache(string message, int englishChance, out string partiallyScrambled)
        {
            

            
            string[,] resultArray;

            if (lastTranslation == message)
            {
                resultArray = _translationCache;
            }
            else
            {
                var sb = new StringBuilder();
                sb.Append(". ");
                sb.Append(message);
                //Add " at the end to account for missing punctuation
                sb.Append("\"");

                resultArray = translateRecursiveToCache(prepareStringForTranslation(sb.ToString()));
                _translationCache = resultArray;
                lastTranslation = message;

            }
            //20221129 Hans: For using a dictionary
            /*if(_translationCache != null && _translationCache.TryGetValue(message, out resultArray))
            {}
            else
            {
                var sb = new StringBuilder();
                sb.Append(". ");
                sb.Append(message);
                //Add " at the end to account for missing punctuation
                sb.Append("\"");

                if (_translationCache == null)
                    _translationCache = new Dictionary<string, string[,]>();

                resultArray = translateRecursiveToCache(prepareStringForTranslation(sb.ToString()));
                _translationCache[message] = resultArray;
            }*/

            

            StringBuilder og = new StringBuilder();
            StringBuilder tr = new StringBuilder();
            for (int i = 0; i < resultArray.GetLength(0); i++)
            {
                tr.Append(resultArray[i, 1]);
                if(Random.Next(100) <= englishChance && englishChance != 0)
                {
                    og.Append(resultArray[i, 0]);
                }
                else
                {
                    og.Append(resultArray[i, 1]);
                }
            }

            string result = sanitizeStringAfterTranslation(tr.ToString());
            partiallyScrambled = sanitizeStringAfterTranslation(og.ToString());

            partiallyScrambled = partiallyScrambled.Substring(2, partiallyScrambled.Length - 3);
            return result.Substring(2, result.Length - 3);

        }




        private string translateRecursive(string message, int englishChance, out string partiallyScrambled, int lastLength = -1, int stateBeforeMatchAfterMatch = 0, string oldPiece ="")
        {
            partiallyScrambled = "";
            if (string.IsNullOrEmpty(message) || message.All(x => x == ' '))
            {
                partiallyScrambled = message;
                return message;
            }
            string originalSnippet = message;
            string snippetToLower = originalSnippet.ToLower();
            int currentLength = _translationsArrayedByLengthAccessor.Length - 1;
            if (lastLength != -1)
                currentLength = lastLength;

            StringBuilder sb = new StringBuilder();
            StringBuilder sbp = new StringBuilder();

            for (int i = currentLength; i >= 0; i--)
            {
                if (_translationsArrayedByLengthAccessor[i] != null && snippetToLower.Length >= i + 1)
                {
                    KeyValuePair<string, string> entry;
                    if (stringContainsDictionaryKey(snippetToLower, _translationsArrayedByLengthAccessor[i], i + 1, out entry))
                    {
                        bool success = (Random.Next(100) <= englishChance && englishChance != 0);
                        int whereFound = snippetToLower.IndexOf(entry.Key);
                        string match = snippetToLower.Substring(whereFound, entry.Key.Length);
                        string beforeMatch = originalSnippet.Substring(0, whereFound);
                        string afterMatch = originalSnippet.Substring(whereFound + entry.Key.Length);
                        int wherefoundForCap = whereFound;


                        //originalSnippet.
                        string stringWhereFound = originalSnippet.Substring(whereFound, match.Length);
                        StringBuilder oSb = new StringBuilder();
                        if (stateBeforeMatchAfterMatch == 2)
                        {
                            oSb.Append(oldPiece);
                            wherefoundForCap += oldPiece.Length; ;
                        }
                        oSb.Append(originalSnippet);
                        if (stateBeforeMatchAfterMatch == 1)
                            oSb.Append(oldPiece);

                        string oldSnippedForCap = oSb.ToString();

                        char charBeforeMatchForCap = ' ';
                        char charAfterMatchForCap = ' ';
                        if (wherefoundForCap != 0)
                            charBeforeMatchForCap = oldSnippedForCap[wherefoundForCap - 1];
                        if (wherefoundForCap + match.Length <= oldSnippedForCap.Length - 1)
                            charAfterMatchForCap = oldSnippedForCap[wherefoundForCap + match.Length];


                        string beforeMatchPartially = "";
                        sb.Append(translateRecursive(beforeMatch, englishChance, out beforeMatchPartially, i, 1, stringWhereFound));
                        sbp.Append(beforeMatchPartially);

                        int cap = 0;
                        if ((stringWhereFound.Count(x => char.IsLetter(x)) >= 2 && !stringWhereFound.Any(x => char.IsLower(x))) || stringWhereFound.All(x => char.IsUpper(x)) && (char.IsUpper(charBeforeMatchForCap) || char.IsUpper(charAfterMatchForCap)))// ) && (char.IsUpper(beforeMatch.Last()) || char.IsUpper(afterMatch.First())) && !stringWhereFound.Any(x => char.IsLetter(x) && char.IsLower(x)) )
                        {
                            sb.Append(entry.Value.ToUpper());
                            cap = 2;
                        }
                        else if (stringWhereFound.Any(x => char.IsUpper(x)))
                        {
                            sb.Append(capitalizeStringAtIndex(entry.Value, getIndexOfFirstLetter(entry.Value)));
                            cap = 1;
                        }
                        else
                            sb.Append(entry.Value);

                        string translated = "";
                        if (success)
                        {
                            translated = originalSnippet.Substring(whereFound, match.Length);
                        }
                        else
                        {
                            if (cap == 2)
                            {
                                translated = entry.Value.ToUpper();
                            }
                            else if (cap == 1)
                            {
                                translated = capitalizeStringAtIndex(entry.Value, getIndexOfFirstLetter(entry.Value));
                            }
                            else
                                translated = entry.Value;
                        }
                        sbp.Append(translated);

                        string afterMatchPartially = "";
                        sb.Append(translateRecursive(afterMatch, englishChance, out afterMatchPartially, i, 2, stringWhereFound));
                        sbp.Append(afterMatchPartially);
                        partiallyScrambled = sbp.ToString();
                        return sb.ToString();


                    }
                }
            }
            partiallyScrambled = message;
            return message;
        }


        private string[,] translateRecursiveToCache(string message, int lastLength = -1, int stateBeforeMatchAfterMatch = 0, string oldPiece = "")
        {
            
            if (string.IsNullOrEmpty(message) || message.All(x => x == ' '))
            {
                return new string[,]{ {message, message } };
            }
            string originalSnippet = message;
            string snippetToLower = originalSnippet.ToLower();
            int currentLength = _translationsArrayedByLengthAccessor.Length - 1;
            if (lastLength != -1)
                currentLength = lastLength;

            for (int i = currentLength; i >= 0; i--)
            {
                if (_translationsArrayedByLengthAccessor[i] != null && snippetToLower.Length >= i + 1)
                {
                    KeyValuePair<string, string> entry;
                    if (stringContainsDictionaryKey(snippetToLower, _translationsArrayedByLengthAccessor[i], i + 1, out entry))

                    {

                        int whereFound = snippetToLower.IndexOf(entry.Key);
                        string match = snippetToLower.Substring(whereFound, entry.Key.Length);
                        string beforeMatch = originalSnippet.Substring(0, whereFound);
                        string afterMatch = originalSnippet.Substring(whereFound + entry.Key.Length);

                        int wherefoundForCap = whereFound;

                        string stringWhereFound = originalSnippet.Substring(whereFound, match.Length);
                        StringBuilder oSb = new StringBuilder();
                        if (stateBeforeMatchAfterMatch == 2)
                        {
                            oSb.Append(oldPiece);
                            wherefoundForCap += oldPiece.Length; ;
                        }
                        oSb.Append(originalSnippet);
                        if (stateBeforeMatchAfterMatch == 1)
                            oSb.Append(oldPiece);

                        string oldSnippedForCap = oSb.ToString();

                        char charBeforeMatchForCap = ' ';
                        char charAfterMatchForCap = ' ';
                        if (wherefoundForCap != 0)
                            charBeforeMatchForCap = oldSnippedForCap[wherefoundForCap - 1];
                        if (wherefoundForCap + match.Length <= oldSnippedForCap.Length - 1)
                            charAfterMatchForCap = oldSnippedForCap[wherefoundForCap + match.Length];


                        string translated = "";


                        if ((stringWhereFound.Count(x => char.IsLetter(x)) >= 2 && !stringWhereFound.Any(x => char.IsLower(x))) || stringWhereFound.All(x => char.IsUpper(x)) && (char.IsUpper(charBeforeMatchForCap) || char.IsUpper(charAfterMatchForCap)))// ) && (char.IsUpper(beforeMatch.Last()) || char.IsUpper(afterMatch.First())) && !stringWhereFound.Any(x => char.IsLetter(x) && char.IsLower(x)) )
                        {
                            translated = entry.Value.ToUpper();
                        }
                        else if (originalSnippet.Substring(whereFound, match.Length).Any(x => char.IsUpper(x)))
                        {
                            translated = capitalizeStringAtIndex(entry.Value, getIndexOfFirstLetter(entry.Value));
                        }
                        else
                            translated = entry.Value;



                        var beforeArray = translateRecursiveToCache(beforeMatch, i, 1, stringWhereFound);
                        var afterArray = translateRecursiveToCache(afterMatch, i, 2, stringWhereFound);
                        var resultArray = new string[beforeArray.GetLength(0) + afterArray.GetLength(0) + 1, 2];

                        Array.Copy(beforeArray, 0, resultArray, 0, beforeArray.Length);
                        Array.Copy(new string[,] { { originalSnippet.Substring(whereFound, match.Length), translated } }, 0, resultArray, beforeArray.Length, 2);
                        Array.Copy(afterArray, 0, resultArray, beforeArray.Length + 2, afterArray.Length);


                        return resultArray;



                    }
                }
            }
            
            return new string[,] { { message, message } };
        }


        private bool stringContainsDictionaryKey(string snippet, Dictionary<string, string> dict, int length, out KeyValuePair<string, string> result)
        {
            result = default(KeyValuePair<string, string>);
            for (int i = 0; i <= snippet.Length-length; i++)
            {
                string key = snippet.Substring(i, length);
                string value = "";
                bool success = dict.TryGetValue(key, out value);
                if (success)
                {
                    result = new KeyValuePair<string, string>(key, value);
                    return success;
                }
            }
            return false;
        }
        string capitalizeStringAtIndex(string input, int index)
        {
            if (string.IsNullOrEmpty(input))
                return "";
            StringBuilder sb = new StringBuilder();
            sb.Append(input.Substring(0, index));
            sb.Append(input.Substring(index, 1).ToUpper());
            sb.Append(input.Substring(index + 1));
            return sb.ToString();
        }
        int getIndexOfFirstLetter(string input)
        {
            var index = 0;
            foreach (var c in input)
                if (char.IsLetter(c))
                    return index;
                else
                    index++;

            return 0;
        }

        private static KeyValuePair<string, string>[] GetCodeCombinations(string original,string translation, string prefixCode, string suffixCode)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            StringBuilder sbOG = new StringBuilder();
            StringBuilder sbTr = new StringBuilder();
            string[,] prefixCodes = getPrefixArray(prefixCode);
            string[,] suffixCodes = getSuffixArray(suffixCode);
            
            for (int x = 0; x < prefixCodes.GetLength(0); x++)
            {
                for (int y = 0; y < suffixCodes.GetLength(0); y++)
                {
                    sbOG.Append(prefixCodes[x,0]);
                    sbTr.Append(prefixCodes[x,1]);
                    sbOG.Append(original);
                    sbTr.Append(translation);
                    sbOG.Append(suffixCodes[y,0]);
                    sbTr.Append(suffixCodes[y,1]);
                    result.Add(new KeyValuePair<string,string>(sbOG.ToString(), sbTr.ToString()));
                    sbOG.Clear();
                    sbTr.Clear();
                }
            }
            return result.ToArray();
        }
        private static string[] GetCodeCombinations(string original, string prefixCode, string suffixCode)
        {
            List<string> result = new List<string>();

            StringBuilder sbOG = new StringBuilder();
            string[,] prefixCodes = getPrefixArray(prefixCode);
            string[,] suffixCodes = getSuffixArray(suffixCode);

            for (int x = 0; x < prefixCodes.GetLength(0); x++)
            {
                for (int y = 0; y < suffixCodes.GetLength(0); y++)
                {
                    sbOG.Append(prefixCodes[x, 1]);
                    sbOG.Append(original);
                    sbOG.Append(suffixCodes[y, 1]);
                    result.Add(sbOG.ToString());
                    sbOG.Clear();
                }
            }
            return result.ToArray();
        }

        private string prepareStringForTranslation(string message)
        {
            string prepped = message;
            prepped = prepped.Replace(" ", "  ");
            prepped = prepped.Replace(",", ",,");
            prepped = prepped.Replace(";", ";;");
            prepped = prepped.Replace(".", "..");
            prepped = prepped.Replace("!", "!!");
            prepped = prepped.Replace("?", "??");
            prepped = prepped.Replace("-", "--");
            return prepped;
        }

        private string sanitizeStringAfterTranslation(string message)
        {
            string sanitized = message;
            sanitized = removeSingleOccurence(message, new char[]{ ' ', '-'});
            sanitized = sanitized.Replace("  "," ");
            sanitized = sanitized.Replace(",,",",");
            sanitized = sanitized.Replace(";;",";");
            sanitized = sanitized.Replace("..",".");
            sanitized = sanitized.Replace("!!","!");
            sanitized = sanitized.Replace("??","?");
            sanitized = sanitized.Replace("--","-");
            return sanitized;
        }
        private string removeSingleOccurence(string message, char[] chars)
        {
            string result = message;
            List<int> indexOfSingle = new List<int>();
            for (int i = 0; i < message.Length; i++)
            {
                char before = default(char);
                char after = default(char);

                if (i != 0)
                    before = message[i - 1];

                if(i != message.Length-1)
                    after = message[i + 1];

                foreach (var item in chars)
                {
                    if (message[i] == item && before != item && after != item)
                    {
                        indexOfSingle.Add(i);
                    }
                }
            }
            int removed = 0;
            foreach (var item in indexOfSingle)
            {
                result = result.Remove(item - removed, 1);
                removed++;
            }
            return result;
        }
        private static string[,] getPrefixArray(string code)
        {

            //no code specified, take as is
            if (string.IsNullOrEmpty(code))
                return new string[,] { { "","" } };

            switch (code)
            {
                
                //starting word
                case "w":
                    return new string[,] { { " ", " " }, { "-", "-" }, { "\"", "\"" } };
                //starting sequence
                case "seq":
                    return new string[,] { { ",  ", ",  " }, { ";  ", ";  " } };
                //starting sentence
                case "sen":
                    return new string[,] { { ".  ", ".  " }, { "!  ", "!  " }, { "?  ", "?  " }, { "-  ", "-  " }, { " \"", " \"" } };
                //starting sequence and sentence
                case "ss":
                    return new string[,] { { ",  ", ",  " }, { ";  ", ";  " }, { ".  ", ".  " }, { "!  ", "!  " }, { "?  ", "?  " }, { "-  ", "-  " }, { " \"", " \"" } };
                //starting word, append to former
                case "ap":
                    return new string[,] { { " ", "" }, { "-", "" } };
                default:
                    return new string[,] { { "", "" } };
            }
        }

        private static string[,] getSuffixArray(string code)
        {

            //no code specified, take as is
            if (string.IsNullOrEmpty(code))
                return new string[,] { { "", "" } };

            switch (code)
            {
                //ending only word (to use with seq, sen and ss, otherwise w)
                case "ow":
                    return new string[,] { { " ", " " }, { "-", "-" } };
                //ending word
                case "w":
                    return new string[,] { { " ", " " }, { ",", "," }, { ";", ";" }, { ".", "." }, { "!", "!" }, { "?", "?" }, { "-", "-" }, { "\"", "\"" } };
                //ending sequence
                case "seq":
                    return new string[,] { { ",", "," }, { ";", ";" } };
                //ending sentence
                case "sen":
                    return new string[,] { { ".", "." }, { "!", "!" }, { "?", "?" }, { "-", "-" }, { "\"", "\"" } };
                //ending sequence and sentence
                case "ss":
                    return new string[,] { { ",", "," }, { ";", ";" }, { ".", "." }, { "!", "!" }, { "?", "?" }, { "-", "-" }, { "\"", "\"" } };
                //starting word, append to former
                case "ap":
                    return new string[,] { { " ", "" }, { "-", "" } };
                default:
                    return new string[,] { { "", "" } };
            }
        }
    }
}

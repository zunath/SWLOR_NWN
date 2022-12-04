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
            UpdateDictionaries();
        }
        //For using the Translator externally, to test and implement languages without having to compile.
        public BaseRecursiveLanguageTranslator(string generalLoc, string specificLoc)
        {
            langFileLocation = specificLoc;
            generalFileLocation = generalLoc;
            UpdateDictionaries();
        }

        public string langFileLocation = "";
        public string generalFileLocation = "";
        /// <summary>
        /// Should this translator instance use the cache based translation algorithm or go through translation every time?
        /// </summary>
        public bool UseCache = true;

        /// <summary>
        /// Should this translator instance load the general translations file? 
        /// For example, Shyriiwook would be implemented with this set to false, as the wookiee would have their own words for everything in that file, being physically unable to pronounce them as the general universe.
        /// </summary>
        public bool UseGeneralTranslations=true;

        /// <summary>
        /// Clear the cache, which in its current iteration is merely the last translation. Mainly for debugging and benchmarking purposes now.
        /// </summary>
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

        /// <summary>
        /// Array of dictionaries, where the index is the length of the words - 1. 
        /// <example>_translationsArrayedByLengthAccessor[0] will be a dictionary of all keys with length 1, ..., _translationsArrayedByLengthAccessor[n] will be a dicitionary with all keys length n-1</example>
        /// </summary>
        private Dictionary<string, string>[] _translationsArrayedByLengthAccessor
        {
            get
            {
                if (_translationsArrayedByLength != null)
                    return _translationsArrayedByLength;
                else
                {
                    UpdateDictionaries();
                    return _translationsArrayedByLength;
                }
            }
        }

        /// <summary>
        /// Loads the dictionaries from the embedded ressource CSVs, the general file being in the LanguageService namespace and the language specific ones being in the .{language} namespaces.
        /// Notably, there is a different dictionary for every word length in the defining files, to enhance performance. 
        /// The algorithm will check the dictionary for the longest segments first and once it didnt find any returns for it, it will never check this dictionary again. 
        /// </summary>
        private void updateDictionaries(Stream generalDefinitions, Stream LanguageDefinition)
        {
            Dictionary<string, string>[] dictionariesBylengthArrayed = null;
            Dictionary<int, Dictionary<string, string>> dictionariesBylength = new Dictionary<int, Dictionary<string, string>>();
            using (var reader = new StreamReader(LanguageDefinition))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    var res = line.Split(';');

                    //skipping the header.
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

                    //Add the self translating definitions
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
                using (var reader = new StreamReader(generalDefinitions))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {

                        var res = line.Split(';');

                        //general translations i.e. monikers are added for words which dont have their specific translation in-language (like viscara -> viscara)
                        if (!dictionariesBylength.ContainsKey(prepareStringForTranslation(res[0]).Length))
                            dictionariesBylength.Add(prepareStringForTranslation(res[0]).Length, new Dictionary<string, string>() { [prepareStringForTranslation(res[0].ToLower())] = prepareStringForTranslation(res[0].ToLower()) });
                        else
                        {
                            Dictionary<string, string> dictatlength = dictionariesBylength[prepareStringForTranslation(res[0]).Length];
                            if (!dictatlength.ContainsKey(prepareStringForTranslation(res[0].ToLower())))
                                dictatlength.Add(prepareStringForTranslation(res[0].ToLower()), prepareStringForTranslation(res[0].ToLower()));
                        }
                    }
                }
            }
            dictionariesBylengthArrayed = new Dictionary<string, string>[dictionariesBylength.Keys.Max()];
            foreach (var item in dictionariesBylength)
            {
                dictionariesBylengthArrayed[item.Key - 1] = item.Value;
            }
            _translationsArrayedByLength = dictionariesBylengthArrayed;
        }
        //load the dicitonaries from the embedded csv which are compiled
        private void updateDictionariesFromEmbedded()
        {
            using (var langStream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(string.Format("{0}.LanguageSpecific.csv", this.GetType().Namespace)))
            {
                using (var generalStream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("SWLOR.Game.Server.Service.LanguageService.GeneralTranslations.csv"))
                {
                    updateDictionaries(generalStream, langStream);
                }
            }
        }
        /// <summary>
        /// Update the dictionaries from any supplied csv file for this translator instance. For external use. Debugging and design purposes.
        /// </summary>
        private void updateDictionariesFromCustom()
        {
            using (var langStream = File.OpenRead(langFileLocation))
            {
                using (var generalStream = File.OpenRead(generalFileLocation))
                {
                    updateDictionaries(generalStream, langStream);
                }
            }
        }
        //Decides on Location from where to load the language definitions, for external usage of the translator for ease of language creation and debugging
        public void UpdateDictionaries()
        {
            if (string.IsNullOrEmpty(langFileLocation) || string.IsNullOrEmpty(generalFileLocation))
                updateDictionariesFromEmbedded();
            else
                updateDictionariesFromCustom();
        }

        ///<param name = "message" > The message to be translated. </ param >
        ///<param name = "englishChance" > The chance to receive english text instead of the alien language for the comprehension part. </ param >
        ///<param name = "partiallyScrambled" > The partially comprehended output, partially based on the english chance </ param >
        ///<returns> The output as complete alien language </returns >
        /// <summary>
        /// General translate call that decides between using the cache or not and returns the output to any of its users.
        /// </summary>
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            if (UseCache)
                return translateFromCache(message, englishChance, out partiallyScrambled);

            else
            {
                partiallyScrambled = "";
                var sb = new StringBuilder();

                //Fake the end of the former sentence, to better recognize the start of the new sentence
                sb.Append(". ");
                sb.Append(message);
                //Add " at the end to account for missing punctuation, i.e. faking proper sentence end with the ending of the input
                sb.Append("\"");

                string result = sanitizeStringAfterTranslation(translateRecursive(prepareStringForTranslation(sb.ToString()), englishChance, out partiallyScrambled));
                partiallyScrambled = sanitizeStringAfterTranslation(partiallyScrambled);
                partiallyScrambled = partiallyScrambled.Substring(2, partiallyScrambled.Length - 3);
                return result.Substring(2, result.Length - 3);
            }
        }

        ///<param name = "message" > The message to be translated. </ param >
        ///<param name = "englishChance" > The chance to receive english text instead of the alien language for the comprehension part. </ param >
        ///<param name = "partiallyScrambled" > The partially comprehended output, partially based on the english chance </ param >
        ///<returns> The output as complete alien language </returns >
        /// <summary>
        /// Translation call that will refer to the cache first and use the already stored array there for translations before going through the algorithm. 
        /// When it goes through the algorithm, it will save the result into the cache for successive use.
        /// </summary>
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
                //Fake the end of the former sentence, to better recognize the start of the new sentence
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
        ///<remarks>
        ///Does not prevent duplicate chars as defined in getArrayOfCharactersToPreventDoubleOutput().
        ///!Largely carryover for benchmarking and performance test. The cache method is preferred for implementation.
        ///</remarks>
        ///<param name = "message" > The message to be translated. </ param >
        ///<param name = "englishChance" > The chance to receive english text instead of the alien language for the comprehension part. </ param >
        ///<param name = "partiallyScrambled" > The partially comprehended output, partially based on the english chance </ param >
        ///<param name = "lastLength" > The current key length that the former method call was at when iterating the dictionaries, so that a dictionary that has no matching keys will only be searched once. </ param >
        ///<param name = "stateBeforeMatchAfterMatch" > Inform the recursive call whether its for the string infront or behind the former call. This will be used to decide on capitalization. </ param >
        ///<param name = "oldPiece" > Inform the recursive call which string segment was translated before it. This will be used to decide on capitalization. </ param >
        ///<returns> The output as complete alien language </returns >
        /// <summary>
        /// Recursive translation, that will search the message for any matching segment in its defining dictionary array. 
        /// Starting with the dictionary with the longest keys, it will iterate through all dicitonaries, until a match is found.
        /// The stringparts before and after the match will be handed to their own recursive calls of this method. 
        /// The recursion ends when a call returns itself when the message is null or empty, all whitespaces pr has not found a single match.
        /// </summary>
        private string translateRecursive(string message, int englishChance, out string partiallyScrambled, int lastLength = -1, int stateBeforeMatchAfterMatch = 0, string oldPiece ="")
        {
            partiallyScrambled = "";
            if (_translationsArrayedByLengthAccessor == null)
                return message;
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
                    int whereFound;
                    KeyValuePair<string, string> entry;
                    if (stringContainsDictionaryKey(snippetToLower, _translationsArrayedByLengthAccessor[i], i + 1, out entry, out whereFound))
                    {
                        bool success = (Random.Next(100) <= englishChance && englishChance != 0);
                        //int whereFound = snippetToLower.IndexOf(entry.Key);
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

        ///<param name = "message" > The message to be translated. </ param >
        ///<param name = "lastLength" > The current key length that the former method call was at when iterating the dictionaries, so that a dictionary that has no matching keys will only be searched once. </ param >
        ///<param name = "stateBeforeMatchAfterMatch" > Inform the recursive call whether its for the string infront or behind the former call. This will be used to decide on capitalization. </ param >
        ///<param name = "oldPiece" > Inform the recursive call which string segment was translated before it. This will be used to decide on capitalization. </ param >
        ///<returns> A 2-dimensional array of all the translated segments where [n,0] is the nth english segment and [n,1] is the in-universe language segment </returns >
        /// <summary>
        /// Recursive translation, that will search the message for any matching segment in its defining dictionary array. 
        /// Starting with the dictionary with the longest keys, it will iterate through all dicitonaries, until a match is found.
        /// The stringparts before and after the match will be handed to their own recursive calls of this method. 
        /// The recursion ends when a call returns itself when the message is null or empty, all whitespaces pr has not found a single match.
        /// </summary>
        private string[,] translateRecursiveToCache(string message, int lastLength = -1, int stateBeforeMatchAfterMatch = 0, string oldPiece = "")
        {
            if (_translationsArrayedByLengthAccessor == null)
                return new string[,] { { message, message } };

            if(string.IsNullOrEmpty(message))
            {
                return new string[0, 2];
            }
            if (message.All(x => x == ' '))
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
                int whereFound;
                if (_translationsArrayedByLengthAccessor[i] != null && snippetToLower.Length >= i + 1)
                {
                    KeyValuePair<string, string> entry;
                    if (stringContainsDictionaryKey(snippetToLower, _translationsArrayedByLengthAccessor[i], i + 1, out entry, out whereFound))
                    {
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
                        int indexTranslated = 0;
                        int lengthTranslated = translated.Length;

                        //When the cipher definition results in double special characters like '', like definition for z being ' and ied being 'a, zied would turn into ''a. With this, double '' will be prevented
                        if (beforeArray.Length > 0 && snippetsJoinAtSameCharacter(beforeArray[getIndexOfLastEntryNotEmpty(beforeArray) , 1], translated, getArrayOfCharactersToPreventDoubleOutput()))
                        {
                            indexTranslated = 1;
                            lengthTranslated -= 1;
                        }
                        if (afterArray.Length > 0 && lengthTranslated > 0 && snippetsJoinAtSameCharacter(translated, afterArray[getIndexOfFirstEntryNotEmpty(afterArray),1], getArrayOfCharactersToPreventDoubleOutput()))
                        {
                            lengthTranslated -= 1;
                        }

                        bool translationOmitted = lengthTranslated == 0 || indexTranslated + lengthTranslated > translated.Length; 
                        Array.Copy(beforeArray, 0, resultArray, 0, beforeArray.Length);
                        Array.Copy(new string[,] { { originalSnippet.Substring(whereFound, match.Length), !translationOmitted ? translated.Substring(indexTranslated, lengthTranslated) : string.Empty } }, 0, resultArray, beforeArray.Length, 2);
                        Array.Copy(afterArray, 0, resultArray, beforeArray.Length + 2, afterArray.Length);
                        return resultArray;
                    }
                }
            }
            return new string[,] { { message, message } };
        }

        ///<param name = "snippet" > The snippet looking for a fitting key. </ param >
        ///<param name = "dict" > The dicitonary to be searched. </ param >
        ///<param name = "length" > The length of the keys in the dictionary provided. </ param >
        ///<param name = "result" > The matching KeyValuePair, if one is found. </ param >
        ///<param name = "whereFound" > Starting int where the key was found </ param >
        ///<returns> Bool state whether or not there was a match found </returns >
        /// <summary>
        /// Checks a provided string against dictionary keys and returns the match state and the matching key, if there is one.
        /// </summary>
        private bool stringContainsDictionaryKey(string snippet, Dictionary<string, string> dict, int length, out KeyValuePair<string, string> result, out int whereFound)
        {
            whereFound = 0;
            result = default(KeyValuePair<string, string>);
            for (int i = 0; i <= snippet.Length-length; i++)
            {
                string key = snippet.Substring(i, length);
                string value = "";
                bool success = dict.TryGetValue(key, out value);
                if (success)
                {
                    result = new KeyValuePair<string, string>(key, value);
                    whereFound = i;
                    return success;
                }
            }
            return false;
        }

        ///<returns> The input string capitalized at index. </returns >
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

        ///<returns> The index of the first letter in the input string. </returns >
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

        ///<param name = "original" > The english segment to be added as a key </ param >
        ///<param name = "translation" >The in universe language equivalent </ param >
        ///<param name = "prefixCode" > The prefix code defined in the csv-file: w, seq, sen, ss, ap, {empty} </ param >
        ///<param name = "suffixCode" > The suffixCode code defined in the csv-file: ow, w, seq, sen, ss, ap, {empty} </ param >
        ///<returns> An array of key value pairs to be added to a translation dicitonary </returns >
        /// <summary>
        /// This will return an array of all the permutations between the segment definitions and the prefix codes for ease of defining a letter segment that will only be translated at a word ending.
        /// </summary>
        /// <example>
        /// The definition "nar;do;w;w;;;" in the csv-file will result in the permutations ' do ', '-do ', '-do-', ' do-', ' do.', ' do!',.... and so forth, so only a standalone 'do' will be translated as 'nar'. 
        /// The result will have the same permutations, so ' do!' will turn into ' nar!'. The exception to this is the code 'ap', which will omit the characters in the result and APpend the translation to any preceeding or following words.
        /// </example>
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
        //As above, only for self-translations.
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

        ///<param name = "message" > The english message to be prepared for translation.</ param >
        ///<returns> The english string prepared for translation </returns >
        /// <example>
        /// To support the defining codes, all the spaces within those premutations will need to be doubled. For example, if ';ch;;w;' and ';a;w;w;' are definitions, an ending ch and a single a have their own specific definitons.
        /// If the signs weren't doubled, the text "such a" would run into a conflict. Either 'ch ' or ' a ' would be translated according to their special definitions, but not both. By doubling the characters from the prefix
        /// codes, "such a" turns into "such  a", so that both 'ch ' and ' a ' will be translated as defined. After this translation process, it is only important that sanitizeStringAfterTranslation is called, which:
        /// 1 - removes all the single occurences of ' ' and '-' resulting from the append code definition.
        /// 2 - turns all the double characters into single characters again.
        /// </example>
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

        ///<param name = "message" > The message to be sanitized after translation.</ param >
        ///<returns> The sanitized string</returns >
        /// <summary>
        /// See prepareStringForTranslation() for the process and reasoning.
        /// </summary>
        public virtual string sanitizeStringAfterTranslation(string message)
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

        ///<param name = "message" > The message to be sanitized after translation.</ param >
        ///<param name = "chars" > The chars to be removed. </ param >
        ///<returns> The sanitized string</returns >
        /// <summary>
        /// Removes all single, consecutive occurences of the defined chars. See prepareStringForTranslation() for the process and reasoning.
        /// </summary>
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
        private int getIndexOfLastEntryNotEmpty(string[,] array)
        {
            for (int i = array.GetLength(0)-1; i >= 0 ; i--)
            {
                if (!string.IsNullOrEmpty(array[i, 1]))
                    return i;
            }
            return 0;
        }
        private int getIndexOfFirstEntryNotEmpty(string[,] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                if (!string.IsNullOrEmpty(array[i, 1]))
                    return i;
            }
            return 0;
        }

        //Find if 2 strings would join at the same character, but only if the character is in the defined array
        private bool snippetsJoinAtSameCharacter(string firstString, string secondString, char[] chars)
        {
            if (string.IsNullOrEmpty(firstString) || string.IsNullOrEmpty(secondString))
                return false;

            foreach (var item in chars)
            {
                if (firstString.Last() == item && secondString.First() == item)
                    return true;
            }
            return false;
        }

        //The standard array of characters to be prevented in the ciphered output. This will prevent any unintended double 's, resulting from the fallback cipher.
        private char[] getArrayOfCharactersToPreventDoubleOutput()
        {
            return new char[]{ '\''};
        }

        ///<param name = "code" > The code defined in the csv-file. </ param >
        ///<returns> An array of char segments to be added to segment definitions to satisfy the desired word-starting, etc definitions. </returns >
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

        ///<param name = "code" > The code defined in the csv-file. </ param >
        ///<returns> An array of char segments to be added to segment definitions to satisfy the desired word-ending, etc definitions. </returns >
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

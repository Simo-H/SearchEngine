using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Wnlib;
using WnLexicon;
using NHunspell;
using WordNetClasses;

namespace SearchEngine.PostQuery
{
    /// <summary>
    /// This class returns all the documents containing the words of the query. The class methods also enhancing the query by adding semantic 
    /// terms which are synonyms of the term in the query, thus maybe finding more relevant documents. 
    /// </summary>
    class Searcher
    {
        /// <summary>
        /// A stemmer for stemming the query.
        /// </summary>
        Stemmer stemmer;
        /// <summary>
        /// A parser, used to parse the qurey in the same method used in the pre query process.
        /// </summary>
        Parse parser;
        /// <summary>
        /// A indexer that was built by the pre query engine, the indexer hold all the needed information to return queries results.
        /// </summary>
        public Indexer index;
        /// <summary>
        /// This int represent the number of synonyms term taken in account enhancing the query.
        /// </summary>
        public int numberOfsynonyms;
        /// <summary>
        /// The ctor of the searcher
        /// </summary>
        /// <param name="index">referncing an index</param>
        /// <param name="numberOfsynonyms">the number of synonyms the searcher will add to the query</param>
        public Searcher(ref Indexer index, int numberOfsynonyms)
        {
            stemmer = new Stemmer();
            parser = new Parse();
            this.index = index;
            this.numberOfsynonyms = numberOfsynonyms;
        }
        /// <summary>
        /// This method returned a parsed query strings.
        /// </summary>
        /// <param name="query">the string of the query entered by the user</param>
        /// <returns></returns>
        public string[] ParseQuery(string query)
        {

            List<string> parseQuery = new List<string>();
            string[] stringSeparators = new string[] { " ", "\n", "...", "--", "?", ")", "(", "[", "]", "\"", "&", "_", ";", "~", "|" };
            string[] textArray = query.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            List<string> textArrayList = new List<string>();
            for (int i = 0; i < textArray.Length; i++)
            {
                string checkWord = parser.cutAllsigns(textArray[i]);
                if (checkWord != null | textArray.Length == 0)
                {
                    textArrayList.Add(checkWord);
                }
            }
            textArrayList.Add("");
            textArrayList.Add("");
            textArrayList.Add("");
            textArrayList.Add("");
            for (int i = 0; i < textArrayList.Count - 4; i++)
            {
                string parsedTerm1;
                string parsedTerm2;
                if (parser.checkForStopWord(textArrayList[i]) == 1 && !textArrayList[i].Equals("between"))
                {
                    continue;
                }
                else
                {
                    int jump = parser.parseTerm(ref textArrayList, i, out parsedTerm1, out parsedTerm2);
                    if (jump >= 0)
                    {
                        i += jump;
                        //stemmer
                        if (Properties.Settings.Default.stemmer)
                        {
                            parsedTerm1 = stemmer.stemTerm(parsedTerm1);
                        }
                        parseQuery.Add(parsedTerm1);
                        // AddTermUniqe(parsedTerm1, uniqeTermsAtDoc);
                        if (parsedTerm2 != null)
                        {
                            if (Properties.Settings.Default.stemmer)
                            {
                                parsedTerm2 = stemmer.stemTerm(parsedTerm2);
                            }
                            parseQuery.Add(parsedTerm2);

                            //AddTermUniqe(parsedTerm2, uniqeTermsAtDoc);
                        }
                    }
                    else
                    {
                        if (parsedTerm1 != null && !textArrayList[i].Equals("between") && !parsedTerm1.Equals(""))
                        {
                            if (Properties.Settings.Default.stemmer)
                            {
                                parsedTerm1 = stemmer.stemTerm(parsedTerm1);
                            }
                            parseQuery.Add(parsedTerm1);
                        }
                    }

                }
            }
            string[] q = parseQuery.ToArray();
            return q;
        }
        /// <summary>
        /// This method finds all the documents which hold the terms in the query, filtered by the language specified by the user
        /// </summary>
        /// <param name="parseQuery">the term in the query</param>
        /// <param name="language">the original language which the document were writen</param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, int>> AllQueryOccurrences(string[] parseQuery, string language)
        {
            Dictionary<string, Dictionary<string, int>> QueryPerformances = new Dictionary<string, Dictionary<string, int>>();
            foreach (string term in parseQuery)
            {
                if (!index.mainTermDictionary.ContainsKey(term) || QueryPerformances.Keys.Contains(term))
                {
                    continue;
                }

                Dictionary<string, int> DocAndTf = new Dictionary<string, int>();
                DocAndTf = termDocsAndTf(term, language);
                QueryPerformances[term] = DocAndTf;
            }
            return QueryPerformances;
        }
        /// <summary>
        /// This method use the posting file to return all the documents holding a specific term. the method first get the correct pointer 
        /// of the term in the posting file from the term dictionary.
        /// </summary>
        /// <param name="term">the term which the method will search for</param>
        /// <param name="language">the language which the documents will be returned in</param>
        /// <returns></returns>
        public Dictionary<string, int> termDocsAndTf(string term, string language)
        {

            Dictionary<string, int> termDocsAndTf = new Dictionary<string, int>();
            string postingFilePath = Properties.Settings.Default.postingFiles;
            if (Properties.Settings.Default.stemmer)
            {
                postingFilePath = postingFilePath + "\\PostingS.bin";
            }
            else
            {
                postingFilePath = postingFilePath + "\\Posting.bin";
            }
            using (FileStream fileStream = new FileStream(postingFilePath, FileMode.Open))
            {
                BinaryReader br = new BinaryReader(fileStream);
                long position = index.mainTermDictionary[term].postingfilepointer;
                br.BaseStream.Position = position;
                string term1 = br.ReadString();
                string AllPerformances = br.ReadString();
                string[] stringSeparators = new string[] { " ", ">", "<", ",", "#" };
                string[] DocumentAndShowsArray = AllPerformances.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < DocumentAndShowsArray.Count(); i++)
                {
                    if (language.Equals("All languages"))
                    {
                        termDocsAndTf[DocumentAndShowsArray[i]] = Int32.Parse(DocumentAndShowsArray[i + 1]);
                    }
                    else
                    {
                        if (language.Equals(index.documentDictionary[DocumentAndShowsArray[i]].originalLanguage))
                        {
                            termDocsAndTf[DocumentAndShowsArray[i]] = Int32.Parse(DocumentAndShowsArray[i + 1]);
                        }
                    }
                    i++;
                }
                fileStream.Flush();
                fileStream.Close();
                br.Close();
            }
            return termDocsAndTf;
        }
        /// <summary>
        /// This method add synonyms word to the term in the query using to kinds of external tool. first is the word net project, second
        /// is the hunspell thesaures dictionary
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<string> AddSemantic(List<string> query)
        {
            List<string> Synonyms = new List<string>();
            query = query.Distinct().ToList();
            Synonyms.AddRange(HunspellSynonymsList(query));
            Synonyms.AddRange(WordNetSynonymsList(query));
            if (!Properties.Settings.Default.stemmer)
            {
                int i = 0;
                int count = query.Count;
                while (i< count)
                {
                    Synonyms.Add(stemmer.stemTerm(query[i]));
                    i++;
                }
            }
            Synonyms.AddRange(WordNetExtraTerms(query));
            Synonyms = Synonyms.Distinct().ToList();
            Synonyms = Synonyms.Except(query).ToList();
            //Synonyms = Synonyms.Take(query.Count*2).ToList();
            if (Properties.Settings.Default.stemmer)
            {
                for (int i = 0; i < Synonyms.Count; i++)
                {
                    Synonyms[i] = stemmer.stemTerm(Synonyms[i]);
                }
            }
            List<string> synonymsAfterSplit = new List<string>();
            foreach (var VARIABLE in Synonyms)
            {
                synonymsAfterSplit.AddRange(VARIABLE.Split(new char[] {' ','-'},StringSplitOptions.RemoveEmptyEntries));
            }
            Synonyms = synonymsAfterSplit.Distinct().ToList();
            //Synonyms = Synonyms.Except(query).ToList();
            Synonyms = RemoveDuplicatesAndQueryTerms(Synonyms,query);
            return Synonyms;
        }
        /// <summary>
        /// this method return a list of synonyms from the hunspell thesaraus dictionary.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<string> HunspellSynonymsList(List<string> query)
        {
            List<string> listOfSynonyms = new List<string>();
            MyThes thes = new MyThes(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9)+"th_en_US_new.dat");
            using (Hunspell hunspell = new Hunspell(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9)+"en_us.aff", Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9)+"en_US.dic"))
            {
                for (int i = 0; i < query.Count-1;  i++)
                {
                    string queryTerm = query[i]+" "+query[i+1];
                    ThesResult tr = thes.Lookup(queryTerm, hunspell);
                    if (tr == null)
                    {
                        continue;
                    }
                    foreach (ThesMeaning meaning in tr.Meanings)
                    {
                        listOfSynonyms.AddRange(meaning.Synonyms);
                    }
                }
                if (listOfSynonyms.Count == 0)
                {
                    foreach (string queryTerm in query)
                    {
                        ThesResult tr = thes.Lookup(queryTerm, hunspell);
                        if (tr == null)
                        {
                            continue;
                        }
                        foreach (ThesMeaning meaning in tr.Meanings)
                        {
                            listOfSynonyms.AddRange(meaning.Synonyms);
                        }
                        listOfSynonyms = listOfSynonyms.Take(2).ToList();
                    }
                }
                    foreach (var VARIABLE in listOfSynonyms)
                    {
                        VARIABLE.Trim(' ');
                    }
                
                return listOfSynonyms;
            }
        }

        public List<string> WordNetSynonymsList(List<string> query)
        {
            WordNetClasses.WN wnc = new WordNetClasses.WN(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9) + "PostQuery\\WordNet\\dict\\");
            List<string> WordNetSynonymsList = new List<string>();
            foreach (string term in query)
            {
                string[] a = Lexicon.FindSynonyms(term, PartsOfSpeech.Noun, true);
            if (a != null)
            {
                WordNetSynonymsList.AddRange(a);

            }
            string[] b = Lexicon.FindSynonyms(term, PartsOfSpeech.Verb, true);
            if (b != null)
            {
                WordNetSynonymsList.AddRange(b);

            }
            string[] c = Lexicon.FindSynonyms(term, PartsOfSpeech.Adj, true);
            if (c != null)
            {
                WordNetSynonymsList.AddRange(c);

            }
                WordNetSynonymsList = WordNetSynonymsList.Take(2).ToList();
                foreach (var VARIABLE in WordNetSynonymsList)
                {
                    VARIABLE.Trim(' ');
                }
            }
            return WordNetSynonymsList.Distinct().ToList();
            
        }

        public List<string> RemoveDuplicatesAndQueryTerms(List<string> list,List<string> query)
        {
            List<string> a = new List<string>();
            foreach (string var in list)
            {
                if (!a.Contains(var) && !query.Contains(var))
                {
                    a.Add(var);
                }
            }
            return a;
        }

        public List<string> WordNetExtraTerms(List<string> query)
        {
            List<string> returnedList = new List<string>();
            foreach (string term in query)
            {
            Wnlib.Search se = new Wnlib.Search(term, true, Wnlib.PartOfSpeech.of("noun"), new SearchType("DERIVATION"), 0);
            string[] s = Strings.Split(se.buf, Constants.vbLf);
            if (s.Length > 6)
            {
                string at5 = s[5];
                string at6 = s[6];
                at5 = at5.Split(new string[] {"--"}, StringSplitOptions.RemoveEmptyEntries)[0];
                at6 = at6.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries)[0];
                string[] arrayAt5= at5.Split(new string[] {" ", "\t",","  },StringSplitOptions.RemoveEmptyEntries );
                string[] arrayAt6 = at6.Split(new string[] { " ", "=>", "\t","," }, StringSplitOptions.RemoveEmptyEntries);
                returnedList.AddRange(arrayAt5);
                returnedList.AddRange(arrayAt6);
                for (int i = 0; i < returnedList.Count; i++)
                {
                    returnedList[i] = returnedList[i].Trim(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' });
                }
            }
            }
            return returnedList;
        }
    }
}


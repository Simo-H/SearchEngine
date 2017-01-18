using SearchEngine.PreQuery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wnlib;
using WnLexicon;
using NHunspell;

namespace SearchEngine.PostQuery
{
    class Searcher
    {
        Stemmer stemmer;
        Parse parser;
        Indexer index;
        public int numberOfsynonyms;

        public Searcher(ref Indexer index, int numberOfsynonyms)
        {
            stemmer = new Stemmer();
            parser = new Parse();
            this.index = index;
            this.numberOfsynonyms = numberOfsynonyms;
        }

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

        public Dictionary<string, Dictionary<string, int>> AllQueryPerformances(string[] parseQuery, string language)
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

        public List<string> AddSemantic(List<string> query)
        {
            WordNetClasses.WN wnc = new WordNetClasses.WN(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9) + "PostQuery\\WordNet\\dict\\");
            List<string> Synonyms = new List<string>();
            if (Properties.Settings.Default.stemmer)
            {
                List<string> stemmedQuery = new List<string>();
                foreach (string term in query)
                {
                    stemmedQuery.Add(stemmer.stemTerm(term));
                }
                query = stemmedQuery;
            }
            else
            {
                int i = 0;
                int count = query.Count;
                while (i< count)
                {
                    query.Add(stemmer.stemTerm(query[i]));
                    i++;
                }
            }
            //foreach (string queryTerm in query)
            //{
                //string[] a = Lexicon.FindSynonyms(queryTerm, PartsOfSpeech.Noun, true);
                //if (a != null)
                //{
                //Synonyms.AddRange(a);
                    
                //}
                //string[] b = Lexicon.FindSynonyms(queryTerm, PartsOfSpeech.Verb, true);
                //if (b != null)
                //{
                //    Synonyms.AddRange(b);

                //}
                //string[] c = Lexicon.FindSynonyms(queryTerm, PartsOfSpeech.Adj, true);
                //if (c != null)
                //{
                //    Synonyms.AddRange(c);

                //}
                
                //new semnatic function also add synonyms
            //}
            Synonyms.AddRange(HunspellSynonymsList(query));
            if (Properties.Settings.Default.stemmer)
            {
                
                for (int i = 0; i < Synonyms.Count; i++)
                {
                    Synonyms[i] = stemmer.stemTerm(Synonyms[i]);
                }
            }
            query.AddRange(Synonyms.Distinct<string>());
            return query;
        }

        public List<string> HunspellSynonymsList(List<string> query)
        {
            List<string> listOfSynonyms = new List<string>();
            MyThes thes = new MyThes(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9)+"th_en_US_new.dat");
            using (Hunspell hunspell = new Hunspell(Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9)+"en_us.aff", Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 9)+"en_US.dic"))
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

                }
                return listOfSynonyms;
            }
        }
    }
}


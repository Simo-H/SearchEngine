using SearchEngine.PreQuery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class Searcher
    {
        Stemmer stemmer;
        Parse parser;
        public Dictionary<string, Dictionary<string, int>> QueryPerformances;
        Indexer index;

        public Searcher(ref Indexer index)
        {
            stemmer = new Stemmer();
            parser = new Parse();
            QueryPerformances= new Dictionary<string, Dictionary<string, int>>();
            this.index = index;
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
        public void AllQueryPerformances(string[] parseQuery, string language)
        {
            foreach (string term in parseQuery)
            {
                if (QueryPerformances.Keys.Contains(term))
                {
                    continue;
                }
                Dictionary<string, int> DocAndTf = new Dictionary<string, int>();
                DocAndTf = DicOfDocAndTf(term,language);
                QueryPerformances[term] = DocAndTf;
            }
        }
        public Dictionary<string,int> DicOfDocAndTf(string term, string language)
        {

            Dictionary<string, int> DocumentAndShows = new Dictionary<string, int>();
             string postingFilePath= Properties.Settings.Default.postingFiles;
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
                string AllPerformances= br.ReadString();
                string[] stringSeparators = new string[] { " ", ">","<", ",", "#" };
                string[] DocumentAndShowsArray = AllPerformances.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < DocumentAndShowsArray.Count(); i++)
                {
                   // if (language.Equals("All languages"))
                  //  {
                   //     DocumentAndShows[DocumentAndShowsArray[i]] = Int32.Parse(DocumentAndShowsArray[i + 1]);
                  //  }
                  //  else
                  //  {
                   //     if (language.Equals(index.documentDictionary[DocumentAndShowsArray[i]].originalLanguage))
                   //     {
                            DocumentAndShows[DocumentAndShowsArray[i]] = Int32.Parse(DocumentAndShowsArray[i + 1]);
                   //     }
                 //   }
                    i++;
                }
                fileStream.Flush();
                fileStream.Close();
                br.Close();
            }
            return DocumentAndShows;
        }


    }
}


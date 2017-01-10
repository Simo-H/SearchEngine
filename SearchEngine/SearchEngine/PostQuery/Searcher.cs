using SearchEngine.PreQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class Searcher
    {
        Stemmer stemmer;
        Parse parser;
        public Searcher()
        {
            stemmer = new Stemmer();
            parser = new Parse();
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

    }
}


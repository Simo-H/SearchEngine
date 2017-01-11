using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class PostQueryEngine
    {
        Ranker ranker;
        Searcher searcher;


        public PostQueryEngine(ref Indexer indexer)
        {
            searcher= new Searcher(ref indexer);
            ranker= new Ranker(ref indexer,  ref searcher);
        }

        public  void retrive( string query, string language)
        {
            string[] parseQuery = searcher.ParseQuery(query);
            Dictionary<string, Dictionary<string, int>> QueryPerformances =new Dictionary<string, Dictionary<string, int>>();
            QueryPerformances= searcher.AllQueryPerformances(parseQuery, language);
            ConcurrentDictionary<string, double> ranking = ranker.Ranke(parseQuery, QueryPerformances);
            List<KeyValuePair<string, double>> l = new List<KeyValuePair<string, double>>();
            l= ranker.printRankToFile(ranking, "\\rank.txt");
        }

    }
}

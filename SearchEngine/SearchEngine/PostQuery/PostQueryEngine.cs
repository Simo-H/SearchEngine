using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class PostQueryEngine
    {
        Ranker ranker;
        Searcher searcher;
        public PostQueryEngine(ref Indexer indexer,string query ,string language)
        {
            searcher= new Searcher(ref indexer);
            ranker= new Ranker(ref indexer,  ref searcher);
            string[] parseQuery = searcher.ParseQuery(query);
            searcher.AllQueryPerformances(parseQuery,language);
            ConcurrentDictionary<string, double> ranking= ranker.BM25(parseQuery);
            var myList = ranking.ToList();
            myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            List<string> result =new List<string>();

        }

        //public SortedDictionary<double,string> sortRank(ConcurrentDictionary<string, double> ranking)
        //{
        //    SortedDictionary<double, string> sort = new SortedDictionary<double, string>();
        //    foreach (string fileName in ranking)
        //    {

        //    }
        //}

    }
}

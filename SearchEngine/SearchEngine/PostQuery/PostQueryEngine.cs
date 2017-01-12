using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class PostQueryEngine:INotifyPropertyChanged
    {
        public Ranker ranker;
        public Searcher searcher;
        private Optimizer opt;
        public static int queryid=1;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        private Dictionary<int,List<string>> queriesResults;
        public Dictionary<int,List<string>> QueriesResults
        {
            get { return queriesResults; }
            set
            {
                queriesResults = value;
                NotifyPropertyChanged("QueriesResults");
            }
        }


        public PostQueryEngine(ref Indexer indexer)
        {
            searcher= new Searcher(ref indexer,3);
            //ranker= new Ranker(ref indexer,  ref searcher);
            //opt = new Optimizer(ref indexer);
            //opt.Optimize(Properties.Settings.Default.postingFiles+"\\qrels.txt");
        }

        public  List<KeyValuePair<int,string>> retriveSingleQuery(string query, string language,int queryId)
        {
            string[] parseQuery = searcher.ParseQuery(query);
            List<string> queryList = searcher.AddSemantic(parseQuery.ToList());
            Dictionary<string, Dictionary<string, int>> QueryPerformances =new Dictionary<string, Dictionary<string, int>>();
            QueryPerformances= searcher.AllQueryPerformances(parseQuery, language);
            ConcurrentDictionary<string, double> ranking = ranker.Ranke(parseQuery, QueryPerformances);
            List<KeyValuePair<int, string>> l = new List<KeyValuePair<int, string>>();
            return ranker.sortRanking(ranking, queryId);
        }

        public void userManualSingleQuery(string query, string language)
        {
            List<KeyValuePair<int,string>> results = retriveSingleQuery(query, language, queryid);
            QueriesResults[queryid] = ranker.ResultDocsList(results);
            ranker.writeSingleQueryToFile("Query "+queryid, results);
            queryid++;
        }


        public void queriesFile(string QueriesFilePath,string language)
        {
            using (FileStream queriesTextFileStream = new FileStream(QueriesFilePath, FileMode.Open))
            {
                StreamReader streamReader = new StreamReader(queriesTextFileStream);
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] queryLine = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<KeyValuePair<int, string>> results = retriveSingleQuery(queryLine[1], language, Int32.Parse(queryLine[0]));
                    QueriesResults[queryid] = ranker.ResultDocsList(results);
                    ranker.writeSingleQueryToFile("Query " + queryLine[0], results);
                }
            }
        }


    }
}

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
        public static int queryid=100;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        private ObservableDictionary<int,List<string>> queriesResults;
        public ObservableDictionary<int,List<string>> QueriesResults
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
            ranker= new Ranker(ref indexer,  ref searcher,1.2,100,0.75);
            //opt = new Optimizer(ref indexer);
            //opt.Optimize(Properties.Settings.Default.postingFiles+"\\qrels.txt");
        }

        public  void retriveSingleQuery(string query, string language,int queryId)
        {
            string[] parseQuery = searcher.ParseQuery(query);
            List<string> queryList = searcher.AddSemantic(parseQuery.ToList());
            //List<string> queryList = parseQuery.ToList();
            Dictionary<string, Dictionary<string, int>> QueryPerformances =new Dictionary<string, Dictionary<string, int>>();
            QueryPerformances= searcher.AllQueryPerformances(queryList.ToArray(), language);
            ConcurrentDictionary<string, double> ranking = ranker.Ranke(queryList.ToArray(), QueryPerformances);
            QueriesResults[queryId]= ranker.sortRanking(ranking);
           
        }
        //****************************************************************//
        public void userManualSingleQuery(string query, string language, string ResultsFilePath)
        {
            QueriesResults = new ObservableDictionary<int, List<string>>();            
            retriveSingleQuery(query,language,queryid);         
            queryid++;
            if (queryid>999)
            {
                queryid = 100;
            }
            ranker.writeSingleQueryToFile(ResultsFilePath, QueriesResults);
        }

        //****************************************************************//

        public void queriesFile(string QueriesFilePath,string language, string ResultsFilePath)
        {
            QueriesResults = new ObservableDictionary<int, List<string>>();
            using (FileStream queriesTextFileStream = new FileStream(QueriesFilePath, FileMode.Open))
            {
                StreamReader streamReader = new StreamReader(queriesTextFileStream);
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] queryLine = line.Split(new char[] { ' ','\t' }, StringSplitOptions.RemoveEmptyEntries);
                    string q="";
                    for (int i = 1; i < queryLine.Length; i++)
                    {
                        q += queryLine[i]+" ";
                    }
                    retriveSingleQuery(q, language, Int32.Parse(queryLine[0]));
                }
            }
            ranker.writeSingleQueryToFile(ResultsFilePath, QueriesResults);
        }


    }
}

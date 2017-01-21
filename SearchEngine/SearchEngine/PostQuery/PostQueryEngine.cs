using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class PostQueryEngine : INotifyPropertyChanged
    {
        /// <summary>
        /// The Post query engine, managing and encapsulating all the process of query answering.
        /// </summary>
        public Ranker ranker;
        public Searcher searcher;
        /// <summary>
        /// For inner analitic uses.
        /// </summary>
        private Optimizer opt;
        /// <summary>
        /// a counter which given name to a user query without previous query ID.
        /// </summary>
        public static int queryid = 100;

        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Part of the MVVM implemention.
        /// </summary>
        /// <param name="propName"></param>
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        /// <summary>
        /// Observable dictionary which hold the queries result. used by the view in the MVVM architacture
        /// </summary>
        private ObservableDictionary<int, List<string>> queriesResults;
        public ObservableDictionary<int, List<string>> QueriesResults
        {
            get { return queriesResults; }
            set
            {
                queriesResults = value;
                NotifyPropertyChanged("QueriesResults");
            }
        }

        /// <summary>
        /// post query engine ctor
        /// </summary>
        /// <param name="indexer"></param>
        public PostQueryEngine(ref Indexer indexer)
        {
            searcher = new Searcher(ref indexer, 3);
            ranker = new Ranker(ref indexer, ref searcher, 1.2, 100, 0.75);
            //opt = new Optimizer(ref indexer);
            //opt.Optimize(Properties.Settings.Default.postingFiles+"\\qrels.txt");
        }
        /// <summary>
        /// This method retrive a single query results
        /// </summary>
        /// <param name="query"></param>
        /// <param name="language"></param>
        /// <param name="queryId"></param>
        public void retriveSingleQuery(string query, string language, int queryId)
        {
            string[] parseQuery = searcher.ParseQuery(query);
            List<string> semanticQuery = searcher.AddSemantic(parseQuery.ToList());
            List<string> queryList = parseQuery.ToList();
            Dictionary<string, Dictionary<string, int>> QueryTermsOccurrences = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, int>> SemanticQuery = new Dictionary<string, Dictionary<string, int>>();
            QueryTermsOccurrences = searcher.AllQueryOccurrences(queryList.ToArray(), language);
            SemanticQuery = searcher.AllQueryOccurrences(semanticQuery.ToArray(), language);
            //List<string> cluster = searcher.index.buildCarrot2(parseQuery, QueryPerformances);
            ConcurrentDictionary<string, double> ranking = ranker.CalculateTotalRank(queryList.ToArray(), semanticQuery, QueryTermsOccurrences, SemanticQuery);

            QueriesResults[queryId] = ranker.sortRanking(ranking);

        }
        //****************************************************************//
        /// <summary>
        /// This method returns result to the query given by the user in the manual query text line. writes the query result to a file
        /// in a specified path
        /// </summary>
        /// <param name="query"></param>
        /// <param name="language"></param>
        /// <param name="ResultsFilePath"></param>
        public void userManualSingleQuery(string query, string language, string ResultsFilePath)
        {
            QueriesResults = new ObservableDictionary<int, List<string>>();
            retriveSingleQuery(query, language, queryid);
            queryid++;
            if (queryid > 999)
            {
                queryid = 100;
            }
            if (ResultsFilePath != null && !ResultsFilePath.Equals(""))
            {
                ranker.writeSingleQueryToFile(ResultsFilePath, QueriesResults);
            }
        }

        //****************************************************************//
        /// <summary>
        /// This method returns result the queries file given by the user, writes the queries result to a file in a specified path
        /// </summary>
        /// <param name="QueriesFilePath"></param>
        /// <param name="language"></param>
        /// <param name="ResultsFilePath"></param>
        public void queriesFile(string QueriesFilePath, string language, string ResultsFilePath)
        {

            try
            {
                QueriesResults = new ObservableDictionary<int, List<string>>();

                using (FileStream queriesTextFileStream = new FileStream(QueriesFilePath, FileMode.Open))
                {
                    StreamReader streamReader = new StreamReader(queriesTextFileStream);
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string[] queryLine = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        string q = "";
                        for (int i = 1; i < queryLine.Length; i++)
                        {
                            q += queryLine[i] + " ";
                        }
                        retriveSingleQuery(q, language, Int32.Parse(queryLine[0]));
                    }
                }
                ranker.writeSingleQueryToFile(ResultsFilePath, QueriesResults);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Invalid format for queries file, please check if the correct file was loaded.");
            }

        }


    }
}

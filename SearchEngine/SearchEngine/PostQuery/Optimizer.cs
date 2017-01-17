using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchEngine.PreQuery;

namespace SearchEngine.PostQuery
{
    class Optimizer
    {
        private Dictionary<int, Dictionary<string, int>> qrelsDictionary;
        private Indexer indexer;
        private PostQueryEngine postQuery;
        public Optimizer(ref Indexer indexer)
        {
            this.indexer = indexer;
            postQuery = new PostQueryEngine(ref this.indexer);
            qrelsDictionary = new Dictionary<int, Dictionary<string, int>>();
            postQuery.searcher = new Searcher(ref indexer, 5);
        }
        public void ReadQrels(string qrelsFilePath)
        {
            using (FileStream qrelsTextFileStream = new FileStream(qrelsFilePath, FileMode.Open))
            {
                StreamReader streamReader = new StreamReader(qrelsTextFileStream);
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] qrel = line.Split(new char[] {' ',},StringSplitOptions.RemoveEmptyEntries);
                    if (!qrelsDictionary.ContainsKey(Int32.Parse(qrel[0])))
                    {
                        Dictionary<string, int> queryKeyValuePair = new Dictionary<string, int>();
                        qrelsDictionary[Int32.Parse(qrel[0])] = queryKeyValuePair;
                        queryKeyValuePair[qrel[2]] = Int32.Parse(qrel[3]);
                    }

                    qrelsDictionary[Int32.Parse(qrel[0])][qrel[2]] = Int32.Parse(qrel[3]);
                }
            }
        }

        public double Optimize(string qrelsFilePath,string path)
        {
            double optk1;
            double optk2;
            double optb;
            ReadQrels(qrelsFilePath);
            double bestScore = testRun(out optk1,out optk2,out optb,path);
            double totalrecall = optimalRecall();
            Debug.WriteLine("k1= "+optk1+" k2= "+" b= "+optb);
            Debug.WriteLine(bestScore / totalrecall);
            return bestScore / totalrecall;
        }

        public int compareResults(ObservableDictionary<int, List<string>> QueriesResults)
        {
            int recall = 0;
            foreach (int queryResult in QueriesResults.Keys)
            {
                //Ranker.top50Results(
                foreach (string docNo in QueriesResults[queryResult])
                {
                    if (qrelsDictionary[queryResult].ContainsKey(docNo) && qrelsDictionary[queryResult][docNo]== 1)
                    {
                        recall++;
                    }
                }
            }
            return recall;
        }

        public double testRun(out double optK1,out double optK2, out double optB,string path)
        {
            postQuery = new PostQueryEngine(ref indexer);
            int bestScore = 0;
            double bestk1 = 0;
            double bestk2 = 0;
            double bestb = 0;
            for (double k1 = 1.2; k1 <= 1.2; k1+=0.1)
            {
                for (double k2 = 100; k2 <= 100; k2+=5)
                {
                    for (double b = 0.5; b <= 0.5; b+=0.01)
                    {
                        postQuery.ranker = new Ranker(ref indexer,ref postQuery.searcher,k1,k2,b );
                        postQuery.queriesFile(Properties.Settings.Default.postingFiles + "\\queries.txt", "All languages", path);
                        int score = compareResults(postQuery.QueriesResults);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestk1 = k1;
                            bestk2 = k2;
                            bestb = b;
                        }
                    }
                }
            }
            optK1 = bestk1;
            optK2 = bestk2;
            optB = bestb;
            return bestScore;
        }

        public double optimalRecall()
        {
            int totalRecall = 0;
            foreach (var qrel in qrelsDictionary.Values)
            {
                foreach (var result in qrel.Values)
                {
                    if (result == 1)
                    {
                        totalRecall++;
                    }
                }
            }
            return totalRecall;
        }
        public void findOptimizedParameters(string path)
        {
            Optimize(Properties.Settings.Default.postingFiles + "\\qrels.txt",path);
        }
    }
}

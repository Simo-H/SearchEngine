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
    class Ranker
    {
        Indexer indexer;
        Searcher shearch;
        double k1;
        double k2;
        double K;
        double b;
        int N;
        int ri;
        int R;
        double avgDocLenght;
        int BonusAllQueryInDocument;
        int BonusTermInTitle;
        public List<double> maxmin;
        public Ranker(ref Indexer indexer, ref Searcher shearch, double k1, double k2, double b)
        {
            this.indexer = indexer;
            this.shearch = shearch;
            maxmin = new List<double>();
            BonusAllQueryInDocument = 10;
            BonusTermInTitle = 2;
            N = 0;
            ri = 0;
            R = 0;
            this.k1 = k1;
            this.k2 = k2;
            this.b = b;
            avgDocLenght = 0;

        }
        public ConcurrentDictionary<string, double>  BM25(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
        {
            if (N == 0)
            {
                N = indexer.documentDictionary.Count();
                foreach (string item in indexer.documentDictionary.Keys)
                {
                    avgDocLenght += indexer.documentDictionary[item].totalNumberInDoc;
                }
                avgDocLenght = avgDocLenght / N;

            }
            ConcurrentDictionary<string, double> docList = new ConcurrentDictionary<string, double>();
            foreach (Dictionary<string, int> term in QueryPerformances.Values)
            {
                foreach (string doc in term.Keys)
                {
                    docList[doc] = 0;
                }
            }
            foreach (string doc in docList.Keys)
            {
                //DocumentInfo f = indexer.documentDictionary[doc];
                int dl = indexer.documentDictionary[doc].totalNumberInDoc;
                double totalRankeForDoc=0;
                double rankeTermAtDoc = 0;
                int CounterTerminDoc = 0;
                foreach (string term in QueryPerformances.Keys)
                {
                  double qfi = System.Convert.ToDouble(countNumberOfoccurencesInQuery(q,term));
                    double df = indexer.mainTermDictionary[term].df;
                    if (QueryPerformances[term].ContainsKey(doc))
                    {
                        CounterTerminDoc++;
                        double tf =( System.Convert.ToDouble(QueryPerformances[term][doc]));
                        //double firstPart = Math.Log(((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N - ni - R + ri + 0.5)));
                        double firstPart = Math.Log((N - df + 0.5) / (df + 0.5));
                        K = k1 * ((1 - b) + b * (dl / avgDocLenght));
                        double secondPart = ((k1 + 1) * tf) / (K + tf);
                        double third = ((k2 + 1) * qfi) / (k2 + qfi);
                        rankeTermAtDoc = firstPart * secondPart * third;
                        maxmin.Add(rankeTermAtDoc);/////////////////////////////////////
                        totalRankeForDoc += rankeTermAtDoc;
                    }
                }
                docList[doc] = totalRankeForDoc;// +Math.Pow(3, CheckingTitle(doc, q))+ Math.Pow(2,CounterTerminDoc);
                
            }
            return docList;
        }


        public ConcurrentDictionary<string, double> Sim(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
        {

            ConcurrentDictionary<string, double> docList = new ConcurrentDictionary<string, double>();
            foreach (Dictionary<string, int> term in QueryPerformances.Values)
            {
                foreach (string doc in term.Keys)
                {
                    docList[doc] = 0;
                }
            }


            double rankeTermAtDoc = 0;
            foreach (string doc in docList.Keys)
            {

                double maxfi = indexer.documentDictionary[doc].maxTF;
                foreach (string term in QueryPerformances.Keys)
                {
                    double qfi =1;
                    if (QueryPerformances[term].ContainsKey(doc))
                    {
                        double fi = (System.Convert.ToDouble(QueryPerformances[term][doc]));
                        double tfi = fi / maxfi;
                        double idf = Math.Log(N/indexer.mainTermDictionary[term].df);

                        rankeTermAtDoc += tfi*idf* qfi;
                    }
                }
                docList[doc]= rankeTermAtDoc;
                
            }
            return docList;
        }
        public double CheckingTitle(string docName, string[] q)
        {
            int count = 0;
            for (int i = 0; i < q.Length; i++)
            {
              bool containing=indexer.documentDictionary[docName].title.Contains(q[i]);
                if (containing)
                {
                    count++;
                }
            }
            return count;
        }

        public List<string> sortRanking(ConcurrentDictionary<string, double> ranking)
        {
            List<KeyValuePair<string,double>> myList = ranking.ToList();
            myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            myList.Reverse(0, myList.Count);
            List<string> sortedRanking = new List<string>();
            foreach (var result in myList)
            {
                sortedRanking.Add(result.Key);
            }
            //sortedRanking = top50Results(sortedRanking);
            return sortedRanking;
        }


        public static List<string> top50Results(List<string> allResults)
        {
            return allResults.Take(50).ToList();
        }

 
        public void writeSingleQueryToFile(string  filePath, ObservableDictionary<int, List<string>> rankingList)
        {
 
            using (FileStream newFileStream = new FileStream(filePath, FileMode.Create))
            {
                StreamWriter bw = new StreamWriter(newFileStream);
                string iter = "0";
                string Rank = "0";
                string float_sim = "0";
                string Run_id = "mt";

                
                             
               foreach (int qcode in rankingList.Keys)
                {
                    //for (int i = 0; i < rankingList[qcode].Count; i++)
                   int mo= Math.Min(51, rankingList[qcode].Count);
                      for (int i = 0; i < mo; i++)

                        {

                            bw.Write(qcode.ToString()+" ");
                    bw.Write(iter+" ");
                    bw.Write(rankingList[qcode][i]+" ");
                    bw.Write(Rank+" ");
                    bw.Write(float_sim+" ");
                    bw.WriteLine(Run_id);


                    }

                }
                bw.Flush();
            }
        }

        public ConcurrentDictionary<string, double> Ranke(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
        {
            ConcurrentDictionary<string, double> rank25 = new ConcurrentDictionary<string, double>();
            ConcurrentDictionary<string, double> rankSim = new ConcurrentDictionary<string, double>();
            ConcurrentDictionary<string, double> total = new ConcurrentDictionary<string, double>();

            rank25 = BM25(q, QueryPerformances);
            rankSim=Sim(q, QueryPerformances);
            foreach (string item in rank25.Keys)
            {
                total[item]=0.25*rank25[item]+0.75*rankSim[item];
            }
            return rank25;
        }

        public int countNumberOfoccurencesInQuery(string[] queryArray,string query)
        {
            int counter = 0;
            foreach (string term in queryArray)
            {
                if (term.Equals(query))
                {
                    counter++;
                }
            }
            return counter;
        }

    }
}

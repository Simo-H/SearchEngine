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

        public Ranker(ref Indexer indexer, ref Searcher shearch,double k1,double k2,double b)
        {
            this.indexer = indexer;
            this.shearch = shearch;
            BonusAllQueryInDocument = 1;
            BonusTermInTitle = 1;
            N = indexer.documentDictionary.Count();
            ri = 0;
            R = 0;
            this.k1 = k1;
            this.k2 = k2;
            this.b = b;
            avgDocLenght = 0;
            foreach (string item in indexer.documentDictionary.Keys)
            {
                avgDocLenght= avgDocLenght+ indexer.documentDictionary[item].totalNumberInDoc;
            }
            avgDocLenght = avgDocLenght / N;
        }

        public ConcurrentDictionary<string, double>  BM25(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
        {
            ConcurrentDictionary <string,double> docList = new ConcurrentDictionary<string, double>();
            foreach (Dictionary<string, int> term in QueryPerformances.Values)
            {
                foreach (string doc in term.Keys)
                {
                    docList[doc] = 0;
                }
            }
            foreach (string doc in docList.Keys)
            {
                DocumentInfo f = indexer.documentDictionary[doc];
                int dl = indexer.documentDictionary[doc].totalNumberInDoc;
                double totalRankeForDoc=0;
                double rankeTermAtDoc = 0;
                int CounterTerminDoc = 0;
                foreach (string term in QueryPerformances.Keys)
                {
                    double qfi = System.Convert.ToDouble(countNumberOfoccurencesInQuery(q,term) / System.Convert.ToDouble( q.Length));
                    double ni = QueryPerformances[term].Count;
                    if (QueryPerformances[term].ContainsKey(doc))
                    {
                        CounterTerminDoc++;
                        double fi =( System.Convert.ToDouble(QueryPerformances[term][doc]));
                        double firstPart = ((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N - ni - R + ri + 0.5));
                        K = k1 * ((1 - b) + b * (dl / avgDocLenght));
                        double secondPart = ((k1 + 1) * fi) / (K + fi);
                        double third = ((k2 + 1) * qfi) / (k2 + qfi);
                        rankeTermAtDoc = firstPart * secondPart * third;
                        rankeTermAtDoc = Math.Log(rankeTermAtDoc);
                        totalRankeForDoc += rankeTermAtDoc;
                    }
                }
                docList[doc] = totalRankeForDoc;
                if (CounterTerminDoc==q.Length)
                {
                    docList[doc] = docList[doc] + BonusAllQueryInDocument + CheckingTitle(doc, q);
                }
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
            return count* BonusTermInTitle;
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
                    for (int i = 0; i < rankingList[qcode].Count; i++)
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
            ConcurrentDictionary<string, double> rank = new ConcurrentDictionary<string, double>();
            rank = BM25(q, QueryPerformances);
            return rank;
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

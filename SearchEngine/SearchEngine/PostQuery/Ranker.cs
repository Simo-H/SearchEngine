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

        public Ranker(ref Indexer indexer, ref Searcher shearch)
        {
            this.indexer = indexer;
            this.shearch = shearch;
            BonusAllQueryInDocument = 1;
            BonusTermInTitle = 1;
            N = indexer.documentDictionary.Count();
            ri = 0;
            R = 0;
            k1 = 1.2;
            k2 = 100;
            b = 0.75;
            avgDocLenght = 0;
            foreach (string item in indexer.documentDictionary.Keys)
            {
                avgDocLenght= avgDocLenght+ indexer.documentDictionary[item].totalNumberInDoc;
            }
            avgDocLenght = avgDocLenght / N;
        }

        public ConcurrentDictionary<string, double>  BM25(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
        {
            double qfi = System.Convert.ToDouble(1 / System.Convert.ToDouble( q.Length));
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
                foreach (Dictionary<string, int> term in QueryPerformances.Values)
                {
                    double ni = term.Values.Count();
                    if (term.Keys.Contains(doc))
                    {
                        CounterTerminDoc++;
                        double fi =( System.Convert.ToDouble(term[doc])) / (System.Convert.ToDouble(dl));
                      //  double firstPart = ((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N - ni- R + ri + 0.5));
                      //  K = k1*((1 - b) + b * (dl/ avgDocLenght));
                      //  //double secondPart=((k1+1)*fi)/ (K + fi);
                      //  //double third = ((k2 + 1) * qfi) / (k2 + qfi);
                      // // rankeTermAtDoc = firstPart * secondPart * third;
                      ////  rankeTermAtDoc = Math.Log(rankeTermAtDoc);
                        totalRankeForDoc = totalRankeForDoc + rankeTermAtDoc+ fi;
                    }
                }
                docList[doc] = totalRankeForDoc;
                if (CounterTerminDoc==q.Length)
                {
                    docList[doc] = docList[doc] + BonusAllQueryInDocument+ CheckingTitle(doc, q);
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

        public List<KeyValuePair<int,string>> printRankToFile(ConcurrentDictionary<string, double> ranking, string fileName, int queryId)
        {
            List < KeyValuePair < int,string>> Best50 = new List<KeyValuePair<int, string>>();
             string filePath = Properties.Settings.Default.postingFiles + fileName;
            var myList = ranking.ToList();
            myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            myList.Reverse(0, myList.Count);
            using (FileStream newFileStream = new FileStream(filePath, FileMode.Create))
            {
                StreamWriter bw = new StreamWriter(newFileStream);
                foreach (var item in myList)
                {
                    bw.WriteLine(item.Key);
                    bw.WriteLine(item.Value);

                }
                for (int i = 0; i < 51; i++)
                {
                    KeyValuePair<int, string> kp = new KeyValuePair<int, string>(queryId,myList[i].Key);
                    Best50.Add(kp);
                }
                bw.Flush();
            }
            return Best50;
        }

        public ConcurrentDictionary<string, double> Ranke(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
        {
            ConcurrentDictionary<string, double> rank = new ConcurrentDictionary<string, double>();
            rank = BM25(q, QueryPerformances);
            return rank;
        }


    }
}

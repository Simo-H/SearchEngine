using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class Ranker
    {
        double k1;
        double k2;
        double K;
        double b;
        int N;
        int ri;
        int R;
     //   double fi;
     //   double Qfi;
     //   int Length;
     //   int dl;
        double avgDocLenght;
        Indexer indexer;
        Searcher shearch;
        public Ranker(ref Indexer indexer, ref Searcher shearch)
        {
            this.indexer = indexer;
            this.shearch = shearch;
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
        public ConcurrentDictionary<string, double>  BM25(string[] q)
        {
            double qfi = System.Convert.ToDouble(1 / System.Convert.ToDouble( q.Length));
            ConcurrentDictionary <string,double> docList = new ConcurrentDictionary<string, double>();
            foreach (Dictionary<string, int> term in shearch.QueryPerformances.Values)
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

            double totalRanke=0;
                double rankerdoc = 0;
                foreach (Dictionary<string, int> term in shearch.QueryPerformances.Values)
                {
                    double ni = term.Values.Count();
                    if (term.Keys.Contains(doc))
                    {
                    double fi =( System.Convert.ToDouble(term[doc])) / (System.Convert.ToDouble(dl));
                        double firstPart = ((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N - ni- R + ri + 0.5));
                        K = k1*((1 - b) + b * (dl/ avgDocLenght));
                        double secondPart=((k1+1)*fi)/ (K + fi);
                        double third = ((k2 + 1) * qfi) / (k2 + qfi);
                        totalRanke = firstPart * secondPart * third;
                        totalRanke = Math.Log(totalRanke);
                    }

                }
                rankerdoc = rankerdoc + totalRanke;
                docList[doc] = rankerdoc;

            }
            return docList;
        }
    }
}

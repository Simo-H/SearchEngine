﻿using SearchEngine.PreQuery;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    /// <summary>
    /// This class ranks all the documents returned from the searcher which holds the words of the query and ranking them according to 
    /// various methods.
    /// </summary>
    class Ranker
    {
        /// <summary>
        /// An indexer used to get relevant data to preform the ranking.
        /// </summary>
        Indexer indexer;
        /// <summary>
        /// a searcher used for returning the initial documents for ranking
        /// </summary>
        Searcher search;
        /// <summary>
        /// Parameters for ranking, self explaintory
        /// </summary>
        double k1_BM25Parameter;
        double k2_BM25Parameter;
        double K_BM25Parameter;
        double b_BM25Parameter;
        double N_TotalNumberOfDocuments;
        double avgDocLenght;
        /// <summary>
        /// the amount different bonuses
        /// </summary>
        int Bonus_AllQueryTermsInDocument;
        int BonusQueryTermInTitle;
        /// <summary>
        /// an analasys varaible, return the max and min ranking. used for normalizing the rankings.
        /// </summary>
        public List<double> maxmin;
        /// <summary>
        /// ctor 
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="search"></param>
        /// <param name="k1Bm25Parameter">bm25 var</param>
        /// <param name="k2Bm25Parameter">bm25 var</param>
        /// <param name="bBm25Parameter">bm25 var</param>
        public Ranker(ref Indexer indexer, ref Searcher search, double k1Bm25Parameter, double k2Bm25Parameter, double bBm25Parameter)
        {
            this.indexer = indexer;
            this.search = search;
            maxmin = new List<double>();
            Bonus_AllQueryTermsInDocument = 20;
            BonusQueryTermInTitle = 10;
            this.k1_BM25Parameter = k1Bm25Parameter;
            this.k2_BM25Parameter = k2Bm25Parameter;
            this.b_BM25Parameter = bBm25Parameter;
            N_TotalNumberOfDocuments = indexer.documentDictionary.Count();
            foreach (string item in indexer.documentDictionary.Keys)
            {
                avgDocLenght += indexer.documentDictionary[item].totalNumberInDoc;
            }
            avgDocLenght = avgDocLenght / N_TotalNumberOfDocuments;

        }
        /// <summary>
        /// This method calculate the ranking by the BM-25 equation
        /// </summary>
        /// <param name="q">query terms</param>
        /// <param name="QueryOccurrences"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, double>  BM25(string[] q, Dictionary<string, Dictionary<string, int>> QueryOccurrences)
        {
            ConcurrentDictionary<string, double> docList = new ConcurrentDictionary<string, double>();
            foreach (Dictionary<string, int> term in QueryOccurrences.Values)
            {
                foreach (string doc in term.Keys)
                {
                    docList[doc] = 0;
                }
            }
            foreach (string doc in docList.Keys)
            {
                //DocumentInfo f = indexer.documentDictionary[doc];
                int documentLength = indexer.documentDictionary[doc].totalNumberInDoc;
                double totalRankeForDoc=0;
                double rankeTermAtDoc = 0;
                foreach (string term in QueryOccurrences.Keys)
                {
                  double qfi = System.Convert.ToDouble(countNumberOfoccurencesInQuery(q,term));
                    double df = indexer.mainTermDictionary[term].df;
                    if (QueryOccurrences[term].ContainsKey(doc))
                    {
                        double tf =( System.Convert.ToDouble(QueryOccurrences[term][doc]));
                        //double firstPart = Math.Log(((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (N_TotalNumberOfDocuments - ni - R + ri + 0.5)));
                        double firstPart = Math.Log((N_TotalNumberOfDocuments - df + 0.5) / (df + 0.5));
                        K_BM25Parameter = k1_BM25Parameter * ((1 - b_BM25Parameter) + b_BM25Parameter * (documentLength / avgDocLenght));
                        double secondPart = ((k1_BM25Parameter + 1) * tf) / (K_BM25Parameter + tf);
                        double third = ((k2_BM25Parameter + 1) * qfi) / (k2_BM25Parameter + qfi);
                        rankeTermAtDoc = firstPart * secondPart * third;
                        maxmin.Add(rankeTermAtDoc);/////////////////////////////////////
                        totalRankeForDoc += rankeTermAtDoc;
                    }
                }
                docList[doc] = totalRankeForDoc;
                    
            }
            return docList;
        }

       
    


        public ConcurrentDictionary<string, double> CosSim(string[] q, Dictionary<string, Dictionary<string, int>> QueryPerformances)
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
            double denominatorW = 0;
            double denominatorWq = 0;

            foreach (string doc in docList.Keys)
            {

                double maxfi = indexer.documentDictionary[doc].maxTF;
                foreach (string term in QueryPerformances.Keys)
                {
                    double qfi = 1;
                    if (QueryPerformances[term].ContainsKey(doc))
                    {
                        double fi = (System.Convert.ToDouble(QueryPerformances[term][doc]));
                        double tfi = fi / maxfi;

                        double idf =Math.Log (N_TotalNumberOfDocuments / indexer.mainTermDictionary[term].df);

                        rankeTermAtDoc += (tfi * idf * qfi);
                        denominatorW += (tfi * idf)*(tfi * idf);
                        denominatorWq += qfi * qfi;

                    }
                }
                double T = indexer.documentDictionary[doc].W;
                double F =T* denominatorWq;
                docList[doc] =(double) rankeTermAtDoc/ (double)Math.Sqrt(F);

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
        /// <summary>
        /// This method sort the ranked documents from high to low, used to present the highly matched documents first
        /// </summary>
        /// <param name="ranking"></param>
        /// <returns></returns>
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

        /// <summary>
        /// This method takes the first 50 best ranked documents.
        /// </summary>
        /// <param name="allResults"></param>
        /// <returns></returns>
        public static List<string> top50Results(List<string> allResults)
        {
            return allResults.Take(50).ToList();
        }

        /// <summary>
        /// This method write the ranking result to a file so it can be further analized in the treceval program.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="rankingList"></param>
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
                   int mo= Math.Min(50, rankingList[qcode].Count);
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
        /// <summary>
        /// This method combining all the rank results from the various methods to a single rank to each document.
        /// </summary>
        /// <param name="q"></param>
        /// <param name="QueryPerformances"></param>
        /// <returns></returns>
        public ConcurrentDictionary<string, double> CalculateTotalRank(string[] q,List<string> semantic, Dictionary<string, Dictionary<string, int>> QueryPerformances, Dictionary<string, Dictionary<string, int>> Semantic)
        {
            ConcurrentDictionary<string, double> rank25 = new ConcurrentDictionary<string, double>();
            //ConcurrentDictionary<string, double> rankSim = new ConcurrentDictionary<string, double>();
            ConcurrentDictionary<string, double> total = new ConcurrentDictionary<string, double>();
            //ConcurrentDictionary<string, double> CosSimr = new ConcurrentDictionary<string, double>();
            ConcurrentDictionary<string, double> bonuses = new ConcurrentDictionary<string, double>();
            //CosSimr = CosSim(q, QueryOccurrences);
            ConcurrentDictionary<string, double> semanticQuery = new ConcurrentDictionary<string, double>();
            bonuses = addBonuses(q, QueryPerformances);
            rank25 = BM25(q, QueryPerformances);
            semanticQuery = BM25(semantic.ToArray(), Semantic);
            foreach (string item in rank25.Keys)
            {
                if (semanticQuery.ContainsKey(item))
                {
                total[item] = rank25[item] + bonuses[item] + 2;
                    
                }
                else
                {
                    total[item] = rank25[item] + bonuses[item];
                }
            }
            return total;
        }
        /// <summary>
        /// This methods calculate the number of occurrences of a specific term in the query. this calculation is needed for the various
        /// ranking methods.
        /// </summary>
        /// <param name="queryArray"></param>
        /// <param name="query"></param>
        /// <returns></returns>
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

        //public ConcurrentDictionary<string, double> ICF(string[] q, Dictionary<string, Dictionary<string, int>> QueryOccurrences)
        //{
        //    //double tf = (System.Convert.ToDouble(QueryOccurrences[term][doc]));

        //}
        public ConcurrentDictionary<string,double> addBonuses(string[] q, Dictionary<string, Dictionary<string, int>> QueryOccurrences)
        {
            double Bonus = 0;
            ConcurrentDictionary<string,double> DocumentBonuses = new ConcurrentDictionary<string, double>();

            foreach (var termResult in QueryOccurrences)
            {
                foreach (string doc in termResult.Value.Keys)
                {
                    if (!DocumentBonuses.ContainsKey(doc))
                    {
                        DocumentBonuses[doc] = 0;
                    }
                    DocumentBonuses[doc] += Bonus_AllQueryTermsInDocument;
                }

            }

            foreach (string doc in DocumentBonuses.Keys)
            {
                DocumentBonuses[doc] += CheckingTitle(doc, q) * BonusQueryTermInTitle;

            }
            return DocumentBonuses;
            }

        public ConcurrentDictionary<string, double> combineSemanticQuery(ConcurrentDictionary<string, double> originalQuery, ConcurrentDictionary<string, double> SemanticQuery)
        {
            foreach (string originalQueryDoc in originalQuery.Keys)
            {
                if (SemanticQuery.Keys.Contains(originalQueryDoc))
                {
                    originalQuery[originalQueryDoc] += 2;
                }
            }
            return originalQuery;
        }
    }
    }


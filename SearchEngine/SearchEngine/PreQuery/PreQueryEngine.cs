using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using SearchEngine;
using Microsoft.VisualBasic;

namespace SearchEngine.PreQuery
{
    /// <summary>
    /// This class is the main class for the pre-query process. encapsulating relevant operations such as stemming,parsing,indexing etc
    /// 
    /// </summary>
    class PreQueryEngine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        
        /// <summary>
        /// A stop watch used to measure the time it take to process the corpus.
        /// </summary>
        public Stopwatch stopWatch;
        /// <summary>
        /// readfile object used to read the file of the corpus
        /// </summary>
        ReadFile rf = new ReadFile();
        /// <summary>
        /// indexer object used to build the inverse index from the data extracted from the documents
        /// </summary>        
        public Indexer indexer;
        /// <summary>
        /// the parser used to parse each word in the documents text
        /// </summary>
        public Parse parser;

        

        private List<string> languagesList;
        public List<string> LanguagesList
        {
            get { return languagesList; }
            set
            {
                languagesList = value;
                NotifyPropertyChanged("LanguagesList");
            }
        }


        /// <summary>
        /// pre query engine ctor
        /// </summary>
        public PreQueryEngine()
        {           
            //string stopWordsText = System.IO.File.ReadAllText(Properties.Settings.Default.sourceFilesPath+"\\stop_words.txt");
            //stopWords = stopWordsText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            indexer = new Indexer();
            parser = new Parse();
            stopWatch = new Stopwatch();
        }
        /// <summary>
        /// This is the main method of the pre query engine. This is method processes all documents in the corpus and building the index.
        /// </summary>
        public void engine()
        {
            stopWatch.Start();
            string[] files = rf.getCorpusFilesFromSource();
            ConcurrentBag<string> languagesConcurrentBag = new ConcurrentBag<string>();
            //Thread t = new Thread(() => indexer.mergeQueueFirstThread());
            //t.Start();
            foreach (string filePath in files)
            {
                if (filePath.EndsWith("stop_words.txt"))
                    continue;
                string[] docs = rf.seperateDocumentsFromFile(filePath);
                ConcurrentDictionary < string, Dictionary<string, int>> ContinuTermsFileDic = new ConcurrentDictionary<string, Dictionary<string, int>>();
                ConcurrentDictionary<string, string> tempFileDictionary = new ConcurrentDictionary<string, string>();
                Parallel.ForEach(docs, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (doc) =>
                {                    
                    
                    Stemmer stemmer = new Stemmer();
                    Dictionary<string, int> uniqeTermsAtDoc = new Dictionary<string, int>();
                    string metaData;
                    string text;
                    rf.getMetaDataAndTextFromDoc(doc, out metaData, out text);
                    string docNo = indexer.AddDocFromMetaData(metaData);
                    //if (docNo.Equals("FBIS4-11824"))
                    //{
                    //    string s = filePath.ToString();
                    //}
                    languagesConcurrentBag.Add(indexer.documentDictionary[docNo].originalLanguage);
                    string[] stringSeparators = new string[] { " ", "\n" ,"...","--","?",")","(", "[", "]", "\"", "&", "_",";", "~", "|" };
                    string[] textArray = text.ToLower().Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < textArray.Length; i++)
                    {
                        textArray[i] = parser.cutAllsigns(textArray[i]);
                    }
                    if (textArray.Length == 0)
                    {
                        return;
                    }
                    List<string> textList = textArray.ToList();
                    textList.Add("");
                    textList.Add("");
                    textList.Add("");
                    textList.Add("");
                    string lastParsTerm = "";

                    for (int i = 0; i < textList.Count - 4; i++)
                    {
                        string parsedTerm1;
                        string parsedTerm2;
                        if (parser.checkForStopWord(textList[i]) == 1 && !textList[i].Equals("between"))
                        {
                            continue;
                        }
                        else
                        {
                            int jump = parser.parseTerm(ref textList, i, out parsedTerm1, out parsedTerm2);
                            if (jump >= 0)
                            {
                                i += jump;
                                //stemmer
                                if (Properties.Settings.Default.stemmer)
                                {
                                    parsedTerm1 = stemmer.stemTerm(parsedTerm1);
                                }
                                AddTermUniqe(parsedTerm1, uniqeTermsAtDoc);
                                if (i>0)
                                {
                                        AddContinuingTerm(lastParsTerm, parsedTerm1, ContinuTermsFileDic);
                                }
                                lastParsTerm = parsedTerm1;


                                if (parsedTerm2 != null)
                                {
                                    if (Properties.Settings.Default.stemmer)
                                    {
                                        parsedTerm2 = stemmer.stemTerm(parsedTerm2);
                                    }
                                    AddTermUniqe(parsedTerm2, uniqeTermsAtDoc);
                                   // lastParsTerm = parsedTerm2;

                                }
                            }
                            else
                            {
                                if (parsedTerm1 != null && !textList[i].Equals("between") && !parsedTerm1.Equals(""))
                                {
                                    if (Properties.Settings.Default.stemmer)
                                    {
                                        parsedTerm1 = stemmer.stemTerm(parsedTerm1);
                                    }
                                    AddTermUniqe(parsedTerm1, uniqeTermsAtDoc);
                                    if (i > 0)
                                    {
                                        AddContinuingTerm(lastParsTerm, parsedTerm1, ContinuTermsFileDic);
                                    }
                                    lastParsTerm = parsedTerm1;

                                }
                            }

                        }
                    }
                    indexer.AddToMetaData(uniqeTermsAtDoc, docNo);

                    indexer.addUniqueDicToTempDic(ref tempFileDictionary, uniqeTermsAtDoc, docNo);

                    indexer.addUniqueDicToMainDic(uniqeTermsAtDoc);
                    
                });

                indexer.addFileDicToDisk(tempFileDictionary);
                AddContinuingDicToMain(ContinuTermsFileDic);
            }
            indexer.stop = false;
            //t.Join();
            indexer.mergeQueue();
            indexer.updateTermPointers();
            indexer.saveTermDictionary();
            indexer.saveDocumentDictionary();
            stopWatch.Stop();
            LanguagesList = new List<string>(languagesConcurrentBag.Distinct());
            WriteLanguagesToDisk(languagesList);
            int sum=indexer.countNumbers();
            System.Windows.MessageBox.Show("Inverted index is complete. \nNumber of terms: "+ indexer.mainTermDictionary.Count()+".\nNumber of documents: "+indexer.documentDictionary.Count()+"\nRun time: "+stopWatch.ElapsedMilliseconds/1000);
            
        }
        /// <summary>
        /// a utility method used to add a term to a temp-doc dictionary
        /// </summary>
        /// <param name="Uniqe"></param>
        /// <param name="UniqeFromDoc"></param>
        public void AddTermUniqe(string Uniqe, Dictionary<string, int> UniqeFromDoc)
        {
            if (UniqeFromDoc.ContainsKey(Uniqe))
            {
                UniqeFromDoc[Uniqe]++;
            }
            else
            {
                UniqeFromDoc[Uniqe] = 1;
            }

           

        }
        /// <summary>
        /// This method return a dictionary of terms and their cf. serving the user gui show function
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,int> showDictionary()
        {
            Dictionary<string, int> dic = new Dictionary<string, int>();
            foreach (var entry in indexer.mainTermDictionary)
            {
                dic[entry.Key] = entry.Value.cf;
            }
            return dic;
        }
        public void PrintFBIS3366(Dictionary<string, int> Dic)
        {
            List<string> a = Dic.Keys.ToList();
            a.Sort();

            using (FileStream fileStream = new FileStream(Properties.Settings.Default.postingFiles + "\\FBIS_3366.bin", FileMode.OpenOrCreate))
            {
                StreamWriter sw = new StreamWriter(fileStream);
                foreach (string term in a)
                {
                    sw.WriteLine(term);
                }
                foreach (string term in a)
                {
                    sw.WriteLine(Dic[term]);
                }
                sw.Flush();
                sw.Close();
            }

        }
        /// <summary>
        /// This method reset the dictionaries in the indexer, deleting their content.
        /// </summary>
        public void reset()
        {
            indexer.documentDictionary = new ConcurrentDictionary<string, DocumentInfo>();
            indexer.mainTermDictionary = new ConcurrentDictionary<string, TermInfo>();
            
        }
        /// <summary>
        /// Part of the MVVM architacture
        /// </summary>
        /// <param name="propName"></param>
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        /// <summary>
        /// This methods write all the languages to a file.
        /// </summary>
        /// <param name="list"></param>
        public void WriteLanguagesToDisk(List<string> list)
        {
            string data = "";
            foreach (string language in list)
            {
                data += language + " ";
            }
            File.WriteAllText(Properties.Settings.Default.postingFiles+"\\Languages.txt",data);
        }
        /// <summary>
        /// This method reads all the languages from the file language
        /// </summary>
        public void ReadLanguagesFile()
        {
            string file = File.ReadAllText(Properties.Settings.Default.postingFiles + "\\Languages.txt");
            List<string> list = file.Split(new char[] { ' ' },StringSplitOptions.RemoveEmptyEntries).ToList();
            list.Remove("language");
            list.Remove("not");
            list.Remove("found");
            LanguagesList = list;

        }
        /// <summary>
        /// This methods add terms that comes after a specific term in the text. holding only 5 terms, the ones that occured the most.
        /// </summary>
        /// <param name="term"></param>
        /// <param name="nextTerm"></param>
        /// <param name="TermsContinuingDicAtDoc"></param>
        public void AddContinuingTerm( string term,string nextTerm, ConcurrentDictionary<string, Dictionary<string, int> > TermsContinuingDicAtDoc)
        {
            if (term != null)
            {


                if (TermsContinuingDicAtDoc.ContainsKey(term))
                {
                    if (TermsContinuingDicAtDoc[term].ContainsKey(nextTerm))
                    {
                        TermsContinuingDicAtDoc[term][nextTerm]++;
                    }
                    else
                    {
                        TermsContinuingDicAtDoc[term][nextTerm] = 1;
                    }
                }
                else
                {
                    Dictionary<string, int> a = new Dictionary<string, int>();
                    a[nextTerm] = 1;
                    TermsContinuingDicAtDoc[term] = a;
                }
            }
        }
        /// <summary>
        /// This method 
        /// </summary>
        /// <param name="TermsContinuingDicAtDoc"></param>
        public void AddContinuingDicToMain(ConcurrentDictionary<string, Dictionary<string, int>> TermsContinuingDicAtDoc)
        {
            int numOfMembers;
            List<KeyValuePair<string, int>> mergeList;
            List<KeyValuePair<string, int>> myList;
            List<KeyValuePair<string, int>> termList;

            foreach (string term in TermsContinuingDicAtDoc.Keys)
            {
                if (term != "")
                {

                myList = TermsContinuingDicAtDoc[term].ToList();
                myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                myList.Reverse(0, myList.Count);
                    if (5 > myList.Count)
                    {
                    numOfMembers = myList.Count;
                    }
                else
                {
                    numOfMembers = 5;

                }

                if (indexer.mainTermDictionary[term].completion != null)
                    {
                    termList = indexer.mainTermDictionary[term].completion.ToList();
                        mergeList = termList.Concat(myList.GetRange(0, numOfMembers)).ToList();
                    mergeList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
                    mergeList.Reverse();
                    myList = mergeList;
                    }
                    Dictionary<string, int> a = new Dictionary<string, int>();
                for (int i = 0; i < 5 && i< myList.Count; i++)
                {
                    a[myList[i].Key] = myList[i].Value;

                }
                    indexer.mainTermDictionary[term].completion = a;
                }
                
            }
        }


    }
}

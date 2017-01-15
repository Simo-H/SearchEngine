using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using Microsoft.VisualBasic.FileIO;

namespace SearchEngine.PreQuery
{
    /// <summary>
    /// This class manage and create the inverted index of term and documents, holding a main term dictionary used to store all the
    /// terms in the corpus and the relevant information about them, the document dictionary holding the information about the documents
    /// and creates the posting file which holds the number of term occurrences in each document.
    /// </summary>
    class Indexer
    {
        /// <summary>
        /// The document dictionary holds the information about each document in the corpus.
        /// </summary>
        public ConcurrentDictionary<string, DocumentInfo> documentDictionary;
        /// <summary>
        /// a queue which holds all the temporary posting files that are ready to be merged.
        /// </summary>
        ConcurrentQueue<string> mergePathQueue;
        /// <summary>
        /// The main dictionary of term. holds the information about every term in the corpus.
        /// </summary>
        public ConcurrentDictionary<string, TermInfo> mainTermDictionary;
        /// <summary>
        /// a static variable used to give posting files their names.
        /// </summary>
        static int binaryFileCode = 0;
        /// <summary>
        /// A mutex helping prevent races while naming a posting file
        /// </summary>
        Mutex fileNameMutex;
        /// <summary>
        /// A mutex helping prevent races while using the ready to be merged posting files queue
        /// </summary>
        Mutex mergeQueueMutex = new Mutex();
        /// <summary>
        /// a bool which helps prevent races, stopping the thread merging posting files.
        /// </summary>
        public bool stop;
        /// <summary>
        /// ctor of the class
        /// </summary>
        public Indexer()
        {
            documentDictionary = new ConcurrentDictionary<string, DocumentInfo>();
            stop = true;
            fileNameMutex = new Mutex();
            mergePathQueue = new ConcurrentQueue<string>();
            mainTermDictionary = new ConcurrentDictionary<string, TermInfo>();            
        }
        /// <summary>
        /// This methods adds a entry to the doc dictionary, extracting the document info from the meta data of the document.
        /// </summary>
        /// <param name="docMetaData">document meta data string</param>
        /// <returns>return the position of the name of the document</returns>
        public string AddDocFromMetaData(string docMetaData)
        {
            string[] stringSeparators = new string[] { " ", "\n", "<", ">" };
            string[] seperatedMetaData = docMetaData.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            int docNumber = Array.IndexOf(seperatedMetaData, "DOCNO");
            documentDictionary[seperatedMetaData[docNumber + 1]] = new DocumentInfo(seperatedMetaData);
            return seperatedMetaData[docNumber + 1];
        }
        /// <summary>
        /// extracting the document info which needed processing and adding it to the relevant document information.
        /// </summary>
        /// <param name="uniqeDictionary">A temp dictionary holding the posting and document info after document processing</param>
        /// <param name="docName">the name of the document as can be found in the dictionary keys list</param>
        public void AddToMetaData(Dictionary<string, int> uniqeDictionary, string docName)
        {
            int max = 0;
            int totalNumOfWord=0;
            foreach (string term in uniqeDictionary.Keys)
            {
                totalNumOfWord = totalNumOfWord + uniqeDictionary[term];
                if (uniqeDictionary[term] > max)
                {
                    max = uniqeDictionary[term];
                }
            }

            documentDictionary[docName].maxTF = max;
            documentDictionary[docName].uniqueTerms = uniqeDictionary.Count;
            documentDictionary[docName].totalNumberInDoc = totalNumOfWord;
        }
        /// <summary>
        /// This methods add a term and its information to the main term dictionary or updates the term info if it was already in the dictionary.
        /// </summary>
        /// <param name="term">The soon to be added term</param>
        /// <param name="tf">The amount of time the term occures in the document</param>
        /// 
        //public void addTermToMainDictionary(string term, int tf)
        //{
        //    TermInfo ti;
        //    if (mainTermDictionary.TryGetValue(term, out ti))
        //    {
        //        ti.df++;
        //        ti.cf += tf;
        //    }
        //    else
        //    {
        //        mainTermDictionary[term] = new TermInfo();
        //        mainTermDictionary[term].df++;
        //        mainTermDictionary[term].cf += tf;
        //    }
        //}
        /// <summary>
        /// Adds all the terms in a doc-temp dictionary to a larger file-temp dictionary.
        /// </summary>
        /// <param name="tempDictionary">a reference to a dictonary which combine all the terms and their information in a given file</param>
        /// <param name="uniqueDictionary">a dictionary of a document</param>
        /// <param name="docNo">the name of the document which the doc-temp dictionary belongs to</param>

        public void addUniqueDicToTempDic(ref ConcurrentDictionary<string, string> tempDic, Dictionary<string, int> uniqueDic, string doc)
        {
            foreach (string term in uniqueDic.Keys)
            {
                string posting;
                if (tempDic.TryGetValue(term, out posting))
                {
                    posting += "#" + doc + "," + uniqueDic[term];
                }
                else
                {
                    tempDic[term] = "#" + doc + "," + uniqueDic[term];
                }
            }
        }
        //public void addUniqueDictonaryToTempDictionary(ref ConcurrentDictionary<string, ConcurrentDictionary<string, int>> tempDictionary, Dictionary<string, int> uniqueDictionary, string docNo)
        //{
        //    foreach (string tempTerm in uniqueDictionary.Keys)
        //    {
        //        addTermToMainDictionary(tempTerm, uniqueDictionary[tempTerm]);
        //        //PostingInfo pi = new PostingInfo(uniqueDictionary[tempTerm]);
        //        ConcurrentDictionary<string, int> ud;
        //        if (tempDictionary.TryGetValue(tempTerm, out ud))
        //        {
        //            ud[docNo] = uniqueDictionary[tempTerm];
        //        }
        //        else
        //        {
        //            tempDictionary[tempTerm] = new ConcurrentDictionary<string, int>();
        //            tempDictionary[tempTerm][docNo] = uniqueDictionary[tempTerm];
        //        }
        //    }
        //}
        /// <summary>
        /// This method writes a file-temp dictionary to a binary file on disk in a sorted manner
        /// </summary>
        /// <param name="tempDic">the dictionary of term belonging to a docs of a certein file</param>
        /// <param name="stream">the file stream which the method would write to</param>
        public void Serialize(ConcurrentDictionary<string, string> tempDic, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            List<string> sl = tempDic.Keys.ToList();
            sl.Sort();
            //writer.Write(tempDic.Count);
            foreach (var term in sl)
            {
                writer.Write(term);
                writer.Write(tempDic[term]);

            }
            writer.Flush();

        }
        /// <summary>
        /// merges two binary posting files into one binary posting sorted file. the method read only one term from each file, comparing it
        /// and write to a new merged posting file which is added to the posting file ready to be merged queue. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="path2"></param>
        /// <param name="newFilePath"></param>
        public void mergeDocFiles(string path, string path2, string newFilePath)
        {
            using (FileStream fileStream1 = new FileStream(path, FileMode.Open))
            {
                using (FileStream fileStream2 = new FileStream(path2, FileMode.Open))
                {
                    using (FileStream newFileStream = new FileStream(newFilePath, FileMode.Create))
                    {
                        BinaryReader br1 = new BinaryReader(fileStream1);
                        BinaryReader br2 = new BinaryReader(fileStream2);
                        BinaryWriter bw = new BinaryWriter(newFileStream);                        
                        string term1 = br1.ReadString();
                        string posting1 = br1.ReadString();
                        string term2 = br2.ReadString();
                        string posting2 = br2.ReadString();
                        long eof1 = br1.BaseStream.Length;
                        long eof2 = br2.BaseStream.Length;                        
                        while (br1.BaseStream.Position != eof1 && br2.BaseStream.Position != eof2)
                        {
                            int compare = String.Compare(term1, term2);
                            if (compare < 0)//t1 is first
                            {
                                bw.Write(term1);
                                bw.Write(posting1);
                                term1 = br1.ReadString();
                                posting1 = br1.ReadString();
                            }
                            else if (compare == 0)
                            {
                                bw.Write(term1);
                                bw.Write(posting1 + posting2);
                                term1 = br1.ReadString();
                                posting1 = br1.ReadString();
                                term2 = br2.ReadString();
                                posting2 = br2.ReadString();
                            }
                            else//t2 is first
                            {
                                bw.Write(term2);
                                bw.Write(posting2);
                                term2 = br2.ReadString();
                                posting2 = br2.ReadString();
                            }
                        }

                        while (br1.BaseStream.Position != eof1)
                        {
                            bw.Write(term1);
                            bw.Write(posting1);
                            term1 = br1.ReadString();
                            posting1 = br1.ReadString();
                        }
                        while (br2.BaseStream.Position != eof2)
                        {
                            bw.Write(term2);
                            bw.Write(posting2);
                            term2 = br2.ReadString();
                            posting2 = br2.ReadString();
                        }
                        bw.Flush();
                        newFileStream.Close();
                    }
                }
            }
            mergePathQueue.Enqueue(newFilePath);
        }
        /// <summary>
        /// writes all the terms in the temp-file dictionary to a binary file on disk.
        /// </summary>
        /// <param name="tempDic">a temp dictionary of term that belongs to all the docs from a specific file</param>
        public void addFileDicToDisk(ConcurrentDictionary<string, string> tempDic)
        {
            fileNameMutex.WaitOne();
            string fileName = binaryFileCode.ToString();
            if (Properties.Settings.Default.stemmer)
                fileName += "s";
            binaryFileCode++;
            fileNameMutex.ReleaseMutex();
            using (FileStream fileStream = new FileStream(Properties.Settings.Default.postingFiles + "\\" + fileName + ".bin", FileMode.Create))
            {
                //binaryPostingObject bpo = new binaryPostingObject(docNo, uniqeTermsAtDoc);
                Serialize(tempDic, fileStream);
                fileStream.Close();
            }

            mergePathQueue.Enqueue(Properties.Settings.Default.postingFiles + "\\" + fileName + ".bin");
        }
        /// <summary>
        /// This method is used by a special thread designated to merge binary posting files while other threads keep processing the corpus.
        /// the threads merges 2 binary posting file and deletes them after merging them to a new binary postnig file.
        /// the threads will stop when the boolean "stop" will changed to false;
        /// </summary>
        public void mergeQueueFirstThread()
        {
            while (stop)
            {
                if (mergePathQueue.Count > 1)
                {
                    fileNameMutex.WaitOne();
                    string fileName = binaryFileCode.ToString();
                    if (Properties.Settings.Default.stemmer)
                        fileName += "s";
                    binaryFileCode++;
                    fileNameMutex.ReleaseMutex();
                    mergeQueueMutex.WaitOne();
                    string path1;
                    mergePathQueue.TryDequeue(out path1);
                    string path2;
                    mergePathQueue.TryDequeue(out path2);
                    mergeDocFiles(path1, path2, Properties.Settings.Default.postingFiles + "\\" + fileName + ".bin");
                    mergeQueueMutex.ReleaseMutex();
                    File.Delete(path1);
                    File.Delete(path2);
                }
            }
        }
        /// <summary>
        /// This method is similar to mergeQueueFirstThread methods but contain no stop boolean. used by the main thread of the corpus 
        /// processing.
        /// </summary>
        public void mergeQueue()
        {

            while (mergePathQueue.Count > 1)
            {
                int count = mergePathQueue.Count;
                fileNameMutex.WaitOne();
                string fileName = binaryFileCode.ToString();
                if (Properties.Settings.Default.stemmer)
                    fileName += "s";
                binaryFileCode++;
                fileNameMutex.ReleaseMutex();
                mergeQueueMutex.WaitOne();
                string path1;
                mergePathQueue.TryDequeue(out path1);
                string path2;
                mergePathQueue.TryDequeue(out path2);
                mergeDocFiles(path1, path2, Properties.Settings.Default.postingFiles + "\\" + fileName + ".bin");
                mergeQueueMutex.ReleaseMutex();
                File.Delete(path1);
                File.Delete(path2);
            }
        }
        /// <summary>
        /// This methods adds the terms from a temp dictionary generated from a single doc to a more larger temp dictionary conating all the 
        /// term from a single file(containing many documents).
        /// </summary>
        /// <param name="uniqueDictionary">a term dictionary from a single document</param>
        public void addUniqueDicToMainDic(Dictionary<string, int> uniqueDictionary)
        {            
            foreach (string term in uniqueDictionary.Keys)
            {
                TermInfo ti;
                if (mainTermDictionary.TryGetValue(term, out ti))
                {
                    ti.df++;
                    ti.cf += uniqueDictionary[term];
                }
                else
                {
                    mainTermDictionary[term] = new TermInfo();
                    ti = mainTermDictionary[term];
                    ti.df++;
                    ti.cf += uniqueDictionary[term];
                }
            }

        }
        /// <summary>
        /// This methods scans the complete posting files,extract each term pointer and updating the main term dictionary
        /// </summary>
        public void updateTermPointers()
        {
            string postingFilePath;
            mergePathQueue.TryDequeue(out postingFilePath);
            using (FileStream fileStream = new FileStream(postingFilePath, FileMode.Open))
            {
                BinaryReader br = new BinaryReader(fileStream);
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    long position = br.BaseStream.Position;
                    string term = br.ReadString();
                    br.ReadString();
                    mainTermDictionary[term].postingfilepointer = position;
                }
                fileStream.Flush();
                fileStream.Close();
                br.Close();

            }

            string newPostingFilePath;
            if (Properties.Settings.Default.stemmer)
            {

                newPostingFilePath ="PostingS.bin";

            }
            else
            {
                newPostingFilePath = "Posting.bin";

            }
 
            FileSystem.RenameFile(postingFilePath, newPostingFilePath);

        }
        /// <summary>
        /// This method saves a term dictionary to disk
        /// </summary>
        public void saveTermDictionary()
        {
            string fileName;
            if(Properties.Settings.Default.stemmer)
            {
                fileName = Properties.Settings.Default.postingFiles + "\\TermDictionaryStemmer.bin";
            }
            else
            {
                fileName = Properties.Settings.Default.postingFiles + "\\TermDictionary.bin";
            }
            using (FileStream newFileStream = new FileStream(fileName, FileMode.Create))
            {
                BinaryWriter bw = new BinaryWriter(newFileStream);                
                foreach (var item in mainTermDictionary)
                {
                    bw.Write(item.Key);
                    bw.Write(item.Value.df);
                    bw.Write(item.Value.cf);
                    bw.Write(item.Value.postingfilepointer);
                    string completions="";
                    if (item.Value.completion.Count!=0)
                    {
                        foreach (string next in item.Value.completion.Keys)
                        {
                            completions += next + "&&&" + item.Value.completion[next] + "&&&";
                        }
                    }
                    else
                    {
                        completions = " ";
                    }
                    bw.Write(completions);
                }
                bw.Flush();
            }
        }
        /// <summary>
        /// This method saves a document dictionary to disk.
        /// </summary>
        public void saveDocumentDictionary()
        {
            string fileName;
            if (Properties.Settings.Default.stemmer)
            {
                fileName = Properties.Settings.Default.postingFiles + "\\DocumentDictionaryStemmer.bin";
            }
            else
            {
                fileName = Properties.Settings.Default.postingFiles + "\\DocumentDictionary.bin";
            }
            using (FileStream newFileStream = new FileStream(fileName, FileMode.Create))
            {
                BinaryWriter bw = new BinaryWriter(newFileStream);
                foreach (var item in documentDictionary)
                {
                    bw.Write(item.Key);
                    bw.Write(item.Value.uniqueTerms);
                    bw.Write(item.Value.originalLanguage);
                    bw.Write(item.Value.maxTF);
                    bw.Write(item.Value.title);
                    bw.Write(item.Value.totalNumberInDoc);
                }
                bw.Flush();
            }
        }
        /// <summary>
        /// This method loads a term dictionary from disk to memory
        /// </summary>
        public void loadTermDictionary()
        {
            string fileName;
            mainTermDictionary = new ConcurrentDictionary<string, TermInfo>();
            if (Properties.Settings.Default.stemmer)
            {
                fileName = Properties.Settings.Default.postingFiles + "\\TermDictionaryStemmer.bin";
            }
            else
            {
                fileName = Properties.Settings.Default.postingFiles + "\\TermDictionary.bin";
            }
            using (FileStream newFileStream = new FileStream(fileName, FileMode.Open))
            {
                BinaryReader br = new BinaryReader(newFileStream);
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    string term = br.ReadString();
                    int df = br.ReadInt32();
                    int cf = br.ReadInt32();
                    long pointer = br.ReadInt64();
                    string continuing = br.ReadString();
                    mainTermDictionary[term] = new TermInfo();
                    mainTermDictionary[term].df = df;
                    mainTermDictionary[term].cf = cf;
                    mainTermDictionary[term].postingfilepointer = pointer;

                    string[] stringSeparators = new string[] { "&&&" };
                    string[] DocumentAndShowsArray = continuing.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    if (DocumentAndShowsArray!=null)
                    {
                        for (int i = 0; i < DocumentAndShowsArray.Count(); i = i + 2)
                        {
                            mainTermDictionary[term].completion[DocumentAndShowsArray[i]] = Int32.Parse(DocumentAndShowsArray[i + 1]);
                        }  
                    }
                    else
                    {
                        mainTermDictionary[term].completion = new Dictionary<string, int>();
                    }
                }
            }
        }
        /// <summary>
        /// This method loads a document dictionary from disk to memory
        /// </summary>
        public void loadDocumentDictionary()
        {
            string fileName;
            documentDictionary = new ConcurrentDictionary<string, DocumentInfo>();
            if (Properties.Settings.Default.stemmer)
            {
                fileName = Properties.Settings.Default.postingFiles + "\\DocumentDictionaryStemmer.bin";
            }
            else
            {
                fileName = Properties.Settings.Default.postingFiles + "\\DocumentDictionary.bin";
            }
            using (FileStream newFileStream = new FileStream(fileName, FileMode.Open))
            {
                BinaryReader br = new BinaryReader(newFileStream);
                while (br.BaseStream.Position != br.BaseStream.Length)
                {
                    string DocNo = br.ReadString();
                    int uniqueTerms = br.ReadInt32();
                    string originalLanguage = br.ReadString();
                    int maxTF = br.ReadInt32();
                    string title = br.ReadString();
                    int totalnumberofterms = br.ReadInt32();


                    documentDictionary[DocNo] = new DocumentInfo(uniqueTerms, originalLanguage, maxTF, title, totalnumberofterms);
                    
                }
            }
        }
        public void sortedFrequency()
        {
            List<int> listSort = new List<int>();
            foreach (var item in mainTermDictionary)
            {
                listSort.Add(item.Value.cf);
            }
            listSort.Sort();
            using (FileStream fileStream = new FileStream(Properties.Settings.Default.postingFiles + "\\abc", FileMode.Create))
            {
                StreamWriter sw = new StreamWriter(fileStream);
                for (int i = listSort.Count - 1; i >= listSort.Count - 1-10; i--)

                    {
                        sw.WriteLine(listSort[i]);
                    foreach (var item in mainTermDictionary)
                    {
                        if (item.Value.cf== listSort[i])
                        {
                            sw.WriteLine(item.Key);
                        }
                    }


                }
                sw.Flush();
                sw.Close();
            }

        }
        public int countNumbers()
        {
            int sum = 0;
            foreach (var item in mainTermDictionary)
            {
                if (item.Key.Contains("0")|| item.Key.Contains("2") || item.Key.Contains("3") || item.Key.Contains("4") || item.Key.Contains("5") || item.Key.Contains("6") ||item.Key.Contains("7") || item.Key.Contains("8") ||item.Key.Contains("9") )
                {
                    sum++;
                }
            }
            return sum;
        }
    }
}


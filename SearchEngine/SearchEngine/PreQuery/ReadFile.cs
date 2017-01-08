using SearchEngine.Properties;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace SearchEngine.PreQuery
{
    /// <summary>
    /// This class reads the files of the corpus, separating documents. extracting their meta data and text.
    /// </summary>
    public class ReadFile
    {
        /// <summary>
        /// this is the corpus files source directory
        /// </summary>
        static string sourceFilesPath = Properties.Settings.Default.sourceFilesPath;
        //static string afterSeperationfilesPath = Properties.Settings.Default.postingFiles;

        //
        public ReadFile() { }

        /// <summary>
        /// This method return all the files paths from the source folder.
        /// </summary>
        /// <returns>string array containing docs file paths</returns>
        public string[] getCorpusFilesFromSource()
        {
            return Directory.GetFiles(sourceFilesPath);
         
        }
        /// <summary>
        /// Separates all documents from a given file
        /// </summary>
        /// <param name="filePath">the file path of the soon to be seperated file</param>
        /// <returns>string array each string contains a doc</returns>
        public string[] seperateDocumentsFromFile(string filePath)
        {            
            string text = System.IO.File.ReadAllText(filePath);
            string[] stringSeparators = new string[] {"<DOC>"};
            if (text[0] == '\n')
                text = text.Substring(1);
            return text.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);             
        }
        /// <summary>
        /// separate the meta data and text from a given document
        /// </summary>
        /// <param name="doc">the given doc</param>
        /// <param name="metaData">getting out the meta data</param>
        /// <param name="text">getting out the text</param>
        public void getMetaDataAndTextFromDoc(string doc,out string metaData,out string text)
        {
            string[] stringSeparators = new string[] { "[Text]", "</TEXT>" };
            string[] splitTest = doc.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (splitTest.Length == 3)
            {
                metaData = splitTest[0];
                text = splitTest[1];
            }
            else
            {
                stringSeparators = new string[] { "<TEXT>", "</TEXT>" };
                splitTest = doc.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                metaData = splitTest[0];
                text = splitTest[1];
            }
        }
    }
}
           

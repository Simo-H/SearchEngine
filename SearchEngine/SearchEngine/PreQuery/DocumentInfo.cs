using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PreQuery
{
    /// <summary>
    /// This class is used to describe all the relevant information about a document
    /// </summary>
    public class DocumentInfo
    {
        /// <summary>
        /// The amount of unique term in the document (without duplicates).
        /// </summary>
        public int uniqueTerms;
        /// <summary>
        /// The original language of the document as described in the meta data of the document.
        /// </summary>
        public string originalLanguage;
        /// <summary>
        /// The amount of occurrences of the most occurred term in the document.
        /// </summary>
        public int maxTF;
        /// <summary>
        /// The title of the document as described in the meta data of the document.
        /// </summary>
        public string title;
        private static Dictionary<string, string> spelling = new Dictionary<string, string>();
        /// <summary>
        /// The total number of words in document.
        /// </summary>
        public int totalNumberInDoc;
        /// <summary>
        /// The normal ctor of a documentInfo.
        /// </summary>
        /// <param name="uniqueTerms"></param>
        /// <param name="originalLanguage"></param>
        /// <param name="maxTF"></param>
        /// <param name="title"></param>
        public DocumentInfo(int uniqueTerms,string originalLanguage,int maxTF,string title, int totalNumberInDoc)
        {
            this.uniqueTerms = uniqueTerms;
            this.originalLanguage = originalLanguage;
            this.maxTF = maxTF;
            this.title = title;
            this.totalNumberInDoc = totalNumberInDoc;
        }
        /// <summary>
        /// A more complex ctor of the object, given a meta data of a document, the ctor extract the document info and create an object describing the document.
        /// </summary>
        /// <param name="seperatedMetaData">document meta data</param>
        public DocumentInfo(string[] seperatedMetaData)
        {      
            if(spelling.Count == 0)
                CorrectSpelling();
            int docTitleStart = Array.IndexOf(seperatedMetaData, "TI");
            int docTitleEnd = Array.IndexOf(seperatedMetaData, "/TI");
            if(docTitleStart != -1)
            {
                for (int i = docTitleStart+1; i < docTitleEnd-1; i++)
                {
                    title += seperatedMetaData[i]+" ";
                }
                title += seperatedMetaData[docTitleEnd-1];
            }
            else
            {
                title ="Title not found";
            }
            int docLanguage = Array.IndexOf(seperatedMetaData, "P=105");
            if(docLanguage != -1)
            {
                originalLanguage = seperatedMetaData[docLanguage + 1];
                originalLanguage = originalLanguage.Trim(',', '-', ';').ToLower();
                int i;
                if (!Int32.TryParse(originalLanguage,out i))
                {
                    string correctedLanguage;
                    if (spelling.TryGetValue(originalLanguage, out correctedLanguage))
                    {
                        originalLanguage = correctedLanguage;
                    }
                    else if (originalLanguage.Equals("the"))
                    {
                        originalLanguage = "Language not found";
                    }

                }
                else
                {
                    originalLanguage = "Language not found";
                }
            }
            else
            {
                originalLanguage = "Language not found";
            }                    
        }

        public void CorrectSpelling()
        {
            spelling["arabi"] = "arabic";
            spelling["cambodia"] = "cambodian";
            spelling["eng"] = "english";
            spelling["engligh"] = "english";
            spelling["enhglish"] = "english";
            spelling["enlgish"] = "english";
            spelling["hebrew3"] = "hebrew";
            spelling["itaian"] = "italian";
            spelling["japanse"] = "japanese";
            spelling["kinyarwand"] = "kinyarwanda";
            spelling["rusian"] = "russian";
            spelling["russia"] = "russian";
            spelling["russia"] = "russian";
            spelling["slovenian"] = "slovene";
            spelling["span"] = "spanish";
            spelling["spansih"] = "spanish";
            spelling["tigrigna"] = "tigrinya";

        }
    }
    
}

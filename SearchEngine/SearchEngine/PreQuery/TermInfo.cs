using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;

namespace SearchEngine.PreQuery
{
    /// <summary>
    /// This class represent information about a term in the main term dictionary
    /// </summary>
    class TermInfo
    {
        /// <summary>
        /// Total number of occurrences in the corpus
        /// </summary>
        public int cf = 0;
        /// <summary>
        /// The amount of document containing the term
        /// </summary>
        public int df = 0;
        /// <summary>
        /// a pointer to the term's posting in the posting file
        /// </summary>
        public long postingfilepointer;
        public TermInfo()
        {
        }
    }
}

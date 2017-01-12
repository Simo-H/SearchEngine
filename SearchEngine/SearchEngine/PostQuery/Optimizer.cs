using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine.PostQuery
{
    class Optimizer
    {
        Dictionary<int,Dictionary<string,int>> qrelsDictionary = new Dictionary<int, Dictionary<string, int>>();
        public void ReadQrels(string qrelsFilePath)
        {
            using (FileStream qrelsTextFileStream = new FileStream(qrelsFilePath, FileMode.Open))
            {
                StreamReader streamReader = new StreamReader(qrelsTextFileStream);
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] qrel = line.Split(new char[] {' '},StringSplitOptions.RemoveEmptyEntries);
                    if (!qrelsDictionary.ContainsKey(Int32.Parse(qrel[0])))
                    {
                        Dictionary<string, int> queryKeyValuePair = new Dictionary<string, int>();
                        qrelsDictionary[Int32.Parse(qrel[0])] = queryKeyValuePair;
                        queryKeyValuePair[qrel[2]] = Int32.Parse(qrel[3]);
                    }

                    qrelsDictionary[Int32.Parse(qrel[0])][qrel[2]] = Int32.Parse(qrel[3]);
                }
            }
        }

        public void Optimize(string qrelsFilePath)
        {
            ReadQrels(qrelsFilePath);
        }
    }
}

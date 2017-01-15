using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using SearchEngine.PostQuery;
using SearchEngine.PreQuery;

namespace SearchEngine
{
    public class ViewModel:INotifyPropertyChanged
    {
        private PreQueryEngine pq;
        PostQueryEngine postQuery;
        private Optimizer opt;
        public event PropertyChangedEventHandler PropertyChanged;
        Thread engineThread;
        private Searcher searcher;
        private string query;

        public string Query
        {
            get { return query; }
            set { query = value; }
        }
        private int myVar;

        
        public List<ResultsSingleQuery> QueriesResults
        {
            get
            {
                List<ResultsSingleQuery> resultList = new List<ResultsSingleQuery>();
                foreach (int item in postQuery.QueriesResults.Keys)
                {
                    resultList.Add(new ResultsSingleQuery(item, postQuery.QueriesResults[item].Count, Ranker.top50Results(postQuery.QueriesResults[item])));
                }                
                return resultList;
            }

        }

        public List<string> LanguagesList
        {
            get
            {
                if (pq != null && pq.LanguagesList != null)
                {
                    List<string> languageList = new List<string>(pq.LanguagesList);
                    languageList.Remove("Language not found");
                    languageList.Sort();
                    languageList.Insert(0,"All languages");
                    return languageList;
                }
                return null;
            }
        }
        private bool stemmerIsChecked;
        public bool StemmerIsChecked
        {
            get { return stemmerIsChecked; }
            set
            {
                stemmerIsChecked = value;
                Properties.Settings.Default.stemmer = stemmerIsChecked;
                Properties.Settings.Default.Save();
            }
        }
        private string path1;

        public string Path1
        {
            get { return path1; }
            set
            {
                path1 = value;
                Properties.Settings.Default.sourceFilesPath = Path1;
                Properties.Settings.Default.stopWordFilePath = Path1;
                Properties.Settings.Default.Save();
                NotifyPropertyChanged("Path1");
            }
        }
        private string path2;

        public string Path2
        {
            get { return path2; }
            set
            {
                path2 = value;
                Properties.Settings.Default.postingFiles = Path2;
                Properties.Settings.Default.Save();
                NotifyPropertyChanged("Path2");
            }
        }

        private string path3;

        public string Path3
        {
            get { return path3; }
            set
            {
                path3 = value;
                NotifyPropertyChanged("Path3");
            }
        }

        private string selectedLanguage;

        public string SelectedLanguage
        {
            get { return selectedLanguage; }
            set { selectedLanguage = value; }
        }

        public ViewModel()
        {
            StemmerIsChecked = false;
            pq = new PreQueryEngine();
            postQuery = new PostQueryEngine(ref pq.indexer);
            searcher = new Searcher(ref pq.indexer,3);
            opt = new Optimizer(ref pq.indexer);
            pq.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                NotifyPropertyChanged(e.PropertyName);
            };
        }
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public void RunQueryEngine()
        {
            if (Directory.Exists(Path1) && Directory.Exists(Path2))
            {
                pq.reset();
                engineThread = new Thread(() => pq.engine());
                engineThread.Start();
            }
            else
                System.Windows.MessageBox.Show("One of the paths given is invalid");
        }

        public void ResetDictionariesAndFiles()
        {
            if (Directory.Exists(Path2))
            {
                if (engineThread != null && engineThread.IsAlive)
                    engineThread.Abort();
                pq.reset();
                string[] filesInPostingFolder = Directory.GetFiles(Properties.Settings.Default.postingFiles);
                foreach (string path in filesInPostingFolder)
                {
                    File.Delete(path);
                }
                System.Windows.MessageBox.Show("Reset complete.\nPosting file and dictionaries were deleted.");
            }
            else
            {
                System.Windows.MessageBox.Show("Reset failed.\nPlease enter a valid path.");
            }
        }

        public void Browse(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            string s = result.ToString();
            if (s == "OK")
            {
                string buttenName = ((System.Windows.Controls.Button)sender).Name;
                if (buttenName.Equals("browse1"))
                {
                    Path1 = dialog.SelectedPath;
                }
                if (buttenName.Equals("browse2"))
                {
                    Path2 = dialog.SelectedPath;
                }
            }
        }

        public void ShowDictionary()
        {
            if (Directory.Exists(Path1) && Directory.Exists(Path2))
            {
                if (pq != null && pq.showDictionary().Count != 0)
                {
                    ShowDictionary show = new ShowDictionary(pq.showDictionary());
                    show.ShowDialog();
                }
                else
                {
                    System.Windows.MessageBox.Show("Dictionary is empty, please load a dictionary or run the query engine");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Error showing term dictionary.\nPlease check if the paths are correct and you already created a dictionary.");
            }
        }

        public void LoadDictionaries()
        {
            string filePath1 = Properties.Settings.Default.stemmer ? "\\TermDictionaryStemmer.bin" : "\\TermDictionary.bin";
            string filePath2 = Properties.Settings.Default.stemmer ? "\\DocumentDictionaryStemmer.bin" : "\\DocumentDictionary.bin";
            if (File.Exists(Path2 + filePath1) && File.Exists(Path2 + filePath2) && File.Exists(Path2+"\\Languages.txt"))
            {
                pq.indexer.loadTermDictionary();
                pq.indexer.loadDocumentDictionary();
                pq.ReadLanguagesFile();

                System.Windows.MessageBox.Show("Dictionary loaded successfully!");
            }
            else
            {
                System.Windows.MessageBox.Show("Dictionary not loaded.\nPlease check if the paths are correct and you already created a dictionary.");
            }
        }

        
        public void Search()
        {
           postQuery.userManualSingleQuery(Query, selectedLanguage);
            
        }

        public void browseFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true){
                
                Path3 = dlg.FileName;
            }
        }
        public void SearchQueryFile()
        {
            if (File.Exists(Path3))
            {
                postQuery.queriesFile(Path3,SelectedLanguage);
            }
        }

        public void Optimize()
        {
            opt.findOptimizedParameters();
        }
    };
}


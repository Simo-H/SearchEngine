using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SearchEngine.PreQuery;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;

namespace SearchEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window,INotifyPropertyChanged
    {
        Thread engineThread;
        public event PropertyChangedEventHandler PropertyChanged;
        string corpusAndStopWordFilesPath = Properties.Settings.Default.sourceFilesPath;
        public string CorpusAndStopWordFilesPath
        {
            get { return corpusAndStopWordFilesPath; }
            set { corpusAndStopWordFilesPath = value;                
            }
        }
        string postinAndDictionaryFilesPath = Properties.Settings.Default.postingFiles;
        public string PostinAndDictionaryFilesPath
        {
            get { return postinAndDictionaryFilesPath; }
            set
            {
                postinAndDictionaryFilesPath = value;
            }
        }
        private bool stemmerIsChecked;
        public bool StemmerIsChecked
        {
            get { return stemmerIsChecked; }
            set
            {
                stemmerIsChecked = value;
                //OnPropertyChanged("StemmerIsChecked");
            }
        }
        PreQueryEngine pq;
        public MainWindow()
        {
            InitializeComponent();
            //if(!Directory.Exists(Properties.Settings.Default.postingFiles))
            //Properties.Settings.Default.stemmer = StemmerIsChecked;
            //Properties.Settings.Default.Save();
            //pq = new PreQueryEngine();
            
            //CorpusAndStopWordFilesPath = Properties.Settings.Default.sourceFilesPath;
            //pq.engine();





        }
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            // ... A List.
            List<string> data = new List<string>();
            data.Add("English");
            data.Add("French");
            data.Add("Spanish");
            data.Add("Russian");
            data.Add("Polish");
            data.Add("German");
            data.Add("Korean");
            data.Add("Japanese");
            data.Add("Cambodian");
            // ... Get the ComboBox reference.
            var comboBox = sender as System.Windows.Controls.ComboBox;

            // ... Assign the ItemsSource to the List.
            comboBox.ItemsSource = data;

            // ... Make the first item selected.
            comboBox.SelectedIndex = 0;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ... Get the ComboBox.
            var comboBox = sender as System.Windows.Controls.ComboBox;

            // ... Set SelectedItem as Window Title.
            string value = comboBox.SelectedItem as string;
            this.Title = "Selected: " + value;
            
        }
        private void run_query_engine_onClick(object sender, RoutedEventArgs e)
        {
            
            string path2 = aPath.Text;
            //if (Path.Text == "")
            //{
            //    path2 = postinAndDictionaryFilesPath;
            //}
            string path = Path.Text;
            
            //if (Path.Text == "")
            //{
            //    path = CorpusAndStopWordFilesPath;
            //}
            if (Directory.Exists(path) && Directory.Exists(path2))
            {
                Properties.Settings.Default.sourceFilesPath = path;
                Properties.Settings.Default.postingFiles = path2;
                Properties.Settings.Default.stemmer = StemmerIsChecked;
                Properties.Settings.Default.stopWordFilePath = path;
                Properties.Settings.Default.Save();
                pq = new PreQueryEngine();
                engineThread = new Thread(() => pq.engine());
                engineThread.Start();
                
                
            }
            else
                System.Windows.MessageBox.Show("One of the paths given is invalid");
        }

        private void browse1_onClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            string s = result.ToString();
            if(s == "OK")
            {
                Path.Text = dialog.SelectedPath;
            }
        }

        private void browse2_onClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            string s = result.ToString();
            if (s == "OK")
            {
                aPath.Text = dialog.SelectedPath;
            }
        }

        private void stem_isChecked(object sender, RoutedEventArgs e)
        {            
                StemmerIsChecked = stemmer.IsChecked.Value;            
        }
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void reset_onClick(object sender, RoutedEventArgs e)
        {            
            if (Directory.Exists(aPath.Text))
            {

            if(engineThread != null &&  engineThread.IsAlive)
                engineThread.Abort();
            pq = new PreQueryEngine();
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

        private void showDictionary_onClick(object sender, RoutedEventArgs e)
        {
            
            if (Directory.Exists(Path.Text) && Directory.Exists(aPath.Text))
            {
                Properties.Settings.Default.sourceFilesPath = Path.Text;
                Properties.Settings.Default.postingFiles = aPath.Text;
                Properties.Settings.Default.stemmer = StemmerIsChecked;
                Properties.Settings.Default.stopWordFilePath = Path.Text;
                Properties.Settings.Default.Save();
                //pq = new PreQueryEngine();
                if (pq!= null && pq.showDictionary().Count !=0)
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

        private void LoadDictionary(object sender, RoutedEventArgs e)
        {
            string filePath1 = Properties.Settings.Default.stemmer ? "\\TermDictionaryStemmer.bin" : "\\TermDictionary.bin";
            string filePath2 = Properties.Settings.Default.stemmer ? "\\DocumentDictionaryStemmer.bin" : "\\DocumentDictionary.bin";
            if (File.Exists(aPath.Text+ filePath1) && File.Exists(aPath.Text + filePath2))
            {
                Properties.Settings.Default.sourceFilesPath = Path.Text;
                Properties.Settings.Default.postingFiles = aPath.Text;
                Properties.Settings.Default.stemmer = StemmerIsChecked;
                Properties.Settings.Default.stopWordFilePath = Path.Text;
                Properties.Settings.Default.Save();
            pq = new PreQueryEngine();
            pq.indexer.loadTermDictionary();
            pq.indexer.loadDocumentDictionary();
            System.Windows.MessageBox.Show("Dictionary loaded successfully!");
            }
            else
            {
                System.Windows.MessageBox.Show("Dictionary not loaded.\nPlease check if the paths are correct and you already created a dictionary.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBlock_TouchEnter(object sender, TouchEventArgs e)
        {

        }

        private void Path_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void aPath_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
        
}

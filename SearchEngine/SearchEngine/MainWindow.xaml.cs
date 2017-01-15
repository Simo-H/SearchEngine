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
using SearchEngine.PostQuery;

namespace SearchEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel vm;
        private ResultsAllQueries results;
        public MainWindow()
        {

            InitializeComponent();
            vm = new ViewModel();
            DataContext = vm;
            
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            string value = comboBox.SelectedItem as string;
            this.Title = "Language selected: " + value;
        }
        private void run_query_engine_onClick(object sender, RoutedEventArgs e)
        {
            vm.RunQueryEngine();
        }

        private void browse_onClick(object sender, RoutedEventArgs e)
        {
            vm.Browse(sender, e);
        }

        private void reset_onClick(object sender, RoutedEventArgs e)
        {
            vm.ResetDictionariesAndFiles();
        }

        private void showDictionary_onClick(object sender, RoutedEventArgs e)
        {
            vm.ShowDictionary();
        }

        private void LoadDictionary(object sender, RoutedEventArgs e)
        {
            vm.LoadDictionaries();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void GO_onClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(filePath4.Text))
            {
    results = new ResultsAllQueries(ref vm);
            vm.Search();
            results.ShowDialog();

            }
            else
            {
                System.Windows.MessageBox.Show("The given paths is invalid");
 
            }

        }

        private void optimize_Click(object sender, RoutedEventArgs e)
        {
            //results.ShowDialog(); 
            vm.Optimize();
        }


        private void GOQueryFile_onClick(object sender, RoutedEventArgs e)
        {

            results = new ResultsAllQueries(ref vm);
            vm.SearchQueryFile();
            results.ShowDialog();
        }

        private void browseFile(object sender, RoutedEventArgs e)
        {

            vm.browseFile();
        }

        //private void Populating(object sender, PopulatingEventArgs e)
        //{
        //    string text = AutoCompleteBox1.Text;
        //    if (vm.foundInTermDic(text))
        //    {
                
        //    List<string> candidateList = vm.getPopulating(text);
        //    AutoCompleteBox1.ItemsSource = candidateList;
        //    AutoCompleteBox1.
        //    }
            
        //}
    }
            
        
}

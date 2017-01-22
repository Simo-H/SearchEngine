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
        private bool IsStem;
        public MainWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
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
            IsStem = (bool)stemmer.IsChecked;
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
            IsStem = (bool)stemmer.IsChecked;
            vm.LoadDictionaries();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void GO_onClick(object sender, RoutedEventArgs e)
        {
            if ((bool)stemmer.IsChecked!=IsStem)
            {
                System.Windows.MessageBox.Show("The loaded dictionary does not match to the query requested, please load the correct dictionary.");
                return;
            }
            if (Auto.Text.Length != 0)
            {
                string check = Auto.Text.Trim(new char[] { ' ' });
                if (check.Length != 0)
                {
                    results = new ResultsAllQueries(ref vm);
                    vm.Search();
                    results.ShowDialog();

                }
                else
                {
                    System.Windows.MessageBox.Show("Query search line is empty. Please enter a query.");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Query search line is empty. Please enter a query.");
            }
        }

        private void optimize_Click(object sender, RoutedEventArgs e)
        {
            //results.ShowDialog(); 
            vm.Optimize();
        }


        private void GOQueryFile_onClick(object sender, RoutedEventArgs e)
        {
            if ((bool)stemmer.IsChecked != IsStem)
            {
                System.Windows.MessageBox.Show("The loaded dictionary does not match to the query requested, please load the correct dictionary.");
                return;
            }
            if (File.Exists(filePath.Text) && Directory.Exists(filePath4.Text))
            {
                results = new ResultsAllQueries(ref vm);
                vm.SearchQueryFile();
                results.ShowDialog();

            }
            else
            {
                System.Windows.MessageBox.Show("Invalid path. please check the queries file path and the save-to path");
            }

        }

        private void browseFile(object sender, RoutedEventArgs e)
        {

            vm.browseFile();
        }

        private void ShowResults(object sender, RoutedEventArgs e)
        {
            if (results != null)
            {
                results = new ResultsAllQueries(ref vm);
                results.ShowDialog();
            }
            else
            {
                System.Windows.MessageBox.Show("No results yet. please enter a query.");
            }
        }

        private void ResetResults(object sender, RoutedEventArgs e)
        {
            vm.resetQueries();

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

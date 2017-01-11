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
            vm.Search();
        }
    }

        
}

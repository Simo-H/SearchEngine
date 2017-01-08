using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SearchEngine
{
    /// <summary>
    /// Interaction logic for ShowDictionary.xaml
    /// </summary>
    public partial class ShowDictionary : Window
    {
        public class listItem
        {
            private string term;

            public string Term
            {
                get { return term; }
                set { term = value; }
            }
            private int cf;

            public int CF
            {
                get { return cf; }
                set { cf = value; }
            }
            

        }
        public ShowDictionary(Dictionary<string,int> dic)
        {
            InitializeComponent();
            List<string> list = dic.Keys.ToList();
            list.Sort();
            foreach (var item in list)
            {
                listView.Items.Add(new listItem { Term = item, CF = dic[item] });
            }
            

        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ok(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

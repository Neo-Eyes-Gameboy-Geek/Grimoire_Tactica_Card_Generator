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
using System.Windows.Shapes;

namespace Grimoire_Tactica_Card_Generator
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog(string prompt)
        {
            //The input 
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            TBL_Prompt.Text = prompt;
        }

        private void BTN_OK_Click(object sender, RoutedEventArgs e)
        {
            //If they hit ok then they must be done and they didnt cancel
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            //Once everything is drawn give control to the textbox so you can just type a responce
            TBX_Responce.SelectAll();
            TBX_Responce.Focus();
        }
        //Lastly an interface to get the results of the textbox once the dialog returns
        public string Answer()
        {
            return TBX_Responce.Text;
        }
    }
}

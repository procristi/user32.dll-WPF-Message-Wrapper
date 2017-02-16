using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CopyDataExtensions;

namespace WpfReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CopyData copy = null;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Test Receiver";
            this.Loaded += MainWindow_Loaded;

        }


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                copy = new CopyData(this);
                copy.OnDataReceived += copy_OnDataReceived;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        void copy_OnDataReceived(object sender, EventArgs e)
        {
            lst.Items.Add(sender);
        }


    }
}

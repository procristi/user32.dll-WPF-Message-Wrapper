using CopyDataExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
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

namespace WpfSender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CopyData s;
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Test Sender";
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            s = new CopyData(this);
            s.OnDataReceived += s_OnDataReceived;
        }

        void s_OnDataReceived(object sender, EventArgs e)
        {
            MessageBox.Show((string)sender);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CopyData s = new CopyData(this, false);
            CopyStatus status = s.SendMessage("Test message 1234567890 1234567890 1234567890 1234567890", "Test Receiver");
            status = s.SendMessage("Test message A", "SpeechCristi");

        }
    }


}

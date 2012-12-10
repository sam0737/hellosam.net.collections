using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Hellosam.Net.Collections.Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConcurrentObservableDictionary<string, string> dict;
        private const int ThreadCount = 5;
        private const int ItemCount = 10000;

        public MainWindow()
        {
            dict = new ConcurrentObservableDictionary<string, string>();
            InitializeComponent();
            listTarget.ItemsSource = dict;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            dict.Clear();

            var threads = new List<Thread>();
            for (int i = 0; i < ThreadCount; i++)
            {
                var t = new Thread(Add);
                threads.Add(t);
                t.Start("T" + i);
            }

            var a = Environment.TickCount;
            new Thread(delegate(object o)
            {
                foreach (var t in threads)
                {
                    t.Join();
                }

                this.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    (Action)(delegate
                    {
                        var b = Environment.TickCount;
                        textStatus.Text = string.Format("Done in {0}", (b - a));
                    }));
            }).Start();
        }

        void Add(object o)
        {
            for (int i = 0; i < ItemCount; i++)
                dict.Add((string)o + '-' + i, "1");
            for (int i = 0; i < ItemCount; i += 3)
                dict.Remove((string)o + '-' + i);
        }
    }
}

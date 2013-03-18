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
using System.Windows.Shapes;using System.Windows.Threading;

namespace Hellosam.Net.Collections.Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class MainWindowContext
        {
            public ThreadSafeObservableDictionary<string, string> Dictionary { get; set; }
        }

        private const int ThreadCount = 5;
        private const int ItemCount = 10000;

        private Random random = new Random();
        private ThreadSafeObservableDictionary<string, string> dict;

        public MainWindow()
        {
            dict = new ThreadSafeObservableDictionary<string, string>();
            this.DataContext = new MainWindowContext()
                {
                    Dictionary = dict
                };
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            dict.Clear();

            var threads = new List<Thread>();
            for (int i = 0; i < ThreadCount; i++)
            {
                var t = new Thread(Add);
                t.Name = "T" + i;
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
            // Mass Create, then mass removal.
            var seq = Enumerable.Range(0, ItemCount).Shuffle(random).ToArray();
            foreach (var i in seq)
                dict.Add(string.Format("{0}-{1:00000}", o, i), "1");
            foreach (var i in  Enumerable.Range(0, seq.Length - 3))
            {
                if (!dict.Remove(string.Format("{0}-{1:00000}", o, i)))
                    throw new ApplicationException("cannot remove this");
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            dict.Clear();
        }
    }

    public static class Util
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            T[] elements = source.ToArray();
            for (int i = elements.Length - 1; i >= 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                // ... except we don't really need to swap it fully, as we can
                // return it immediately, and afterwards it's irrelevant.
                int swapIndex = rng.Next(i + 1);
                yield return elements[swapIndex];
                elements[swapIndex] = elements[i];
            }
        }
    }
}

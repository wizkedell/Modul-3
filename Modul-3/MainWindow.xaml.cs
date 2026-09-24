using Modul_3.ViewModels;
using System.Windows;

namespace Modul_3
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
        }
    }
}
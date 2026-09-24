using Modul_2._0.ViewModels;
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

namespace Modul_2._0.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для P_01_4.xaml
    /// </summary>
    public partial class P_01_4 : Window
    {
        public P_01_4()
        {
            InitializeComponent();
            Initiz();

        }
        private void Initiz()
        {
            if (DataContext is SecondViewModel vm)
            {
                vm.Initiz();

            }
        }
    }
}

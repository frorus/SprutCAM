using MahApps.Metro.Controls;
using SprutCAM.ViewModel;

namespace SprutCAM
{
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
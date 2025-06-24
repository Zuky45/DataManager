using DataManager.CentralManager;
using System.Windows;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for ClearOption.xaml
    /// </summary>
    public partial class ClearOption : Window
    {
        public bool IsModel { get; set; } = false;
        public bool IsData { get; set; } = false;
        public bool IsBoth { get; set; } = false;
        private readonly Manager _manager = Manager.Instance;
        public ClearOption()
        {
            InitializeComponent();
        }
        private void ClearBoth_Click(object sender, RoutedEventArgs e)
        {
            if (_manager.SelectedModel == null || _manager.SelectedData == null)
            {
                MessageBox.Show("No data to clear.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show("Are you sure you want to clear all data?", "Clear Data", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this.DialogResult = true;
                IsBoth = true;
                this.Close();
            }
        }
        private void ClearModel_Click(object sender, RoutedEventArgs e)
        {
            if (_manager.SelectedModel == null)
            {
                MessageBox.Show("No model to clear.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show("Are you sure you want to clear the model?", "Clear Model", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this.DialogResult = true;
                IsModel = true;
                this.Close();
            }
        }
        private void ClearData_Click(object sender, RoutedEventArgs e)
        {
            if (_manager.SelectedData == null)
            {
                MessageBox.Show("No dataset to clear.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (MessageBox.Show("Are you sure you want to clear the dataset?", "Clear Dataset", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this.DialogResult = true;
                IsData = true;
                this.Close();
            }
        }





    }
}

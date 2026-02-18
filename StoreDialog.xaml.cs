using System.Windows;
using System.Windows.Controls;

namespace LancelotWPF
{
    public partial class StoreDialog : Window
    {
        public StoreDialog() { InitializeComponent(); }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbName.Text) || string.IsNullOrWhiteSpace(TbCity.Text) || CbType.SelectedItem == null)
            {
                MessageBox.Show("Заполните название, город и тип.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var type = ((ComboBoxItem)CbType.SelectedItem).Content.ToString();
            DB.Execute("INSERT INTO Stores(Name,City,Address,StoreType) VALUES(@n,@ci,@a,@t)",
                ("@n", TbName.Text), ("@ci", TbCity.Text), ("@a", TbAddress.Text), ("@t", type));
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

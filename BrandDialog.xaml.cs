using System.Windows;
using System.Windows.Controls;

namespace LancelotWPF
{
    public partial class BrandDialog : Window
    {
        public BrandDialog() { InitializeComponent(); }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbName.Text) || CbSegment.SelectedItem == null)
            {
                MessageBox.Show("Заполните название и сегмент.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var seg = ((ComboBoxItem)CbSegment.SelectedItem).Content.ToString();
            DB.Execute("INSERT INTO Brands(Name,Segment,Description) VALUES(@n,@s,@d)",
                ("@n", TbName.Text), ("@s", seg), ("@d", TbDesc.Text));
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

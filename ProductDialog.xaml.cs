using System;
using System.Data;
using System.Linq;
using System.Windows;

namespace LancelotWPF
{
    public partial class ProductDialog : Window
    {
        private readonly int? _productId;

        public ProductDialog(int? productId = null)
        {
            InitializeComponent();
            _productId = productId;
            if (productId.HasValue) TbTitle.Text = "Редактировать товар";
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Fill combos
            CbBrand.ItemsSource    = DB.Query("SELECT BrandId,Name FROM Brands ORDER BY Name").DefaultView;
            CbCategory.ItemsSource = DB.Query("SELECT CategoryId,Name FROM Categories ORDER BY Name").DefaultView;

            if (_productId.HasValue)
            {
                var dt = DB.Query("SELECT * FROM Products WHERE ProductId=@id", ("@id", _productId.Value));
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                TbArticle.Text  = r["Article"].ToString();
                TbName.Text     = r["Name"].ToString();
                TbSize.Text     = r["Size"].ToString();
                TbColor.Text    = r["Color"].ToString();
                TbMaterial.Text = r["Material"].ToString();
                TbPrice.Text    = r["Price"].ToString();
                TbStock.Text    = r["Stock"].ToString();

                CbBrand.SelectedValue    = r["BrandId"];
                CbCategory.SelectedValue = r["CategoryId"];
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbArticle.Text) ||
                string.IsNullOrWhiteSpace(TbName.Text)    ||
                CbBrand.SelectedValue == null              ||
                CbCategory.SelectedValue == null)
            {
                MessageBox.Show("Заполните обязательные поля: Артикул, Название, Бренд, Категория.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TbPrice.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal price))
            {
                MessageBox.Show("Укажите корректную цену.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(TbStock.Text, out int stock);

            if (_productId.HasValue)
            {
                DB.Execute(@"UPDATE Products SET
                    Article=@a, Name=@n, BrandId=@b, CategoryId=@c,
                    Size=@sz, Color=@col, Material=@mat, Price=@p, Stock=@st
                    WHERE ProductId=@id",
                    ("@a",  TbArticle.Text), ("@n",  TbName.Text),
                    ("@b",  CbBrand.SelectedValue), ("@c",  CbCategory.SelectedValue),
                    ("@sz", TbSize.Text),    ("@col", TbColor.Text),
                    ("@mat",TbMaterial.Text),("@p",  price),
                    ("@st", stock),          ("@id",  _productId.Value));
            }
            else
            {
                DB.Execute(@"INSERT INTO Products
                    (Article,Name,BrandId,CategoryId,Size,Color,Material,Price,Stock)
                    VALUES(@a,@n,@b,@c,@sz,@col,@mat,@p,@st)",
                    ("@a",  TbArticle.Text), ("@n",  TbName.Text),
                    ("@b",  CbBrand.SelectedValue), ("@c",  CbCategory.SelectedValue),
                    ("@sz", TbSize.Text),    ("@col", TbColor.Text),
                    ("@mat",TbMaterial.Text),("@p",  price),
                    ("@st", stock));
            }
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

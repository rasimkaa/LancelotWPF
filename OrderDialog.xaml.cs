using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LancelotWPF
{
    public record OrderItemRow(int ProductId, string Label, int Quantity, decimal Price)
    {
        public string Total => $"{Quantity * Price:N0} руб.";
        public string PriceStr => $"{Price:N0} руб.";
    }

    public partial class OrderDialog : Window
    {
        private readonly ObservableCollection<OrderItemRow> _items = new();

        public OrderDialog()
        {
            InitializeComponent();
            ItemsGrid.ItemsSource = _items;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Магазины
            var stores = DB.Query("SELECT StoreId, Name FROM Stores ORDER BY Name");
            CbStore.ItemsSource = stores.DefaultView;
            CbStore.DisplayMemberPath = "Name";
            CbStore.SelectedValuePath = "StoreId";

            // Товары
            var products = DB.Query(@"SELECT ProductId, 
        Article+' — '+Name+' ('+FORMAT(Price,'N0')+' руб.)' AS Label, 
        Price FROM Products ORDER BY Article");
            CbProduct.ItemsSource = products.DefaultView;
            CbProduct.DisplayMemberPath = "Label";
            CbProduct.SelectedValuePath = "ProductId";
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (CbProduct.SelectedItem is not System.Data.DataRowView row) return;
            if (!int.TryParse(TbQty.Text, out int qty) || qty < 1) qty = 1;

            int pid = Convert.ToInt32(row["ProductId"]);
            decimal price = Convert.ToDecimal(row["Price"]);
            string label = row["Label"].ToString() ?? "";

            bool found = false;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ProductId == pid)
                {
                    _items[i] = _items[i] with { Quantity = _items[i].Quantity + qty };
                    found = true; break;
                }
            }
            if (!found) _items.Add(new OrderItemRow(pid, label, qty, price));
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            decimal total = 0;
            foreach (var it in _items) total += it.Quantity * it.Price;
            TbTotal.Text = $"{total:N0} руб.";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbStore.SelectedValue == null) { MessageBox.Show("Выберите магазин."); return; }
            if (_items.Count == 0) { MessageBox.Show("Добавьте хотя бы один товар."); return; }

            decimal total = 0;
            foreach (var it in _items) total += it.Quantity * it.Price;

            var status = ((ComboBoxItem)CbStatus.SelectedItem).Content.ToString();
            int storeId = Convert.ToInt32(CbStore.SelectedValue);

            var orderId = DB.Scalar(@"
        INSERT INTO Orders(StoreId,Status,TotalAmount)
        VALUES(@sid,@st,@tot);
        SELECT SCOPE_IDENTITY();",
                ("@sid", storeId), ("@st", status), ("@tot", total));

            int oid = Convert.ToInt32(orderId);
            foreach (var it in _items)
                DB.Execute(@"INSERT INTO OrderItems(OrderId,ProductId,Quantity,UnitPrice)
                     VALUES(@o,@p,@q,@u)",
                    ("@o", oid), ("@p", it.ProductId), ("@q", it.Quantity), ("@u", it.Price));

            DialogResult = true;
        }   

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

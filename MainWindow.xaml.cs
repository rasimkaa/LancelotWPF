using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LancelotWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentRendered += (_, _) => LoadDashboard();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void ShowPage(UIElement page, string title)
        {
            if (PageDash != null) PageDash.Visibility = Visibility.Collapsed;
            if (PageProducts != null) PageProducts.Visibility = Visibility.Collapsed;
            if (PageBrands != null) PageBrands.Visibility = Visibility.Collapsed;
            if (PageStores != null) PageStores.Visibility = Visibility.Collapsed;
            if (PageOrders != null) PageOrders.Visibility = Visibility.Collapsed;
            if (page != null) page.Visibility = Visibility.Visible;
            if (TbPageTitle != null) TbPageTitle.Text = title;
        }

        private void NavDash_Checked(object s, RoutedEventArgs e) { ShowPage(PageDash, "Дашборд"); LoadDashboard(); }
        private void NavProducts_Checked(object s, RoutedEventArgs e) { ShowPage(PageProducts, "Товары"); LoadProducts(); }
        private void NavBrands_Checked(object s, RoutedEventArgs e) { ShowPage(PageBrands, "Бренды"); LoadBrands(); }
        private void NavStores_Checked(object s, RoutedEventArgs e) { ShowPage(PageStores, "Магазины"); LoadStores(); }
        private void NavOrders_Checked(object s, RoutedEventArgs e) { ShowPage(PageOrders, "Заказы"); LoadOrders(); }

        private void LoadDashboard()
        {
            try
            {
                if (KpiProducts != null)
                    KpiProducts.Text = DB.Scalar("SELECT COUNT(*) FROM Products")?.ToString() ?? "—";
                if (KpiStores != null)
                    KpiStores.Text = DB.Scalar("SELECT COUNT(*) FROM Stores")?.ToString() ?? "—";
                if (KpiOrders != null)
                    KpiOrders.Text = DB.Scalar("SELECT COUNT(*) FROM Orders")?.ToString() ?? "—";
                if (KpiRevenue != null)
                {
                    var rev = DB.Scalar("SELECT ISNULL(SUM(TotalAmount),0) FROM Orders");
                    KpiRevenue.Text = $"{Convert.ToDecimal(rev):N0}";
                }
                if (DashBrandsGrid != null)
                    DashBrandsGrid.ItemsSource = DB.Query(
                        "SELECT Name AS [Бренд], Segment AS [Сегмент], Description AS [Описание] FROM Brands").DefaultView;
                if (DashOrdersGrid != null)
                    DashOrdersGrid.ItemsSource = DB.Query(@"
                        SELECT TOP 8 o.OrderId AS [№], s.Name AS [Магазин],
                               CONVERT(varchar,o.OrderDate,104) AS [Дата],
                               o.Status AS [Статус],
                               FORMAT(o.TotalAmount,'N0')+' руб.' AS [Сумма]
                        FROM Orders o JOIN Stores s ON o.StoreId=s.StoreId
                        ORDER BY o.OrderDate DESC").DefaultView;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void LoadProducts(string search = "")
        {
            try
            {
                var sql = @"
                    SELECT p.ProductId, p.Article, p.Name,
                           b.Name AS Brand, c.Name AS Category,
                           p.Size, p.Color, p.Material,
                           FORMAT(p.Price,'N0') AS Price,
                           p.Stock
                    FROM Products p
                    JOIN Brands b     ON p.BrandId=b.BrandId
                    JOIN Categories c ON p.CategoryId=c.CategoryId
                    WHERE @s='' OR p.Name LIKE @like OR p.Article LIKE @like
                    ORDER BY p.Article";
                ProductsGrid.ItemsSource = DB.Query(sql,
                    ("@s", search), ("@like", $"%{search}%")).DefaultView;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void ProductSearch_Changed(object sender, TextChangedEventArgs e)
            => LoadProducts(TbProductSearch.Text.Trim());

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ProductDialog();
            if (dlg.ShowDialog() == true) LoadProducts();
        }

        private void BtnEditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not DataRowView row) { Info("Выберите товар."); return; }
            int id = Convert.ToInt32(row["ProductId"]);
            var dlg = new ProductDialog(id);
            if (dlg.ShowDialog() == true) LoadProducts();
        }

        private void BtnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not DataRowView row) { Info("Выберите товар."); return; }
            if (Confirm($"Удалить товар «{row["Name"]}»?"))
            {
                DB.Execute("DELETE FROM Products WHERE ProductId=@id", ("@id", row["ProductId"]));
                LoadProducts();
            }
        }

        private void LoadBrands()
        {
            try
            {
                BrandsGrid.ItemsSource = DB.Query(
                    "SELECT BrandId,Name,Segment,Description FROM Brands ORDER BY BrandId").DefaultView;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnAddBrand_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new BrandDialog();
            if (dlg.ShowDialog() == true) LoadBrands();
        }

        private void BtnDeleteBrand_Click(object sender, RoutedEventArgs e)
        {
            if (BrandsGrid.SelectedItem is not DataRowView row) { Info("Выберите бренд."); return; }
            if (Confirm($"Удалить бренд «{row["Name"]}»?\nВсе товары этого бренда тоже будут удалены."))
            {
                DB.Execute("DELETE FROM Products WHERE BrandId=@id", ("@id", row["BrandId"]));
                DB.Execute("DELETE FROM Brands   WHERE BrandId=@id", ("@id", row["BrandId"]));
                LoadBrands();
            }
        }

        private void LoadStores()
        {
            try
            {
                StoresGrid.ItemsSource = DB.Query(
                    "SELECT StoreId,Name,City,Address,StoreType FROM Stores ORDER BY City,Name").DefaultView;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnAddStore_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new StoreDialog();
            if (dlg.ShowDialog() == true) LoadStores();
        }

        private void BtnDeleteStore_Click(object sender, RoutedEventArgs e)
        {
            if (StoresGrid.SelectedItem is not DataRowView row) { Info("Выберите магазин."); return; }
            if (Confirm($"Удалить магазин «{row["Name"]}»?"))
            {
                DB.Execute("DELETE FROM Stores WHERE StoreId=@id", ("@id", row["StoreId"]));
                LoadStores();
            }
        }

        private void LoadOrders()
        {
            try
            {
                OrdersGrid.ItemsSource = DB.Query(@"
                    SELECT o.OrderId, s.Name AS Store,
                           CONVERT(varchar,o.OrderDate,104) AS OrderDate,
                           o.Status,
                           FORMAT(o.TotalAmount,'N0') AS TotalAmount
                    FROM Orders o JOIN Stores s ON o.StoreId=s.StoreId
                    ORDER BY o.OrderDate DESC").DefaultView;
                OrderItemsGrid.ItemsSource = null;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not DataRowView row) return;
            int orderId = Convert.ToInt32(row["OrderId"]);
            OrderItemsGrid.ItemsSource = DB.Query(@"
                SELECT p.Name AS Product, oi.Quantity,
                       FORMAT(oi.UnitPrice,'N0') AS UnitPrice,
                       FORMAT(oi.Quantity*oi.UnitPrice,'N0') AS Total
                FROM OrderItems oi JOIN Products p ON oi.ProductId=p.ProductId
                WHERE oi.OrderId=@id", ("@id", orderId)).DefaultView;
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OrderDialog();
            if (dlg.ShowDialog() == true) LoadOrders();
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not DataRowView row) { Info("Выберите заказ."); return; }
            if (Confirm($"Удалить заказ №{row["OrderId"]}?"))
            {
                DB.Execute("DELETE FROM Orders WHERE OrderId=@id", ("@id", row["OrderId"]));
                LoadOrders();
            }
        }

        private static bool Confirm(string msg) =>
            MessageBox.Show(msg, "Подтверждение", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

        private static void Info(string msg) =>
            MessageBox.Show(msg, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowError(Exception ex) =>
            MessageBox.Show($"Ошибка подключения к БД:\n{ex.Message}\n\nПроверьте строку подключения в DB.cs",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
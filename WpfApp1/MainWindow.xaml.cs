using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Model;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public predpriEntities db = new predpriEntities();

        public MainWindow()
        {
            InitializeComponent();

            dpPurDate.SelectedDate = DateTime.Today;
            dpOrdStart.SelectedDate = DateTime.Today;
            cbOrdStatus.SelectedIndex = 0;
            cbEqSt.SelectedIndex = 0;

            RefreshAll();
        }

        // -------- helpers --------
        private bool TryDec(string s, out decimal v)
        {
            s = (s ?? "").Trim().Replace(',', '.');
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
        }

        private void EnsureStock(int materialId)
        {
            var st = db.MaterialStock.FirstOrDefault(x => x.MaterialId == materialId);
            if (st == null)
            {
                db.MaterialStock.Add(new MaterialStock { MaterialId = materialId, Qty = 0m });
                db.SaveChanges();
            }
        }

        private void RefreshAll()
        {
            // справочники
            dgMat.ItemsSource = db.Material.ToList();
            dgProd.ItemsSource = db.Product.ToList();
            dgEmp.ItemsSource = db.Employee.ToList();
            dgEq.ItemsSource = db.Equipment.ToList();

            // комбики
            cbBomMat.ItemsSource = db.Material.ToList();
            cbPurMat.ItemsSource = db.Material.ToList();
            cbPurSupplier.ItemsSource = db.Supplier.ToList();
            cbOrdClient.ItemsSource = db.Client.ToList();
            cbOrdProd.ItemsSource = db.Product.ToList();

            cbAsOrder.ItemsSource = db.ProductionOrder.ToList();
            cbAsEmp.ItemsSource = db.Employee.ToList();
            cbAsEq.ItemsSource = db.Equipment.ToList();

            // данные
            dgPur.ItemsSource = db.PurchaseItem.ToList();
            dgStock.ItemsSource = db.MaterialStock.ToList();
            dgOrd.ItemsSource = db.ProductionOrder.ToList();
            dgAs.ItemsSource = db.OrderAssignment.ToList();
        }

        // =========================
        // 1) Материалы
        // =========================
        private void DgMat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var m = dgMat.SelectedItem as Material;
            if (m == null) return;
            tbMatName.Text = m.Name;
            tbMatUnit.Text = m.Unit;
            tbMatMin.Text = m.MinQty.ToString(CultureInfo.InvariantCulture);
        }

        private void MatAdd_Click(object sender, RoutedEventArgs e)
        {
            decimal min;
            if (!TryDec(tbMatMin.Text, out min)) { MessageBox.Show("Мин. остаток числом"); return; }

            var m = new Material
            {
                Name = tbMatName.Text.Trim(),
                Unit = tbMatUnit.Text.Trim(),
                MinQty = min
            };

            db.Material.Add(m);
            db.SaveChanges();
            EnsureStock(m.MaterialId);

            RefreshAll();
        }

        private void MatUpd_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgMat.SelectedItem as Material;
            if (sel == null) { MessageBox.Show("Выбери материал"); return; }

            var m = db.Material.First(x => x.MaterialId == sel.MaterialId);

            decimal min;
            if (!TryDec(tbMatMin.Text, out min)) { MessageBox.Show("Мин. остаток числом"); return; }

            m.Name = tbMatName.Text.Trim();
            m.Unit = tbMatUnit.Text.Trim();
            m.MinQty = min;

            db.SaveChanges();
            RefreshAll();
        }

        private void MatDel_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgMat.SelectedItem as Material;
            if (sel == null) { MessageBox.Show("Выбери материал"); return; }

            db.Material.Remove(db.Material.First(x => x.MaterialId == sel.MaterialId));
            db.SaveChanges();
            RefreshAll();
        }

        private void MatRef_Click(object sender, RoutedEventArgs e) { RefreshAll(); }

        // =========================
        // 1) Продукция + BOM
        // =========================
        private void DgProd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var p = dgProd.SelectedItem as Product;
            if (p == null) return;

            tbProdSku.Text = p.Sku;
            tbProdName.Text = p.Name;
            tbProdDesc.Text = p.Description;

            dgBom.ItemsSource = db.BillOfMaterials.Where(x => x.ProductId == p.ProductId).ToList();
        }

        private void ProdAdd_Click(object sender, RoutedEventArgs e)
        {
            db.Product.Add(new Product
            {
                Sku = tbProdSku.Text.Trim(),
                Name = tbProdName.Text.Trim(),
                Description = tbProdDesc.Text
            });
            db.SaveChanges();
            RefreshAll();
        }

        private void ProdUpd_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgProd.SelectedItem as Product;
            if (sel == null) { MessageBox.Show("Выбери продукт"); return; }

            var p = db.Product.First(x => x.ProductId == sel.ProductId);
            p.Sku = tbProdSku.Text.Trim();
            p.Name = tbProdName.Text.Trim();
            p.Description = tbProdDesc.Text;

            db.SaveChanges();
            RefreshAll();
        }

        private void ProdDel_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgProd.SelectedItem as Product;
            if (sel == null) { MessageBox.Show("Выбери продукт"); return; }

            db.Product.Remove(db.Product.First(x => x.ProductId == sel.ProductId));
            db.SaveChanges();
            RefreshAll();
        }

        private void ProdRef_Click(object sender, RoutedEventArgs e) { RefreshAll(); }

        private void BomAdd_Click(object sender, RoutedEventArgs e)
        {
            var p = dgProd.SelectedItem as Product;
            if (p == null) { MessageBox.Show("Выбери продукт слева"); return; }
            if (cbBomMat.SelectedValue == null) { MessageBox.Show("Выбери материал"); return; }

            decimal q;
            if (!TryDec(tbBomQty.Text, out q) || q <= 0) { MessageBox.Show("Кол-во на 1 шт > 0"); return; }

            db.BillOfMaterials.Add(new BillOfMaterials
            {
                ProductId = p.ProductId,
                MaterialId = Convert.ToInt32(cbBomMat.SelectedValue),
                QtyPerUnit = q
            });
            db.SaveChanges();

            dgBom.ItemsSource = db.BillOfMaterials.Where(x => x.ProductId == p.ProductId).ToList();
        }

        private void BomDel_Click(object sender, RoutedEventArgs e)
        {
            var b = dgBom.SelectedItem as BillOfMaterials;
            if (b == null) { MessageBox.Show("Выбери строку BOM"); return; }

            db.BillOfMaterials.Remove(db.BillOfMaterials.First(x => x.BillOfMaterialsId == b.BillOfMaterialsId));
            db.SaveChanges();

            var p = dgProd.SelectedItem as Product;
            if (p != null)
                dgBom.ItemsSource = db.BillOfMaterials.Where(x => x.ProductId == p.ProductId).ToList();
        }

        private void BomRef_Click(object sender, RoutedEventArgs e)
        {
            var p = dgProd.SelectedItem as Product;
            if (p != null)
                dgBom.ItemsSource = db.BillOfMaterials.Where(x => x.ProductId == p.ProductId).ToList();
        }

        // =========================
        // 2) Поставки + склад (автопересчёт при приходе)
        // =========================
        private void PurAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cbPurSupplier.SelectedValue == null || cbPurMat.SelectedValue == null)
            {
                MessageBox.Show("Выбери поставщика и материал");
                return;
            }

            decimal qty, price;
            if (!TryDec(tbPurQty.Text, out qty) || qty <= 0) { MessageBox.Show("Qty > 0"); return; }
            if (!TryDec(tbPurPrice.Text, out price) || price < 0) { MessageBox.Show("Price >= 0"); return; }

            DateTime date = dpPurDate.SelectedDate.HasValue ? dpPurDate.SelectedDate.Value : DateTime.Today;
            int supplierId = Convert.ToInt32(cbPurSupplier.SelectedValue);
            int matId = Convert.ToInt32(cbPurMat.SelectedValue);

            // создаём Purchase (шапку) и строку PurchaseItem
            var pur = new Purchase { SupplierId = supplierId, PurchaseDate = date };
            db.Purchase.Add(pur);
            db.SaveChanges();

            db.PurchaseItem.Add(new PurchaseItem
            {
                PurchaseId = pur.PurchaseId,
                MaterialId = matId,
                Qty = qty,
                Price = price
            });

            // склад +qty
            EnsureStock(matId);
            db.MaterialStock.First(x => x.MaterialId == matId).Qty += qty;

            db.SaveChanges();
            RefreshAll();
        }

        private void StockRef_Click(object sender, RoutedEventArgs e)
        {
            dgStock.ItemsSource = db.MaterialStock.ToList();
        }

        // =========================
        // 3) Заказы + статус + авторасчёт материалов
        // =========================
        private void DgOrd_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var o = dgOrd.SelectedItem as ProductionOrder;
            if (o == null) return;

            dgOrdMat.ItemsSource = db.OrderMaterial.Where(x => x.ProductionOrderId == o.ProductionOrderId).ToList();
        }

        private void OrdAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cbOrdClient.SelectedValue == null || cbOrdProd.SelectedValue == null)
            {
                MessageBox.Show("Выбери клиента и продукцию");
                return;
            }

            decimal qty;
            if (!TryDec(tbOrdQty.Text, out qty) || qty <= 0) { MessageBox.Show("Qty > 0"); return; }

            var o = new ProductionOrder
            {
                ClientId = Convert.ToInt32(cbOrdClient.SelectedValue),
                ProductId = Convert.ToInt32(cbOrdProd.SelectedValue),
                Qty = qty,
                DateStart = dpOrdStart.SelectedDate.HasValue ? dpOrdStart.SelectedDate.Value : DateTime.Today,
                DateEnd = dpOrdEnd.SelectedDate,
                Status = ((ComboBoxItem)cbOrdStatus.SelectedItem).Content.ToString()
            };

            db.ProductionOrder.Add(o);
            db.SaveChanges();

            RefreshAll();
        }

        private void OrdToWork_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgOrd.SelectedItem as ProductionOrder;
            if (sel == null) { MessageBox.Show("Выбери заказ"); return; }

            var o = db.ProductionOrder.First(x => x.ProductionOrderId == sel.ProductionOrderId);
            o.Status = "in_work";
            db.SaveChanges();
            RefreshAll();
        }

        // авторасчёт материалов по заказу = BOM * Qty
        private void OrdCalc_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgOrd.SelectedItem as ProductionOrder;
            if (sel == null) { MessageBox.Show("Выбери заказ"); return; }

            int orderId = sel.ProductionOrderId;
            var order = db.ProductionOrder.First(x => x.ProductionOrderId == orderId);

            // удалить старые строки
            var old = db.OrderMaterial.Where(x => x.ProductionOrderId == orderId).ToList();
            for (int i = 0; i < old.Count; i++) db.OrderMaterial.Remove(old[i]);
            db.SaveChanges();

            // добавить новые по BOM
            var bom = db.BillOfMaterials.Where(x => x.ProductId == order.ProductId).ToList();
            for (int i = 0; i < bom.Count; i++)
            {
                db.OrderMaterial.Add(new OrderMaterial
                {
                    ProductionOrderId = orderId,
                    MaterialId = bom[i].MaterialId,
                    RequiredQty = bom[i].QtyPerUnit * order.Qty,
                    IssuedQty = 0m
                });
            }
            db.SaveChanges();

            dgOrdMat.ItemsSource = db.OrderMaterial.Where(x => x.ProductionOrderId == orderId).ToList();
        }

        // списание: уменьшить склад на RequiredQty
        private void OrdIssue_Click(object sender, RoutedEventArgs e)
        {
            var sel = dgOrd.SelectedItem as ProductionOrder;
            if (sel == null) { MessageBox.Show("Выбери заказ"); return; }

            int orderId = sel.ProductionOrderId;

            var mats = db.OrderMaterial.Where(x => x.ProductionOrderId == orderId).ToList();
            if (mats.Count == 0) { MessageBox.Show("Сначала нажми 'Рассчитать материалы'"); return; }

            // проверка остатков
            for (int i = 0; i < mats.Count; i++)
            {
                EnsureStock(mats[i].MaterialId);
                var stock = db.MaterialStock.First(x => x.MaterialId == mats[i].MaterialId);
                if (stock.Qty < mats[i].RequiredQty)
                {
                    MessageBox.Show("Не хватает материала на складе (MaterialId=" + mats[i].MaterialId + ")");
                    return;
                }
            }

            // списание
            for (int i = 0; i < mats.Count; i++)
            {
                var stock = db.MaterialStock.First(x => x.MaterialId == mats[i].MaterialId);
                stock.Qty -= mats[i].RequiredQty;
                mats[i].IssuedQty = mats[i].RequiredQty;
            }

            db.SaveChanges();
            RefreshAll();
        }

        // =========================
        // 4) Сотрудники + оборудование + назначения (минимум)
        // =========================
        private void EmpAdd_Click(object sender, RoutedEventArgs e)
        {
            // ФИО одной строкой -> просто в LastName, чтобы не заморачиваться
            decimal rate;
            if (!TryDec(tbEmpRate.Text, out rate)) { MessageBox.Show("Ставка числом"); return; }

            db.Employee.Add(new Employee
            {
                LastName = tbEmpFio.Text.Trim(),
                FirstName = "",
                MiddleName = "",
                Position = tbEmpPos.Text.Trim(),
                Department = tbEmpDept.Text.Trim(),
                Rate = rate
            });

            db.SaveChanges();
            RefreshAll();
        }

        private void EqAdd_Click(object sender, RoutedEventArgs e)
        {
            db.Equipment.Add(new Equipment
            {
                Name = tbEqName.Text.Trim(),
                InventoryNo = tbEqInv.Text.Trim(),
                Status = ((ComboBoxItem)cbEqSt.SelectedItem).Content.ToString()
            });

            db.SaveChanges();
            RefreshAll();
        }

        private void AsEmp_Click(object sender, RoutedEventArgs e)
        {
            if (cbAsOrder.SelectedValue == null || cbAsEmp.SelectedValue == null)
            {
                MessageBox.Show("Выбери заказ и сотрудника");
                return;
            }

            db.OrderAssignment.Add(new OrderAssignment
            {
                ProductionOrderId = Convert.ToInt32(cbAsOrder.SelectedValue),
                EmployeeId = Convert.ToInt32(cbAsEmp.SelectedValue),
                EquipmentId = null
            });

            db.SaveChanges();
            RefreshAll();
        }

        private void AsEq_Click(object sender, RoutedEventArgs e)
        {
            if (cbAsOrder.SelectedValue == null || cbAsEq.SelectedValue == null)
            {
                MessageBox.Show("Выбери заказ и оборудование");
                return;
            }

            db.OrderAssignment.Add(new OrderAssignment
            {
                ProductionOrderId = Convert.ToInt32(cbAsOrder.SelectedValue),
                EmployeeId = null,
                EquipmentId = Convert.ToInt32(cbAsEq.SelectedValue)
            });

            db.SaveChanges();
            RefreshAll();
        }
    }
}

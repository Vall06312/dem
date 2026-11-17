using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace dem
{
    public partial class MainWindow : Window
    {
        DemEntities1 db = new DemEntities1();
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
        bool admin, manager, orders;
        int pid, oid;

        public MainWindow(string role, string fio)
        {
            InitializeComponent();
            admin = role == "Администратор"; manager = role == "Менеджер";
            tbUser.Text = role + (fio == "" ? "" : ": " + fio);
            panelFilters.Visibility = panelButtons.Visibility = V(admin || manager);
            btnAdd.Visibility = btnEdit.Visibility = btnDel.Visibility = V(admin);
            var s = db.Tovar.Select(x => x.postavshik).Distinct().ToList(); s.Insert(0, "Все");
            cbSup.ItemsSource = s;
            cbSup.SelectedIndex = cbSk.SelectedIndex = cbSort.SelectedIndex = 0;
            var f = Directory.GetFiles(dir, "*.jpg").Select(Path.GetFileName).ToList(); f.Insert(0, "picture.png");
            pFoto.ItemsSource = f;
            oPoint.ItemsSource = db.Пункты_выдачи_import.ToList().Select(x => new { x.Id, a = x.index + ", " + x.city + ", " + x.street + ", " + x.house }).ToList();
            oPoint.DisplayMemberPath = "a"; oPoint.SelectedValuePath = "Id";
            oStatus.ItemsSource = new[] { "Новый", "Завершен" };
            Refresh();
        }

        Visibility V(bool b) { return b ? Visibility.Visible : Visibility.Collapsed; }

        void Refresh()
        {
            db = new DemEntities1();
            if (orders)
            {
                var pts = db.Пункты_выдачи_import.ToList().ToDictionary(x => x.Id, x => x.index + ", " + x.city + ", " + x.street + ", " + x.house);
                var l = db.Заказ_import.ToList().Select(x => new
                {
                    x.Id,
                    x.Nomber_zakaz,
                    x.Artikyl_zakaza,
                    x.FIO_klienta,
                    x.Code,
                    x.Status,
                    d1 = x.Date_zakaza,
                    d2 = x.Date_dostavki,
                    point = x.Adress != null && pts.ContainsKey(x.Adress.Value) ? pts[x.Adress.Value] : ""
                }).ToList();
                gridO.ItemsSource = l; tbStatus.Text = "Заказов: " + l.Count; return;
            }
            var q = db.Tovar.ToList();
            string s = tbSearch.Text.Trim().ToLower();
            if (s != "") q = q.Where(x => (x.artikul + " " + x.Name_Tovara + " " + x.opisanie + " " + x.postavshik + " " + x.proisvoditel + " " + x.kategorii_tovara).ToLower().Contains(s)).ToList();
            if (cbSup.SelectedIndex > 0) q = q.Where(x => x.postavshik == (string)cbSup.SelectedItem).ToList();
            if (cbSk.SelectedIndex == 1) q = q.Where(x => x.skidka < 10).ToList();
            if (cbSk.SelectedIndex == 2) q = q.Where(x => x.skidka >= 10 && x.skidka < 15).ToList();
            if (cbSk.SelectedIndex == 3) q = q.Where(x => x.skidka >= 15).ToList();
            if (cbSort.SelectedIndex == 1) q = q.OrderBy(x => x.price).ToList();
            if (cbSort.SelectedIndex == 2) q = q.OrderByDescending(x => x.price).ToList();
            if (cbSort.SelectedIndex == 3) q = q.OrderBy(x => x.skidka).ToList();
            if (cbSort.SelectedIndex == 4) q = q.OrderByDescending(x => x.skidka).ToList();
            var list = q.Select(x => new
            {
                x.Id,
                x.artikul,
                x.Name_Tovara,
                x.kategorii_tovara,
                x.proisvoditel,
                x.postavshik,
                x.opisanie,
                price = Convert.ToDecimal(x.price),
                skidka = Convert.ToInt32(x.skidka),
                Itog = Math.Round(Convert.ToDecimal(x.price) * (100 - Convert.ToInt32(x.skidka)) / 100, 2),
                qty = Convert.ToInt32(x.qty),
                Foto = Path.Combine(dir, File.Exists(Path.Combine(dir, x.Foto ?? "")) ? x.Foto : "picture.png"),
                Fon = Convert.ToInt32(x.skidka) > 15 ? "#2E8B57" : "#FFFFFF",
                Tekst = Convert.ToInt32(x.skidka) > 15 ? "#FFFFFF" : "#000000"
            }).ToList();
            gridP.ItemsSource = list; tbStatus.Text = "Товаров: " + list.Count;
        }

        private void Filter(object sender, RoutedEventArgs e) { Refresh(); }
        private void Cancel(object sender, RoutedEventArgs e) { panelProduct.Visibility = panelOrder.Visibility = Visibility.Collapsed; }
        private void Exit(object sender, RoutedEventArgs e) { new LoginWindow().Show(); Close(); }

        private void Toggle(object sender, RoutedEventArgs e)
        {
            orders = !orders;
            btnOrders.Content = orders ? "Товары" : "Заказы";
            gridP.Visibility = V(!orders); gridO.Visibility = V(orders);
            panelFilters.Visibility = V(!orders && (admin || manager));
            Cancel(sender, e); Refresh();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            if (orders)
            {
                oid = 0; oNum.Text = oComp.Text = oD1.Text = oD2.Text = oClient.Text = oCode.Text = "";
                oPoint.SelectedIndex = -1; oStatus.Text = "Новый"; panelOrder.Visibility = Visibility.Visible;
            }
            else
            {
                pid = 0; pArt.Text = pName.Text = pMan.Text = pSup.Text = pCat.Text = pDesc.Text = "";
                pUnit.Text = "шт."; pPrice.Text = pSkid.Text = pQty.Text = "0"; pFoto.SelectedIndex = 0;
                panelProduct.Visibility = Visibility.Visible;
            }
        }

        private void Edit(object sender, RoutedEventArgs e)
        {
            if (orders)
            {
                if (gridO.SelectedValue == null) { MessageBox.Show("Выберите заказ!"); return; }
                oid = (int)gridO.SelectedValue;
                var o = db.Заказ_import.Find(oid);
                oNum.Text = "" + o.Nomber_zakaz; oComp.Text = o.Artikyl_zakaza; oD1.Text = o.Date_zakaza; oD2.Text = o.Date_dostavki;
                oPoint.SelectedValue = o.Adress; oClient.Text = o.FIO_klienta; oCode.Text = "" + o.Code; oStatus.Text = o.Status;
                panelOrder.Visibility = Visibility.Visible;
            }
            else
            {
                if (gridP.SelectedValue == null) { MessageBox.Show("Выберите товар!"); return; }
                pid = (int)gridP.SelectedValue;
                var p = db.Tovar.Find(pid);
                pArt.Text = p.artikul; pName.Text = p.Name_Tovara; pUnit.Text = p.edenitsa; pPrice.Text = "" + p.price; pSkid.Text = "" + p.skidka;
                pMan.Text = p.proisvoditel; pSup.Text = p.postavshik; pCat.Text = p.kategorii_tovara; pQty.Text = "" + p.qty; pDesc.Text = p.opisanie;
                pFoto.SelectedItem = p.Foto; if (pFoto.SelectedIndex < 0) pFoto.SelectedIndex = 0;
                panelProduct.Visibility = Visibility.Visible;
            }
        }

        private void Del(object sender, RoutedEventArgs e)
        {
            if ((orders ? gridO.SelectedValue : gridP.SelectedValue) == null) { MessageBox.Show("Выберите запись!"); return; }
            if (MessageBox.Show("Удалить запись?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (orders) db.Заказ_import.Remove(db.Заказ_import.Find((int)gridO.SelectedValue));
            else
            {
                var p = db.Tovar.Find((int)gridP.SelectedValue);
                if (db.Заказ_import.ToList().Any(o => o.Artikyl_zakaza != null && o.Artikyl_zakaza.Contains(p.artikul)))
                { MessageBox.Show("Товар есть в заказах — удаление запрещено!"); return; }
                db.Tovar.Remove(p);
            }
            db.SaveChanges(); Refresh();
        }

        private void SaveP(object sender, RoutedEventArgs e)
        {
            decimal price; int sk, qty;
            if (pArt.Text.Trim() == "" || pName.Text.Trim() == "" || !decimal.TryParse(pPrice.Text.Replace('.', ','), out price) || price < 0
                || !int.TryParse(pSkid.Text, out sk) || sk < 0 || sk > 100 || !int.TryParse(pQty.Text, out qty) || qty < 0)
            { MessageBox.Show("Проверьте поля!"); return; }
            var p = pid > 0 ? db.Tovar.Find(pid) : new Tovar();
            if (pid == 0) { p.Id = (db.Tovar.Max(x => (int?)x.Id) ?? 0) + 1; db.Tovar.Add(p); }
            p.artikul = pArt.Text.Trim(); p.Name_Tovara = pName.Text.Trim(); p.edenitsa = pUnit.Text.Trim();
            p.price = price; p.skidka = sk; p.qty = qty;
            p.proisvoditel = pMan.Text.Trim(); p.postavshik = pSup.Text.Trim(); p.kategorii_tovara = pCat.Text.Trim();
            p.opisanie = pDesc.Text.Trim(); p.Foto = (string)pFoto.SelectedItem;
            db.SaveChanges(); Cancel(sender, e); Refresh();
        }

        private void SaveO(object sender, RoutedEventArgs e)
        {
            int num, code;
            if (!int.TryParse(oNum.Text, out num) || !int.TryParse(oCode.Text, out code) || oPoint.SelectedValue == null)
            { MessageBox.Show("Проверьте поля!"); return; }
            var o = oid > 0 ? db.Заказ_import.Find(oid) : new Заказ_import();
            if (oid == 0) { o.Id = (db.Заказ_import.Max(x => (int?)x.Id) ?? 0) + 1; db.Заказ_import.Add(o); }
            o.Nomber_zakaz = num; o.Artikyl_zakaza = oComp.Text.Trim(); o.Date_zakaza = oD1.Text.Trim(); o.Date_dostavki = oD2.Text.Trim();
            o.Adress = (int?)oPoint.SelectedValue; o.FIO_klienta = oClient.Text.Trim(); o.Code = code; o.Status = oStatus.Text.Trim();
            db.SaveChanges(); Cancel(sender, e); Refresh();
        }
    }
}
using System.Linq;
using System.Windows;

namespace dem
{
    public partial class LoginWindow : Window
    {
        DemEntities1 db = new DemEntities1();
        public LoginWindow() { InitializeComponent(); }

        private void Login(object sender, RoutedEventArgs e)
        {
            var u = db.user_import.FirstOrDefault(x => x.Login == tbLogin.Text && x.Password == tbPassword.Password);
            if (u == null) { MessageBox.Show("Неверный логин или пароль!"); return; }
            new MainWindow(u.Role, u.FIO).Show(); Close();
        }
        private void Guest(object sender, RoutedEventArgs e) { new MainWindow("Гость", "").Show(); Close(); }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
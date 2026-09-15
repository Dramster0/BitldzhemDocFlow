using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BitldzhemDocFlow.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryLogin();
        }

        private void TryLogin()
        {
            string login = LoginBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var user = db.Users.Include(u => u.Role)
                               .FirstOrDefault(u => u.Login == login);

            // Проверка пароля по хешу (BCrypt)
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                MessageBox.Show("Неверный логин или пароль.", "Ошибка входа",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Сохраняем данные текущего пользователя
            Session.CurrentUser = user;
            Logger.Log("Вход в систему");

            // Открываем главное окно
            var main = new MainWindow();
            main.Show();
            Close();
        }
    }
}

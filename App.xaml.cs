using System;
using System.Windows;
using BitldzhemDocFlow.Data;

namespace BitldzhemDocFlow
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Создаём базу данных при первом запуске и заполняем справочники.
                using var db = new AppDbContext();
                // ВНИМАНИЕ: строка ниже один раз пересоздаёт базу с новыми (русскими)
                // логинами. После первого успешного запуска её нужно удалить или
                // закомментировать, иначе данные будут стираться при каждом запуске.
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SeedInitialData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось подключиться к базе данных.\n\n" +
                    "Проверьте, что установлен MS SQL Server (или LocalDB),\n" +
                    "и при необходимости измените строку подключения в файле\n" +
                    "Data/AppDbContext.cs.\n\nОшибка: " + ex.Message,
                    "Ошибка базы данных",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}

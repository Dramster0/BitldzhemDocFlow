using System;
using System.Linq;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Models;

namespace BitldzhemDocFlow.Helpers
{
    // Хранит данные текущего вошедшего пользователя.
    // Используется для разграничения прав доступа по ролям (RBAC).
    public static class Session
    {
        public static User? CurrentUser { get; set; }

        public static string RoleName => CurrentUser?.Role?.RoleName ?? string.Empty;

        public static bool IsTechnologist => RoleName == "Технолог";
        public static bool IsDirector => RoleName == "Генеральный директор";
        public static bool IsOperator => RoleName == "Оператор производства";
        public static bool IsLabAssistant => RoleName == "Лаборант";
        public static bool IsQualitySpecialist => RoleName == "Специалист по качеству";
    }

    // Журналирование действий пользователей (см. диплом, таблица ChangeLog).
    public static class Logger
    {
        public static void Log(string action)
        {
            if (Session.CurrentUser == null) return;

            using var db = new AppDbContext();
            db.ChangeLog.Add(new ChangeLogEntry
            {
                UserId = Session.CurrentUser.UserId,
                Action = action,
                ActionDate = DateTime.Now
            });
            db.SaveChanges();
        }
    }

    // Запоминает состояние и размеры главного окна между сессиями (вход/выход),
    // чтобы при смене пользователя окно не сбрасывалось в исходный размер.
    public static class WindowPreference
    {
        // По умолчанию окно ещё не открывалось — используем стандартные размеры.
        public static bool HasValue { get; private set; }
        public static System.Windows.WindowState State { get; set; }
            = System.Windows.WindowState.Normal;
        public static double Width { get; set; } = 1340;
        public static double Height { get; set; } = 800;
        public static double Left { get; set; }
        public static double Top { get; set; }

        // Сохранить текущее состояние окна.
        public static void Save(System.Windows.Window w)
        {
            State = w.WindowState;
            // Если окно развёрнуто, сохраняем "восстановленные" размеры,
            // чтобы при возврате в обычный режим они были корректными.
            if (w.WindowState == System.Windows.WindowState.Normal)
            {
                Width = w.Width;
                Height = w.Height;
                Left = w.Left;
                Top = w.Top;
            }
            HasValue = true;
        }

        // Применить сохранённое состояние к новому окну.
        public static void Apply(System.Windows.Window w)
        {
            if (!HasValue) return;
            w.Width = Width;
            w.Height = Height;
            if (Left != 0 || Top != 0)
            {
                w.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                w.Left = Left;
                w.Top = Top;
            }
            w.WindowState = State;
        }
    }
}

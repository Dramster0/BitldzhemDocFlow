using System;
using System.Linq;
using BitldzhemDocFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BitldzhemDocFlow.Data
{
    public class AppDbContext : DbContext
    {
        // ── Строка подключения ──
        // Используется LocalDB, которая ставится вместе с Visual Studio.
        // Если у вас полноценный MS SQL Server — замените Data Source на имя сервера,
        // например: "Data Source=DESKTOP-XXXX\\SQLEXPRESS;Initial Catalog=BitldzhemDocFlow;Integrated Security=True;TrustServerCertificate=True"
        public const string ConnectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BitldzhemDocFlow;Integrated Security=True;TrustServerCertificate=True";

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<CardStatus> CardStatuses => Set<CardStatus>();
        public DbSet<TechnologicalCard> TechnologicalCards => Set<TechnologicalCard>();
        public DbSet<CardComponent> CardComponents => Set<CardComponent>();
        public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
        public DbSet<LabTest> LabTests => Set<LabTest>();
        public DbSet<Conclusion> Conclusions => Set<Conclusion>();
        public DbSet<ChangeLogEntry> ChangeLog => Set<ChangeLogEntry>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Логины пользователей уникальны
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Login)
                .IsUnique();

            // Одно заключение на одно испытание (связь 1:1)
            modelBuilder.Entity<Conclusion>()
                .HasIndex(c => c.TestId)
                .IsUnique();

            // Отключаем каскадное удаление для всех связей.
            // MS SQL Server не допускает несколько каскадных путей к одной таблице
            // (к Users ведут связи из карт, испытаний, заключений и журнала),
            // поэтому удаление настраивается как Restrict.
            foreach (var fk in modelBuilder.Model.GetEntityTypes()
                                           .SelectMany(t => t.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            base.OnModelCreating(modelBuilder);
        }

        // ── Первичное заполнение базы данных ──
        // Создаёт роли, статусы и тестовых пользователей при первом запуске.
        public void SeedInitialData()
        {
            if (!Roles.Any())
            {
                Roles.AddRange(
                    new Role { RoleName = "Технолог" },
                    new Role { RoleName = "Генеральный директор" },
                    new Role { RoleName = "Оператор производства" },
                    new Role { RoleName = "Лаборант" },
                    new Role { RoleName = "Специалист по качеству" }
                );
                SaveChanges();
            }

            if (!CardStatuses.Any())
            {
                CardStatuses.AddRange(
                    new CardStatus { StatusName = "Черновик" },
                    new CardStatus { StatusName = "На согласовании" },
                    new CardStatus { StatusName = "Утверждён" },
                    new CardStatus { StatusName = "На доработке" }
                );
                SaveChanges();
            }

            if (!Users.Any())
            {
                // Пароль у всех тестовых пользователей: 1234
                string hash = BCrypt.Net.BCrypt.HashPassword("1234");

                int rTech = Roles.First(r => r.RoleName == "Технолог").RoleId;
                int rDir = Roles.First(r => r.RoleName == "Генеральный директор").RoleId;
                int rOper = Roles.First(r => r.RoleName == "Оператор производства").RoleId;
                int rLab = Roles.First(r => r.RoleName == "Лаборант").RoleId;
                int rQual = Roles.First(r => r.RoleName == "Специалист по качеству").RoleId;

                Users.AddRange(
                    new User { FullName = "Иванов И.И.", Login = "технолог", PasswordHash = hash, RoleId = rTech },
                    new User { FullName = "Петров П.П.", Login = "директор", PasswordHash = hash, RoleId = rDir },
                    new User { FullName = "Сидоров С.С.", Login = "оператор", PasswordHash = hash, RoleId = rOper },
                    new User { FullName = "Кузнецова А.А.", Login = "лаборант", PasswordHash = hash, RoleId = rLab },
                    new User { FullName = "Смирнова Е.Е.", Login = "качество", PasswordHash = hash, RoleId = rQual }
                );
                SaveChanges();
            }
        }
    }
}

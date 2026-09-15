using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Helpers;
using BitldzhemDocFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BitldzhemDocFlow.Views
{
    public partial class ConclusionsView : UserControl
    {
        public ConclusionsView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using var db = new AppDbContext();

            // Протоколы, по которым ещё нет заключения
            var existingTestIds = db.Conclusions.Select(c => c.TestId).ToList();
            TestsGrid.ItemsSource = db.LabTests
                .Include(t => t.Batch)
                .Where(t => !existingTestIds.Contains(t.TestId))
                .OrderByDescending(t => t.TestId)
                .ToList();

            ConclusionsGrid.ItemsSource = db.Conclusions
                .Include(c => c.QualitySpecialist)
                .OrderByDescending(c => c.ConclusionId)
                .ToList();
        }

        private void TestsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Подсказка текста заключения по результату испытания
            if (TestsGrid.SelectedItem is LabTest test && string.IsNullOrWhiteSpace(ConclusionBox.Text))
            {
                ConclusionBox.Text = test.IsCompliant
                    ? "Продукция соответствует установленным требованиям. Разрешено к реализации."
                    : "Продукция не соответствует установленным требованиям. К реализации не допускается.";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (TestsGrid.SelectedItem is not LabTest test)
            {
                MessageBox.Show("Выберите протокол испытаний.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(ConclusionBox.Text))
            {
                MessageBox.Show("Введите текст заключения.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            db.Conclusions.Add(new Conclusion
            {
                TestId = test.TestId,
                ConclusionText = ConclusionBox.Text.Trim(),
                ConclusionDate = DateTime.Now,
                QualitySpecialistId = Session.CurrentUser!.UserId
            });
            db.SaveChanges();
            Logger.Log($"Оформлено заключение по протоколу №{test.TestId}");

            ConclusionBox.Clear();
            LoadData();
        }

        // Удаление заключения (конец цепочки — зависимых записей нет).
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ConclusionsGrid.SelectedItem is not Conclusion selected)
            {
                MessageBox.Show("Выберите заключение в таблице.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить заключение №{selected.ConclusionId} (протокол №{selected.TestId})?\n" +
                "Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var conclusion = db.Conclusions.First(c => c.ConclusionId == selected.ConclusionId);
            db.Conclusions.Remove(conclusion);
            db.SaveChanges();
            Logger.Log($"Удалено заключение №{conclusion.ConclusionId}");
            LoadData();
        }
    }
}

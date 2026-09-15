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
    public partial class LabTestsView : UserControl
    {
        public LabTestsView()
        {
            InitializeComponent();
            LoadTests();
            LoadBatches();
        }

        private void LoadTests()
        {
            using var db = new AppDbContext();
            TestsGrid.ItemsSource = db.LabTests
                .Include(t => t.Batch)
                .Include(t => t.LabAssistant)
                .OrderByDescending(t => t.TestId)
                .ToList();
        }

        private void LoadBatches()
        {
            using var db = new AppDbContext();
            var batches = db.ProductionBatches
                .OrderByDescending(b => b.BatchId)
                .ToList();
            BatchCombo.ItemsSource = batches;
            if (batches.Count > 0) BatchCombo.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (BatchCombo.SelectedItem is not ProductionBatch batch)
            {
                MessageBox.Show("Нет зарегистрированных партий.\n" +
                    "Сначала оператор должен создать партию продукции.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(ResultBox.Text))
            {
                MessageBox.Show("Введите результат испытания.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            db.LabTests.Add(new LabTest
            {
                BatchId = batch.BatchId,
                TestDate = DateTime.Now,
                Result = ResultBox.Text.Trim(),
                IsCompliant = CompliantCheck.IsChecked == true,
                LabAssistantId = Session.CurrentUser!.UserId
            });
            db.SaveChanges();
            Logger.Log($"Зарегистрирован протокол испытаний для партии «{batch.BatchNumber}»");

            ResultBox.Clear();
            CompliantCheck.IsChecked = true;
            LoadTests();
        }

        // Удаление протокола испытания с проверкой связанного заключения.
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (TestsGrid.SelectedItem is not LabTest selected)
            {
                MessageBox.Show("Выберите протокол испытания.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var test = db.LabTests
                         .Include(t => t.Conclusion)
                         .Include(t => t.Batch)
                         .First(t => t.TestId == selected.TestId);

            // Если по протоколу оформлено заключение — запрещаем удаление
            if (test.Conclusion != null)
            {
                MessageBox.Show(
                    "По этому протоколу оформлено заключение о соответствии.\n" +
                    "Сначала удалите связанное заключение.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить протокол испытания №{test.TestId} (партия «{test.Batch?.BatchNumber}»)?\n" +
                "Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            db.LabTests.Remove(test);
            db.SaveChanges();
            Logger.Log($"Удалён протокол испытания №{test.TestId}");
            LoadTests();
        }
    }
}

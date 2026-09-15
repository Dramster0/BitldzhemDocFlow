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
    public partial class BatchesView : UserControl
    {
        public BatchesView()
        {
            InitializeComponent();
            LoadBatches();
        }

        private void LoadBatches()
        {
            using var db = new AppDbContext();
            BatchesGrid.ItemsSource = db.ProductionBatches
                .Include(b => b.Card)
                .OrderByDescending(b => b.BatchId)
                .ToList();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var win = new BatchEditWindow();
            if (win.ShowDialog() == true) LoadBatches();
        }

        // Удаление партии с проверкой связанных испытаний.
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (BatchesGrid.SelectedItem is not ProductionBatch selected)
            {
                MessageBox.Show("Выберите партию.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            var batch = db.ProductionBatches
                          .Include(b => b.Tests)
                          .First(b => b.BatchId == selected.BatchId);

            // Если есть связанные испытания — запрещаем удаление
            if (batch.Tests.Any())
            {
                MessageBox.Show(
                    $"По этой партии зарегистрировано испытаний: {batch.Tests.Count}.\n" +
                    "Сначала удалите связанные лабораторные испытания.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить партию «{batch.BatchNumber}»?\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            db.ProductionBatches.Remove(batch);
            db.SaveChanges();
            Logger.Log($"Удалена партия «{batch.BatchNumber}»");
            LoadBatches();
        }
    }
}

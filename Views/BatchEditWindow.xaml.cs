using System;
using System.Linq;
using System.Windows;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Helpers;
using BitldzhemDocFlow.Models;

namespace BitldzhemDocFlow.Views
{
    public partial class BatchEditWindow : Window
    {
        public BatchEditWindow()
        {
            InitializeComponent();
            LoadCards();
        }

        // В список попадают только утверждённые карты
        private void LoadCards()
        {
            using var db = new AppDbContext();
            var cards = db.TechnologicalCards
                .Where(c => c.Status!.StatusName == "Утверждён")
                .OrderBy(c => c.CardName)
                .ToList();
            CardCombo.ItemsSource = cards;
            if (cards.Count > 0) CardCombo.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (CardCombo.SelectedItem is not TechnologicalCard card)
            {
                MessageBox.Show("Нет утверждённых технологических карт.\n" +
                    "Сначала технолог должен создать карту, а директор — утвердить её.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(BatchNumberBox.Text))
            {
                MessageBox.Show("Введите номер партии.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();
            db.ProductionBatches.Add(new ProductionBatch
            {
                CardId = card.CardId,
                BatchNumber = BatchNumberBox.Text.Trim(),
                ProductionDate = DateTime.Now
            });
            db.SaveChanges();
            Logger.Log($"Зарегистрирована партия «{BatchNumberBox.Text.Trim()}»");

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Helpers;
using BitldzhemDocFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BitldzhemDocFlow.Views
{
    public partial class CardEditWindow : Window
    {
        private readonly int? _cardId;
        private ObservableCollection<CardComponent> _components = new();

        public CardEditWindow(int? cardId)
        {
            InitializeComponent();
            _cardId = cardId;

            if (_cardId.HasValue)
                LoadCard(_cardId.Value);
            else
                ComponentsGrid.ItemsSource = _components;
        }

        private void LoadCard(int id)
        {
            using var db = new AppDbContext();
            var card = db.TechnologicalCards
                         .Include(c => c.Components)
                         .First(c => c.CardId == id);

            TitleText.Text = $"Технологическая карта №{card.CardId}";
            CardNameBox.Text = card.CardName;
            ProductNameBox.Text = card.ProductName;
            ProcessBox.Text = card.ProcessDescription;

            _components = new ObservableCollection<CardComponent>(card.Components);
            ComponentsGrid.ItemsSource = _components;

            // Если карта возвращена на доработку — показываем комментарий директора
            if (!string.IsNullOrWhiteSpace(card.DirectorComment))
            {
                CommentText.Text = "Замечания директора: " + card.DirectorComment;
                CommentText.Visibility = Visibility.Visible;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(CardNameBox.Text) ||
                string.IsNullOrWhiteSpace(ProductNameBox.Text))
            {
                MessageBox.Show("Заполните название карты и наименование продукции.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Завершаем редактирование в гриде, чтобы последняя строка зафиксировалась
            ComponentsGrid.CommitEdit();

            using var db = new AppDbContext();

            TechnologicalCard card;
            if (_cardId.HasValue)
            {
                card = db.TechnologicalCards
                         .Include(c => c.Components)
                         .First(c => c.CardId == _cardId.Value);
                // Удаляем старый состав, чтобы перезаписать
                db.CardComponents.RemoveRange(card.Components);
            }
            else
            {
                var draft = db.CardStatuses.First(s => s.StatusName == "Черновик");
                card = new TechnologicalCard
                {
                    StatusId = draft.StatusId,
                    AuthorId = Session.CurrentUser!.UserId,
                    CreatedDate = DateTime.Now
                };
                db.TechnologicalCards.Add(card);
            }

            card.CardName = CardNameBox.Text.Trim();
            card.ProductName = ProductNameBox.Text.Trim();
            card.ProcessDescription = ProcessBox.Text.Trim();

            // Сохраняем компоненты
            card.Components = _components
                .Where(c => !string.IsNullOrWhiteSpace(c.ComponentName))
                .Select(c => new CardComponent
                {
                    ComponentName = c.ComponentName,
                    Quantity = c.Quantity,
                    Unit = c.Unit
                }).ToList();

            db.SaveChanges();
            Logger.Log(_cardId.HasValue
                ? $"Карта №{card.CardId} отредактирована"
                : $"Создана карта №{card.CardId}");

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

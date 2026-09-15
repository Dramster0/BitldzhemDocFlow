using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Helpers;
using BitldzhemDocFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace BitldzhemDocFlow.Views
{
    public partial class CardsView : UserControl
    {
        private List<TechnologicalCard> _allCards = new();

        public CardsView()
        {
            InitializeComponent();
            LoadStatusFilter();
            LoadCards();
            ApplyRolePermissions();
        }

        // Кнопки видны только тем ролям, которым они разрешены
        private void ApplyRolePermissions()
        {
            NewBtn.Visibility = Session.IsTechnologist ? Visibility.Visible : Visibility.Collapsed;
            EditBtn.Visibility = Session.IsTechnologist ? Visibility.Visible : Visibility.Collapsed;
            SendBtn.Visibility = Session.IsTechnologist ? Visibility.Visible : Visibility.Collapsed;
            ApproveBtn.Visibility = Session.IsDirector ? Visibility.Visible : Visibility.Collapsed;
            RejectBtn.Visibility = Session.IsDirector ? Visibility.Visible : Visibility.Collapsed;
            // Удалять карты может технолог (свои черновики и возвращённые на доработку)
            DeleteBtn.Visibility = Session.IsTechnologist ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadStatusFilter()
        {
            using var db = new AppDbContext();
            var statuses = db.CardStatuses.ToList();
            statuses.Insert(0, new CardStatus { StatusId = 0, StatusName = "Все статусы" });
            StatusFilter.ItemsSource = statuses;
            StatusFilter.DisplayMemberPath = "StatusName";
            StatusFilter.SelectedIndex = 0;
        }

        private void LoadCards()
        {
            using var db = new AppDbContext();
            var query = db.TechnologicalCards
                          .Include(c => c.Status)
                          .Include(c => c.Author)
                          .AsQueryable();

            // Оператор видит только утверждённые карты
            if (Session.IsOperator)
                query = query.Where(c => c.Status!.StatusName == "Утверждён");

            // Директор видит карты на согласовании и утверждённые
            if (Session.IsDirector)
                query = query.Where(c => c.Status!.StatusName == "На согласовании"
                                      || c.Status!.StatusName == "Утверждён");

            _allCards = query.OrderByDescending(c => c.CardId).ToList();
            ApplyFilter();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (_allCards == null) return;

            IEnumerable<TechnologicalCard> result = _allCards;

            string search = SearchBox.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                result = result.Where(c =>
                    c.CardName.ToLower().Contains(search) ||
                    c.ProductName.ToLower().Contains(search));

            if (StatusFilter.SelectedItem is CardStatus st && st.StatusId != 0)
                result = result.Where(c => c.StatusId == st.StatusId);

            CardsGrid.ItemsSource = result.ToList();
        }

        private TechnologicalCard? Selected => CardsGrid.SelectedItem as TechnologicalCard;

        private void CardsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            var win = new CardEditWindow(null);
            if (win.ShowDialog() == true) LoadCards();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { Warn("Выберите карту."); return; }
            var win = new CardEditWindow(Selected.CardId);
            if (win.ShowDialog() == true) LoadCards();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { Warn("Выберите карту."); return; }
            ChangeStatus(Selected.CardId, "На согласовании",
                $"Карта №{Selected.CardId} отправлена на согласование");
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { Warn("Выберите карту."); return; }
            using var db = new AppDbContext();
            var card = db.TechnologicalCards.Include(c => c.Status)
                         .First(c => c.CardId == Selected.CardId);
            var approved = db.CardStatuses.First(s => s.StatusName == "Утверждён");
            card.StatusId = approved.StatusId;
            card.ApprovedDate = DateTime.Now;
            db.SaveChanges();
            Logger.Log($"Карта №{card.CardId} утверждена");
            LoadCards();
        }

        private void Reject_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { Warn("Выберите карту."); return; }
            var dlg = new CommentWindow();
            if (dlg.ShowDialog() != true) return;

            using var db = new AppDbContext();
            var card = db.TechnologicalCards.First(c => c.CardId == Selected.CardId);
            var status = db.CardStatuses.First(s => s.StatusName == "На доработке");
            card.StatusId = status.StatusId;
            card.DirectorComment = dlg.Comment;
            db.SaveChanges();
            Logger.Log($"Карта №{card.CardId} возвращена на доработку");
            LoadCards();
        }

        private void ChangeStatus(int cardId, string statusName, string logMessage)
        {
            using var db = new AppDbContext();
            var card = db.TechnologicalCards.First(c => c.CardId == cardId);
            var status = db.CardStatuses.First(s => s.StatusName == statusName);
            card.StatusId = status.StatusId;
            db.SaveChanges();
            Logger.Log(logMessage);
            LoadCards();
        }

        // Удаление карты с проверками: статус и наличие связанных партий.
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { Warn("Выберите карту."); return; }

            using var db = new AppDbContext();
            var card = db.TechnologicalCards
                         .Include(c => c.Status)
                         .Include(c => c.Components)
                         .Include(c => c.Batches)
                         .First(c => c.CardId == Selected.CardId);

            // Утверждённую карту удалять нельзя
            if (card.Status?.StatusName == "Утверждён")
            {
                Warn("Нельзя удалить утверждённую карту.\n" +
                     "Удалять можно только черновики или карты на доработке.");
                return;
            }

            // Если есть связанные партии — запрещаем удаление
            if (card.Batches.Any())
            {
                Warn($"По этой карте зарегистрировано партий: {card.Batches.Count}.\n" +
                     "Сначала удалите связанные партии продукции, затем карту.");
                return;
            }

            // Подтверждение
            var confirm = MessageBox.Show(
                $"Удалить технологическую карту «{card.CardName}»?\n" +
                "Это действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            // Удаляем состав карты, затем саму карту
            db.CardComponents.RemoveRange(card.Components);
            db.TechnologicalCards.Remove(card);
            db.SaveChanges();
            Logger.Log($"Удалена карта №{card.CardId} «{card.CardName}»");
            LoadCards();
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

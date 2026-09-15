using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitldzhemDocFlow.Helpers;

namespace BitldzhemDocFlow.Views
{
    public partial class MainWindow : Window
    {
        private readonly List<Button> _menuButtons = new();

        public MainWindow()
        {
            InitializeComponent();
            // Восстанавливаем размер/состояние окна от предыдущей сессии
            WindowPreference.Apply(this);
            UserNameText.Text = Session.CurrentUser?.FullName;
            UserRoleText.Text = Session.RoleName;
            BuildMenu();
        }

        // Сохраняем состояние окна при любом его закрытии (в т.ч. сменой пользователя).
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            WindowPreference.Save(this);
            base.OnClosing(e);
        }

        private void BuildMenu()
        {
            var items = new List<(string Icon, string Title, Func<UserControl> Factory)>();

            if (Session.IsTechnologist)
                items.Add(("\uE8A5", "Технологические карты", () => new CardsView()));
            if (Session.IsDirector)
                items.Add(("\uE73E", "Согласование карт", () => new CardsView()));
            if (Session.IsOperator)
            {
                items.Add(("\uE8A5", "Утверждённые карты", () => new CardsView()));
                items.Add(("\uE7B8", "Партии продукции", () => new BatchesView()));
            }
            if (Session.IsLabAssistant)
                items.Add(("\uE9D9", "Лабораторные испытания", () => new LabTestsView()));
            if (Session.IsQualitySpecialist)
                items.Add(("\uE73A", "Заключения", () => new ConclusionsView()));

            items.Add(("\uE9F9", "Отчёты", () => new ReportsView()));
            items.Add(("\uE81C", "Журнал действий", () => new ChangeLogView()));

            foreach (var item in items)
            {
                var btn = BuildMenuButton(item.Icon, item.Title);
                var factory = item.Factory;
                btn.Click += (s, e) =>
                {
                    ContentArea.Content = factory();
                    HighlightButton(btn);
                };
                _menuButtons.Add(btn);
                MenuPanel.Children.Add(btn);
            }

            if (items.Count > 0)
            {
                ContentArea.Content = items[0].Factory();
                HighlightButton(_menuButtons[0]);
            }
        }

        private Button BuildMenuButton(string icon, string title)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0xA0, 0x62)),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 28
            });
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            var btn = new Button
            {
                Content = panel,
                Height = 52,
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "bd";
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.PaddingProperty, new Thickness(12, 0, 12, 0));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            var hoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x2E)), "bd"));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            return btn;
        }

        private void HighlightButton(Button active)
        {
            foreach (var btn in _menuButtons)
            {
                bool isActive = btn == active;
                btn.ApplyTemplate();
                if (btn.Template.FindName("bd", btn) is Border bd)
                    bd.Background = isActive
                        ? new SolidColorBrush(Color.FromRgb(0xC0, 0xA0, 0x62))
                        : Brushes.Transparent;

                if (btn.Content is StackPanel sp)
                {
                    foreach (var child in sp.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            if (tb.FontFamily.Source == "Segoe MDL2 Assets")
                                tb.Foreground = isActive
                                    ? new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A))
                                    : new SolidColorBrush(Color.FromRgb(0xC0, 0xA0, 0x62));
                            else
                                tb.Foreground = isActive
                                    ? new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A))
                                    : Brushes.White;
                        }
                    }
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Выход из системы");
            Session.CurrentUser = null;
            new LoginWindow().Show();
            Close();
        }
    }
}

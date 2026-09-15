using System.Windows;

namespace BitldzhemDocFlow.Views
{
    public partial class CommentWindow : Window
    {
        public string Comment { get; private set; } = string.Empty;

        public CommentWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommentBox.Text))
            {
                MessageBox.Show("Введите комментарий.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Comment = CommentBox.Text.Trim();
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

using System.Linq;
using System.Windows.Controls;
using BitldzhemDocFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace BitldzhemDocFlow.Views
{
    public partial class ChangeLogView : UserControl
    {
        public ChangeLogView()
        {
            InitializeComponent();
            LoadLog();
        }

        private void LoadLog()
        {
            using var db = new AppDbContext();
            LogGrid.ItemsSource = db.ChangeLog
                .Include(l => l.User)
                .OrderByDescending(l => l.LogId)
                .Take(500)
                .ToList();
        }
    }
}

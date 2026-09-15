using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BitldzhemDocFlow.Data;
using BitldzhemDocFlow.Helpers;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BitldzhemDocFlow.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            ReportType.SelectedIndex = 0;
        }

        private void ReportType_Changed(object sender, SelectionChangedEventArgs e)
        {
            BuildReport();
        }

        // Строим отчёт в виде DataTable — он же используется для экспорта в Excel
        private DataTable BuildReport()
        {
            var table = new DataTable();
            using var db = new AppDbContext();

            if (ReportType.SelectedIndex == 0)
            {
                // Отчёт по технологическим картам
                table.TableName = "Технологические карты";
                table.Columns.Add("№");
                table.Columns.Add("Название карты");
                table.Columns.Add("Продукция");
                table.Columns.Add("Статус");
                table.Columns.Add("Автор");
                table.Columns.Add("Дата создания");

                var cards = db.TechnologicalCards
                    .Include(c => c.Status)
                    .Include(c => c.Author)
                    .OrderBy(c => c.CardId)
                    .ToList();

                foreach (var c in cards)
                    table.Rows.Add(c.CardId, c.CardName, c.ProductName,
                        c.Status?.StatusName, c.Author?.FullName,
                        c.CreatedDate.ToString("dd.MM.yyyy"));
            }
            else
            {
                // Отчёт по лабораторным испытаниям
                table.TableName = "Лабораторные испытания";
                table.Columns.Add("№");
                table.Columns.Add("Партия");
                table.Columns.Add("Дата");
                table.Columns.Add("Результат");
                table.Columns.Add("Соответствие");
                table.Columns.Add("Лаборант");

                var tests = db.LabTests
                    .Include(t => t.Batch)
                    .Include(t => t.LabAssistant)
                    .OrderBy(t => t.TestId)
                    .ToList();

                foreach (var t in tests)
                    table.Rows.Add(t.TestId, t.Batch?.BatchNumber,
                        t.TestDate.ToString("dd.MM.yyyy"), t.Result,
                        t.IsCompliant ? "Соответствует" : "Не соответствует",
                        t.LabAssistant?.FullName);
            }

            ReportGrid.ItemsSource = table.DefaultView;
            return table;
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var table = BuildReport();
            if (table.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Книга Excel (*.xlsx)|*.xlsx",
                FileName = table.TableName + ".xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.AddWorksheet(table.TableName.Length > 31
                    ? table.TableName.Substring(0, 31) : table.TableName);

                // Заголовок документа
                ws.Cell(1, 1).Value = "ООО «Битлджем» — " + table.TableName;
                ws.Range(1, 1, 1, table.Columns.Count).Merge().Style.Font.Bold = true;

                // Шапка таблицы
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    var cell = ws.Cell(3, col + 1);
                    cell.Value = table.Columns[col].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Данные
                for (int row = 0; row < table.Rows.Count; row++)
                    for (int col = 0; col < table.Columns.Count; col++)
                        ws.Cell(row + 4, col + 1).Value = table.Rows[row][col]?.ToString();

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);

                Logger.Log($"Сформирован отчёт «{table.TableName}» (Excel)");
                MessageBox.Show("Отчёт сохранён:\n" + dlg.FileName, "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить файл: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

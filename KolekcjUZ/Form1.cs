using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;

namespace KolekcjUZ
{
    public partial class Form1 : Form
    {
        private string masterConnectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;";
        private string currentDatabaseName = "";

        public Form1()
        {
            InitializeComponent();
            this.IsMdiContainer = true;
        }

        private void btnCreateDb_Click(object sender, EventArgs e)
        {
            Form inputForm = new Form() { Width = 350, Height = 150, Text = "Nowa baza SQL Server" };
            Label lbl = new Label() { Text = "Nazwa bazy danych:", Left = 20, Top = 20, Width = 120 };
            TextBox txt = new TextBox() { Left = 150, Top = 18, Width = 150 };
            Button btnOk = new Button() { Text = "Stwórz", Left = 150, Top = 60, Width = 80, DialogResult = DialogResult.OK };

            inputForm.Controls.AddRange(new Control[] { lbl, txt, btnOk });
            inputForm.AcceptButton = btnOk;

            if (inputForm.ShowDialog() == DialogResult.OK)
            {
                string dbName = txt.Text.Trim().Replace(" ", "_");
                if (string.IsNullOrEmpty(dbName)) return;

                string sqlQuery = $"CREATE DATABASE [{dbName}]";

                using (SqlConnection conn = new SqlConnection(masterConnectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(sqlQuery, conn)) { cmd.ExecuteNonQuery(); }

                        currentDatabaseName = dbName;
                        MessageBox.Show($"Baza danych '{dbName}' została utworzona pomyślnie!", "Sukces");
                        OtworzKreatorTabeli();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd SQL: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OtworzKreatorTabeli()
        {
            if (string.IsNullOrEmpty(currentDatabaseName)) return;

            Form tableForm = new Form { Text = $"Kreator Tabel - Baza: {currentDatabaseName}", MdiParent = this, Width = 550, Height = 450 };

            Label lblTableName = new Label() { Text = "Nazwa Tabeli:", Top = 15, Left = 15, Width = 100 };
            TextBox txtTableName = new TextBox() { Top = 12, Left = 120, Width = 150 };
            Label lblColName = new Label() { Text = "Nazwa Kolumny:", Top = 55, Left = 15, Width = 100 };
            TextBox txtColName = new TextBox() { Top = 52, Left = 120, Width = 150 };
            Label lblType = new Label() { Text = "Typ Danych:", Top = 55, Left = 280, Width = 80 };

            ComboBox cmbType = new ComboBox() { Top = 52, Left = 360, Width = 120 };
            cmbType.Items.AddRange(new string[] { "INT", "VARCHAR(250)", "DECIMAL(18,2)", "BIT" });
            cmbType.SelectedIndex = 1;

            Button btnAddCol = new Button() { Text = "Dodaj Kolumnę", Top = 90, Left = 120, Width = 120 };
            ListView lvColumns = new ListView() { Top = 130, Left = 15, Width = 500, Height = 200, View = View.Details };
            lvColumns.Columns.Add("Nazwa kolumny", 250);
            lvColumns.Columns.Add("Typ danych", 230);

            Button btnGenerateTable = new Button() { Text = "Zatwierdź i Stwórz Tabelę w SQL Server", Top = 350, Left = 15, Width = 500, Height = 40 };
            List<KeyValuePair<string, string>> columnsList = new List<KeyValuePair<string, string>>();

            btnAddCol.Click += (s, ev) => {
                string colName = txtColName.Text.Trim().Replace(" ", "_");
                string colType = cmbType.SelectedItem?.ToString() ?? "VARCHAR(250)";
                if (string.IsNullOrEmpty(colName)) return;

                ListViewItem item = new ListViewItem(colName);
                item.SubItems.Add(colType);
                lvColumns.Items.Add(item);
                columnsList.Add(new KeyValuePair<string, string>(colName, colType));
                txtColName.Clear();
                txtColName.Focus();
            };

            btnGenerateTable.Click += (s, ev) => {
                string tableName = txtTableName.Text.Trim().Replace(" ", "_");
                if (string.IsNullOrEmpty(tableName) || columnsList.Count == 0) return;

                string sqlQuery = $"CREATE TABLE [{tableName}] (Id INT IDENTITY(1,1) PRIMARY KEY";
                foreach (var col in columnsList)
                {
                    sqlQuery += $", [{col.Key}] {col.Value}";
                }
                sqlQuery += ");";

                string specificDbConnectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={currentDatabaseName};Integrated Security=True;TrustServerCertificate=True;";

                using (SqlConnection conn = new SqlConnection(specificDbConnectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand(sqlQuery, conn)) { cmd.ExecuteNonQuery(); }
                        MessageBox.Show($"Tabela '{tableName}' została utworzona w bazie '{currentDatabaseName}'!", "Sukces");
                        tableForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd SQL przy tworzeniu tabeli: {ex.Message}");
                    }
                }
            };

            tableForm.Controls.AddRange(new Control[] { lblTableName, txtTableName, lblColName, txtColName, lblType, cmbType, btnAddCol, lvColumns, btnGenerateTable });
            tableForm.Show();
        }

        private void btnListDbs_Click(object sender, EventArgs e)
        {
            Form selectDbForm = new Form() { Width = 350, Height = 170, Text = "Wybierz bazę z SQL Server" };
            Label lbl = new Label() { Text = "Wpisz nazwę bazy:", Left = 20, Top = 20, Width = 120 };
            TextBox txt = new TextBox() { Left = 150, Top = 18, Width = 150 };
            Button btnOk = new Button() { Text = "Połącz", Left = 150, Top = 60, Width = 80, DialogResult = DialogResult.OK };

            selectDbForm.Controls.AddRange(new Control[] { lbl, txt, btnOk });
            selectDbForm.AcceptButton = btnOk;

            if (selectDbForm.ShowDialog() == DialogResult.OK)
            {
                string dbName = txt.Text.Trim();
                if (string.IsNullOrEmpty(dbName)) return;

                string checkConnectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Integrated Security=True;TrustServerCertificate=True;";
                using (SqlConnection conn = new SqlConnection(checkConnectionString))
                {
                    try
                    {
                        conn.Open();
                        currentDatabaseName = dbName;
                        MessageBox.Show($"Pomyślnie połączono z bazą '{dbName}'", "Połączono");
                        OtworzKreatorTabeli();
                    }
                    catch
                    {
                        MessageBox.Show($"Nie znaleziono bazy o nazwie '{dbName}' na serwerze.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
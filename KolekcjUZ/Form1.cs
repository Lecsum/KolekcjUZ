using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;
using System.Data;

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

            RozbudujMenu();
        }

        private void RozbudujMenu()
        {
            
            Button btnManageData = new Button() { Text = "Zarządzaj Danymi", Left = 160, Top = 12, Size = new System.Drawing.Size(120, 35) };
            btnManageData.Click += (s, e) => WybierzBazeITabele(false);

            Button btnAddTable = new Button() { Text = "Dodaj Tabelę", Left = 295, Top = 12, Size = new System.Drawing.Size(120, 35), BackColor = System.Drawing.Color.LightCyan };
            btnAddTable.Click += (s, e) => UruchomDodawanieTabeliDoBazy();

            Button btnDeleteTable = new Button() { Text = "Usuń Tabelę", Left = 430, Top = 12, Size = new System.Drawing.Size(120, 35), BackColor = System.Drawing.Color.MistyRose };
            btnDeleteTable.Click += (s, e) => WybierzBazeITabele(true);

            Button btnDeleteDb = new Button() { Text = "Usuń Bazę", Left = 565, Top = 12, Size = new System.Drawing.Size(120, 35), BackColor = System.Drawing.Color.LightCoral };
            btnDeleteDb.Click += (s, e) => UruchomUsuwanieBazy();

            if (this.Controls.Find("panelTopMenu", true).Length > 0)
            {
                Control panel = this.Controls.Find("panelTopMenu", true)[0];
                panel.Controls.AddRange(new Control[] { btnManageData, btnAddTable, btnDeleteTable, btnDeleteDb });
            }
        }

        private void UruchomDodawanieTabeliDoBazy()
        {
            Form selectDbForm = new Form() { Width = 350, Height = 170, Text = "Wybierz bazę do edycji" };
            Label lbl = new Label() { Text = "Wybierz bazę danych:", Left = 20, Top = 20, Width = 120 };
            ComboBox cmbDatabases = new ComboBox() { Left = 150, Top = 18, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnOk = new Button() { Text = "Otwórz kreator", Left = 150, Top = 60, Width = 120, DialogResult = DialogResult.OK };

            selectDbForm.Controls.AddRange(new Control[] { lbl, cmbDatabases, btnOk });
            selectDbForm.AcceptButton = btnOk;

            using (SqlConnection conn = new SqlConnection(masterConnectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) cmbDatabases.Items.Add(reader["name"].ToString());
                    }
                    if (cmbDatabases.Items.Count > 0) cmbDatabases.SelectedIndex = 0;
                    else { MessageBox.Show("Brak dostępnych baz."); return; }
                }
                catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return; }
            }

            if (selectDbForm.ShowDialog() == DialogResult.OK)
            {
                currentDatabaseName = cmbDatabases.SelectedItem?.ToString() ?? "";
                if (!string.IsNullOrEmpty(currentDatabaseName))
                {
                    OtworzKreatorTabeli(); 
                }
            }
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

                        OtworzMenedzerDanych(tableName);
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

        private void OtworzMenedzerDanych(string tableName)
        {
            Form dataForm = new Form { Text = $"Dane tabeli: {tableName} (Baza: {currentDatabaseName})", MdiParent = this, Width = 800, Height = 550 };

            DataGridView dgv = new DataGridView
            {
                Top = 15,
                Left = 15,
                Width = 750,
                Height = 250,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            FlowLayoutPanel panelFields = new FlowLayoutPanel { Top = 280, Left = 15, Width = 750, Height = 110, AutoScroll = true };

            Button btnSaveRecord = new Button { Text = "Dodaj Rekord do Bazy", Top = 405, Left = 15, Width = 750, Height = 35 };

            Button btnDeleteRecord = new Button { Text = "Usuń Zaznaczony Rekord", Top = 450, Left = 15, Width = 750, Height = 35, BackColor = System.Drawing.Color.MistyRose };

            string connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={currentDatabaseName};Integrated Security=True;TrustServerCertificate=True;";
            List<TextBox> textBoxesList = new List<TextBox>();
            List<string> columnNames = new List<string>();

            Action OdswiezDane = () => {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        SqlDataAdapter adapter = new SqlDataAdapter($"SELECT * FROM [{tableName}]", conn);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgv.DataSource = dt;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas odczytu danych: {ex.Message}");
                    }
                }
            };

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand($"SELECT TOP 0 * FROM [{tableName}]", conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var schemaTable = reader.GetSchemaTable();
                        if (schemaTable != null)
                        {
                            foreach (DataRow row in schemaTable.Rows)
                            {
                                string columnName = row["ColumnName"].ToString();
                                bool isIdentity = (bool)row["IsIdentity"];

                                if (isIdentity) continue;

                                Panel p = new Panel { Width = 220, Height = 50 };
                                Label l = new Label { Text = columnName, Top = 5, Left = 5, Width = 200 };
                                TextBox t = new TextBox { Top = 25, Left = 5, Width = 200, Tag = columnName };

                                p.Controls.AddRange(new Control[] { l, t });
                                panelFields.Controls.Add(p);

                                textBoxesList.Add(t);
                                columnNames.Add(columnName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd pobierania struktury tabeli: {ex.Message}");
                }
            }

            OdswiezDane();

            btnSaveRecord.Click += (s, ev) => {
                if (columnNames.Count == 0) return;

                string cols = string.Join(", ", columnNames);
                List<string> paramNames = new List<string>();
                foreach (var name in columnNames) paramNames.Add("@" + name);
                string paramsJoined = string.Join(", ", paramNames);

                string insertQuery = $"INSERT INTO [{tableName}] ({cols}) VALUES ({paramsJoined})";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            foreach (var tb in textBoxesList)
                            {
                                string colName = tb.Tag.ToString();

                                if (string.IsNullOrEmpty(tb.Text))
                                {
                                    insertCmd.Parameters.AddWithValue("@" + colName, DBNull.Value);
                                }
                                else
                                {
                                    insertCmd.Parameters.AddWithValue("@" + colName, tb.Text);
                                }
                            }
                            insertCmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Pomyślnie dodano nowy rekord!", "Sukces");
                        foreach (var tb in textBoxesList) tb.Clear();
                        OdswiezDane();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisu rekordu: {ex.Message}");
                    }
                }
            };

            btnDeleteRecord.Click += (s, ev) => {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Zaznacz najpierw cały wiersz w tabeli, który chcesz usunąć!", "Informacja");
                    return;
                }

                var selectedRow = dgv.SelectedRows[0];
                string idValue = selectedRow.Cells["Id"].Value?.ToString() ?? "";

                if (string.IsNullOrEmpty(idValue)) return;

                DialogResult result = MessageBox.Show($"Czy na pewno chcesz usunąć rekord o Id = {idValue}?", "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        try
                        {
                            conn.Open();
                            string deleteQuery = $"DELETE FROM [{tableName}] WHERE Id = @Id";
                            using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
                            {
                                deleteCmd.Parameters.AddWithValue("@Id", idValue);
                                deleteCmd.ExecuteNonQuery();
                            }
                            MessageBox.Show("Rekord został pomyślnie usunięty.", "Sukces");
                            OdswiezDane(); 
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Błąd SQL podczas usuwania rekordu: {ex.Message}");
                        }
                    }
                }
            };

            dataForm.Controls.AddRange(new Control[] { dgv, panelFields, btnSaveRecord, btnDeleteRecord });
            dataForm.Show();
        }

        private void btnListDbs_Click(object sender, EventArgs e)
        {
            WybierzBazeITabele(false);
        }

        private void WybierzBazeITabele(bool trybUsuwaniaTabeli)
        {
            Form selectDbForm = new Form() { Width = 350, Height = 170, Text = "Wybierz bazę z SQL Server" };
            Label lbl = new Label() { Text = "Wybierz bazę danych:", Left = 20, Top = 20, Width = 120 };
            ComboBox cmbDatabases = new ComboBox() { Left = 150, Top = 18, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnOk = new Button() { Text = "Dalej", Left = 150, Top = 60, Width = 80, DialogResult = DialogResult.OK };

            selectDbForm.Controls.AddRange(new Control[] { lbl, cmbDatabases, btnOk });
            selectDbForm.AcceptButton = btnOk;

            using (SqlConnection conn = new SqlConnection(masterConnectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) cmbDatabases.Items.Add(reader["name"].ToString());
                    }
                    if (cmbDatabases.Items.Count > 0) cmbDatabases.SelectedIndex = 0;
                    else { MessageBox.Show("Brak dostępnych baz."); return; }
                }
                catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return; }
            }

            if (selectDbForm.ShowDialog() == DialogResult.OK)
            {
                currentDatabaseName = cmbDatabases.SelectedItem?.ToString() ?? "";
                if (!string.IsNullOrEmpty(currentDatabaseName))
                {
                    OtworzWyborTabeli(trybUsuwaniaTabeli);
                }
            }
        }

        private void OtworzWyborTabeli(bool trybUsuwaniaTabeli)
        {
            string dbConnectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={currentDatabaseName};Integrated Security=True;TrustServerCertificate=True;";
            Form tableSelectForm = new Form() { Width = 350, Height = 150, Text = trybUsuwaniaTabeli ? "USUWANIE TABELI" : "Wybierz tabelę" };
            Label lblT = new Label() { Text = "Wybierz tabelę:", Left = 20, Top = 20, Width = 120 };
            ComboBox cmbTables = new ComboBox() { Left = 150, Top = 18, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnAction = new Button()
            {
                Text = trybUsuwaniaTabeli ? "USUŃ TABELĘ" : "Otwórz dane",
                Left = 150,
                Top = 60,
                Width = 120,
                DialogResult = DialogResult.OK,
                BackColor = trybUsuwaniaTabeli ? System.Drawing.Color.Red : System.Drawing.Color.LightGray
            };

            tableSelectForm.Controls.AddRange(new Control[] { lblT, cmbTables, btnAction });
            tableSelectForm.AcceptButton = btnAction;

            using (SqlConnection conn = new SqlConnection(dbConnectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT name FROM sys.tables", conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) cmbTables.Items.Add(reader["name"].ToString());
                    }
                    if (cmbTables.Items.Count > 0) cmbTables.SelectedIndex = 0;
                    else { MessageBox.Show("Ta baza nie ma tabel."); return; }
                }
                catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return; }
            }

            if (tableSelectForm.ShowDialog() == DialogResult.OK)
            {
                string tableName = cmbTables.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrEmpty(tableName)) return;

                if (trybUsuwaniaTabeli)
                {
                    DialogResult result = MessageBox.Show($"CZY NA PEWNO chcesz bezpowrotnie USUNĄĆ tabelę [{tableName}] z bazy [{currentDatabaseName}]?", "OSTRZEŻENIE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        using (SqlConnection conn = new SqlConnection(dbConnectionString))
                        {
                            try
                            {
                                conn.Open();
                                using (SqlCommand dropCmd = new SqlCommand($"DROP TABLE [{tableName}]", conn)) { dropCmd.ExecuteNonQuery(); }
                                MessageBox.Show($"Tabela '{tableName}' została pomyślnie usunięta!");
                            }
                            catch (Exception ex) { MessageBox.Show($"Błąd SQL: {ex.Message}"); }
                        }
                    }
                }
                else
                {
                    OtworzMenedzerDanych(tableName);
                }
            }
        }

        private void UruchomUsuwanieBazy()
        {
            Form selectDbForm = new Form() { Width = 350, Height = 170, Text = "USUWANIE BAZY DANYCH" };
            Label lbl = new Label() { Text = "Wybierz bazę do USUNIĘCIA:", Left = 20, Top = 20, Width = 150 };
            ComboBox cmbDatabases = new ComboBox() { Left = 170, Top = 18, Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            Button btnOk = new Button() { Text = "USUŃ BAZĘ", Left = 170, Top = 60, Width = 100, DialogResult = DialogResult.OK, BackColor = System.Drawing.Color.Red };

            selectDbForm.Controls.AddRange(new Control[] { lbl, cmbDatabases, btnOk });
            selectDbForm.AcceptButton = btnOk;

            using (SqlConnection conn = new SqlConnection(masterConnectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) cmbDatabases.Items.Add(reader["name"].ToString());
                    }
                    if (cmbDatabases.Items.Count > 0) cmbDatabases.SelectedIndex = 0;
                    else { MessageBox.Show("Brak baz do usunięcia."); return; }
                }
                catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return; }
            }

            if (selectDbForm.ShowDialog() == DialogResult.OK)
            {
                string dbToDelete = cmbDatabases.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrEmpty(dbToDelete)) return;

                DialogResult result = MessageBox.Show($"CZY NA PEWNO chcesz całkowicie skasować bazę danych [{dbToDelete}]? Stracisz wszystkie zawarte w niej tabele i rekordy!", "KRYTYCZNE OSTRZEŻENIE", MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
                if (result == DialogResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(masterConnectionString))
                    {
                        try
                        {
                            conn.Open();
                            string alterQuery = $"ALTER DATABASE [{dbToDelete}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                            string dropQuery = $"DROP DATABASE [{dbToDelete}]";

                            using (SqlCommand cmdAlter = new SqlCommand(alterQuery, conn)) { cmdAlter.ExecuteNonQuery(); }
                            using (SqlCommand cmdDrop = new SqlCommand(dropQuery, conn)) { cmdDrop.ExecuteNonQuery(); }

                            MessageBox.Show($"Baza danych '{dbToDelete}' została trwale usunięta z serwera.", "Sukces");
                        }
                        catch (Exception ex) { MessageBox.Show($"Błąd SQL podczas usuwania bazy: {ex.Message}"); }
                    }
                }
            }
        }
    }
}
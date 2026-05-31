using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Windows.Forms;
using System.Data;
using System.Drawing;

namespace KolekcjUZ
{
    public partial class Form1 : Form
    {
        private string masterConnectionString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;";
        private string currentDatabaseName = "";

        // Lista typów oferowanych użytkownikowi w kreatorach (odpowiada enum ColumnType z diagramu klas).
        private readonly string[] dostepneTypy = new string[]
        {
            "INT", "VARCHAR(250)", "DECIMAL(18,2)", "BIT", "DATETIME", "FLOAT"
        };

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

            // NOWE: edycja schematu istniejącej tabeli (dodaj / edytuj / usuń kolumnę).
            Button btnEditSchema = new Button() { Text = "Edytuj Schemat", Left = 700, Top = 12, Size = new System.Drawing.Size(120, 35), BackColor = System.Drawing.Color.LightYellow };
            btnEditSchema.Click += (s, e) => UruchomEdycjeSchematu();

            // NOWE: definiowanie relacji (kluczy obcych) między tabelami.
            Button btnRelations = new Button() { Text = "Definiuj Relacje", Left = 835, Top = 12, Size = new System.Drawing.Size(130, 35), BackColor = System.Drawing.Color.LightGreen };
            btnRelations.Click += (s, e) => UruchomDefiniowanieRelacji();

            if (this.Controls.Find("panelTopMenu", true).Length > 0)
            {
                Control panel = this.Controls.Find("panelTopMenu", true)[0];
                panel.Controls.AddRange(new Control[] { btnManageData, btnAddTable, btnDeleteTable, btnDeleteDb, btnEditSchema, btnRelations });
            }
        }

        // ----------------------------------------------------------------------
        //  POMOCNICZE: połączenia, listy baz/tabel/kolumn, dialogi wyboru
        // ----------------------------------------------------------------------

        private string PolaczenieZBaza(string dbName)
        {
            return $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Integrated Security=True;TrustServerCertificate=True;";
        }

        private List<string> PobierzBazyDanych()
        {
            List<string> bazy = new List<string>();
            using (SqlConnection conn = new SqlConnection(masterConnectionString))
            {
                conn.Open();
                string query = "SELECT name FROM sys.databases WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) bazy.Add(reader["name"].ToString());
                }
            }
            return bazy;
        }

        private List<string> PobierzTabele(string dbName)
        {
            List<string> tabele = new List<string>();
            using (SqlConnection conn = new SqlConnection(PolaczenieZBaza(dbName)))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT name FROM sys.tables ORDER BY name", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) tabele.Add(reader["name"].ToString());
                }
            }
            return tabele;
        }

        // Reprezentacja kolumny (odpowiada klasie Column z diagramu klas).
        private class KolumnaInfo
        {
            public string Nazwa;
            public string TypOpis;     // np. "varchar(250)", "int", "decimal(18,2)"
            public bool Nullable;
            public bool IsIdentity;
        }

        private List<KolumnaInfo> PobierzKolumny(string dbName, string tableName)
        {
            List<KolumnaInfo> kolumny = new List<KolumnaInfo>();
            using (SqlConnection conn = new SqlConnection(PolaczenieZBaza(dbName)))
            {
                conn.Open();
                string query = @"
                    SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
                           c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.IS_NULLABLE,
                           COLUMNPROPERTY(OBJECT_ID(c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
                    FROM INFORMATION_SCHEMA.COLUMNS c
                    WHERE c.TABLE_NAME = @t
                    ORDER BY c.ORDINAL_POSITION";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@t", tableName);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dataType = reader["DATA_TYPE"].ToString().ToLower();
                            string opis = dataType;

                            if (dataType == "varchar" || dataType == "nvarchar" || dataType == "char" || dataType == "nchar")
                            {
                                object len = reader["CHARACTER_MAXIMUM_LENGTH"];
                                if (len != DBNull.Value)
                                {
                                    int l = Convert.ToInt32(len);
                                    opis += l == -1 ? "(MAX)" : $"({l})";
                                }
                            }
                            else if (dataType == "decimal" || dataType == "numeric")
                            {
                                object prec = reader["NUMERIC_PRECISION"];
                                object scale = reader["NUMERIC_SCALE"];
                                if (prec != DBNull.Value && scale != DBNull.Value)
                                    opis += $"({Convert.ToInt32(prec)},{Convert.ToInt32(scale)})";
                            }

                            kolumny.Add(new KolumnaInfo
                            {
                                Nazwa = reader["COLUMN_NAME"].ToString(),
                                TypOpis = opis,
                                Nullable = reader["IS_NULLABLE"].ToString() == "YES",
                                IsIdentity = reader["IsIdentity"] != DBNull.Value && Convert.ToInt32(reader["IsIdentity"]) == 1
                            });
                        }
                    }
                }
            }
            return kolumny;
        }

        // Dialog wyboru bazy danych. Zwraca nazwę bazy lub null.
        private string WybierzBazeDialog(string tytul)
        {
            List<string> bazy;
            try { bazy = PobierzBazyDanych(); }
            catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return null; }

            if (bazy.Count == 0) { MessageBox.Show("Brak dostępnych baz."); return null; }

            Form f = new Form() { Width = 350, Height = 170, Text = tytul, StartPosition = FormStartPosition.CenterParent };
            Label lbl = new Label() { Text = "Wybierz bazę danych:", Left = 20, Top = 20, Width = 120 };
            ComboBox cmb = new ComboBox() { Left = 150, Top = 18, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.Items.AddRange(bazy.Cast<object>().ToArray());
            cmb.SelectedIndex = 0;
            Button btnOk = new Button() { Text = "Dalej", Left = 150, Top = 60, Width = 100, DialogResult = DialogResult.OK };
            f.Controls.AddRange(new Control[] { lbl, cmb, btnOk });
            f.AcceptButton = btnOk;

            if (f.ShowDialog() == DialogResult.OK)
                return cmb.SelectedItem?.ToString();
            return null;
        }

        // Dialog wyboru tabeli w danej bazie. Zwraca nazwę tabeli lub null.
        private string WybierzTabeleDialog(string dbName, string tytul)
        {
            List<string> tabele;
            try { tabele = PobierzTabele(dbName); }
            catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return null; }

            if (tabele.Count == 0) { MessageBox.Show("Ta baza nie ma tabel."); return null; }

            Form f = new Form() { Width = 350, Height = 170, Text = tytul, StartPosition = FormStartPosition.CenterParent };
            Label lbl = new Label() { Text = "Wybierz tabelę:", Left = 20, Top = 20, Width = 120 };
            ComboBox cmb = new ComboBox() { Left = 150, Top = 18, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.Items.AddRange(tabele.Cast<object>().ToArray());
            cmb.SelectedIndex = 0;
            Button btnOk = new Button() { Text = "Dalej", Left = 150, Top = 60, Width = 100, DialogResult = DialogResult.OK };
            f.Controls.AddRange(new Control[] { lbl, cmb, btnOk });
            f.AcceptButton = btnOk;

            if (f.ShowDialog() == DialogResult.OK)
                return cmb.SelectedItem?.ToString();
            return null;
        }

        // ----------------------------------------------------------------------
        //  WALIDACJA DANYCH (krok "Dane poprawne?" ze schematu blokowego)
        // ----------------------------------------------------------------------

        // Próbuje skonwertować tekst z formularza na typ kolumny.
        // Zwraca true gdy poprawne. Pusty tekst -> NULL (jeśli dozwolony).
        private bool SprobujKonwertowac(Type clrType, string tekst, out object wartosc, out string blad)
        {
            blad = null;
            tekst = tekst?.Trim() ?? "";

            if (tekst.Length == 0)
            {
                wartosc = DBNull.Value;
                return true;
            }

            try
            {
                if (clrType == typeof(string))
                {
                    wartosc = tekst;
                    return true;
                }

                if (clrType == typeof(bool))
                {
                    string t = tekst.ToLower();
                    if (t == "1" || t == "true" || t == "tak" || t == "yes" || t == "prawda") { wartosc = true; return true; }
                    if (t == "0" || t == "false" || t == "nie" || t == "no" || t == "fałsz" || t == "falsz") { wartosc = false; return true; }
                    wartosc = null;
                    blad = "wartość logiczna (dozwolone: 0/1, tak/nie, true/false)";
                    return false;
                }

                if (clrType == typeof(DateTime))
                {
                    if (DateTime.TryParse(tekst, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt) ||
                        DateTime.TryParse(tekst, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        wartosc = dt;
                        return true;
                    }
                    wartosc = null;
                    blad = "data/czas (np. 2025-01-31 lub 31.01.2025)";
                    return false;
                }

                // Typy liczbowe i pozostałe — próba konwersji niezależnej od kultury.
                wartosc = Convert.ChangeType(tekst, clrType, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                wartosc = null;
                if (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(short) || clrType == typeof(byte))
                    blad = "liczba całkowita";
                else if (clrType == typeof(decimal) || clrType == typeof(double) || clrType == typeof(float))
                    blad = "liczba (kropka jako separator dziesiętny)";
                else
                    blad = $"wartość typu {clrType.Name}";
                return false;
            }
        }

        // ----------------------------------------------------------------------
        //  ISTNIEJĄCE: dodawanie tabeli, tworzenie/usuwanie bazy
        // ----------------------------------------------------------------------

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
            cmbType.Items.AddRange(dostepneTypy);
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

                string specificDbConnectionString = PolaczenieZBaza(currentDatabaseName);

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

        // ----------------------------------------------------------------------
        //  MENEDŻER DANYCH — pełny CRUD (Create / Read / Update / Delete)
        // ----------------------------------------------------------------------

        private void OtworzMenedzerDanych(string tableName)
        {
            Form dataForm = new Form { Text = $"Dane tabeli: {tableName} (Baza: {currentDatabaseName})", MdiParent = this, Width = 800, Height = 600 };

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

            Label lblHint = new Label
            {
                Top = 270,
                Left = 15,
                Width = 750,
                Height = 18,
                ForeColor = System.Drawing.Color.DimGray,
                Text = "Kliknij wiersz, aby wczytać go do formularza i edytować. Puste pole = NULL."
            };

            FlowLayoutPanel panelFields = new FlowLayoutPanel { Top = 292, Left = 15, Width = 750, Height = 110, AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };

            Button btnSaveRecord = new Button { Text = "Dodaj jako NOWY rekord", Top = 410, Left = 15, Width = 365, Height = 35, BackColor = System.Drawing.Color.Honeydew };
            Button btnUpdateRecord = new Button { Text = "Zapisz ZMIANY w zaznaczonym", Top = 410, Left = 400, Width = 365, Height = 35, BackColor = System.Drawing.Color.LightCyan };
            Button btnClearForm = new Button { Text = "Wyczyść formularz (nowy rekord)", Top = 452, Left = 15, Width = 365, Height = 30 };
            Button btnDeleteRecord = new Button { Text = "Usuń Zaznaczony Rekord", Top = 452, Left = 400, Width = 365, Height = 30, BackColor = System.Drawing.Color.MistyRose };

            string connectionString = PolaczenieZBaza(currentDatabaseName);
            List<TextBox> textBoxesList = new List<TextBox>();
            List<string> columnNames = new List<string>();
            List<Type> columnTypes = new List<Type>();      // typ .NET każdej edytowalnej kolumny (do walidacji)
            string[] selectedId = { null };                 // Id aktualnie wczytanego rekordu (null = tryb dodawania)

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

            // Budowa formularza na podstawie schematu tabeli (pomijamy kolumnę IDENTITY).
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
                                Type clrType = row["DataType"] as Type ?? typeof(string);

                                if (isIdentity) continue;

                                Panel p = new Panel { Width = 220, Height = 50 };
                                Label l = new Label { Text = $"{columnName} ({clrType.Name})", Top = 5, Left = 5, Width = 200 };
                                TextBox t = new TextBox { Top = 25, Left = 5, Width = 200, Tag = columnName };

                                p.Controls.AddRange(new Control[] { l, t });
                                panelFields.Controls.Add(p);

                                textBoxesList.Add(t);
                                columnNames.Add(columnName);
                                columnTypes.Add(clrType);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd pobierania struktury tabeli: {ex.Message}");
                }
            }

            // Kliknięcie wiersza -> wczytanie wartości do formularza (tryb edycji / UPDATE).
            dgv.SelectionChanged += (s, ev) => {
                if (dgv.SelectedRows.Count == 0) return;
                var rowView = dgv.SelectedRows[0];
                if (rowView.Cells["Id"]?.Value == null) return;

                selectedId[0] = rowView.Cells["Id"].Value.ToString();
                foreach (var tb in textBoxesList)
                {
                    string colName = tb.Tag.ToString();
                    object val = rowView.Cells[colName]?.Value;
                    tb.Text = (val == null || val == DBNull.Value) ? "" : val.ToString();
                }
                btnUpdateRecord.Text = $"Zapisz ZMIANY (Id = {selectedId[0]})";
            };

            btnClearForm.Click += (s, ev) => {
                selectedId[0] = null;
                foreach (var tb in textBoxesList) tb.Clear();
                dgv.ClearSelection();
                btnUpdateRecord.Text = "Zapisz ZMIANY w zaznaczonym";
                if (textBoxesList.Count > 0) textBoxesList[0].Focus();
            };

            OdswiezDane();

            // CREATE — dodanie nowego rekordu (z walidacją typów).
            btnSaveRecord.Click += (s, ev) => {
                if (columnNames.Count == 0) return;

                if (!ZbierzIWalidujWartosci(textBoxesList, columnTypes, out Dictionary<string, object> wartosci))
                    return; // komunikat błędu został już pokazany

                string cols = string.Join(", ", columnNames.Select(c => $"[{c}]"));
                string paramsJoined = string.Join(", ", columnNames.Select(c => "@" + c));
                string insertQuery = $"INSERT INTO [{tableName}] ({cols}) VALUES ({paramsJoined})";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            foreach (var kv in wartosci)
                                insertCmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                            insertCmd.ExecuteNonQuery();
                        }
                        MessageBox.Show("Pomyślnie dodano nowy rekord! (zmiany zapisane automatycznie)", "Sukces");
                        selectedId[0] = null;
                        foreach (var tb in textBoxesList) tb.Clear();
                        OdswiezDane();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas zapisu rekordu: {ex.Message}");
                    }
                }
            };

            // UPDATE — zapis zmian w zaznaczonym rekordzie (z walidacją typów).
            btnUpdateRecord.Click += (s, ev) => {
                if (string.IsNullOrEmpty(selectedId[0]))
                {
                    MessageBox.Show("Najpierw kliknij w tabeli wiersz, który chcesz edytować.", "Informacja");
                    return;
                }
                if (columnNames.Count == 0) return;

                if (!ZbierzIWalidujWartosci(textBoxesList, columnTypes, out Dictionary<string, object> wartosci))
                    return;

                string setClause = string.Join(", ", columnNames.Select(c => $"[{c}] = @{c}"));
                string updateQuery = $"UPDATE [{tableName}] SET {setClause} WHERE Id = @__Id";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            foreach (var kv in wartosci)
                                updateCmd.Parameters.AddWithValue("@" + kv.Key, kv.Value ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@__Id", selectedId[0]);
                            int rows = updateCmd.ExecuteNonQuery();
                            MessageBox.Show($"Zaktualizowano rekord (Id = {selectedId[0]}). Zmienione wiersze: {rows}.", "Sukces");
                        }
                        OdswiezDane();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas aktualizacji rekordu: {ex.Message}");
                    }
                }
            };

            // DELETE — usunięcie zaznaczonego rekordu (operacja destrukcyjna -> potwierdzenie).
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
                            selectedId[0] = null;
                            foreach (var tb in textBoxesList) tb.Clear();
                            OdswiezDane();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Błąd SQL podczas usuwania rekordu: {ex.Message}");
                        }
                    }
                }
            };

            dataForm.Controls.AddRange(new Control[] { dgv, lblHint, panelFields, btnSaveRecord, btnUpdateRecord, btnClearForm, btnDeleteRecord });
            dataForm.Show();
        }

        // Zbiera wartości z formularza, waliduje je względem typów kolumn.
        // Zwraca false i pokazuje komunikat, jeśli któraś wartość jest niepoprawna.
        private bool ZbierzIWalidujWartosci(List<TextBox> pola, List<Type> typy, out Dictionary<string, object> wynik)
        {
            wynik = new Dictionary<string, object>();
            for (int i = 0; i < pola.Count; i++)
            {
                string colName = pola[i].Tag.ToString();
                Type clrType = typy[i];
                if (!SprobujKonwertowac(clrType, pola[i].Text, out object val, out string blad))
                {
                    MessageBox.Show($"Niepoprawna wartość w polu '{colName}'.\nOczekiwano: {blad}.", "Błąd walidacji danych", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    pola[i].Focus();
                    return false;
                }
                wynik[colName] = val;
            }
            return true;
        }

        // ----------------------------------------------------------------------
        //  EDYTOR SCHEMATU — modyfikacja kolumn istniejącej tabeli
        // ----------------------------------------------------------------------

        private void UruchomEdycjeSchematu()
        {
            string db = WybierzBazeDialog("Edycja schematu — wybór bazy");
            if (string.IsNullOrEmpty(db)) return;
            currentDatabaseName = db;

            string tabela = WybierzTabeleDialog(db, "Edycja schematu — wybór tabeli");
            if (string.IsNullOrEmpty(tabela)) return;

            OtworzEdytorSchematu(tabela);
        }

        private void OtworzEdytorSchematu(string tableName)
        {
            string connectionString = PolaczenieZBaza(currentDatabaseName);

            Form f = new Form { Text = $"Schemat tabeli: {tableName} (Baza: {currentDatabaseName})", MdiParent = this, Width = 640, Height = 520 };

            ListView lv = new ListView { Top = 15, Left = 15, Width = 600, Height = 220, View = View.Details, FullRowSelect = true };
            lv.Columns.Add("Kolumna", 240);
            lv.Columns.Add("Typ", 220);
            lv.Columns.Add("NULL?", 70);
            lv.Columns.Add("Klucz", 60);

            Label lblName = new Label { Text = "Nazwa kolumny:", Top = 250, Left = 15, Width = 100 };
            TextBox txtName = new TextBox { Top = 247, Left = 120, Width = 160 };
            Label lblType = new Label { Text = "Typ:", Top = 250, Left = 300, Width = 40 };
            ComboBox cmbType = new ComboBox { Top = 247, Left = 345, Width = 140, DropDownStyle = ComboBoxStyle.DropDown };
            cmbType.Items.AddRange(dostepneTypy);
            cmbType.SelectedIndex = 1;
            CheckBox chkNullable = new CheckBox { Text = "Dopuszcza NULL", Top = 248, Left = 500, Width = 120, Checked = true };

            Button btnAdd = new Button { Text = "Dodaj kolumnę", Top = 290, Left = 15, Width = 180, Height = 35, BackColor = System.Drawing.Color.Honeydew };
            Button btnAlter = new Button { Text = "Zmień zaznaczoną kolumnę", Top = 290, Left = 210, Width = 200, Height = 35, BackColor = System.Drawing.Color.LightCyan };
            Button btnDrop = new Button { Text = "Usuń zaznaczoną kolumnę", Top = 290, Left = 425, Width = 190, Height = 35, BackColor = System.Drawing.Color.MistyRose };

            Label lblStatus = new Label { Top = 340, Left = 15, Width = 600, Height = 120, ForeColor = System.Drawing.Color.DimGray, Text = "Wskazówka: zmiany w schemacie są zapisywane automatycznie. Kolumny IDENTITY (Id) nie można usunąć ani zmienić." };

            Action odswiez = () => {
                lv.Items.Clear();
                try
                {
                    foreach (var k in PobierzKolumny(currentDatabaseName, tableName))
                    {
                        var item = new ListViewItem(k.Nazwa);
                        item.SubItems.Add(k.TypOpis);
                        item.SubItems.Add(k.Nullable ? "TAK" : "NIE");
                        item.SubItems.Add(k.IsIdentity ? "PK/ID" : "");
                        item.Tag = k;
                        lv.Items.Add(item);
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Błąd odczytu schematu: {ex.Message}"); }
            };

            lv.SelectedIndexChanged += (s, ev) => {
                if (lv.SelectedItems.Count == 0) return;
                var k = lv.SelectedItems[0].Tag as KolumnaInfo;
                if (k == null) return;
                txtName.Text = k.Nazwa;
                cmbType.Text = k.TypOpis.ToUpper();
                chkNullable.Checked = k.Nullable;
            };

            odswiez();

            // Dodanie kolumny do istniejącej tabeli (ALTER TABLE ... ADD).
            btnAdd.Click += (s, ev) => {
                string nazwa = txtName.Text.Trim().Replace(" ", "_");
                string typ = cmbType.Text.Trim();
                if (string.IsNullOrEmpty(nazwa) || string.IsNullOrEmpty(typ)) { MessageBox.Show("Podaj nazwę i typ kolumny."); return; }

                string nullSql = chkNullable.Checked ? "NULL" : "NOT NULL";
                string sql = $"ALTER TABLE [{tableName}] ADD [{nazwa}] {typ} {nullSql}";
                WykonajNonQuery(connectionString, sql, $"Dodano kolumnę '{nazwa}'.");
                odswiez();
            };

            // Zmiana zaznaczonej kolumny: nazwa (sp_rename) i/lub typ (ALTER COLUMN).
            btnAlter.Click += (s, ev) => {
                if (lv.SelectedItems.Count == 0) { MessageBox.Show("Zaznacz kolumnę do zmiany."); return; }
                var k = lv.SelectedItems[0].Tag as KolumnaInfo;
                if (k == null) return;
                if (k.IsIdentity) { MessageBox.Show("Nie można modyfikować kolumny klucza głównego (IDENTITY)."); return; }

                string nowaNazwa = txtName.Text.Trim().Replace(" ", "_");
                string nowyTyp = cmbType.Text.Trim();
                if (string.IsNullOrEmpty(nowaNazwa) || string.IsNullOrEmpty(nowyTyp)) { MessageBox.Show("Podaj nazwę i typ."); return; }
                string nullSql = chkNullable.Checked ? "NULL" : "NOT NULL";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        // 1) Zmiana nazwy, jeśli różna.
                        if (!string.Equals(nowaNazwa, k.Nazwa, StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand cmd = new SqlCommand("EXEC sp_rename @obj, @new, 'COLUMN'", conn))
                            {
                                cmd.Parameters.AddWithValue("@obj", $"{tableName}.{k.Nazwa}");
                                cmd.Parameters.AddWithValue("@new", nowaNazwa);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 2) Zmiana typu / NULL.
                        string alter = $"ALTER TABLE [{tableName}] ALTER COLUMN [{nowaNazwa}] {nowyTyp} {nullSql}";
                        using (SqlCommand cmd = new SqlCommand(alter, conn)) { cmd.ExecuteNonQuery(); }

                        MessageBox.Show($"Kolumna zaktualizowana ('{k.Nazwa}' -> '{nowaNazwa}', {nowyTyp}).", "Sukces");
                    }
                    catch (Exception ex) { MessageBox.Show($"Błąd zmiany kolumny: {ex.Message}"); }
                }
                odswiez();
            };

            // Usunięcie kolumny (operacja destrukcyjna -> potwierdzenie).
            btnDrop.Click += (s, ev) => {
                if (lv.SelectedItems.Count == 0) { MessageBox.Show("Zaznacz kolumnę do usunięcia."); return; }
                var k = lv.SelectedItems[0].Tag as KolumnaInfo;
                if (k == null) return;
                if (k.IsIdentity) { MessageBox.Show("Nie można usunąć kolumny klucza głównego (IDENTITY)."); return; }

                DialogResult r = MessageBox.Show($"Czy na pewno usunąć kolumnę [{k.Nazwa}] z tabeli [{tableName}]? Utracisz dane w tej kolumnie!", "OSTRZEŻENIE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;

                string sql = $"ALTER TABLE [{tableName}] DROP COLUMN [{k.Nazwa}]";
                WykonajNonQuery(connectionString, sql, $"Usunięto kolumnę '{k.Nazwa}'.");
                odswiez();
            };

            f.Controls.AddRange(new Control[] { lv, lblName, txtName, lblType, cmbType, chkNullable, btnAdd, btnAlter, btnDrop, lblStatus });
            f.Show();
        }

        // ----------------------------------------------------------------------
        //  EDYTOR RELACJI — klucze obce między tabelami (typ kolumny RELATION)
        // ----------------------------------------------------------------------

        private void UruchomDefiniowanieRelacji()
        {
            string db = WybierzBazeDialog("Relacje — wybór bazy");
            if (string.IsNullOrEmpty(db)) return;
            currentDatabaseName = db;
            OtworzEdytorRelacji();
        }

        private void OtworzEdytorRelacji()
        {
            string connectionString = PolaczenieZBaza(currentDatabaseName);
            List<string> tabele;
            try { tabele = PobierzTabele(currentDatabaseName); }
            catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}"); return; }
            if (tabele.Count < 1) { MessageBox.Show("Baza nie zawiera tabel."); return; }

            Form f = new Form { Text = $"Relacje między tabelami (Baza: {currentDatabaseName})", MdiParent = this, Width = 720, Height = 560 };

            ListView lv = new ListView { Top = 15, Left = 15, Width = 680, Height = 200, View = View.Details, FullRowSelect = true };
            lv.Columns.Add("Nazwa relacji (FK)", 220);
            lv.Columns.Add("Z tabeli.kolumny", 220);
            lv.Columns.Add("Do tabeli.kolumny", 220);

            Label lblFrom = new Label { Text = "Tabela źródłowa:", Top = 235, Left = 15, Width = 110 };
            ComboBox cmbFromTable = new ComboBox { Top = 232, Left = 130, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFromTable.Items.AddRange(tabele.Cast<object>().ToArray());

            RadioButton rbNewCol = new RadioButton { Text = "Utwórz nową kolumnę-klucz:", Top = 268, Left = 15, Width = 200, Checked = true };
            TextBox txtNewCol = new TextBox { Top = 266, Left = 220, Width = 160 };
            RadioButton rbExistingCol = new RadioButton { Text = "Użyj istniejącej kolumny:", Top = 298, Left = 15, Width = 200 };
            ComboBox cmbFromCol = new ComboBox { Top = 296, Left = 220, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };

            Label lblTo = new Label { Text = "Tabela docelowa:", Top = 335, Left = 15, Width = 110 };
            ComboBox cmbToTable = new ComboBox { Top = 332, Left = 130, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbToTable.Items.AddRange(tabele.Cast<object>().ToArray());
            Label lblToCol = new Label { Text = "Kolumna docelowa:", Top = 335, Left = 330, Width = 120 };
            ComboBox cmbToCol = new ComboBox { Top = 332, Left = 455, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };

            Button btnCreate = new Button { Text = "Utwórz relację", Top = 375, Left = 15, Width = 300, Height = 38, BackColor = System.Drawing.Color.LightGreen };
            Button btnDelete = new Button { Text = "Usuń zaznaczoną relację", Top = 375, Left = 330, Width = 285, Height = 38, BackColor = System.Drawing.Color.MistyRose };

            Label lblHint = new Label { Top = 425, Left = 15, Width = 680, Height = 70, ForeColor = System.Drawing.Color.DimGray, Text = "Relacja = klucz obcy. Kolumna źródłowa musi być typu zgodnego z kolumną docelową (zwykle INT i kolumna Id). Nowo tworzona kolumna-klucz ma typ INT." };

            Action zaladujKolumnyZrodla = () => {
                cmbFromCol.Items.Clear();
                if (cmbFromTable.SelectedItem == null) return;
                foreach (var k in PobierzKolumny(currentDatabaseName, cmbFromTable.SelectedItem.ToString()))
                    cmbFromCol.Items.Add(k.Nazwa);
                if (cmbFromCol.Items.Count > 0) cmbFromCol.SelectedIndex = 0;
            };

            Action zaladujKolumnyCelu = () => {
                cmbToCol.Items.Clear();
                if (cmbToTable.SelectedItem == null) return;
                foreach (var k in PobierzKolumny(currentDatabaseName, cmbToTable.SelectedItem.ToString()))
                    cmbToCol.Items.Add(k.Nazwa);
                // Domyślnie wskaż klucz główny Id, jeśli istnieje.
                int idx = cmbToCol.Items.IndexOf("Id");
                cmbToCol.SelectedIndex = idx >= 0 ? idx : (cmbToCol.Items.Count > 0 ? 0 : -1);
            };

            Action odswiezRelacje = () => {
                lv.Items.Clear();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        string q = @"
                            SELECT fk.name AS FkName, tp.name AS FromTable, cp.name AS FromCol,
                                   tr.name AS ToTable, cr.name AS ToCol
                            FROM sys.foreign_keys fk
                            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                            JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
                            JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
                            JOIN sys.tables tr ON tr.object_id = fk.referenced_object_id
                            JOIN sys.columns cr ON cr.object_id = tr.object_id AND cr.column_id = fkc.referenced_column_id
                            ORDER BY fk.name";
                        using (SqlCommand cmd = new SqlCommand(q, conn))
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new ListViewItem(reader["FkName"].ToString());
                                item.SubItems.Add($"{reader["FromTable"]}.{reader["FromCol"]}");
                                item.SubItems.Add($"{reader["ToTable"]}.{reader["ToCol"]}");
                                item.Tag = new string[] { reader["FromTable"].ToString(), reader["FkName"].ToString() };
                                lv.Items.Add(item);
                            }
                        }
                    }
                    catch (Exception ex) { MessageBox.Show($"Błąd odczytu relacji: {ex.Message}"); }
                }
            };

            cmbFromTable.SelectedIndexChanged += (s, ev) => zaladujKolumnyZrodla();
            cmbToTable.SelectedIndexChanged += (s, ev) => zaladujKolumnyCelu();
            rbNewCol.CheckedChanged += (s, ev) => { txtNewCol.Enabled = rbNewCol.Checked; cmbFromCol.Enabled = !rbNewCol.Checked; };

            if (cmbFromTable.Items.Count > 0) cmbFromTable.SelectedIndex = 0;
            if (cmbToTable.Items.Count > 0) cmbToTable.SelectedIndex = 0;
            odswiezRelacje();

            btnCreate.Click += (s, ev) => {
                if (cmbFromTable.SelectedItem == null || cmbToTable.SelectedItem == null) { MessageBox.Show("Wybierz tabelę źródłową i docelową."); return; }
                string fromTable = cmbFromTable.SelectedItem.ToString();
                string toTable = cmbToTable.SelectedItem.ToString();
                string toCol = cmbToCol.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(toCol)) { MessageBox.Show("Wybierz kolumnę docelową."); return; }

                string fromCol;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();

                        if (rbNewCol.Checked)
                        {
                            fromCol = txtNewCol.Text.Trim().Replace(" ", "_");
                            if (string.IsNullOrEmpty(fromCol)) fromCol = $"{toTable}Id";
                            string addCol = $"ALTER TABLE [{fromTable}] ADD [{fromCol}] INT NULL";
                            using (SqlCommand cmd = new SqlCommand(addCol, conn)) { cmd.ExecuteNonQuery(); }
                        }
                        else
                        {
                            fromCol = cmbFromCol.SelectedItem?.ToString();
                            if (string.IsNullOrEmpty(fromCol)) { MessageBox.Show("Wybierz istniejącą kolumnę źródłową."); return; }
                        }

                        string fkName = $"FK_{fromTable}_{fromCol}_{toTable}";
                        string addFk = $"ALTER TABLE [{fromTable}] ADD CONSTRAINT [{fkName}] FOREIGN KEY ([{fromCol}]) REFERENCES [{toTable}]([{toCol}])";
                        using (SqlCommand cmd = new SqlCommand(addFk, conn)) { cmd.ExecuteNonQuery(); }

                        MessageBox.Show($"Utworzono relację: {fromTable}.{fromCol} -> {toTable}.{toCol}", "Sukces");
                    }
                    catch (Exception ex) { MessageBox.Show($"Błąd tworzenia relacji: {ex.Message}"); return; }
                }
                txtNewCol.Clear();
                zaladujKolumnyZrodla();
                odswiezRelacje();
            };

            btnDelete.Click += (s, ev) => {
                if (lv.SelectedItems.Count == 0) { MessageBox.Show("Zaznacz relację do usunięcia."); return; }
                var tag = lv.SelectedItems[0].Tag as string[];
                if (tag == null) return;
                string fromTable = tag[0];
                string fkName = tag[1];

                DialogResult r = MessageBox.Show($"Czy na pewno usunąć relację [{fkName}]?", "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;

                string sql = $"ALTER TABLE [{fromTable}] DROP CONSTRAINT [{fkName}]";
                WykonajNonQuery(connectionString, sql, $"Usunięto relację '{fkName}'.");
                odswiezRelacje();
            };

            f.Controls.AddRange(new Control[]
            {
                lv, lblFrom, cmbFromTable, rbNewCol, txtNewCol, rbExistingCol, cmbFromCol,
                lblTo, cmbToTable, lblToCol, cmbToCol, btnCreate, btnDelete, lblHint
            });
            f.Show();
        }

        // Pomocnicze wykonanie zapytania bez wyniku + komunikat o sukcesie.
        private void WykonajNonQuery(string connectionString, string sql, string komunikatSukcesu)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn)) { cmd.ExecuteNonQuery(); }
                    if (!string.IsNullOrEmpty(komunikatSukcesu))
                        MessageBox.Show(komunikatSukcesu, "Sukces");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd SQL: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ----------------------------------------------------------------------
        //  ISTNIEJĄCE: wybór bazy/tabeli, usuwanie tabeli i bazy
        // ----------------------------------------------------------------------

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
            string dbConnectionString = PolaczenieZBaza(currentDatabaseName);
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

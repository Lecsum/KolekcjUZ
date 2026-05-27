using System;
using System.Windows.Forms;

namespace DynamicDatabaseApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            // TO JEST KLUCZOWE - bez tego designer się nie załaduje:
            InitializeComponent();

            this.IsMdiContainer = true;
        }

        private void btnCreateDb_Click(object sender, EventArgs e)
        {
            Form childForm = new Form();
            childForm.Text = "Nowa Baza Danych";
            childForm.MdiParent = this;
            childForm.Width = 400;
            childForm.Height = 300;

            Label lbl = new Label() { Text = "Nazwa bazy:", Top = 20, Left = 20 };
            TextBox txt = new TextBox() { Top = 20, Left = 130, Width = 200 };
            Button btnSave = new Button() { Text = "Stwórz", Top = 60, Left = 130 };

            childForm.Controls.AddRange(new Control[] { lbl, txt, btnSave });
            childForm.Show();
        }

        private void btnListDbs_Click(object sender, EventArgs e)
        {
            Form listForm = new Form();
            listForm.Text = "Lista Baz Danych";
            listForm.MdiParent = this;
            listForm.Width = 500;
            listForm.Height = 400;

            ListBox listBox = new ListBox() { Dock = DockStyle.Fill };
            listBox.Items.Add("Baza_Klientów.json");
            listBox.Items.Add("Magazyn_Książek.json");

            listForm.Controls.Add(listBox);
            listForm.Show();
        }
    }
}
namespace KolekcjUZ
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelTopMenu = new System.Windows.Forms.Panel();
            this.btnListDbs = new System.Windows.Forms.Button();
            this.btnCreateDb = new System.Windows.Forms.Button();
            this.panelTopMenu.SuspendLayout();
            this.SuspendLayout();
   
            this.panelTopMenu.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panelTopMenu.Controls.Add(this.btnListDbs);
            this.panelTopMenu.Controls.Add(this.btnCreateDb);
            this.panelTopMenu.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTopMenu.Location = new System.Drawing.Point(0, 0);
            this.panelTopMenu.Name = "panelTopMenu";
            this.panelTopMenu.Size = new System.Drawing.Size(984, 60);
            this.panelTopMenu.TabIndex = 1;
    
            this.btnListDbs.Location = new System.Drawing.Point(160, 12);
            this.btnListDbs.Name = "btnListDbs";
            this.btnListDbs.Size = new System.Drawing.Size(130, 35);
            this.btnListDbs.TabIndex = 1;
            this.btnListDbs.Text = "Otwórz Bazę";
            this.btnListDbs.UseVisualStyleBackColor = true;
            this.btnListDbs.Click += new System.EventHandler(this.btnListDbs_Click);
   
            this.btnCreateDb.Location = new System.Drawing.Point(12, 12);
            this.btnCreateDb.Name = "btnCreateDb";
            this.btnCreateDb.Size = new System.Drawing.Size(132, 35);
            this.btnCreateDb.TabIndex = 0;
            this.btnCreateDb.Text = "Stwórz Nową Bazu";
            this.btnCreateDb.UseVisualStyleBackColor = true;
            this.btnCreateDb.Click += new System.EventHandler(this.btnCreateDb_Click);

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.panelTopMenu);
            this.IsMdiContainer = true;
            this.Name = "Form1";
            this.Text = "Kreator Baz Danych SQL Server";
            this.panelTopMenu.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelTopMenu;
        private System.Windows.Forms.Button btnListDbs;
        private System.Windows.Forms.Button btnCreateDb;
    }
}
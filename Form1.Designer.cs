namespace FromSoftMapConverter
{
    partial class FromSoftMapConverter
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>s
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FromSoftMapConverter));
            ribbon = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openCtrlOToolStripMenuItem = new ToolStripMenuItem();
            label1 = new Label();
            label2 = new Label();
            statusLabel = new Label();
            ribbon.SuspendLayout();
            SuspendLayout();
            // 
            // ribbon
            // 
            ribbon.BackColor = Color.FromArgb(224, 224, 224);
            ribbon.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            ribbon.Location = new Point(0, 0);
            ribbon.Name = "ribbon";
            ribbon.Size = new Size(295, 24);
            ribbon.TabIndex = 0;
            ribbon.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openCtrlOToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openCtrlOToolStripMenuItem
            // 
            openCtrlOToolStripMenuItem.Name = "openCtrlOToolStripMenuItem";
            openCtrlOToolStripMenuItem.Size = new Size(150, 22);
            openCtrlOToolStripMenuItem.Text = "Open (Ctrl+O)";
            openCtrlOToolStripMenuItem.Click += OpenCtrlOToolStripMenuItem_Click;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.BackColor = Color.FromArgb(224, 224, 224);
            label1.ForeColor = Color.Gray;
            label1.Location = new Point(117, 4);
            label1.Name = "label1";
            label1.Size = new Size(174, 15);
            label1.TabIndex = 1;
            label1.Text = "© Pear, 2023 All rights reserved.";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(4, 31);
            label2.Name = "label2";
            label2.Size = new Size(42, 15);
            label2.TabIndex = 2;
            label2.Text = "Status:";
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(45, 31);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(57, 15);
            statusLabel.TabIndex = 3;
            statusLabel.Text = "Waiting...";
            // 
            // FromSoftMapConverter
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(295, 57);
            Controls.Add(statusLabel);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(ribbon);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = ribbon;
            Margin = new Padding(2);
            Name = "FromSoftMapConverter";
            Text = "FromSoftMapConverter";
            KeyDown += FromSoftMapConverter_KeyDown;
            ribbon.ResumeLayout(false);
            ribbon.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip ribbon;
        private ToolStripMenuItem fileToolStripMenuItem;
        private Label label1;
        private ToolStripMenuItem openCtrlOToolStripMenuItem;
        private Label label2;
        private Label statusLabel;
    }
}
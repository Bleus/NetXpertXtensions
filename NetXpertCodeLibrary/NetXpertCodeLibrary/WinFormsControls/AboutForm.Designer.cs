namespace NetXpertCodeLibrary.WinForms
{
	partial class AboutForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			Cobblestone.Classes.ConsoleColor consoleColor7 = new Cobblestone.Classes.ConsoleColor();
			Cobblestone.Classes.ConsoleColor consoleColor8 = new Cobblestone.Classes.ConsoleColor();
			Cobblestone.Classes.ConsoleStyle consoleStyle3 = new Cobblestone.Classes.ConsoleStyle();
			Cobblestone.Classes.ConsoleColor consoleColor9 = new Cobblestone.Classes.ConsoleColor();
			this.logonForm = new Cobblestone.Forms.Controls.BorderedGroupBox();
			this.borderedGroupBox2 = new Cobblestone.Forms.Controls.BorderedGroupBox();
			this._about = new Cobblestone.Forms.Controls.CLI_API();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.logonForm.SuspendLayout();
			this.borderedGroupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			this.SuspendLayout();
			//
			// logonForm
			//
			this.logonForm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.logonForm.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(65)))), ((int)(((byte)(42)))), ((int)(((byte)(29)))));
			this.logonForm.BorderColor = System.Drawing.Color.Gold;
			this.logonForm.BorderRadius = 5;
			this.logonForm.BorderWidth = 3;
			this.logonForm.Controls.Add(this.borderedGroupBox2);
			this.logonForm.Controls.Add(this.pictureBox1);
			this.logonForm.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.logonForm.ForeColor = System.Drawing.Color.White;
			this.logonForm.LabelIndent = -10;
			this.logonForm.Location = new System.Drawing.Point(0, 11);
			this.logonForm.Name = "logonForm";
			this.logonForm.Size = new System.Drawing.Size(400, 507);
			this.logonForm.TabIndex = 4;
			this.logonForm.TabStop = false;
			this.logonForm.Text = "About the Cobblestone App";
			//
			// borderedGroupBox2
			//
			this.borderedGroupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.borderedGroupBox2.BorderColor = System.Drawing.Color.Gold;
			this.borderedGroupBox2.BorderRadius = 3;
			this.borderedGroupBox2.BorderWidth = 1;
			this.borderedGroupBox2.Controls.Add(this._about);
			this.borderedGroupBox2.LabelIndent = 10;
			this.borderedGroupBox2.Location = new System.Drawing.Point(10, 75);
			this.borderedGroupBox2.Name = "borderedGroupBox2";
			this.borderedGroupBox2.Size = new System.Drawing.Size(378, 419);
			this.borderedGroupBox2.TabIndex = 2;
			this.borderedGroupBox2.TabStop = false;
			this.borderedGroupBox2.Text = " About ";
			//
			// _about
			//
			this._about.BackColor = System.Drawing.Color.Black;
			this._about.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this._about.BufferSize = 2147483647;
			this._about.Clipboard = "";
			this._about.Colors = consoleColor7;
			this._about.CurPos = 0;
			this._about.DefaultColor = consoleColor8;
			this._about.Dock = System.Windows.Forms.DockStyle.Fill;
			this._about.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._about.ForeColor = System.Drawing.Color.LightGray;
			this._about.Location = new System.Drawing.Point(3, 16);
			this._about.Name = "_about";
			this._about.ReadOnly = true;
			//this._about.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this._about.SelectionEnd = 0;
			this._about.Size = new System.Drawing.Size(372, 400);
			consoleStyle3.BackColor = System.Drawing.Color.Black;
			consoleStyle3.Color = consoleColor9;
			consoleStyle3.FontGroup = Cobblestone.Classes.ConsoleFonts.MonoSpaced;
			consoleStyle3.FontSize = new decimal(new int[] {
            975,
            0,
            0,
            131072});
			consoleStyle3.ForeColor = System.Drawing.Color.LightGray;
			this._about.Style = consoleStyle3;
			this._about.TabIndex = 0;
			this._about.WordWrap = false;
			//
			// pictureBox1
			//
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.White;
			this.pictureBox1.Image = global::Cobblestone.Properties.Resources.Cobblestone;
			this.pictureBox1.Location = new System.Drawing.Point(5, 16);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(390, 53);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			//
			// pictureBox2
			//
			this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox2.Image = global::Cobblestone.Properties.Resources.CloseSquare;
			this.pictureBox2.Location = new System.Drawing.Point(396, 0);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(15, 15);
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.pictureBox2.TabIndex = 5;
			this.pictureBox2.TabStop = false;
			this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
			this.pictureBox2.DoubleClick += new System.EventHandler(this.pictureBox2_Click);
			//
			// AboutForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Chartreuse;
			this.ClientSize = new System.Drawing.Size(411, 518);
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(this.logonForm);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "AboutForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "AboutForm";
			this.TopMost = true;
			this.TransparencyKey = System.Drawing.Color.Chartreuse;
			this.logonForm.ResumeLayout(false);
			this.borderedGroupBox2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Controls.BorderedGroupBox logonForm;
		private Controls.BorderedGroupBox borderedGroupBox2;
		private Controls.CLI_API _about;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.PictureBox pictureBox2;
	}
}
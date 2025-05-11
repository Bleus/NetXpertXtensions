using NetXpertExtensions.Controls;

namespace SpellCheckTool
{
	partial class SpellChecker
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpellChecker));
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.borderedGroupBox1 = new NetXpertExtensions.Controls.BorderedGroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.button3 = new System.Windows.Forms.Button();
			this.groupBox1 = new NetXpertExtensions.Controls.BorderedGroupBox();
			this.richTextBox2 = new System.Windows.Forms.RichTextBox();
			this.button4 = new System.Windows.Forms.Button();
			this.label1 = new NetXpertExtensions.Controls.TextBoxWithLabel();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel5 = new System.Windows.Forms.ToolStripStatusLabel();
			this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.borderedGroupBox1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// richTextBox1
			// 
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Location = new System.Drawing.Point(0, 0);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.Size = new System.Drawing.Size(416, 488);
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.richTextBox1);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.borderedGroupBox1);
			this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
			this.splitContainer1.Panel2.Controls.Add(this.button4);
			this.splitContainer1.Panel2.Controls.Add(this.label1);
			this.splitContainer1.Size = new System.Drawing.Size(800, 488);
			this.splitContainer1.SplitterDistance = 416;
			this.splitContainer1.TabIndex = 1;
			// 
			// borderedGroupBox1
			// 
			this.borderedGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.borderedGroupBox1.BorderColor = System.Drawing.SystemColors.ControlText;
			this.borderedGroupBox1.BorderRadius = 5;
			this.borderedGroupBox1.BorderWidth = 2;
			this.borderedGroupBox1.Controls.Add(this.label2);
			this.borderedGroupBox1.Controls.Add(this.button2);
			this.borderedGroupBox1.Controls.Add(this.button1);
			this.borderedGroupBox1.Controls.Add(this.label3);
			this.borderedGroupBox1.Controls.Add(this.label4);
			this.borderedGroupBox1.Controls.Add(this.comboBox1);
			this.borderedGroupBox1.Controls.Add(this.checkBox1);
			this.borderedGroupBox1.Controls.Add(this.button3);
			this.borderedGroupBox1.LabelIndent = 10;
			this.borderedGroupBox1.Location = new System.Drawing.Point(11, 38);
			this.borderedGroupBox1.Name = "borderedGroupBox1";
			this.borderedGroupBox1.Size = new System.Drawing.Size(357, 116);
			this.borderedGroupBox1.TabIndex = 11;
			this.borderedGroupBox1.TabStop = false;
			this.borderedGroupBox1.Text = "Actions";
			// 
			// label2
			// 
			this.label2.Image = global::SpellCheckTool.Properties.Resources.checkmark;
			this.label2.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.label2.Location = new System.Drawing.Point(163, 82);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(152, 23);
			this.label2.TabIndex = 9;
			this.label2.Text = "\"word\" has been added!";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(12, 52);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 3;
			this.button2.Text = "Replace";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 23);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 2;
			this.button1.Text = "Ignore";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(97, 56);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(20, 15);
			this.label3.TabIndex = 4;
			this.label3.Text = "->";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(119, 56);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(74, 15);
			this.label4.TabIndex = 8;
			this.label4.Text = "Suggestions:";
			// 
			// comboBox1
			// 
			this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox1.FormattingEnabled = true;
			this.comboBox1.Location = new System.Drawing.Point(203, 53);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(137, 23);
			this.comboBox1.TabIndex = 5;
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(97, 26);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(92, 19);
			this.checkBox1.TabIndex = 7;
			this.checkBox1.Text = "All Instances";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(12, 82);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(118, 23);
			this.button3.TabIndex = 6;
			this.button3.Text = "Add to Dictionary";
			this.button3.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.BorderColor = System.Drawing.SystemColors.ControlText;
			this.groupBox1.BorderRadius = 5;
			this.groupBox1.BorderWidth = 2;
			this.groupBox1.Controls.Add(this.richTextBox2);
			this.groupBox1.LabelIndent = 10;
			this.groupBox1.Location = new System.Drawing.Point(11, 160);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(10, 3, 10, 10);
			this.groupBox1.Size = new System.Drawing.Size(357, 325);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Definitions";
			// 
			// richTextBox2
			// 
			this.richTextBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.richTextBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox2.Location = new System.Drawing.Point(10, 19);
			this.richTextBox2.Name = "richTextBox2";
			this.richTextBox2.ReadOnly = true;
			this.richTextBox2.Size = new System.Drawing.Size(337, 296);
			this.richTextBox2.TabIndex = 0;
			this.richTextBox2.Text = "";
			// 
			// button4
			// 
			this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button4.Enabled = false;
			this.button4.Location = new System.Drawing.Point(303, 9);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(65, 23);
			this.button4.TabIndex = 9;
			this.button4.Text = "Correct";
			this.button4.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.None;
			this.label1.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.None;
			this.label1.AutoSize = true;
			this.label1.BackColor = System.Drawing.Color.Transparent;
			this.label1.HideSelection = true;
			this.label1.InputBackColor = System.Drawing.SystemColors.Window;
			this.label1.InputFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.label1.InputForeColor = System.Drawing.SystemColors.WindowText;
			this.label1.InputMaximumSize = new System.Drawing.Size(0, 0);
			this.label1.LabelText = "Word Not Found:";
			this.label1.Location = new System.Drawing.Point(11, 9);
			this.label1.Margin = new System.Windows.Forms.Padding(0);
			this.label1.Name = "label1";
			this.label1.ReadOnly = false;
			this.label1.SelectedText = "";
			this.label1.SelectionLength = 0;
			this.label1.SelectionStart = 0;
			this.label1.Size = new System.Drawing.Size(289, 23);
			this.label1.TabIndex = 0;
			this.label1.TextBoxAlign = System.Windows.Forms.HorizontalAlignment.Left;
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabel3,
            this.toolStripStatusLabel4,
            this.toolStripStatusLabel5});
			this.statusStrip1.Location = new System.Drawing.Point(0, 491);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(800, 22);
			this.statusStrip1.TabIndex = 2;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(44, 17);
			this.toolStripStatusLabel1.Text = "Words:";
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.AutoSize = false;
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(118, 17);
			this.toolStripStatusLabel2.Text = "0";
			this.toolStripStatusLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// toolStripStatusLabel3
			// 
			this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
			this.toolStripStatusLabel3.Size = new System.Drawing.Size(87, 17);
			this.toolStripStatusLabel3.Text = "Dictionary Size:";
			// 
			// toolStripStatusLabel4
			// 
			this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
			this.toolStripStatusLabel4.Size = new System.Drawing.Size(13, 17);
			this.toolStripStatusLabel4.Text = "0";
			this.toolStripStatusLabel4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// toolStripStatusLabel5
			// 
			this.toolStripStatusLabel5.Name = "toolStripStatusLabel5";
			this.toolStripStatusLabel5.Size = new System.Drawing.Size(118, 17);
			this.toolStripStatusLabel5.Text = "toolStripStatusLabel5";
			// 
			// SpellChecker
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 513);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.splitContainer1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SpellChecker";
			this.Text = "Form1";
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.borderedGroupBox1.ResumeLayout(false);
			this.borderedGroupBox1.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private RichTextBox richTextBox1;
		private SplitContainer splitContainer1;
		private TextBoxWithLabel label1;
		private StatusStrip statusStrip1;
		private NetXpertExtensions.Controls.BorderedGroupBox groupBox1;
		private Button button4;
		private Label label4;
		private CheckBox checkBox1;
		private Button button3;
		private ComboBox comboBox1;
		private Label label3;
		private Button button2;
		private Button button1;
		private BorderedGroupBox borderedGroupBox1;
		private RichTextBox richTextBox2;
		private ToolStripStatusLabel toolStripStatusLabel1;
		private ToolStripStatusLabel toolStripStatusLabel2;
		private ToolStripStatusLabel toolStripStatusLabel3;
		private ToolStripStatusLabel toolStripStatusLabel4;
		private ToolStripStatusLabel toolStripStatusLabel5;
		private Label label2;
		private System.ComponentModel.BackgroundWorker backgroundWorker1;
	}
}
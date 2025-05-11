
using System.Windows.Forms;
using System.ComponentModel.Design;

namespace NetXpertCodeLibrary.WinFormsControls.Controls
{
	partial class UpgradedBinaryViewer
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.byteViewer1 = new System.ComponentModel.Design.ByteViewer();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.richTextBox2 = new System.Windows.Forms.RichTextBox();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.richTextBox3 = new System.Windows.Forms.RichTextBox();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage4);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "UpgradedBinaryViewerTabControl";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(0, 0);
			this.tabControl1.TabIndex = 0;
			this.tabControl1.TabIndexChanged += this.TabControl1_TabIndexChanged;
			//
			// tabPage1
			//
			this.tabPage1.Controls.Add( this.richTextBox3 );
			this.tabPage1.Location = new System.Drawing.Point( 4, 22 );
			this.tabPage1.Name = "PlainTextRTB";
			this.tabPage1.Size = new System.Drawing.Size( 0, 0 );
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Plain Text";
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.richTextBox1);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "EncodedTextRTB";
			this.tabPage2.Size = new System.Drawing.Size(0, 0);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Encoded Text";
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.richTextBox2);
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "Base64RTB";
			this.tabPage3.Size = new System.Drawing.Size(0, 0);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Base64";
			// 
			// tabPage4
			// 
			this.tabPage4.Controls.Add( this.byteViewer1 );
			this.tabPage4.Location = new System.Drawing.Point( 4, 22 );
			this.tabPage4.Name = "ByteViewer";
			this.tabPage4.Size = new System.Drawing.Size( 0, 0 );
			this.tabPage4.TabIndex = 3;
			this.tabPage4.Text = "Binary";
			// 
			// richTextBox1
			// 
			this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox1.Font = new System.Drawing.Font( "Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
			this.richTextBox1.Location = new System.Drawing.Point( 0, 0 );
			this.richTextBox1.Name = "EncodedText(RTB1)";
			this.richTextBox1.Size = new System.Drawing.Size( 0, 0 );
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			this.richTextBox1.ReadOnly = true;
			// 
			// byteViewer1
			// 
			this.byteViewer1.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Inset;
			this.byteViewer1.ColumnCount = 1;
			this.byteViewer1.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
			this.byteViewer1.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
			this.byteViewer1.Dock = System.Windows.Forms.DockStyle.None;
			this.byteViewer1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
			this.byteViewer1.Font = new System.Drawing.Font( "Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
			this.byteViewer1.Location = new System.Drawing.Point( 0, 0 );
			this.byteViewer1.Name = "byteViewer1";
			this.byteViewer1.RowCount = 1;
			this.byteViewer1.RowStyles.Add( new System.Windows.Forms.RowStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
			this.byteViewer1.RowStyles.Add( new System.Windows.Forms.RowStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
			this.byteViewer1.Size = new System.Drawing.Size( 0, 0 );
			this.byteViewer1.TabIndex = 0;
			this.byteViewer1.SetDisplayMode( DisplayMode.Auto );
			// 
			// richTextBox2
			// 
			this.richTextBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox2.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.richTextBox2.Location = new System.Drawing.Point(0, 0);
			this.richTextBox2.Name = "Base64(RTB2)";
			this.richTextBox2.Size = new System.Drawing.Size(0, 0);
			this.richTextBox2.TabIndex = 0;
			this.richTextBox2.Text = "";
			this.richTextBox2.ReadOnly = true;
			//
			// richTextBox3
			//
			this.richTextBox3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBox3.Font = new System.Drawing.Font( "Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)) );
			this.richTextBox3.Location = new System.Drawing.Point( 0, 0 );
			this.richTextBox3.Name = "PlainText(RTB3)";
			this.richTextBox3.Size = new System.Drawing.Size( 0, 0 );
			this.richTextBox3.TabIndex = 0;
			this.richTextBox3.Text = "";
			this.richTextBox3.TextChanged += this.RichTextBox3_TextChanged;
			// 
			// UpgradedBinaryViewer
			// 
			this.Controls.Add(this.tabControl1);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.tabPage4.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected ByteViewer byteViewer1;
		protected TabPage tabPage1;
		protected TabPage tabPage2;
		protected TabPage tabPage3;
		protected TabPage tabPage4;
		protected RichTextBox richTextBox1;
		protected RichTextBox richTextBox2;
		protected RichTextBox richTextBox3;
		protected TabControl tabControl1;
		#endregion
	}
}

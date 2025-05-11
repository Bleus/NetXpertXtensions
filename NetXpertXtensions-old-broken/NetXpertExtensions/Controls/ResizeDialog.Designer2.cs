namespace NetXpertExtensions.Controls
{
	partial class ResizeDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			thumbnail = new PictureBox();
			statusStrip1 = new StatusStrip();
			toolStripStatusLabel1 = new ToolStripStatusLabel();
			toolStripFileName = new ToolStripStatusLabel();
			toolStripProgressBar = new ToolStripProgressBar();
			this.borderedGroupBox1 = new BorderedGroupBox();
			this.dimensionsDisplay = new TextBoxWithLabel();
			this.ratioDisplay = new TextBoxWithLabel();
			this.sizeDisplay = new TextBoxWithLabel();
			this.borderedGroupBox2 = new BorderedGroupBox();
			aspectLockCheckBox = new CheckBox();
			label9 = new Label();
			label8 = new Label();
			label1 = new Label();
			this.heightTextBox = new TextBoxWithLabel();
			this.widthTextBox = new TextBoxWithLabel();
			this.borderedGroupBox3 = new BorderedGroupBox();
			invertTextBox = new CheckBox();
			mirrorCheckBox = new CheckBox();
			label12 = new Label();
			label11 = new Label();
			label10 = new Label();
			flipPicResult = new PictureBox();
			flipPicOriginal = new PictureBox();
			applyButton = new Button();
			cancelButton = new Button();
			acceptButton = new Button();
			this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
			((System.ComponentModel.ISupportInitialize)thumbnail).BeginInit();
			statusStrip1.SuspendLayout();
			this.borderedGroupBox1.SuspendLayout();
			this.borderedGroupBox2.SuspendLayout();
			this.borderedGroupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)flipPicResult).BeginInit();
			((System.ComponentModel.ISupportInitialize)flipPicOriginal).BeginInit();
			this.SuspendLayout();
			//
			// thumbnail
			//
			thumbnail.BackColor = Color.Transparent;
			thumbnail.BorderStyle = BorderStyle.Fixed3D;
			thumbnail.Location = new Point( 7, 22 );
			thumbnail.Name = "thumbnail";
			thumbnail.Size = new Size( 165, 190 );
			thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
			thumbnail.TabIndex = 1;
			thumbnail.TabStop = false;
			//
			// statusStrip1
			//
			statusStrip1.Items.AddRange( new ToolStripItem[] { toolStripStatusLabel1, toolStripFileName, toolStripProgressBar } );
			statusStrip1.Location = new Point( 0, 298 );
			statusStrip1.Name = "statusStrip1";
			statusStrip1.Size = new Size( 397, 22 );
			statusStrip1.SizingGrip = false;
			statusStrip1.TabIndex = 8;
			statusStrip1.Text = "statusStrip1";
			//
			// toolStripStatusLabel1
			//
			toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			toolStripStatusLabel1.Size = new Size( 58, 17 );
			toolStripStatusLabel1.Text = "Filename:";
			//
			// toolStripFileName
			//
			toolStripFileName.Name = "toolStripFileName";
			toolStripFileName.Size = new Size( 191, 17 );
			toolStripFileName.Spring = true;
			toolStripFileName.Text = "toolStripStatusLabel2";
			toolStripFileName.TextAlign = ContentAlignment.MiddleLeft;
			//
			// toolStripProgressBar
			//
			toolStripProgressBar.Name = "toolStripProgressBar";
			toolStripProgressBar.Size = new Size( 100, 16 );
			//
			// borderedGroupBox1
			//
			this.borderedGroupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.borderedGroupBox1.BackColor = SystemColors.Control;
			this.borderedGroupBox1.BorderColor = SystemColors.ControlText;
			this.borderedGroupBox1.BorderRadius = 5;
			this.borderedGroupBox1.BorderWidth = 1;
			this.borderedGroupBox1.Controls.Add( this.dimensionsDisplay );
			this.borderedGroupBox1.Controls.Add( this.ratioDisplay );
			this.borderedGroupBox1.Controls.Add( this.sizeDisplay );
			this.borderedGroupBox1.Controls.Add( thumbnail );
			this.borderedGroupBox1.LabelIndent = 10;
			this.borderedGroupBox1.Location = new Point( 5, 6 );
			this.borderedGroupBox1.Name = "borderedGroupBox1";
			this.borderedGroupBox1.Size = new Size( 178, 286 );
			this.borderedGroupBox1.TabIndex = 9;
			this.borderedGroupBox1.TabStop = false;
			this.borderedGroupBox1.Text = "Image:";
			//
			// dimensionsDisplay
			//
			this.dimensionsDisplay.AutoCompleteMode = AutoCompleteMode.None;
			this.dimensionsDisplay.AutoCompleteSource = AutoCompleteSource.None;
			this.dimensionsDisplay.AutoSize = true;
			this.dimensionsDisplay.BackColor = Color.Transparent;
			this.dimensionsDisplay.HideSelection = true;
			this.dimensionsDisplay.InputBackColor = SystemColors.Control;
			this.dimensionsDisplay.InputFont = new Font( "Segoe UI", 9F );
			this.dimensionsDisplay.InputForeColor = SystemColors.WindowText;
			this.dimensionsDisplay.InputMaximumSize = new Size( 0, 0 );
			this.dimensionsDisplay.LabelOnLeft = true;
			this.dimensionsDisplay.LabelText = "Dimensions:";
			this.dimensionsDisplay.Location = new Point( 7, 216 );
			this.dimensionsDisplay.Margin = new Padding( 0 );
			this.dimensionsDisplay.MultiLine = false;
			this.dimensionsDisplay.Name = "dimensionsDisplay";
			this.dimensionsDisplay.ReadOnly = true;
			this.dimensionsDisplay.SelectedText = "";
			this.dimensionsDisplay.SelectionLength = 0;
			this.dimensionsDisplay.SelectionStart = 0;
			this.dimensionsDisplay.Size = new Size( 165, 20 );
			this.dimensionsDisplay.TabIndex = 9;
			this.dimensionsDisplay.TextBoxAlign = HorizontalAlignment.Right;
			this.dimensionsDisplay.TextBoxBorderaStyle = BorderStyle.None;
			this.dimensionsDisplay.TextBoxMaxLength = 32767;
			this.dimensionsDisplay.TextBoxMaxSize = new Size( 0, 0 );
			this.dimensionsDisplay.TextBoxMinSize = new Size( 0, 0 );
			this.dimensionsDisplay.TextBoxSize = new Size( 88, 23 );
			this.dimensionsDisplay.Value = "99,999 x 99,999";
			//
			// ratioDisplay
			//
			this.ratioDisplay.AutoCompleteMode = AutoCompleteMode.None;
			this.ratioDisplay.AutoCompleteSource = AutoCompleteSource.None;
			this.ratioDisplay.AutoSize = true;
			this.ratioDisplay.BackColor = Color.Transparent;
			this.ratioDisplay.HideSelection = true;
			this.ratioDisplay.InputBackColor = SystemColors.Control;
			this.ratioDisplay.InputFont = new Font( "Segoe UI", 9F );
			this.ratioDisplay.InputForeColor = SystemColors.WindowText;
			this.ratioDisplay.InputMaximumSize = new Size( 0, 0 );
			this.ratioDisplay.LabelOnLeft = true;
			this.ratioDisplay.LabelText = "Aspect Ratio:";
			this.ratioDisplay.Location = new Point( 7, 237 );
			this.ratioDisplay.Margin = new Padding( 0 );
			this.ratioDisplay.MultiLine = false;
			this.ratioDisplay.Name = "ratioDisplay";
			this.ratioDisplay.ReadOnly = true;
			this.ratioDisplay.SelectedText = "";
			this.ratioDisplay.SelectionLength = 0;
			this.ratioDisplay.SelectionStart = 0;
			this.ratioDisplay.Size = new Size( 165, 21 );
			this.ratioDisplay.TabIndex = 8;
			this.ratioDisplay.TextBoxAlign = HorizontalAlignment.Right;
			this.ratioDisplay.TextBoxBorderaStyle = BorderStyle.None;
			this.ratioDisplay.TextBoxMaxLength = 32767;
			this.ratioDisplay.TextBoxMaxSize = new Size( 0, 0 );
			this.ratioDisplay.TextBoxMinSize = new Size( 0, 0 );
			this.ratioDisplay.TextBoxSize = new Size( 84, 23 );
			this.ratioDisplay.Value = "3:2";
			//
			// sizeDisplay
			//
			this.sizeDisplay.AutoCompleteMode = AutoCompleteMode.None;
			this.sizeDisplay.AutoCompleteSource = AutoCompleteSource.None;
			this.sizeDisplay.AutoSize = true;
			this.sizeDisplay.BackColor = Color.Transparent;
			this.sizeDisplay.HideSelection = true;
			this.sizeDisplay.InputBackColor = SystemColors.Control;
			this.sizeDisplay.InputFont = new Font( "Segoe UI", 9F );
			this.sizeDisplay.InputForeColor = SystemColors.WindowText;
			this.sizeDisplay.InputMaximumSize = new Size( 0, 0 );
			this.sizeDisplay.LabelOnLeft = true;
			this.sizeDisplay.LabelText = "Size:";
			this.sizeDisplay.Location = new Point( 7, 260 );
			this.sizeDisplay.Margin = new Padding( 0 );
			this.sizeDisplay.MultiLine = false;
			this.sizeDisplay.Name = "sizeDisplay";
			this.sizeDisplay.ReadOnly = true;
			this.sizeDisplay.SelectedText = "";
			this.sizeDisplay.SelectionLength = 0;
			this.sizeDisplay.SelectionStart = 0;
			this.sizeDisplay.Size = new Size( 165, 23 );
			this.sizeDisplay.TabIndex = 7;
			this.sizeDisplay.TextBoxAlign = HorizontalAlignment.Right;
			this.sizeDisplay.TextBoxBorderaStyle = BorderStyle.None;
			this.sizeDisplay.TextBoxMaxLength = 32767;
			this.sizeDisplay.TextBoxMaxSize = new Size( 0, 0 );
			this.sizeDisplay.TextBoxMinSize = new Size( 0, 0 );
			this.sizeDisplay.TextBoxSize = new Size( 130, 23 );
			this.sizeDisplay.Value = "10.2 MB";
			//
			// borderedGroupBox2
			//
			this.borderedGroupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			this.borderedGroupBox2.BorderColor = SystemColors.ControlText;
			this.borderedGroupBox2.BorderRadius = 5;
			this.borderedGroupBox2.BorderWidth = 1;
			this.borderedGroupBox2.Controls.Add( aspectLockCheckBox );
			this.borderedGroupBox2.Controls.Add( label9 );
			this.borderedGroupBox2.Controls.Add( label8 );
			this.borderedGroupBox2.Controls.Add( label1 );
			this.borderedGroupBox2.Controls.Add( this.heightTextBox );
			this.borderedGroupBox2.Controls.Add( this.widthTextBox );
			this.borderedGroupBox2.LabelIndent = 10;
			this.borderedGroupBox2.Location = new Point( 189, 6 );
			this.borderedGroupBox2.Name = "borderedGroupBox2";
			this.borderedGroupBox2.Size = new Size( 201, 113 );
			this.borderedGroupBox2.TabIndex = 10;
			this.borderedGroupBox2.TabStop = false;
			this.borderedGroupBox2.Text = "Resize Options:";
			//
			// aspectLockCheckBox
			//
			aspectLockCheckBox.AutoSize = true;
			aspectLockCheckBox.Location = new Point( 155, 83 );
			aspectLockCheckBox.Name = "aspectLockCheckBox";
			aspectLockCheckBox.Size = new Size( 15, 14 );
			aspectLockCheckBox.TabIndex = 5;
			aspectLockCheckBox.UseVisualStyleBackColor = true;
			//
			// label9
			//
			label9.AutoSize = true;
			label9.Location = new Point( 7, 83 );
			label9.Name = "label9";
			label9.Size = new Size( 128, 15 );
			label9.TabIndex = 4;
			label9.Text = "Maintain Aspect Ratio?";
			//
			// label8
			//
			label8.AutoSize = true;
			label8.Location = new Point( 173, 53 );
			label8.Name = "label8";
			label8.Size = new Size( 20, 15 );
			label8.TabIndex = 3;
			label8.Text = "px";
			label8.DoubleClick += this.label_DoubleClick;
			//
			// label1
			//
			label1.AutoSize = true;
			label1.Location = new Point( 173, 25 );
			label1.Name = "label1";
			label1.Size = new Size( 20, 15 );
			label1.TabIndex = 2;
			label1.Text = "px";
			label1.DoubleClick += this.label_DoubleClick;
			//
			// heightTextBox
			//
			this.heightTextBox.AutoCompleteMode = AutoCompleteMode.None;
			this.heightTextBox.AutoCompleteSource = AutoCompleteSource.None;
			this.heightTextBox.AutoSize = true;
			this.heightTextBox.BackColor = Color.Transparent;
			this.heightTextBox.HideSelection = true;
			this.heightTextBox.InputBackColor = SystemColors.Window;
			this.heightTextBox.InputFont = new Font( "Segoe UI", 9F );
			this.heightTextBox.InputForeColor = SystemColors.WindowText;
			this.heightTextBox.InputMaximumSize = new Size( 60, 24 );
			this.heightTextBox.LabelOnLeft = true;
			this.heightTextBox.LabelText = "Height:";
			this.heightTextBox.Location = new Point( 7, 49 );
			this.heightTextBox.Margin = new Padding( 0 );
			this.heightTextBox.MultiLine = false;
			this.heightTextBox.Name = "heightTextBox";
			this.heightTextBox.ReadOnly = false;
			this.heightTextBox.SelectedText = "";
			this.heightTextBox.SelectionLength = 0;
			this.heightTextBox.SelectionStart = 0;
			this.heightTextBox.Size = new Size( 163, 24 );
			this.heightTextBox.TabIndex = 1;
			this.heightTextBox.TextBoxAlign = HorizontalAlignment.Right;
			this.heightTextBox.TextBoxBorderaStyle = BorderStyle.Fixed3D;
			this.heightTextBox.TextBoxMaxLength = 5;
			this.heightTextBox.TextBoxMaxSize = new Size( 60, 24 );
			this.heightTextBox.TextBoxMinSize = new Size( 0, 0 );
			this.heightTextBox.TextBoxSize = new Size( 60, 23 );
			this.heightTextBox.Value = "";
			this.heightTextBox.KeyPress += this.textBoxWithLabel1_KeyPress;
			this.heightTextBox.KeyUp += this.textBoxWithLabel2_KeyUp;
			this.heightTextBox.Enter += this.textBoxWithLabel_Enter;
			this.heightTextBox.Leave += this.textBoxWithLabel_Leave;
			//
			// widthTextBox
			//
			this.widthTextBox.AutoCompleteMode = AutoCompleteMode.None;
			this.widthTextBox.AutoCompleteSource = AutoCompleteSource.None;
			this.widthTextBox.AutoSize = true;
			this.widthTextBox.BackColor = Color.Transparent;
			this.widthTextBox.HideSelection = true;
			this.widthTextBox.InputBackColor = SystemColors.Window;
			this.widthTextBox.InputFont = new Font( "Segoe UI", 9F );
			this.widthTextBox.InputForeColor = SystemColors.WindowText;
			this.widthTextBox.InputMaximumSize = new Size( 60, 24 );
			this.widthTextBox.LabelOnLeft = true;
			this.widthTextBox.LabelText = "Width:";
			this.widthTextBox.Location = new Point( 7, 22 );
			this.widthTextBox.Margin = new Padding( 0 );
			this.widthTextBox.MultiLine = false;
			this.widthTextBox.Name = "widthTextBox";
			this.widthTextBox.ReadOnly = false;
			this.widthTextBox.SelectedText = "";
			this.widthTextBox.SelectionLength = 0;
			this.widthTextBox.SelectionStart = 0;
			this.widthTextBox.Size = new Size( 163, 24 );
			this.widthTextBox.TabIndex = 0;
			this.widthTextBox.TextBoxAlign = HorizontalAlignment.Right;
			this.widthTextBox.TextBoxBorderaStyle = BorderStyle.Fixed3D;
			this.widthTextBox.TextBoxMaxLength = 5;
			this.widthTextBox.TextBoxMaxSize = new Size( 60, 24 );
			this.widthTextBox.TextBoxMinSize = new Size( 0, 0 );
			this.widthTextBox.TextBoxSize = new Size( 60, 23 );
			this.widthTextBox.Value = "";
			this.widthTextBox.KeyPress += this.textBoxWithLabel1_KeyPress;
			this.widthTextBox.KeyUp += this.textBoxWithLabel1_KeyUp;
			this.widthTextBox.Enter += this.textBoxWithLabel_Enter;
			this.widthTextBox.Leave += this.textBoxWithLabel_Leave;
			//
			// borderedGroupBox3
			//
			this.borderedGroupBox3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			this.borderedGroupBox3.BorderColor = SystemColors.ControlText;
			this.borderedGroupBox3.BorderRadius = 5;
			this.borderedGroupBox3.BorderWidth = 1;
			this.borderedGroupBox3.Controls.Add( invertTextBox );
			this.borderedGroupBox3.Controls.Add( mirrorCheckBox );
			this.borderedGroupBox3.Controls.Add( label12 );
			this.borderedGroupBox3.Controls.Add( label11 );
			this.borderedGroupBox3.Controls.Add( label10 );
			this.borderedGroupBox3.Controls.Add( flipPicResult );
			this.borderedGroupBox3.Controls.Add( flipPicOriginal );
			this.borderedGroupBox3.LabelIndent = 10;
			this.borderedGroupBox3.Location = new Point( 189, 125 );
			this.borderedGroupBox3.Name = "borderedGroupBox3";
			this.borderedGroupBox3.Size = new Size( 201, 137 );
			this.borderedGroupBox3.TabIndex = 11;
			this.borderedGroupBox3.TabStop = false;
			this.borderedGroupBox3.Text = "Flip Options:";
			//
			// invertTextBox
			//
			invertTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			invertTextBox.AutoSize = true;
			invertTextBox.Location = new Point( 177, 115 );
			invertTextBox.Name = "invertTextBox";
			invertTextBox.Size = new Size( 15, 14 );
			invertTextBox.TabIndex = 6;
			invertTextBox.UseVisualStyleBackColor = true;
			invertTextBox.CheckedChanged += this.checkBox3_CheckedChanged;
			//
			// mirrorCheckBox
			//
			mirrorCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			mirrorCheckBox.AutoSize = true;
			mirrorCheckBox.Location = new Point( 177, 95 );
			mirrorCheckBox.Name = "mirrorCheckBox";
			mirrorCheckBox.Size = new Size( 15, 14 );
			mirrorCheckBox.TabIndex = 5;
			mirrorCheckBox.UseVisualStyleBackColor = true;
			mirrorCheckBox.CheckedChanged += this.checkBox2_CheckedChanged;
			//
			// label12
			//
			label12.AutoSize = true;
			label12.Location = new Point( 7, 114 );
			label12.Name = "label12";
			label12.Size = new Size( 114, 15 );
			label12.TabIndex = 4;
			label12.Text = "Invert (top-bottom):";
			//
			// label11
			//
			label11.AutoSize = true;
			label11.Location = new Point( 7, 97 );
			label11.Name = "label11";
			label11.Size = new Size( 101, 15 );
			label11.TabIndex = 3;
			label11.Text = "Mirror (left-right):";
			//
			// label10
			//
			label10.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			label10.AutoSize = true;
			label10.Location = new Point( 84, 50 );
			label10.Name = "label10";
			label10.Size = new Size( 34, 15 );
			label10.TabIndex = 2;
			label10.Text = "to ->";
			label10.TextAlign = ContentAlignment.MiddleCenter;
			//
			// flipPicResult
			//
			flipPicResult.BorderStyle = BorderStyle.FixedSingle;
			flipPicResult.Image = Properties.Resources.Chameleon;
			flipPicResult.InitialImage = Properties.Resources.Chameleon;
			flipPicResult.Location = new Point( 124, 20 );
			flipPicResult.Name = "flipPicResult";
			flipPicResult.Size = new Size( 71, 71 );
			flipPicResult.SizeMode = PictureBoxSizeMode.StretchImage;
			flipPicResult.TabIndex = 1;
			flipPicResult.TabStop = false;
			//
			// flipPicOriginal
			//
			flipPicOriginal.BorderStyle = BorderStyle.FixedSingle;
			flipPicOriginal.Image = Properties.Resources.Chameleon;
			flipPicOriginal.InitialImage = Properties.Resources.Chameleon;
			flipPicOriginal.Location = new Point( 7, 22 );
			flipPicOriginal.Name = "flipPicOriginal";
			flipPicOriginal.Size = new Size( 71, 71 );
			flipPicOriginal.SizeMode = PictureBoxSizeMode.StretchImage;
			flipPicOriginal.TabIndex = 0;
			flipPicOriginal.TabStop = false;
			//
			// applyButton
			//
			applyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			applyButton.Enabled = false;
			applyButton.Location = new Point( 190, 268 );
			applyButton.Name = "applyButton";
			applyButton.Size = new Size( 60, 23 );
			applyButton.TabIndex = 12;
			applyButton.Text = "Apply";
			applyButton.UseVisualStyleBackColor = true;
			applyButton.Click += this.button1_Click;
			//
			// cancelButton
			//
			cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			cancelButton.Location = new Point( 325, 268 );
			cancelButton.Name = "cancelButton";
			cancelButton.Size = new Size( 65, 23 );
			cancelButton.TabIndex = 13;
			cancelButton.Text = "Cancel";
			cancelButton.UseVisualStyleBackColor = true;
			//
			// acceptButton
			//
			acceptButton.Location = new Point( 255, 268 );
			acceptButton.Name = "acceptButton";
			acceptButton.Size = new Size( 65, 23 );
			acceptButton.TabIndex = 14;
			acceptButton.Text = "Accept";
			acceptButton.UseVisualStyleBackColor = true;
			//
			// backgroundWorker1
			//
			this.backgroundWorker1.WorkerReportsProgress = true;
			this.backgroundWorker1.DoWork += this.backgroundWorker1_DoWork;
			this.backgroundWorker1.ProgressChanged += this.backgroundWorker1_ProgressChanged;
			this.backgroundWorker1.RunWorkerCompleted += this.backgroundWorker1_RunWorkerCompleted;
			//
			// ResizeDialog
			//
			this.AcceptButton = acceptButton;
			this.AutoScaleDimensions = new SizeF( 7F, 15F );
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = cancelButton;
			this.ClientSize = new Size( 397, 320 );
			this.Controls.Add( acceptButton );
			this.Controls.Add( cancelButton );
			this.Controls.Add( applyButton );
			this.Controls.Add( this.borderedGroupBox3 );
			this.Controls.Add( this.borderedGroupBox2 );
			this.Controls.Add( this.borderedGroupBox1 );
			this.Controls.Add( statusStrip1 );
			this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
			this.Name = "ResizeDialog";
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Resize Image Tool";
			((System.ComponentModel.ISupportInitialize)thumbnail).EndInit();
			statusStrip1.ResumeLayout( false );
			statusStrip1.PerformLayout();
			this.borderedGroupBox1.ResumeLayout( false );
			this.borderedGroupBox1.PerformLayout();
			this.borderedGroupBox2.ResumeLayout( false );
			this.borderedGroupBox2.PerformLayout();
			this.borderedGroupBox3.ResumeLayout( false );
			this.borderedGroupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)flipPicResult).EndInit();
			((System.ComponentModel.ISupportInitialize)flipPicOriginal).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();
		}

		#endregion
		private PictureBox thumbnail;
		private StatusStrip statusStrip1;
		private BorderedGroupBox borderedGroupBox1;
		private BorderedGroupBox borderedGroupBox2;
		private TextBoxWithLabel heightTextBox;
		private TextBoxWithLabel widthTextBox;
		private CheckBox aspectLockCheckBox;
		private Label label9;
		private BorderedGroupBox borderedGroupBox3;
		private CheckBox invertTextBox;
		private CheckBox mirrorCheckBox;
		private Label label12;
		private Label label11;
		private Label label10;
		private PictureBox flipPicResult;
		private PictureBox flipPicOriginal;
		private ToolStripStatusLabel toolStripStatusLabel1;
		private ToolStripStatusLabel toolStripFileName;
		private Button applyButton;
		private Button cancelButton;
		private TextBoxWithLabel dimensionsDisplay;
		private TextBoxWithLabel ratioDisplay;
		private TextBoxWithLabel sizeDisplay;
		private Label label8;
		private Label label1;
		private ToolStripProgressBar toolStripProgressBar;
		private Button acceptButton;
		private System.ComponentModel.BackgroundWorker backgroundWorker1;
	}
}
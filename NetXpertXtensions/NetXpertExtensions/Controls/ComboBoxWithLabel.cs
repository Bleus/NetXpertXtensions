using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetXpertExtensions;

namespace NetXpertExtensions.Controls
{
	#nullable disable

	public partial class ComboBoxWithLabel : LabelledInputControl<ComboBox>
	{
		public class TranslationTable<T> : IEnumerator<KeyValuePair<T,string>> where T : Enum
		{
			#region Properties
			private readonly List<KeyValuePair<T, string>> _values = new();
			int _pointer = 0;
			#endregion

			#region Constructors
			public TranslationTable() { }
			#endregion

			#region Accessors
			public string this[ T index ]
			{
				get => Translate( index );
				set => this.Add( index, value );
			}

			public T this[ string value ]
			{
				get
				{
					int i = -1; while ( (++i < this._values.Count) && !this._values[ i ].Value.Equals( value, StringComparison.OrdinalIgnoreCase ) ) ;
					return (i < Count) ? this._values[ i ].Key : default(T);
				}
			}

			public int Count => _values.Count;

			public KeyValuePair<T, string> Current => this._values[ this._pointer ];

			object IEnumerator.Current => this._values[ this._pointer ];
			#endregion

			#region Methods
			public int IndexOf( T value )
			{
				int i = -1; while ( (++i < this._values.Count) && !this._values[ i ].Key.Equals( value ) ) ;
				return ( i < this._values.Count) ? i : -1;
			}

			public bool Contains( T value ) => IndexOf( value ) >= 0;

			public void Add( T key, string value )
			{
				int i = IndexOf( key );
				if ( string.IsNullOrWhiteSpace( value ) )
				{
					if (i>=0) this._values.RemoveAt( i );
				}
				else
				{
					KeyValuePair<T, string> v = new( key, value );
					if ( i < 0 ) this._values.Add( v );
					else this._values[ i ] = v;
				}
			}

			public void Add(KeyValuePair<T, string> value) =>
				Add( value.Key, value.Value);

			public string Translate( T index )
			{
				int i = IndexOf( index );
				return i < 0 ? index.ToString() : this._values[ i ].Value;
			}

			public string Remove( T index )
			{
				int i = IndexOf( index );
				string result = i < 0 ? "" : this[ index ];
				if (i>=0) this._values.RemoveAt( i );
				return result;
			}

			public void Dispose() => GC.SuppressFinalize( this );

			public bool MoveNext() => this._pointer++ < Count;

			public void Reset() => this._pointer = 0;

			public static TranslationTable<T> Create()
			{
				var result = new TranslationTable<T>();
				foreach( T e in Enum.GetValues( typeof( T ) ) )
					result.Add( e, e.ToString() );

				return result;
			}
			#endregion
		}

		#region Properties
		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate SelectionChangeCommitted;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate SelectedValueChanged;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate SelectedIndexChanged;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		public event GenericEventDelegate TextUpdate;

		[Browsable( true )]
		[Category( "Action" )]
		[Description( "Bubbles up the event from the ComboBox" )]
		new public event GenericEventDelegate TextChanged;
		#endregion

		#region Constructor
		public ComboBoxWithLabel() : base() => InitializeComponent();
		#endregion

		#region Accessors
		public AutoCompleteMode AutoCompleteMode
		{
			get => this.textBox1.AutoCompleteMode;
			set => this.textBox1.AutoCompleteMode = value;
		}

		public AutoCompleteSource AutoCompleteSource
		{
			get => this.textBox1.AutoCompleteSource;
			set => this.textBox1.AutoCompleteSource = value;
		}

		public AutoCompleteStringCollection AutoCompleteCustomSource
		{
			get => this.textBox1.AutoCompleteCustomSource;
			set => this.textBox1.AutoCompleteCustomSource = value;
		}

		public int SelectionStart
		{
			get => this.textBox1.SelectionStart;
			set => this.textBox1.SelectionStart = value;
		}

		public int SelectionLength
		{
			get => this.textBox1.SelectionLength;
			set => this.textBox1.SelectionLength = value;
		}

		public string SelectedText
		{
			get => this.textBox1.SelectedText;
			set => this.textBox1.SelectedText = value;
		}
		#endregion

		#region Methods
		protected void EnumeratedValue<W>( W value ) where W : Enum
		{
			int i = -1; while ( ++i < textBox1.Items.Count && !textBox1.Items[ i ].Equals( $"{value}" ) ) ;
			if ( i < textBox1.Items.Count ) this.textBox1.SelectedIndex = i;
		}

		protected void EnumeratedValue<W>( W value, TranslationTable<W> translations ) where W : Enum
		{
			int i = translations.IndexOf( value );
			if ( i < textBox1.Items.Count ) this.textBox1.SelectedIndex = i;
		}

		protected W EnumeratedValue<W>() where W : Enum =>
			(W)Enum.Parse( typeof(W), textBox1.Items[ textBox1.SelectedIndex ].ToString() );

		protected W EnumeratedValue<W>( TranslationTable<W> translations ) where W : Enum =>
			translations[ textBox1.Items[ textBox1.SelectedIndex ].ToString() ];

		protected override bool ReadOnlyParser( bool? value )
		{
			if ( value is not null )
				this.textBox1.Enabled = (bool)value;

			return this.textBox1.Enabled;
		}

		protected void PopulateFromEnum<W>( TranslationTable<W> translations, int selectedIndex = 0 ) where W : Enum
		{
			if ( (translations is null) || (translations.Count == 0) ) translations = TranslationTable<W>.Create();

			foreach ( W s in Enum.GetValues( typeof( W ) ) )
				this.textBox1.Items.Add( translations[ s ] );

			this.textBox1.SelectedIndex = Math.Min( translations.Count, selectedIndex );
		}

		protected void PopulateFromCollection<W>( IEnumerable<W> group, int selectedIndex = 0 )
		{
			textBox1.Items.Clear();
			foreach ( W n in group ) 
				this.textBox1.Items.Add( n.ToString() );

			this.textBox1.SelectedIndex = Math.Min( group.Count(), selectedIndex );
		}

		private void TextBox1_SelectionChangeCommitted( object sender, EventArgs e )
		{
			if ( SelectionChangeCommitted is not null )
				SelectionChangeCommitted( sender, e );
		}

		private void TextBox1_SelectedValueChanged( object sender, EventArgs e )
		{
			if (SelectedValueChanged is not null)
				SelectedValueChanged( sender, e );
		}

		private void TextBox1_SelectedIndexChanged( object sender, EventArgs e )
		{
			if (SelectedIndexChanged is not null)
				SelectedIndexChanged( sender, e );
		}

		private void TextBox1_TextUpdate( object sender, EventArgs e )
		{
			if (TextUpdate is not null)
				TextUpdate( sender, e );
		}

		private void TextBox1_TextChanged( object sender, EventArgs e )
		{
			if (TextChanged is not null)
				TextChanged( sender, e );
		}
		#endregion

		#region Designer-required code.
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
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.textBox1.SelectedIndexChanged += this.TextBox1_SelectedIndexChanged;
			this.textBox1.SelectedValueChanged += this.TextBox1_SelectedValueChanged;
			this.textBox1.SelectionChangeCommitted += this.TextBox1_SelectionChangeCommitted;
			this.textBox1.TextChanged += this.TextBox1_TextChanged;
			this.textBox1.TextUpdate += this.TextBox1_TextUpdate;
		}
		#endregion
		#endregion
	}
}

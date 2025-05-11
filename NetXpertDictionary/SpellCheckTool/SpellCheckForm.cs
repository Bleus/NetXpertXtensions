using System.Text.RegularExpressions;

namespace SpellCheckTool
{
	public partial class SpellChecker : Form
	{
		protected readonly RichTextBox _source;
		protected readonly WordBloom _dictionary;

		public SpellChecker(RichTextBox source)
		{
			this._source = source;
			this._dictionary = WordBloom.ImportResourceDictionary();
			InitializeComponent();
		}

		public void Check()
		{
			string[] words = Regex.Split( _source.Text, @"[^\w]" );
			int i = -1;
			while ( ++i < words.Length )
			{
				if (!_dictionary.Validate( words[i] ) ) 
				{
					label1.Text = words[ i ];
					// populate suggestions combo box
				}
			}
		}
	}
}
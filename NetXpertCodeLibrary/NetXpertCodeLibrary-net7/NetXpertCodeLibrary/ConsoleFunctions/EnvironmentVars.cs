using System.Collections;
using System.Text.RegularExpressions;
using NetXpertExtensions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	public class EnvironmentVar
	{
		#region Properties
		public static string REGEX_NAME_PATTERN = /* language=regex */ @"[a-zA-Z][\w]*[a-zA-Z0-9]";
		public static string PATTERN = 
			/* language=regex */ @"(?<name>" + REGEX_NAME_PATTERN + @")[=:](?<value>\x22[^\x22\r\n]*\x22|'[^'\r\n]*'|[\S]+)?";

		public const string PUBLIC = "Public";
		public const int MAX_VALUE_LENGTH = 240;

		protected KeyValuePair<string, string> _var = new KeyValuePair<string, string>("","");
		protected bool _readOnly = false;
		protected string _owner = PUBLIC;
		#endregion

		#region Constructors
		public EnvironmentVar(string name, string value = "", bool readOnly = false, string owner = "")
		{
			if (!IsValidName(name))
				throw new ArgumentNullException("The Variable Name must be defined and be in a valid format.");

			this.Name = name;
			this.Owner = owner;
			this.Value = value;
			this._readOnly = !IsPublic && readOnly;
		}

		public EnvironmentVar(string source, bool readOnly = false, string owner = "")
		{
			MatchCollection matches = Regex.Matches(source,PATTERN, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
			if ((matches.Count > 0) && matches[0].Groups["name"].Success)
			{
				this.Name = matches[0].Groups["name"].Value;
				this.Value = matches[0].Groups["value"].Success ? matches[0].Groups["value"].Value : "";
				this._readOnly = readOnly;
				this.Owner = owner;
			}
			else
				throw new ArgumentException("The provided value is not a recognizable environment variable assignment string.");
		}

		public EnvironmentVar(KeyValuePair<string,string> envVar, bool readOnly = false, string owner = "" )
		{
			if (!IsValidName( envVar.Key ))
				throw new ArgumentNullException( "The Variable Name must be defined and be in a valid format." );

			this.Name = envVar.Key;
			this.Value = envVar.Value;
			this.Owner = owner;
			this._readOnly = !IsPublic && readOnly;
		}
		#endregion

		#region Accessors
		public string Name
		{
			get => this._var.Key;
			protected set
			{
				value = Regex.Replace(value, @"[^\w]", "");
				if (Regex.IsMatch(value,"^" + EnvironmentVar.REGEX_NAME_PATTERN + "$",RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
					this._var = new KeyValuePair<string,string>(value,Value);
			}
		}

		public string Owner
		{
			get => this._owner;
			protected set
			{
				string owner = string.IsNullOrWhiteSpace(value) ? PUBLIC : Regex.Replace(value, @"[^\w\\]", "");
				if ((this._owner.Length==0) || IsPublic) this._owner = (owner.Length > 0) ? owner : PUBLIC;
			}
		}

		public string Value
		{
			get => this._var.Value;
			set
			{
				if (!ReadOnly)
				{
					if (Regex.IsMatch(value.Trim(), @"^(?:""[^""\r\n]*""|'[^'\r\n]')$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase))
						value = value.Substring(1, value.Length - 2);

					if (string.IsNullOrWhiteSpace( value ))
						this._var = new KeyValuePair<string, string>( Name, "" );
					else
					{
						//value = value.Trim(); // No!
						value = (value.Length > MAX_VALUE_LENGTH ? value.Substring( 0, MAX_VALUE_LENGTH ) : value).Replace( "%", "" );
						this._var = new KeyValuePair<string, string>( Name, value );
					}
				}
			}
		}

		public KeyValuePair<string,string> asKvp
		{
			get => this._var;
			set => this._var = value;
		}

		public bool ReadOnly
		{
			get => this._readOnly;
			set => this._readOnly = value;
		}

		public bool IsEmpty => string.IsNullOrEmpty(this.Value);

		public bool IsPublic => this._owner.Equals(PUBLIC, StringComparison.InvariantCultureIgnoreCase);
		#endregion

		#region Methods
		public bool IsOwner(string name) =>
			this.IsPublic || this._owner.Equals(name, StringComparison.InvariantCultureIgnoreCase);

		public bool IsOwner(EnvironmentVar var) => IsOwner(var.Owner);

		public bool AllowMod(string owner) =>
			!this.ReadOnly && (IsPublic || IsOwner(owner));

		public string ApplyTo(string source) =>
			Regex.Replace( source, $"[%]({this.Name})[%]", this.Value, RegexOptions.IgnoreCase );

		public override string ToString() => $"{Name} = \"{Value}\";";

		//public ConsoleListLine ToListLine() =>
		//	new ConsoleListLine(new string[] { this._name, this._owner, '"' + this._value.ToString() + '"', this.ReadOnly ? "[√]" : "[X]" });

		public static implicit operator EnvironmentVar(string source) => new EnvironmentVar(source,false,"");
		public static implicit operator string(EnvironmentVar source) => source.Name + "=" + source.Value;

		public static string CleanEnvName(string name)
		{
			name = name.Filter("abcdefghijklmnopqrstuvwxyz1234567890_", true);
			if (name.Length > 16) name = name.Substring(0, 16);
			return name;
		}

		public static bool IsValidName(string name)
		{
			name = CleanEnvName(name);
			if (name.Length == 0) return false;

			return Regex.IsMatch(name, $"{REGEX_NAME_PATTERN}$",RegexOptions.Compiled | RegexOptions.IgnoreCase);
		}
		#endregion
	}

	public class EnvironmentVars : IEnumerator<EnvironmentVar>
	{
		#region Properties
		protected List<EnvironmentVar> _variables = new List<EnvironmentVar>();
		private int _position = 0;
		#endregion

		#region Constructors
		public EnvironmentVars() { }

		public EnvironmentVars(EnvironmentVar seed) =>
			this.Set(seed);

		public EnvironmentVars(EnvironmentVar[] source) =>
			this.AddRange(source);
		#endregion

		#region Accessors
		public int Count => this._variables.Count;

		public EnvironmentVar this[int index] => this._variables[index];

		public EnvironmentVar this[ string varName ]
		{
			get => varName.Equals( "CWD", StringComparison.OrdinalIgnoreCase )
				? new EnvironmentVar( "CWD", Environment.CurrentDirectory, true, "System" )
				: this[ FindValueByName( varName ) ];
		}

		EnvironmentVar IEnumerator<EnvironmentVar>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Methods
		protected int FindValueByName(string name)
		{
			int i = -1;
			if (!string.IsNullOrWhiteSpace(name) && !name.Equals("CWD", StringComparison.OrdinalIgnoreCase))
				while ((++i < this._variables.Count) && !this._variables[i].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ;
			return (i < this._variables.Count) ? i : -1;
		}

		public bool HasVariable(string name) => !string.IsNullOrWhiteSpace(name) && (name.Equals( "CWD", StringComparison.OrdinalIgnoreCase ) || ( FindValueByName(name) >= 0));

		public bool Set(EnvironmentVar newVar)
		{
			if (newVar.IsEmpty) { return Remove(newVar); }

			if ( !newVar.Name.Equals( "CWD", StringComparison.OrdinalIgnoreCase ) )
			{
				int i = this.FindValueByName( newVar.Name );
				if ( i < 0 )
				{
					this._variables.Add( newVar );
					return true;
				}
				else
					if ( !this._variables[ i ].ReadOnly && this._variables[ i ].IsOwner( newVar ) )
				{
					this._variables[ i ] = newVar;
					return true;
				}
			}

			return false;
		}

		public bool Set(string varName, string varValue, bool readOnly = false, string owner = "") =>
			Set(new EnvironmentVar(varName, varValue, readOnly, owner));

		public bool Set(KeyValuePair<string, string> value, bool readOnly = false, string owner = "") =>
			Set( new EnvironmentVar( value, readOnly, owner ) );

		public int AddRange(EnvironmentVar[] values)
		{
			int c = 0;
			foreach (EnvironmentVar ev in values)
				c += (Set(ev) ? 1 : 0);
			return c;
		}

		public int AddRange(KeyValuePair<string,string>[] values, bool readOnly = false, string owner = "")
		{
			int c = 0;
			foreach (KeyValuePair<string,string> ev in values)
				c += (Set( ev, readOnly, owner ) ? 1 : 0);
			return c;
		}

		public bool Remove(EnvironmentVar varInfo)
		{
			int i = this.FindValueByName(varInfo.Name);
			if ((i>=0) && this._variables[i].IsOwner(varInfo.Owner) ) // !this._variables[i].ReadOnly && 
			{
				this._variables.RemoveAt(i);
				return true;
			}
			return false;
		}

		public bool Remove(string name)
		{
			int i = this.FindValueByName(name);
			if ((i >= 0) && this._variables[i].IsPublic)
			{
				this._variables.RemoveAt(i);
				return true;
			}
			return false;
		}

		public bool Remove(string name, string owner = "") =>
			Remove(new EnvironmentVar(name, "", false, owner));

		public string ApplyTo(string source)
		{
			foreach (EnvironmentVar env in this.ToArray())
				source = env.ApplyTo(source);

			return source;
		}

		public EnvironmentVar[] ToArray()
		{
			List<EnvironmentVar> _temp = new List<EnvironmentVar>( this._variables );
			_temp.Add( new EnvironmentVar( "CWD", Environment.CurrentDirectory, true, "System" ) );
			return _temp.ToArray();
		}

		/// <summary>Loads the currently defined system environment variables and their values into this object.</summary>
		public void ImportSystemEnv()
		{
			IDictionary env = Environment.GetEnvironmentVariables();
			if (env.Count > 0)
				foreach( KeyValuePair<string,string> @var in env )
					this.Set( @var, true, "System" );
		}

		public IEnumerator<EnvironmentVar> GetEnumerator() => this._variables.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this.Count;

		void IEnumerator.Reset() => this._position = 0;

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~AppletParameters() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		void IDisposable.Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
		#endregion
	}
}

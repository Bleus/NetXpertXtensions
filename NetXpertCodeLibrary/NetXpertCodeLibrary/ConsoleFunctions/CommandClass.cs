using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	/// <summary>Facilitates storing and managing a single commmand.</summary>
	public class Command
	{
		#region Properties
		protected string _cmd = "";
		protected bool _processed = false;
		protected CommandLine _cmdLine;
		protected bool _allowCache = true;
		protected UserInfo _user = null;
		protected readonly DateTime _created = DateTime.Now;
		#endregion

		#region Constructors
		public Command(string newCmd, UserInfo user, DateTime? when = null, bool allowCache = true, bool processed = false)
		{
			if ( user is null )
				throw new ArgumentNullException( "You must specify a user to instantiate this class!" );

			if ( string.IsNullOrWhiteSpace( newCmd ) )
				throw new ArgumentException( "You must provide a command to parse in order to instantiate this class!" );

			this._allowCache = allowCache;
			this._cmd = newCmd.TrimStart();
			this._created = (when is null) ? DateTime.Now : (DateTime)when;
			this._cmdLine = new CommandLine( this._cmd );
			this._processed = processed;
			this._user = user;
		}
		#endregion

		#region Operators
		public static bool operator !=(Command left, Command right) => !(left == right);
		public static bool operator ==(Command left, Command right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return left.Cmd.Equals(right.Cmd, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(Command left, string right) => !(left == right);
		public static bool operator ==(Command left, string right)
		{
			if (left is null) return (right is null) || (right == "");
			if (right is null) return false;
			return left.Cmd.Equals(right, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator !=(string left, Command right) => (right != left);
		public static bool operator ==(string left, Command right) => (right == left);

		// Implicit functions to facilitate direct interaction with strings...
		public static implicit operator string( Command source ) => (source is null) ? "" : source._cmd;
		#endregion

		#region Accessors
		/// <summary>Returns a TimeSpan value indicating the age of the object.</summary>
		public TimeSpan Age => DateTime.Now - this.Created;

		/// <summary>Indicates whether or not this command has been processed. NOTE: Once this has been set, it cannot be un-set.</summary>
		public bool Processed
		{
			get => this._processed;
			set => this._processed |= value;
		}

		/// <summary>Returns the first "word" of the stored Command.</summary>
		public string Cmd => (Parts.Length > 0) ? Parts[0] : "";

		/// <summary>Returns the remainder of the Command Line text after the first "word".</summary>
		public string Payload
		{
			get => (Parts.Length > 1) ? Parts[1] : "";
			set
			{
				this._cmd = Parts[0] + " " + value;
				this._cmdLine = new CommandLine( this._cmd );
			}
		}

		/// <summary>Contains/manages the command line information.</summary>
		public CommandLine CmdLine => this._cmdLine;

		/// <summary>Contains the user infomation associated with the instantiator of this command.</summary>
		public UserInfo User => this._user;

		/// <summary>Indicates the privilege level at which the command is to execute.</summary>
		public Ranks AsRank => this._user.Rank;

		/// <summary>Returns the string/data portion of the stored Command parameters.</summary>
		public CommandLineArgs Args => _cmdLine.Args;

		/// <summary>Returns the switches contained in the stored Command parameters.</summary>
		public CommandLineSwitches Switches => _cmdLine.Switches;

		/// <summary>Breaks the stored string into the first word + all the rest.</summary>
		protected string[] Parts =>
			this._cmd.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

		/// <summary>Gets/Sets the moment when this object was created.</summary>
		protected DateTime Created => this._created;

		/// <summary>Indicates whether this command is allowed to be cached.</summary>
		public bool AllowCache => this._allowCache;
		#endregion

		#region Methods
		public string ApplyEnvironment(EnvironmentVars env, bool applyToSource = false)
		{
			string result = env.ApplyTo(_cmd);
			if (applyToSource) _cmd = result;
			return result;
		}

		public override string ToString() => "[" + Age.ToString() + "]:" + _cmd;

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();
		#endregion
	}

	/// <summary>Facilitates managing a collection (queue) of Commands.</summary>
	public class CommandCollection : IEnumerator<Command>
	{
		#region Properties
		/// <summary>Stores the collection of managed Commands.</summary>
		protected List<Command> _collection = new List<Command>();

		/// <summary>Used by the IENumerator functions.</summary>
		private int _position = 0;

		/// <summary>Tracks the "current" position within the queue.</summary>
		protected int _pointer = 0;

		/// <summary>Specifies the maximum number of commands to keep queued.</summary>
		protected byte _cacheLimit = 25;
		#endregion

		#region Constructors
		public CommandCollection(byte cacheLimit = 25) => this._cacheLimit = cacheLimit;
		#endregion

		#region Accessors
		/// <summary>Facilitates interacting with the collection via index.</summary>
		/// <param name="index">The index to Get/Set.</param>
		public Command this[int index]
		{
			get => (index < Count) ? this._collection[Math.Max(0, Math.Min(index, Count - 1))] : null;
			set { if ((index < Count) && (index >= 0)) { this._collection[index] = value; } }
		}
		/// <summary>Reports the total number of commands in the queue.</summary>
		public int Count => this._collection.Count;

		/// <summary>Gets/Sets the number of items that the cache is allowed to manage.</summary>
		public byte CacheLimit
		{
			get => this._cacheLimit;
			protected set
			{
				this._cacheLimit = Math.Max(value, (byte)5);
				if (Count > _cacheLimit)
					Prune();
			}
		}

		/// <summary>Returns all currently Unprocessed Commands in the queue.</summary>
		public CommandCollection Waiting
		{
			get
			{
				CommandCollection results = new CommandCollection();
				foreach (Command cmd in this)
					if (!cmd.Processed)
						results.Add(cmd);

				return results;
			}
		}

		/// <summary>The collection is considered "Active" whenever unprocessed commands exist in the collections.</summary>
		public bool Active => (FirstUnProcessed() >= 0);

		/// <summary>Fetches the previous command string from the _pointer position if there is one, otherwise returns NULL</summary>
		public string Previous => (this._pointer > 0) ? (string)this[--_pointer] : "";

		/// <summary>Fetches the next command string from the _pointer position, if there is one, otherwise returns NULL</summary>
		public string Next => (this._pointer < Count - 1) ? (string)this[++_pointer] : "";

		/// <summary>Returns the next Unprocessed command from the queue (and marks it as processed).</summary>
		public Command NextWaiting
		{
			get
			{
				Command cmd = null;
				int i = FirstUnProcessed();
				if (i >= 0)
				{
					cmd = this[i];
					if (cmd.AllowCache)
					{
						this[i].Processed = true;
						this._pointer = Math.Min(i + 1, Count);
					}
					else
					{
						this._collection.RemoveAt(i);
						this._pointer = Math.Max(this._pointer, Count);
					}
				}
				return cmd;
			}
		}

		// IEnumerator Support Accessors...
		Command IEnumerator<Command>.Current => this[this._position];

		object IEnumerator.Current => this[this._position];
		#endregion

		#region Operators
		//public static CommandCollection operator +(CommandCollection left, Command right)
		//{
		//	left.Add(right);
		//	return left;
		//}

		//public static CommandCollection operator +(CommandCollection left, string right)
		//{
		//	left.Add(right);
		//	return left;
		//}
		#endregion

		#region Methods
		/// <summary>Finds the first unprocessed command in the queue.</summary>
		/// <returns>If no unprocessed commands are in the queue, -1; otherwise the index of the first unprocessed command.</returns>
		public int FirstUnProcessed()
		{
			int i = -1; while ((++i < Count) && this[i].Processed) ;
			return (i < Count) ? i : -1;
		}

		/// <summary>Adds a new Command to the collection and modifies the pointer and prunes as necessary.</summary>
		/// <param name="newCmd">The new Command object to add to the collection.</param>
		/// <param name="suppressCache">If set to TRUE, the added command will not be </param>
		public void Add(Command newCmd)
		{
			this._pointer = Count;
			this._collection.Add(newCmd);
			if (Count > _cacheLimit) Prune();
		}

		/// <summary>Adds an array of new Command objects to the collection and modifies the pointer / prunes as necessary.</summary>
		/// <param name="command">A string containing the new command to add.</param>
		public void Add(string command, UserInfo user, bool allowCache = true)
			=> Add(new Command(command, user, null, allowCache));

		/// <summary>Adds a new command (passed as a string) to the collection and modifies the pointer / prunes as necessary.</summary>
		/// <param name="newCmds">An array of Command objects to add into the collection.</param>
		public void AddRange(Command[] newCmds)
		{
			foreach (Command c in newCmds)
				this.Add(c);
		}

		/// <summary>Adds a new command (passed as a string) to the collection and modifies the pointer and prunes as necessary.</summary>
		/// <param name="command">A string containing the new command to add.</param>
		public void AddRange(string[] newCmds, UserInfo user)
		{
			foreach (string s in newCmds)
				this.Add(s, user);
		}

		/// <summary>Cuts the collection down to it's restricted size.</summary>
		public void Prune()
		{
			if (Count > _cacheLimit)
			{
				while (Count > _cacheLimit)
				{
					this._collection.RemoveAt(0);
					this._pointer -= 1;
					this._position -= 1;
				}
				this._pointer = Math.Max(0, this._pointer);
				this._position = Math.Max(0, this._position);
			}
		}

		/// <summary>Removes all processed commands from the queue.</summary>
		public void Purge()
		{
			int i = 0;
			while (i < Count)
				if (this[i].Processed)
					this._collection.RemoveAt(i);
				else i++;
		}

		/// <summary>Safely advances the collection Pointer by the prescribed amount.</summary>
		/// <param name="amount">The amount by which to advance the Pointer; default = 1</param>
		public void AdvancePointer(int amount = 1) => this._pointer = Math.Min(Math.Max(1, amount), Count);

		/// <summary>Returns the current collection as an array of Command objects.</summary>
		/// <returns></returns>
		public Command[] ToArray() => this._collection.ToArray();

		/// <summary>Clears and resets the collection, deleting any / all queued and past commands!</summary>
		public void Clear() => this._collection = new List<Command>();
		#endregion

		//IEnumerator Support
		public IEnumerator<Command> GetEnumerator() => this._collection.GetEnumerator();

		bool IEnumerator.MoveNext() => (++this._position) < this._collection.Count;

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
	}

	/// <summary>Used to manage the command line input data in conjunction with the cursor.</summary>
	public class CommandInput
	{
		#region Properties
		protected string _data = "";
		protected int _cursor = 0;
		protected Prompt _prompt;
		protected CliColor _defaultColor;
		#endregion

		#region Constructors
		public CommandInput(string prompt = "CLI>", string source = "", CliColor color = null)
		{
			this.Color = color;
			this._data = source;
			this.Prompt = new Prompt(prompt);
		}

		public CommandInput(Prompt prompt, string source = "", CliColor color = null)
		{
			this.Color = color;
			this._data = source;
			this._prompt = prompt;
		}
		#endregion

		#region Accessors
		/// <summary>Reports the length of the stored string.</summary>
		public int Length => this._data.Length;

		/// <summary>Gets/Sets the stored string.</summary>
		public string Data
		{
			get => Cleanup( _data );
			set
			{
				this._data = value;
				Cursor = this._cursor;
			}
		}

		/// <summary>Relates the position of the cursor within the scope of the command.</summary>
		public int Cursor
		{
			get => this._cursor;
			set
			{
				this._cursor = Math.Max(0, Math.Min(value, Length));
				Console.CursorLeft = (Prompt.Length + this._cursor) % Console.BufferWidth;
			}
		}

		/// <summary>Gets/Sets the prompt text to use.</summary>
		public string PromptText => this._prompt.Value;

		public Prompt Prompt
		{
			get => this._prompt;
			set => this._prompt = value;
		}

		/// <summary>Gets/Sets the default color.</summary>
		public CliColor Color
		{
			get => this._defaultColor;
			set { this._defaultColor = (value is null) ? CliColor.CaptureConsole() : value; ; this._defaultColor.ToConsole(); }
		}
		#endregion

		#region Operators
		public static CommandInput operator +(CommandInput left, string right)
		{
			left.InsertAtCursor(right);
			return left;
		}

		public static CommandInput operator +(CommandInput left, char right) => left + right.ToString();
		#endregion

		#region Methods
		/// <summary>Replaces some text combinations with untypable equivalents.</summary>
		protected string Cleanup( string work )
		{
			work = Regex.Replace( work, @"(?<=[^<^])<=(?=[^=$])", "≤" );
			work = Regex.Replace( work, @"(?<=[^>^])>=(?=[^=$])", "≥" );
			work = Regex.Replace( work, @"(?<=[^!^])!=(?=[^=$])", "≠" );
			work = Regex.Replace( work, @"(?<=[^=^])==(?=[^=$])", "=" );
			work = work.Replace( "{", "&lbrace;" ).Replace( "}", "&rbrace;" ); // <-- no markups on the command line!
			return work;
		}

		/// <summary>Finds the initial position of the next word to the left of the cursor.</summary>
		/// <param name="moveCursor">If TRUE, the cursor is moved to the position.</param>
		/// <returns>The initial position of the next word to the left of the cursor.</returns>
		public int WordLeft(bool moveCursor = true)
		{
			if (_cursor > 0)
			{
				string s = _data.Substring(0, _cursor).Trim();
				int pos = s.LastIndexOf(' ') + 1;
				if (pos >= 0)
				{
					if (moveCursor) Cursor = pos;
					return pos;
				}
			}
			return _cursor;
		}

		/// <summary>Finds the initial position of the next word to the right of the cursor.</summary>
		/// <param name="moveCursor">If TRUE, the cursor is moved to the specified position.</param>
		/// <returns>The initial position of the next word to the right of the cursor.</returns>
		public int WordRight(bool moveCursor = true)
		{
			if (_cursor < Length)
			{
				string s = _data.Substring(_cursor).Trim();
				int pos = s.IndexOf(' ') + _cursor;
				if (pos < _cursor)
					pos = int.MaxValue;
				else
					while ((_data[pos] == ' ') && (pos < Length)) pos += 1;

				if (pos > Length) pos = Length;
				if (moveCursor) Cursor = pos;
			}
			return _cursor;
		}

		/// <summary>Inserts the provided text at the cursor and moves the cursor afterward.</summary>
		/// <param name="value">The text to be inserted.</param>
		public void InsertAtCursor(string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				string left = (_cursor > 0) ? this._data.Substring(0, _cursor) : "",
					   right = (_cursor < Length) ? this._data.Substring(_cursor) : "";

				this._data = left + value + right;
				this._cursor += value.Length;
			}
		}

		/// <summary>Deleted a specified number of characters starting at the cursor.</summary>
		/// <param name="howMany">How many characters to delete (default = 1)</param>
		public void DeleteAtCursor(int howMany = 1)
		{
			if (this._cursor < Length)
			{
				string left = (_cursor > 0) ? this._data.Substring(0, _cursor) : "",
					   right = (_cursor + howMany < Length) ? this._data.Substring(_cursor + howMany) : "";

				this._data = left + right;
			}
		}

		/// <summary>Clears the current line, and rebuilds it with the known information.</summary>
		/// <param name="cmd">An optional string to write instead of the one currently stored.</param>
		public void Write(string cmd = null)
		{
			if (cmd is null) cmd = _data;

			if ( cmd.Length + Prompt.Length > Console.BufferWidth )
			{
				int lines = (cmd.Length + Prompt.Length) / Console.BufferWidth;
				Console.CursorTop = Math.Max( 0, Console.CursorTop - lines );
			}

			Con.ClearLine(_defaultColor);
			Prompt.Write(cmd, _defaultColor);
			Con.Cursor = _cursor + Prompt.Length;
		}

		/// <summary>Clears the current line and rebuilds it with the information within this object, followed by a newline and a new prompt.</summary>
		/// <returns>A string containing the command text that was displayed.</returns>
		public Command WriteLn(UserInfo user, bool suppressPrompt = true)
		{
			string result = Data;
			Con.ClearLine();
			Prompt.Write(result, _defaultColor, true);
			if (!suppressPrompt) Prompt.Write();

			this._cursor = suppressPrompt ? result.Length : 0;
			Console.CursorLeft = Prompt.Length + _cursor;
			this._data = "";

			return new Command(result, user);
		}

		/// <summary>Returns the current command text as a Command object.</summary>
		public Command ToCommand(UserInfo user) => new Command(this._data, user);

		public override string ToString() =>
			_prompt.RawPrompt + Data;
		#endregion

		// Facilitates assignment to/from strings...
		//public static implicit operator CommandInput(string source) => new CommandInput(source);
		//public static implicit operator string(CommandInput source) => source._data;
		//public static implicit operator Command(CommandInput source) => source.ToCommand();
		//public static implicit operator CommandInput(Command source) => new CommandInput((string)source);
	}
}

using System.Text.RegularExpressions;
using NetXpertExtensions.Classes;
using NetXpertExtensions;
using IniFileManagement.Values;
using static IniFileManagement.Values.IniLineValueFoundation;

namespace IniFileManagement
{

	public partial class IniBlockMgmt
	{
		#region Properties
		public enum BlockTypes
		{
			Unknown,
			/// <summary>INI and Application Parameters.</summary>
			Params,
			/// <summary>Application-Specific Settings.</summary>
			Config,
			/// <summary>Users' Settings.</summary>
			Settings
		};

		protected string _preamble = string.Empty;
		protected BlockTypes _type = BlockTypes.Unknown;
		protected readonly List<IniGroupMgmt> _groups = new();
		protected readonly IniFileMgmt _root;
		#endregion

		#region Constructors
		protected IniBlockMgmt( IniFileMgmt parent )
		{
			ArgumentNullException.ThrowIfNull( parent, $"You must specify a parent class!" );
			this._root = parent;
		}

		public IniBlockMgmt( IniFileMgmt parent, BlockTypes type )
		{
			ArgumentNullException.ThrowIfNull( parent, $"You must specify a parent class!" );
			this._root = parent;
			this._type = type;
		}

		public IniBlockMgmt( IniFileMgmt parent, BlockTypes type, params IniGroupMgmt[] groups )
		{
			ArgumentNullException.ThrowIfNull( parent, $"You must specify a parent class!" );
			this._root = parent;
			this._type = type;
			this._groups.AddRange( groups );
		}
		#endregion

		#region Accessors
		public IniGroupMgmt? this[ string groupName ]
		{
			get
			{
				if (this.BlockType == BlockTypes.Params) groupName = "params";

				int i = IndexOf( groupName );
				return (i < 0) ? null : this._groups[ i ];
			}
		}

		public int Count => this._groups.Count;

		public virtual BlockTypes BlockType
		{
			get => this._type;
			set
			{
				if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

				if ((this._type == BlockTypes.Unknown) && (value != BlockTypes.Unknown))
					this._type = value;
			}
		}

		public string Preamble
		{
			get => this._preamble;
			set
			{
				if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();
				this._preamble = value.Trim();
			}
		}

		public virtual IniEncryptionKey? EncryptionKey => this.Root.EncryptionKey;

		public IniFileMgmt Root => this._root;

		//public byte[]? EncryptionKey
		//{
		//	private get => this._encrKey is null ? Array.Empty<byte>() : this._encrKey;
		//	set
		//	{
		//		// If the supplied value is a file serial number, extract the key from it
		//		if ((value is not null) && (value.Length == 128))
		//			value = value.Copy( value[ 7 ], 32 );

		//		if ((value is not null) && (value.Length != 32))
		//			throw new ArgumentException( "The supplied EncryptionKey isn't valid." );

		//		this._encrKey = value;
		//		for (int i = 0; i < this._groups.Count; i++)
		//			this._groups[ i ].EncryptionKey = value;
		//	}
		//}
		#endregion

		#region Methods
		private int IndexOf( string groupName )
		{
			int i = -1; while ((++i < Count) && !_groups[ i ].Name.Equals( groupName, StringComparison.OrdinalIgnoreCase )) ;
			return (i < Count) ? i : -1;
		}

		public override string ToString()
		{
			string result = $"{Preamble}\r\n~{BlockType.ToString().ToUpperInvariant()}:\r\n";
			if (BlockType != BlockTypes.Params)
				for (int i = 0; i < this._groups.Count; i++)
					result += (i > 0 ? "\r\n" : "") + this._groups[ i ].Compile();

			return $"{result}~:END\r\n";
		}

		public IniGroupMgmt[] Groups => [ .. this._groups ];

		public static IniBlockMgmt? Parse( IniFileMgmt root, string body )
		{
			ArgumentNullException.ThrowIfNull( root, $"You must specify a parent class!" );
			IniBlockMgmt? result = null;

			if (string.IsNullOrWhiteSpace( body ))
				throw new ArgumentException( "The supplied body text cannot be null, empty or whitespace!" );

			Match m = IniFileMgmt.IniFileParser().Match( body );
			if (m.Success)
			{
				BlockTypes bt = BlockTypeCleaner_Rx().Replace( m.Groups[ "type" ].Value, "" ).UCWords( true ).ToEnum( BlockTypes.Unknown );
				switch (bt)
				{
					case BlockTypes.Settings:
					case BlockTypes.Config:
					default:
						result = new IniBlockMgmt( root, bt ); // new() { _type = bt };
						result._groups.AddRange( IniGroupMgmt.ParseBody( root, m.Groups[ "body" ].Value ) );
						break;
					case BlockTypes.Params:
						result = IniParamMgmt.Parse( root, m.Groups[ 4 ].Value );
						break;
					case BlockTypes.Unknown:
						throw new ArgumentException( $"The value provided for the block type could not be parsed! (\x22{m.Groups[ "type" ].Value}\x22)" );
				}
				result.Preamble = m.Groups[ "preamble" ].Success ? m.Groups[ "preamble" ].Value : string.Empty;
			}
			return result;
		}

		public bool HasGroup( string name ) => IndexOf( name ) >= 0;

		public IniGroupMgmt CreateGroup( string name, string desc = "", params IniLineBase[] lines )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			IniGroupMgmt group = new( Root, name, desc );
			if (lines is not null && lines.Length > 0)
				group.AddRange( lines );

			this.Add( group );
			return group;
		}

		public void Add( IniGroupMgmt group )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			int i = IndexOf( group.Name );
			if (i < 0)
				this._groups.Add( group );
			else
				this._groups[ i ].AddRange( group.ToArray() );
		}

		public IniGroupMgmt? Remove( string name )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			int i = IndexOf( name );
			IniGroupMgmt? result = null;
			if (i >= 0)
			{
				result = this._groups[ i ];
				this._groups.RemoveAt( i );
			}
			return result;
		}

		/// <summary>Merges a supplied <seealso cref="IniBlockMgmt""/> object with this one.</summary>
		/// <param name="block">The block to merge.</param>
		/// <param name="strict">If <b>true</b> (default), the supplied <paramref name="block"/> must match the 
		/// <seealso cref="BlockTypes"/> of this object to be merged. If <b>false</b>, the merge will be performed
		/// regardless of the supplied block's <seealso cref="BlockTypes"/></param>
		public void MergeBlock( IniBlockMgmt block, bool strict = true )
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();

			if ((block.BlockType == BlockType) || (!strict))
				foreach (IniGroupMgmt group in block._groups)
					this.Add( group );
		}

		public void Clear()
		{
			if (Root.ReadOnly) throw IniFileMgmt.ReadOnlyException();
			this._groups.Clear();
		}

		[GeneratedRegex( "[^a-zA-Z\\d]" )]
		private static partial Regex BlockTypeCleaner_Rx();
		#endregion
	}

	public sealed class IniParamMgmt : IniBlockMgmt
	{
		#region Properties
		private IniSerialNumber? _serialNumber = null;
		private IniEncryptionKey? _encrKey = null;
		#endregion

		#region Constructors
		public IniParamMgmt( IniFileMgmt parent, IniGroupMgmt paramGroup ) : base( parent )
		{
			InitParams();
			foreach (var line in this._groups[ 0 ].ToArray())
				if (paramGroup.HasItem( line.Value ))
					this._groups[ 0 ][ line.Value ].Value = line.Value;
		}

		private IniParamMgmt( IniFileMgmt parent ) : base( parent ) => InitParams();
		#endregion

		#region Accessors
		public override BlockTypes BlockType => BlockTypes.Params;

		private IniGroupMgmt Params => this._groups[ 0 ];

		new public IniLineBase? this[ string key ]
		{
			get => this.Params[ key ];
			set
			{
				if (this.Params.HasItem( key ))
				{
					this.Remove( key );
					if (value is not null) this.Add( value );
				}
				else
					if (value is not null) this.Add( value );
			}
		}

		public VersionMgmt Version
		{
			get =>
				this.Params.HasItem( "Version" ) ? VersionMgmt.Parse( this[ "Version" ].Value ) : VersionMgmt.Parse( "1.0.0.0" );
			set
			{
				this[ "Version" ].Value = (this.Root, value is not null ? value.ToString() : "1.0.0.0");
				this.Modifier = "";
			}
		}

		public bool EncryptByDefault
		{
			get => this.Params.HasItem( "EncryptByDefault" ) ? this[ "EncryptByDefault" ].Value.As<bool>() : false;
			set
			{
				this[ "EncryptByDefault" ].Value = (this.Root, value ? "on" : "off");
				this.Modifier = "";
			}
		}

		public DateTime Created =>
			this.Params.HasItem( "Created" ) ? this[ "Created" ].Value.As<DateTime>() : DateTime.Now;

		public DateTime Modified
		{
			get => this.Params.HasItem( "Modified" ) ? this[ "Modified" ].Value.As<DateTime>() : DateTime.Now;
			private set
			{
				if (this.Created == IniFileMgmt.DATE_NOT_SET)
				{
					this[ "Modified" ].Value = (this.Root, DateTime.Now.ToMySqlString());
					this.Modifier = "";
				}
			}
		}

		public string Creator
		{
			get => this.Params.HasItem( "Creator" ) ? this[ "Creator" ].Value : Environment.UserName;
			private set
			{
				this[ "Modified" ].Value = (this.Root, DateTime.Now.ToMySqlString());
				this[ "Creator" ].Value = (this.Root, string.IsNullOrWhiteSpace( value ) ? Environment.UserName : value);
			}
		}

		/// <summary>The name of the last user to modify this data.</summary>
		public string Modifier
		{
			get => this.Params.HasItem( "Modifier" ) ? this[ "Modifier" ].Value : Environment.UserName;
			private set
			{
				this[ "Modified" ].Value = (this.Root, DateTime.Now.ToMySqlString());
				this[ "Modifier" ].Value = (this.Root, string.IsNullOrWhiteSpace( value ) ? Environment.UserName : value);
			}
		}

		public bool ReadOnly
		{
			get => this.Params.HasItem( "ReadOnly" ) && bool.Parse( this[ "ReadOnly" ].Value );
			set => this[ "ReadOnly" ].Value = (this.Root, !ReadOnly && value ? "true" : "false");
		}

		public IniSerialNumber SerialNumber => this._serialNumber;

		public override IniEncryptionKey? EncryptionKey
		{
			get
			{
				if (this._encrKey is null)
				{
					if ((this._serialNumber is null) && this.HasItem( "SerialNo" ))
						this._serialNumber = new IniSerialNumber( this[ "SerialNo" ].Value );

					if (this.HasItem( "EncrKey" ) && (this._serialNumber is not null))
						this._encrKey = new IniEncryptionKey( this._serialNumber );
				}
				return this._encrKey;
			}
		}

		new private void Remove( string key ) => base.Remove( key );
		#endregion

		#region Methods
		private void InitParams()
		{
			this._groups.Clear();
			this._groups.Add( new( this._root, "Params", "DefaultParameterBlock" ) );

			this._serialNumber = new();
			this._encrKey = new( this._serialNumber );

			Params.AddRange(
				[
					new IniLine<Version>( this.Root, "Version", new Version(1,0,0,0), IniComment.Empty(), QuoteTypes.None ),
					new IniLine<bool>( this.Root, "EncryptByDefault", true, IniComment.Empty(), QuoteTypes.None ),
					new IniLine<DateTime>( this.Root, "Created", DateTime.Now, IniComment.Empty(), QuoteTypes.DoubleQuote ),
					new IniLine( this.Root, "Creator", Environment.UserName, IniComment.Empty(), QuoteTypes.DoubleQuote ),
					new IniLine<DateTime>( this.Root, "Modified", DateTime.Now, IniComment.Empty(), QuoteTypes.DoubleQuote ),
					new IniLine( this.Root, "Modifier", Environment.UserName, IniComment.Empty(), QuoteTypes.DoubleQuote ),
					new IniLine<IniEncryptionKey>( this.Root, "EncrKey", this._encrKey, IniComment.Empty(), QuoteTypes.None ),
					new IniLine<IniSerialNumber>( this.Root, "SerialNo", this._serialNumber, IniComment.Empty(), QuoteTypes.None ),
					new IniLine<bool>( this.Root, "ReadOnly", false, IniComment.Empty(), QuoteTypes.None )
				]
			);
		}

		public bool HasItem( string itemName )
		{
			if (string.IsNullOrWhiteSpace( itemName )) return false;
			return this.Params.HasItem( itemName );
		}

		public void Add( IniLineBase iniLine ) =>
			base[ "params" ].Add( iniLine );

		public void AddRange( params IniLineBase[] items ) =>
			base[ "params" ].AddRange( items );

		/// <summary>Prevents using this method on a Parameters block.</summary>
		new private IniGroupMgmt CreateGroup( string name, string desc = "", params IniLineBase[] items ) =>
			throw new NotImplementedException();

		new public static IniParamMgmt? Parse( IniFileMgmt parent, string source )
		{
			(IniLine? serNbr, IniLine? encrKey) = (null, null);
			IniParamMgmt? result = null;
			if (!string.IsNullOrWhiteSpace( source ))
			{
				result = new( parent ) { BlockType = BlockTypes.Params };
				string[] lines = source.Split( [ '\n', '\r' ], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
				foreach (string s in lines)
				{
					dynamic? iLine = IniLineBase.ParseLine( parent, s );
					if (iLine is not null)
					{
						var line = iLine as IniLineBase;
						switch (iLine.Key.ToUpperInvariant())
						{
							case "ENCRKEY": encrKey = (IniLine)line; break;
							case "SERIALNBR":
							case "SERIALNO": serNbr = (IniLine)line; break;
							default:
								if (result._groups[ 0 ].HasItem( line.Key ))
									result._groups[ 0 ][ line.Key ].Value = line.Value;
								else
									result._groups[ 0 ].Add( iLine );
								break;
						}
					}
				}

				if (serNbr is not null)
				{
					IniSerialNumber sN = IniSerialNumber.Parse(serNbr.Value); //.As<IniSerialNumber>();
					result._groups[ 0 ].Add( new IniLine<IniSerialNumber>( parent, serNbr.Key, sN ) );
					if (sN is not null && encrKey is not null)
					{
						IniEncryptionKey? key = IniEncryptionKey.Parse( encrKey.Value, sN );
						result._groups[ 0 ].Add( new IniLine<IniEncryptionKey>( parent, encrKey.Key, key ) );
					}
				}
			}
			return result;
		}

		public static IniParamMgmt Create( IniFileMgmt parent, IniGroupMgmt? group = null )
			=> group is null ? new( parent ) : new( parent, group );

		public override string ToString()
		{
			string result = $"{Preamble}\r\n~PARAMS:\r\n";
			foreach (var line in this._groups[ 0 ].ToArray())
				result += $"{line}\r\n";

			return result + "~:END\r\n";
		}
		#endregion
	}

	public sealed class IniUserMgmt : IniBlockMgmt
	{
		#region Constructors
		private IniUserMgmt( IniFileMgmt parent ) : base( parent ) { }
		#endregion

		#region Accessors
		#endregion
	}
}

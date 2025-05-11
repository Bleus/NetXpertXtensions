using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>Provides a common foundation for managing postal codes.</summary>
	public abstract class PostalCodeFoundation
	{
		#region Properties
		private string _data = "";
		#endregion

		#region Constructors
		protected PostalCodeFoundation() { }

		protected PostalCodeFoundation( string source ) => Data = source;

		protected PostalCodeFoundation( XmlNode node )
		{
			if ( !(node is null) )
				this.Data = node.InnerText;
		}
		#endregion

		#region Operators
		public static bool operator ==( PostalCodeFoundation left, PostalCodeFoundation right )
		{
			if ( left is null ) return (right is null);
			if ( right is null ) return false;
			return (left.Value == right.Value);
		}

		public static bool operator !=( PostalCodeFoundation left, PostalCodeFoundation right ) => !(left == right);


		public static bool operator ==( string left, PostalCodeFoundation right ) => (right == left);
		public static bool operator ==( PostalCodeFoundation left, string right )
		{
			if ( left is null ) return (right is null) || (right.Length == 0);
			if ( right is null ) return false;

			return left.Value.Equals( right, StringComparison.OrdinalIgnoreCase );
		}

		public static bool operator !=( PostalCodeFoundation left, string right ) => !(left == right);
		public static bool operator !=( string left, PostalCodeFoundation right ) => !(right == left);
		#endregion

		#region Accessors
		///<summary>Allows descendent classes to impose some data integrity testing or formatting inbetween the base Data accessor and the User.</summary>
		public virtual string Value
		{
			get => this.Data;
			set => this.Data = value;
		}

		protected string Data
		{
			get => string.IsNullOrWhiteSpace( this._data ) ? "" : this._data;
			set
			{
				value = string.IsNullOrWhiteSpace( value ) ? "" : Regex.Replace( value, @"[^\da-z]", "", RegexOptions.IgnoreCase );
				if ( Validate( value ) ) this._data = value;
			}
		}

		public int Length => this._data.Length;
		#endregion

		#region Methods
		public override string ToString() => this.Value;

		protected XmlNode CreateXmlNode( string tag = "PostalCode", string type = "" ) => $"<{tag}{(string.IsNullOrWhiteSpace( type ) ? "" : $" type='{type}'")}>{Data}</{tag}".ToXmlNode();

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		/// <summary>Specifies the Regex object used to validate the proposed PostalCode data.</summary>
		/// <remarks>Instance-specific code should supply a valid Regex to the static 'Validate( string, Regex )' method.</remarks>
		/// <seealso cref="Validate(string, Regex)"/>
		protected abstract bool Validate( string value );

		/// <summary>Supplies the skeleton container for validating data with a supplied Regex object.</summary>
		/// <param name="validator">A Regex object containing the data-validation pattern for the requisite PostalCode derivative.</param>
		/// <returns>TRUE if the supplied data corresponds with the provided pattern.</returns>
		protected static bool Validate( string source, Regex validator )
		{
			source = source.Filter( /* language=regex */ @"[^a-z0-9]", RegexOptions.IgnoreCase ).ToUpper( CultureInfo.CurrentCulture );
			if ( (validator is null) || string.IsNullOrWhiteSpace( source ) ) return false;

			return validator.IsMatch( source );
		}
		#endregion

		#region Comparer
		public class PostalCodeComparer : IComparer<PostalCodeFoundation>
		{
			public int Compare( PostalCodeFoundation a, PostalCodeFoundation b )
			{
				if ( a is null ) return (b is null) ? 0 : 1;
				if ( b is null ) return -1;

				return a.ToString().CompareTo( b.ToString() );
			}
		}
		#endregion
	}

	/// <summary>Provides a mechanism for managing Canadian PostalCodes.</summary>
	public class PC_Canada : PostalCodeFoundation
	{
		#region Properties
		protected static readonly Regex PATTERN = new Regex( @"^([ABCEGHJKLMNPRSTVXY][\d][ABCEGHJKLMNPRSTVWXYZ])[- ]?([\d][ABCEGHJKLMNPRSTVWXYZ][\d])$", RegexOptions.IgnoreCase );
		#endregion

		#region Constructors
		public PC_Canada() { }

		public PC_Canada( string source ) : base( source ) { }

		public PC_Canada( XmlNode node ) : base( node ) { }
		#endregion

		#region Operators
		public static bool operator ==( string left, PC_Canada right ) => (right == left);
		public static bool operator ==( PC_Canada left, string right )
		{
			if ( left is null ) return (right is null) || (right.Length == 0);
			if ( (right is null) || (right.Length < 7) || !PC_Canada.IsValid( right ) ) return false;

			return ((PostalCodeFoundation)left == (PostalCodeFoundation)new PC_Canada( right ));
		}

		public static bool operator !=( PC_Canada left, string right ) => !(left == right);
		public static bool operator !=( string left, PC_Canada right ) => !(right == left);

		public static implicit operator string( PC_Canada data ) =>
			(data is null) ? "" : data.ToString();

		public static implicit operator PC_Canada( string data ) => new PC_Canada( data );
		#endregion

		#region Methods
		public XmlNode ToXmlNode() => base.CreateXmlNode( "PostalCode", this.GetType().Name );

		public static bool IsValid( string source )
		{
			source = source.Filter( /* language=regex */ @"[^a-z0-9]", RegexOptions.IgnoreCase ).ToUpper( CultureInfo.CurrentCulture );
			return !string.IsNullOrWhiteSpace( source ) && PATTERN.IsMatch( source );
		}

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		protected override bool Validate( string test ) => Validate( test, PATTERN );
		#endregion
	}

	/// <summary>Provides a mechanism for managing American Zip Codes.</summary>
	public class PC_UnitedStates : PostalCodeFoundation
	{
		#region Properties
		protected static readonly Regex PATTERN = new Regex( @"^[0-9]{5}(?:-?[0-9]{4})?$" );
		#endregion

		#region Constructors
		public PC_UnitedStates() { }

		public PC_UnitedStates( string source ) : base( source ) { }

		public PC_UnitedStates( int value ) : base( Math.Abs( value ).ToString() ) { }

		public PC_UnitedStates( XmlNode node ) : base( node ) { }
		#endregion

		#region Operators
		public static bool operator ==( string left, PC_UnitedStates right ) => (right == left);
		public static bool operator ==( PC_UnitedStates left, string right )
		{
			if ( left is null ) return (right is null) || (right.Length == 0);
			if ( (right is null) || (right.Length < 7) || !PC_UnitedStates.IsValid( right ) ) return false;

			return ((PostalCodeFoundation)left == (PostalCodeFoundation)new PC_UnitedStates( right ));
		}

		public static bool operator !=( PC_UnitedStates left, string right ) => !(left == right);
		public static bool operator !=( string left, PC_UnitedStates right ) => !(right == left);

		public static implicit operator string( PC_UnitedStates data ) =>
			(data is null) ? "" : data.ToString();

		public static implicit operator PC_UnitedStates( string data ) => new PC_UnitedStates( data );
		#endregion

		#region Accessors
		#endregion

		#region Methods
		public XmlNode ToXmlNode() => base.CreateXmlNode( "ZipCode", this.GetType().Name );

		public static bool IsValid( string source )
		{
			source = source.Filter( /* language=regex */ @"[^a-z0-9]", RegexOptions.IgnoreCase ).ToUpper( CultureInfo.CurrentCulture );
			return !string.IsNullOrWhiteSpace( source ) && PATTERN.IsMatch( source );
		}

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		protected override bool Validate( string test ) => Validate( test, PATTERN );
		#endregion
	}

	/// <summary>Provides a mechanism for managing United Kingdom PostalCodes.</summary>
	/// <remarks>DOES NOT VALIDATE British Overseas Territories' postal codes!</remarks>
	public class PC_UnitedKingdon : PostalCodeFoundation
	{
		#region Properties
		protected static readonly Regex PATTERN = new Regex( @"^([A-Z][A-HJ-Y]?[0-9][A-Z0-9]? ?[0-9][A-Z]{2}|GIR ?0A{2})$", RegexOptions.IgnoreCase );
		#endregion

		#region Constructors
		public PC_UnitedKingdon() { }

		public PC_UnitedKingdon( string source ) : base( source ) { }

		public PC_UnitedKingdon( XmlNode node ) : base( node ) { }
		#endregion

		#region Operators
		public static bool operator ==( string left, PC_UnitedKingdon right ) => (right == left);
		public static bool operator ==( PC_UnitedKingdon left, string right )
		{
			if ( left is null ) return (right is null) || (right.Length == 0);
			if ( (right is null) || (right.Length < 7) || !PC_UnitedKingdon.IsValid( right ) ) return false;

			return ((PostalCodeFoundation)left == (PostalCodeFoundation)new PC_UnitedKingdon( right ));
		}

		public static bool operator !=( PC_UnitedKingdon left, string right ) => !(left == right);
		public static bool operator !=( string left, PC_UnitedKingdon right ) => !(right == left);

		public static implicit operator string( PC_UnitedKingdon data ) =>
			(data is null) ? "" : data.ToString();

		public static implicit operator PC_UnitedKingdon( string data ) => new PC_UnitedKingdon( data );
		#endregion

		#region Methods
		public XmlNode ToXmlNode() => base.CreateXmlNode( "PostalCode", this.GetType().Name );

		public static bool IsValid( string source )
		{
			source = source.Filter( /* language=regex */ @"[^a-z0-9]", RegexOptions.IgnoreCase ).ToUpper( CultureInfo.CurrentCulture );
			return !string.IsNullOrWhiteSpace( source ) && PATTERN.IsMatch( source );
		}

		public override bool Equals( object obj ) => base.Equals( obj );

		public override int GetHashCode() => base.GetHashCode();

		protected override bool Validate( string test ) => Validate( test, PATTERN );
		#endregion
	}
}

/*
namespace NetXpertCodeLibrary.ConsoleFunctions
{
	//using CobblestoneCommon;
	using System.Drawing;
	using NetXpertCodeLibrary.ContactData;
	public class PostalCodeScreenEditField : ScreenEditField<PC_Canada>
	{
		#region Constructors
		public PostalCodeScreenEditField( string name, PC_Canada data, Point location, Point workAreaHome, int size, string dataName = "", CliColor labelColor = null, CliColor dataColor = null ) :
			base( name, data, location, new Rectangle( workAreaHome.X, workAreaHome.Y, size, 1 ), dataName, dataColor, labelColor ) =>
			Initialize();

		public PostalCodeScreenEditField( string name, PC_Canada data, Point location, int areaX, int areaY, int size, string dataName = "", CliColor labelColor = null, CliColor dataColor = null ) :
			base( name, data, location.X, location.Y, areaX, areaY, size, dataName, dataColor, labelColor ) =>
			Initialize();

		public PostalCodeScreenEditField( string name, PC_Canada data, int locX, int locY, int areaX, int areaY, int size, string dataName = "", CliColor labelColor = null, CliColor dataColor = null ) :
			base( name, data, locX, locY, areaX, areaY, size, dataName, dataColor, labelColor ) =>
			Initialize();
		#endregion

		#region Accessors
		new public Regex FilterPattern
		{
			get => base.FilterPattern;
			protected set => base.FilterPattern = value;
		}

		new public Regex ValidationPattern
		{
			get => base.ValidationPattern;
			protected set => base.ValidationPattern = value;
		}

		new public bool RealTimeValidation
		{
			get => base.RealTimeValidation;
			protected set => base.RealTimeValidation = value;
		}

		new public Rectangle WorkArea
		{
			get => base.WorkArea;
			protected set => base.WorkArea = value;
		}
		#endregion

		#region Methods
		private void Initialize() // Oem1 = colon/semi-colon; Oem2 = slash/question mark
		{
			string pattern = /* language=Regex * /
				@"(?<date>(?<year>20[0-9]{2})[-/](?<month>0?[1-9]|1[0-2])[-/](?<day>[0-2]?[0-9]|3[0-1]))";

			ValidationPattern = new Regex( pattern );
			FilterPattern = new Regex( @"[^0-9a-z]", RegexOptions.IgnoreCase );
			RealTimeValidation = true;
		}

		public override void ProcessKeyStroke( ConsoleKeyInfo keyPressed ) =>
			base.ProcessKeyStroke( keyPressed );

		public override void Write()
		{
			base.DisplayValue( false );
			//ScreenEditController.WriteAt( _location, "{$1}$2:", new object[] { LabelColor, Name } );
			//ScreenEditController.WriteAt( WorkArea.Location, "{$1}$2", new object[] { DataColor, MyScreenValue } );
		}

		public override PC_Canada Parse( string value ) =>
			PC_Canada.IsValid( Value ) ? Value : "";
		#endregion
	}
}
*/


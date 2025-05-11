using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using NetXpertCodeLibrary.Extensions;

namespace NetXpertCodeLibrary.ContactData
{
	/// <summary>Provides a mechanism for validating and managing email addresses (strings).</summary>
	/// <remarks>This class is ASCII Roman-character-dependent. Cyrillic, Kanji or an other non-Roman charactersets will not be parsed or validated with it.</remarks>
	public sealed class EmailAddress : BasicTypedDataFoundation<EmailAddress.EmailType>
	{
		#region Properties
		private string _address = "";
		public enum EmailType : byte { Unknown = 0, Personal = 1, Professional = 2, Public = 4, All = 255 }
		#endregion

		#region Constructors
		public EmailAddress( string addr, EmailType type = EmailType.Unknown ) : base( type ) => Email = addr;

		public EmailAddress( XmlNode source ) : base( source ) =>
			this.Email = source.InnerText.XmlDecode();
		#endregion

		#region Operators
		public static implicit operator string( EmailAddress source ) => 
			source is null ? "" : source.ToString();

		public static implicit operator EmailAddress( string source ) =>
			string.IsNullOrEmpty( source ) ? null : new EmailAddress( source );
		#endregion

		#region Accessors
		public string Email
		{
			get => this._address;
			set
			{
				if ( ValidateAddr( value ) )
					this._address = value;
				else
					throw new FormatException( $"The provided value (\"{value}\") isn't in a recognized email format." );
			}
		}

		public string Name =>
			string.IsNullOrEmpty( this._address ) ? "" : this._address.Split( new char[] { '@' }, 2 )[ 0 ];

		public string Domain =>
			string.IsNullOrEmpty( this._address ) ? "" : this._address.Split( new char[] { '@' }, 2 )[ 1 ];
		#endregion

		#region Methods
		public override string ToString() => Email;

		public override XmlNode ToXmlNode() => base.ToXmlNode( Email );

		public bool Equals( EmailAddress value ) =>
			this._address.Equals( value._address, StringComparison.CurrentCultureIgnoreCase );

		public override bool IsEmpty() => this._address.Length == 0;

		/// <summary>Validates the FORM of an email address in a supplied string.</summary>
		/// <returns>TRUE if the passed value is a valid FORM of email address.</returns>
		/// <remarks>This function cannot determine if the supplied address is an actual email address, only that it conforms to the defined standard for an valid email address.</remarks>
		public static bool ValidateAddr( string value ) =>
			value is null || Regex.IsMatch( value, @"^[\s]+$" ) ? false : (value == "") || new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid( value );

		// If the 'value' is genuinely null, OR is all whitespace, return false; but if it's just EMPTY, or valid, return true.
		// NOTE: We can't use the 'string.IsNullOrWhiteSpace()' function because empty is a valid state, while whitespace and null are not.
		//	string.IsNullOrWhiteSpace( value ) ? false : (value == "") || new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid( value );
		#endregion

		#region Comparer
		public class EmailComparer : IComparer<EmailAddress>
		{
			public int Compare( EmailAddress a, EmailAddress b )
			{
				if ( a is null ) return (b is null) ? 0 : 1;
				if ( b is null ) return -1;

				return a.Email.CompareTo( b.Email );
			}
		}
		#endregion
	}

	public sealed class EmailAddresses : BasicTypedCollection<EmailAddress, EmailAddress.EmailType>
	{
		#region Properties
		#endregion

		#region Constructors
		public EmailAddresses( bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			this.Sorted = sorted;

		public EmailAddresses( EmailAddress email, bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			Add( email, sorted );

		public EmailAddresses( IEnumerable<EmailAddress> addresses, bool sorted = false, int limit = int.MaxValue ) : base( sorted, limit ) =>
			AddRange( addresses, sorted );

		public EmailAddresses( XmlNode node ) : base( node )
		{
			if ( !(node is null) && node.HasChildNodes )
			{
				foreach ( XmlNode child in node.ChildNodes )
					this.Add( new EmailAddress( child ) );
			}
		}
		#endregion

		#region Operators
		public static implicit operator EmailAddresses( EmailAddress[] data ) =>
			data is null ? new EmailAddresses() : new EmailAddresses( data );

		public static implicit operator EmailAddress[]( EmailAddresses data ) =>
			(data is null) ? Array.Empty<EmailAddress>() : data.ToArray();
		#endregion

		#region Accessors
		#endregion

		#region Methods
		protected override int Comparer( EmailAddress a, EmailAddress b ) =>
			string.Compare( a.Email, b.Email, true );

		public override XmlNode ToXmlNode() => base.CreateXmlNode();
		#endregion
	}
}

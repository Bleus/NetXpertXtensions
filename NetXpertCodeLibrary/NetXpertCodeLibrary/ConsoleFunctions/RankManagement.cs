using System;
using System.Collections.Generic;

namespace NetXpertCodeLibrary.ConsoleFunctions
{
	// User "rank" definitions:
	public enum Ranks : short {
		Unknown			=   -1,	// Can only result from attempting (and failing) to parse an unrecognized value into a rank.
		None			=    0,	// Unauthenticated User / only basic functions (like log-on)
		Unverified		=   10,	// Registered, unverified user.
		BasicUser		=  100,	// Authenticated Basic User (ie Employees with no administrative rights or Tenants)
		CompanyUser		=  200, // Elevated Priviledges Users (ie Supervisors or Building Superintendants / Managers)
		CompanyAdmin	=  400,	// Company Level Administrators
		OrgUser			=  500,	// Group Level Users
		OrgAdmin		=  700, // Group Level Administrators
		SystemAdmin		=  800,	// Cobblestone Employees
		GlobalAdmin		=  900,	// Cobblestone Administrators
		SuperUser		= 1000  // Super User / Global Administrator (not an assignable rank, but is automatically granted to the
								// first record in the User Database.
	};

	public class RankManagement
	{
		protected short _baseRank = 0;

		#region Constructors
		public RankManagement() { }

		public RankManagement(short rank) =>
			this.Rank = rank;

		public RankManagement(Ranks rank) =>
			this._baseRank = (short)rank;
		#endregion

		#region Accessors
		public short Rank
		{
			get => this._baseRank;
			set
			{
				if (value >= 0)
					this._baseRank = value;
			}
		}

		public string Name => RankManagement.Convert( this._baseRank ).ToString();

		public Ranks ToRank => RankManagement.Convert(this._baseRank);
		#endregion

		#region Operators
		public static bool operator !=(RankManagement left, int right) => !(left == right);
		public static bool operator ==(RankManagement left, int right) =>(left is null) ? false : (left.Rank == right);
		public static bool operator >(RankManagement left, int right) =>(left is null) ? false : (left.Rank > right);
		public static bool operator <(RankManagement left, int right) =>(left is null) ? false: (left.Rank < right);
		public static bool operator >=(RankManagement left, int right) => (left is null) ? false : (left.Rank >= right);
		public static bool operator <=(RankManagement left, int right) => (left is null) ? false : (left.Rank <= right);
		public static bool operator !=(RankManagement left, Ranks right) => !(left == (int)right);
		public static bool operator ==(RankManagement left, Ranks right) => (left is null) ? false : (left.Rank == (int)right);
		public static bool operator >(RankManagement left, Ranks right) => (left is null) ? false : (left.Rank > (int)right);
		public static bool operator <(RankManagement left, Ranks right) => (left is null) ? false : (left.Rank < (int)right);
		public static bool operator >=(RankManagement left, Ranks right) => (left is null) ? false : (left.Rank >= (int)right);
		public static bool operator <=(RankManagement left, Ranks right) => (left is null) ? false : (left.Rank >= (int)right);
		public static bool operator ==(RankManagement left, short right) => (left == (int)right);
		public static bool operator !=(RankManagement left, short right) => !(left == (int)right);
		public static bool operator >(RankManagement left, short right) => (left > (int)right);
		public static bool operator <(RankManagement left, short right) => (left < (int)right);
		public static bool operator >=(RankManagement left, short right) => (left > (int)right) || (left == (int)right);
		public static bool operator <=(RankManagement left, short right) => (left < (int)right) || (left == (int)right);

		public static bool operator >=(RankManagement left, RankManagement right) => (left > right) || (left == right);
		public static bool operator <=(RankManagement left, RankManagement right) => (left < right) || (left == right);
		public static bool operator !=(RankManagement left, RankManagement right) => !(left == right);
		public static bool operator ==(RankManagement left, RankManagement right)
		{
			if (left is null) return (right is null);
			if (right is null) return false;
			return (left.Rank == right.Rank);
		}

		public static bool operator >(RankManagement left, RankManagement right)
		{
			if ((left is null) || (right is null)) return false;
			return (left.Rank > right.Rank);
		}

		public static bool operator <(RankManagement left, RankManagement right)
		{
			if ((left is null) || (right is null)) return false;
			return (left.Rank < right.Rank);
		}

		public static implicit operator short(RankManagement data) => data._baseRank;
		public static implicit operator RankManagement(short data) => new RankManagement(data);
		public static implicit operator int(RankManagement data) => data._baseRank;
		public static implicit operator RankManagement(int data) => new RankManagement((short)data);
		public static implicit operator Ranks(RankManagement data) => data.ToRank;
		public static implicit operator RankManagement(Ranks data) => new RankManagement(data);
		#endregion

		#region Methods
		public bool IsAllowed(Ranks rank) => IsAllowed((short)rank);

		public bool IsAllowed(RankManagement rank) => IsAllowed(rank.Rank);

		public bool IsAllowed(short rank) => IsAllowed((int)rank);

		/// <summary>Compares a provided Rank requirement against the stored value and returns TRUE is the operation is permitted.</summary>
		/// <param name="rankRequired">A shortInt value indicating the minumum rank required to perform the requested operation.</param>
		/// <returns>TRUE if ths stored value is greater than or equal to the required rank specified, otherwise FALSE.</returns>
		public bool IsAllowed(int rankRequired) => (this._baseRank >= rankRequired);

		/// <summary>Returns the appropriate Rank enumerable value for any provided Short value.</summary>
		/// <param name="rank">A shortint value to be converted to a Rank enumerable value.</param>
		/// <returns>The Rank enumerable value that best corresponds to the provided shortint value.</returns>
		public static Ranks Convert(short rank)
		{
			// The default enumeration sorts the "Unknown(-1)" rank to the TOP of the stack (probably b/c -1 = 0xffff),
			// so I have to sort the list by a custom comparer to put it back at the bottom:
			List<Ranks> ranks = new List<Ranks>((Ranks[])Enum.GetValues(typeof(Ranks)));
			ranks.Sort( new RankComparer() );

			int i = ranks.Count;
			do {
				if (((short)ranks[--i] >= 0) && (rank >= (short)ranks[i]))
					return ranks[i];
			} while (i > 0);
			return Ranks.Unknown;
		}

		/// <summary>Converts a string representation of a rank to a Rank enumerable value.</summary>
		/// <param name="rank">A string to attempt to be parsed into a Rank enumerable value.</param>
		/// <returns>If the string can be parsed, the appropriate Rank enumerable value, otherwise Rank.Unknown</returns>
		/// <remarks>The `name` string can contain either a short integer value, or an actual Ranks name.</remarks>
		public static Ranks Convert( string rank )
		{
			if ( System.Text.RegularExpressions.Regex.IsMatch( rank, @"^-?([0-2]?[\d]{4}|3[01][\d]{3}|32[0-6][\d]{2}|327[0-5][\d]|3276[0-7])$" ) )
				return new RankManagement( Convert( short.Parse( rank ) ) );
			
			return (IsRankString( rank )) ? (Ranks)Enum.Parse( typeof( Ranks ), rank.Trim() ) : Ranks.Unknown;
		}

		public static Ranks Convert(RankManagement rank) => Convert(rank.Rank);

		public class RankComparer : IComparer<Ranks>
		{
			public int Compare( Ranks a, Ranks b ) =>
				((short) a).CompareTo( (short) b );

			//{
			//	short a1 = (short)a, b1 = (short)b;
			//	if ( a1 < b1 ) return -1;
			//	if ( a1 > b1 ) return 1;
			//	return 0;
			//}
		}

		public override bool Equals(object obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public override string ToString() => $"{this.ToRank} ({this._baseRank})";

		public static bool IsRankString(string source)
		{
			string match = "|";
			foreach (string name in Enum.GetNames(typeof(Ranks)))
				match += name + "|";

			return System.Text.RegularExpressions.Regex.IsMatch(source.Trim(), match.Trim(new char[] { '|' }), System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		}
		#endregion
	}
}

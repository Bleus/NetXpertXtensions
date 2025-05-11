using System;
using System.Collections;
using System.Collections.Generic;

namespace NetXpertCodeLibrary
{
	/********************************************************************************
	 * This module is largely obviated by the BinarySearch capabilities of List<>...
	 ********************************************************************************/

	/// <summary>Facilitates performing a Binary Search on any List or Array.</summary>
	/// <remarks>
	/// REMEMBER: This ONLY works on sorted collections! If you want to use this, you should leverage the FindInsertIndex
	/// and InsertItem functions when adding new items to the collection to obviate having to Sort it before every search!
	/// </remarks>
	public static class BinarySearch
	{
		///<summary>Comparers for a given type (T) must compare the object with the specified value and return -1, 0 or 1...</summary>
		///<returns>-1 if the "value" is less than "item", 0 if they're equal, or 1 if "value" is greater than "item".</returns>
		public delegate int Comparer<T>( T item, object value );

		/// <summary>Binary searches a supplied collection using the provided Comparer to find the specified item.</summary>
		/// <typeparam name="T">The Type of the data contained by/within the collection.</typeparam>
		/// <param name="item">The value that we're searching for.</param>
		/// <param name="comparer">A delegate function that will provide the guidance needed to </param>
		/// <param name="collection">The List or Array to search.</param>
		/// <returns>If the item is found, it's index in the collection, otherwise -1.</returns>
		public static int FindItemIndex<T>( dynamic item, Comparer<T> comparer, IList<T> collection )
		{
			//if ( !typeof( T ).IsClass || !typeof( T ).IsValueType || (Nullable.GetUnderlyingType( typeof( T ) ) == null) )
			//	throw new ArgumentException( "T must be a nullable type." );

			if ( (item is null) || (comparer is null) || (collection is null) )
				throw new ArgumentNullException( "You cannot pass NULL values to this function." );

			int i = -1;
			if ( collection.Count < 20 ) // For small collections, this is probably faster.
			{
				while ( (++i < collection.Count) && (comparer( collection[ i ], item ) != 0) ) ;
				return (i < collection.Count) ? i : -1;
			}

			int top = collection.Count, bottom = 0, iterationCap = 50, mid;
			while (--iterationCap > 0) // using iterationCap to put a limit on the depth of the search (no infinite loop!)
			{
				mid = (int)Math.Round((top + bottom) / 2M);
				switch (comparer( collection[ mid ], item ))
				{
					case 0: return mid;				// Found it!
					case -1: top = mid; break;		// it's beneath the midline
					case 1: bottom = mid; break;	// it's above the midline
				}

				if ( comparer( collection[ bottom ], item ) == 0 ) return bottom;
				if ( comparer( collection[ top ], item ) == 0 ) return top;

				if ( top - bottom < 3 ) break;      // If top and bottom are within 2 of each other, and 
			}                                       // we still haven't found the result, it isn't here.
			return -1;
		}

		/// <summary>Searches an Array or List for the proper place to insert the specified record.</summary>
		/// <typeparam name="T">The Type of the data contained by/within the collection.</typeparam>
		/// <param name="item">The value that we're searching for a place to insert.</param>
		/// <param name="comparer">A delegate function that will provide the guidance needed to search properly.</param>
		/// <param name="collection">The List or Array to search.</param>
		/// <returns>The index location where the Item should be inserted into the collection.</returns>
		/// <exception cref="ArgumentNullException">If any of the passed parameters are NULL.</exception>
		/// <remarks>If the search fails for some reason, this will return -1 as the index.</remarks>
		public static int FindInsertIndex<T>( dynamic item, Comparer<T> comparer, IList<T> collection)
		{
			if ( (item is null) || (comparer is null) || (collection is null) )
				throw new ArgumentNullException( "You cannot pass NULL values to this function." );

			if ( (collection.Count == 0) || (comparer( collection[0], item ) < 1) ) return 0; // is the new item the first?
			if ( comparer( collection[ collection.Count - 1 ], item ) > 0 ) return collection.Count; // or the last?

			int i = -1;
			if ( collection.Count < 20 ) // For small collections, this is probably faster.
			{
				while ( (++i < collection.Count) && (comparer( collection[ i ], item ) < 0) ) ;
				return i;
			}

			int top = collection.Count, bottom = 0, iterationCap = 50, mid;
			while ( --iterationCap > 0 ) // using iterationCap to put a limit on the depth of the search (no infinite loop!)
			{
				mid = (int)Math.Round( (top + bottom) / 2M );
				int oneUnder = comparer( collection[ mid - 1 ], item ),
					equal = comparer( collection[ mid ], item ),
					oneOver = comparer( collection[ mid ], item );

				switch (equal)
				{
					case 0: return mid; // exact match!
					case -1:            // it's beneath the midline
						if ( oneUnder > -1 ) return mid - 1;
						top = mid;
						break;
					case 1:             // it's above the midline
						if ( oneOver < 0 ) return mid + 1;
						bottom = mid;
						break;
				}
			}                                       
			return -1;
		}

		/// <summary>Uses the FindItemIndex function to obtain the location of an Item then retrieves that item and returns it.</summary>
		/// <typeparam name="T">The Type of the data contained by/within the collection.</typeparam>
		/// <param name="item">The value that we're searching for.</param>
		/// <param name="comparer">A delegate function that will provide the guidance needed to search properly.</param>
		/// <param name="collection">The List or Array to search.</param>
		/// <returns>The requested item from the collection, if it exists, otherwise an Exception is thrown.</returns>
		/// <remarks>I have to throw the Exception because T might not be a nullable type.</remarks>
		/// <exception cref="KeyNotFoundException">If the requested item was not found in the collection.</exception>
		/// <seealso cref="FindItemIndex{T}(dynamic, Comparer{T}, IList{T})"/>
		public static T GetItem<T>( dynamic item, Comparer<T> comparer, IList<T> collection )
		{
			int i = FindItemIndex<T>( item, comparer, collection );
			if ( i < 0 ) return collection[ i ];
			throw new KeyNotFoundException( "The requested item was not found." );
		}

		/// <summary>Inserts an item into a sorted List using a binary search to find the insertion point.</summary>
		/// <typeparam name="T">The Type that is managed by the List.</typeparam>
		/// <param name="item">The item to be inserted.</param>
		/// <param name="comparer">An InsertComparer delegate to guide the search.</param>
		/// <param name="collection">A List or Array to do the work on.</param>
		/// <returns>A List of T items populated with the contents of the passed "collection" with the new "item" properly inserted.</returns>
		/// <exception cref="ArgumentOutOfRangeException">If the InsertIndex search failed to find an insertion point.</exception>
		public static List<T> InsertItem<T>( T item, Comparer<T> comparer, IList<T> collection )
		{
			List<T> result = new List<T>( collection );
			int i = FindInsertIndex<T>( item, comparer, collection ); ;
			if ( i >= 0 )
				result.Insert( i, item );
			else
				throw new ArgumentOutOfRangeException( "The resolved index was out of bounds (\"" + i.ToString() + "\")" );

			return result;
		}

		/// <summary>Sorts a collection using the BinarySearch algorithm implemented here.</summary>
		/// <typeparam name="T">The Type contained/managed by the collection.</typeparam>
		/// <param name="comparer">An InsertComparer delegate to apply to the collection when inserting items.</param>
		/// <param name="collection">A List or Array of items to sort.</param>
		/// <returns>A NEW list containing all of the items, copied from the "collection", sorted by the criteria supplied in the Delegate.</returns>
		public static List<T> Sort<T>( Comparer<T> comparer, IList<T> collection)
		{
			List<T> result = new List<T>();
			foreach ( T item in collection )
				InsertItem<T>( item, comparer, collection );

			return result;
		}

		/// <summary>A very simple basic string comparison delegate that can be used to search for a string value.</summary>
		/// <typeparam name="T">The Type of the data contained by/within the collection.</typeparam>
		/// <param name="item">An object of type T to compare.</param>
		/// <param name="value">The value to compare against.</param>
		/// <returns>-1 if value is less than item, 0 if they're equal, and 1 if value is greater than item.</returns>
		public static int DefaultComparer<T>( T item, object value ) =>
			item.ToString().CompareTo( value.ToString() );
	}
}

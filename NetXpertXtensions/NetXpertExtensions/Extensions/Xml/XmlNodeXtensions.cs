using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace NetXpertExtensions.Xml
{
	#nullable disable

	public static partial class NetXpertXmlExtensions
	{
		#region XmlNode extensions
		/// <summary>Checks for the existence of a specified attribute in the <i>XmlNode</i> object.</summary>
		/// <param name="attrName">The attribute name you want to validate.</param>
		/// <param name="caseSensitive">If set to <b>TRUE</b>, the search will be done with case sensitivity, otherwise it wont't.</param>
		/// <returns><b>TRUE</b> if the attribute exists in the object, otherwise <b>FALSE</b>.</returns>
		public static bool HasAttribute( this XmlNode source, string attrName, bool caseSensitive = false )
		{
			if ( source.Attributes.Count > 0 )
				foreach ( XmlAttribute a in source.Attributes )
					if ( a.Name.Equals( attrName.Trim(), (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) ) )
						return true;

			return false;
		}

		/// <summary>Fetches an XML attribute from the parent node by a (case-insensitive) name.</summary>
		/// <param name="name">A string containing the name of the desired Attribute.</param>
		/// <param name="caseSensitive">If set to <b>TRUE</b>, the search will be done with case sensitivity, otherwise it wont't.</param>
		/// <returns><b>NULL</b> if the attribute doesn't exist, otherwise an <i>XmlAttribute</i> object corresponding to the requested Attribute.</returns>
		public static XmlAttribute GetAttribute( this XmlNode source, string name, bool caseSensitive = false )
		{
			if ( source.Attributes.Count > 0 )
				foreach ( XmlAttribute a in source.Attributes )
					if ( a.Name.Equals( name.Trim(), (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) ) )
						return a;
			return null;
		}

		/// <summary>If an attribute exists, attached to this <i>XmlNode</i>, with the specified case-insensitive name, this routine will return its value.</summary>
		/// <param name="attrName">A string specifying the case-insensitive name of the desired attribute.</param>
		/// <param name="caseSensitive">If set to <b>TRUE</b>, the search will be done with case sensitivity, otherwise it wont't.</param>
		/// <returns>If an attribute exists with the specified name, the value of that attribute, otherwise an empty string.</returns>
		public static string GetAttributeValue( this XmlNode source, string attrName, bool caseSensitive = false )
		{
			XmlAttribute xmla = source.GetAttribute( attrName, caseSensitive );
			return (xmla is null) ? "" : xmla.Value;
		}

		/// <summary>Returns all xml child elements (nodes) from an <i>XmlNode</i> with the specified tag-name and specifying a named attribute.</summary>
		/// <param name="elementName">The element (tag) name to search for.</param>
		/// <param name="attributeName">The field attribute name to match.</param>
		/// <param name="attrValue">The value to match of the specified attribute.</param>
		/// <param name="caseSensitive">If set to <b>TRUE</b>, the search will be done with case sensitivity, otherwise it wont't.</param>
		/// <returns>An array of all child <i>XmlNode</i>s from the parent <i>XmlNode</i> that matching the search critera.</returns>
		/// <remarks>An array is <i>always</i> returned! -- If no matching elements were found, it will merely be empty.</remarks>
		public static XmlNode[] GetNamedElements( this XmlNode parent, string elementName, string attrName = "", string attrValue = "", bool caseSensitive = false, bool recursive = false )
		{
			XmlNodeList nodes = parent.ChildNodes;
			List<XmlNode> results = new();
			StringComparison sc = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

			if ( nodes.Count > 0 )
				foreach ( XmlNode node in nodes )
				{
					if ( node.Name.Equals( elementName, sc ) && (string.IsNullOrWhiteSpace( attrName ) || ((node.HasAttribute( attrName, caseSensitive ) && (string.IsNullOrEmpty( attrValue.Trim() ) || attrValue.Equals( node.GetAttributeValue( attrName ), sc ))))) )
						results.Add( node );

					if ( recursive ) results.AddRange( node.GetNamedElements( elementName, attrName, attrValue, caseSensitive, recursive ) );
				}

			return results.ToArray();
		}

		/// <summary>Fetches the first named XML element from the parent <i>XmlNode</i> that has a matching attribute with a specified value.</summary>
		/// <param name="elementName">The element (tag) name to search for.</param>
		/// <param name="attrName">The attribute to look for.</param>
		/// <param name="attrValue">The attribute value to look for.</param>
		/// <param name="caseSensitive">If set to <b>true</b>, the search will be done with case sensitivity, otherwise it wont't.</param>
		/// <returns>If found, the <i>XmlNode</i> object corresponding to the first named element with a matching attribute and value pair. Otherwise <i>null</i>.</returns>
		public static XmlNode GetFirstNamedElement( this XmlNode parent, string elementName, string attrName = "", string attrValue = "", bool caseSensitive = false )
		{
			XmlNode[] nodes = parent.GetNamedElements( elementName, attrName, attrValue, caseSensitive );
			return (nodes.Length > 0) ? nodes[ 0 ] : null;
		}

		/// <summary>Takes an XML node in string form and attempts to build an <i>XmlNode</i> object from it.</summary>
		/// <param name="XmlText">A string of Xml text to incorporate into the <i>XmlNode</i> object.</param>
		/// <returns>An <i>XmlNode</i> object populated with the provided Xml content.</returns>
		public static XmlNode Load( this XmlNode parent, string XmlText )
		{
			XmlDocument doc = new();
			if ( XmlText.IndexOf( XML.HEADER ) < 0 ) XmlText = XML.HEADER + XmlText;
			doc.LoadXml( XmlText );
			parent = (doc.ChildNodes.Count > 0) ? doc.ChildNodes[ 0 ] : null;
			return parent;
		}

		/// <summary>Reports whether the value of a specified <i>XmlNode</i> Attribute matches (case-insensitive) a specified value.</summary>
		/// <param name="attrName">The name of the attribute to test.</param>
		/// <param name="value">A string containing the value to be tested (Case-Insensitive!)</param>
		/// <param name="caseSensitive">Specifies whether the check is meant to be case-sensitive.</param>
		/// <returns><b>TRUE</b> if the attribute exists and the values match, <i>or</i> if the attribute doesn't exist and the specified test value is blank, otherwise <b>FALSE</b>.</returns>
		public static bool AttributeValueEquals( this XmlNode source, string attrName, string value, bool caseSensitive = false )
		{
			XmlAttribute xmla = source.GetAttribute( attrName, caseSensitive );
			return xmla is not null && xmla.IsEqualTo( value, caseSensitive );
		}

		/// <summary>Adds the ability for an <i>XmlNode</i> object to import <i>XmlNode</i> objects (from different Xml contexts).</summary>
		/// <remarks>
		/// <b>NOTE:</b> This function will <i><b>not</b></i> make any changes to the calling object!<br/><br/>
		/// To obtain the node that is created by this function, you <i><b>must</b> assign the result to <u>something</u></i>. For example:<br/>
		/// <br/><b>XmlNode sourcePlus = source.ImportNode( newChild );</b><br/><br/>
		/// Wherein <i>sourcePlus</i> will contain the contents of <i>source</i> with the <i>newChild</i> node added, while <i>source</i> itself will remain unmodified.
		/// </remarks>
		/// <returns>A NEW instance of an <i>XmlNode</i> object that is identical to the source XmlNode, with the specified new child node added.</returns>
		/// This section is necessary because the internal, <i>AppendChild</i> function will cough up XML Context exceptions if
		/// you try to use it to append other <i>XmlNode</i>s from separate sources.
		public static XmlNode ImportNode( this XmlNode source, XmlNode child )
		{
			XmlNode result = source;
			if ( !(child is null) )
			{
				string text = Regex.Replace( source.OuterXml.Trim(), $"</{source.Name}>$", $"{child.OuterXml}</{source.Name}>", RegexOptions.IgnoreCase );

				XmlDocument doc = new();
				doc.LoadXml( XML.HEADER + text );
				result = doc.GetFirstNamedElement( source.Name );
			}
			return result;
		}

		/// <summary>Adds the ability to import an array of <i>XmlNode</i>s from different Xml contexts into the source node.</summary>
		/// <remarks>
		/// <b>NOTE:</b> This function will <i><b>not</b></i> make any changes to the calling object!<br/><br/>
		/// To obtain the node that is created by this function, you <i><b>must</b> <u>assign</u> the result to <u>something</u></i>. For example:<br/>
		/// <br/><b>XmlNode sourcePlus = source.ImportNodes( newChildren );</b><br/><br/>
		/// Wherein <i>sourcePlus</i> will contain the contents of <i>source</i> with the <i>newChildren</i> node added, while <i>source</i> itself will remain unmodified.
		/// </remarks>
		/// <returns>A <b>new</b> <i>XmlNode</i> object that is identical to the source <i>XmlNode</i>, with the provided child nodes added.</returns>
		public static XmlNode ImportNodes( this XmlNode source, params XmlNode[] children )
		{
			XmlNode result = source;
			if ( children.Length > 0 )
				foreach ( XmlNode node in children )
					if ( !(node is null) ) result = result.ImportNode( node );

			return result;
		}
		#endregion
	}
}
using System.Xml;

namespace NetXpertExtensions.Xml
{
	#nullable disable
	public static partial class NetXpertXmlExtensions
	{
		/// <summary>Provides an ability to quickly determine if a specified XmlAttribute's value matches (case-insensitive) a provided string.</summary>
		/// <param name="value">A string containing the value to compare with the Attribute.</param>
		/// <param name="caseSensitive">Specifies whether the check is meant to be case-sensitive.</param>
		/// <returns>TRUE if the value matches the specified value regardless of case.</returns>
		public static bool IsEqualTo(this XmlAttribute source, string value, bool caseSensitive = false) =>
			source.Value.Equals(value, (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

		/// <summary>Creates an XmlAttribute object from a KeyValuePair&lt;string,object&gt; object.</summary>
		public static XmlAttribute ToXmlAttribute( this KeyValuePair<string, object> source ) =>
			 $"<node {source.Key}='{source.Value.ToString().XmlEncode()}'></node>".ToXmlNode().Attributes[source.Key];

		/// <summary>Converts an array of KeyValuePair&lt;string,object&gt; objects to an array of XmlAttribute objects.</summary>
		public static XmlAttribute[] ToXmlAttributeArray( this KeyValuePair<string, object>[] source )
		{
			List<XmlAttribute> attrs = new List<XmlAttribute>();
			if ( source is not null )
				foreach ( var attr in source )
					attrs.Add( attr.ToXmlAttribute() );

			return attrs.ToArray();
		}

		/// <summary>Creates an XmlAttributeCollection from an enumerable collection of KeyValuePair&lt;string,object&gt; objects.</summary>
		public static XmlAttributeCollection ToXmlAttributeCollection( this IEnumerable<KeyValuePair<string,object>> source )
		{
			if ( source is null ) return null;
			string xml = "<node";
			foreach ( var kvp in source )
				xml += $" {kvp.Key}='{kvp.Value.ToString().XmlEncode()}'";

			XmlNode node = $"{xml}></node>".ToXmlNode();
			return node.Attributes;
		}
	}
}
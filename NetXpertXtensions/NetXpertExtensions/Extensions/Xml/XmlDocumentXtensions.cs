using System.Xml;
using System.Xml.Linq;
using NetXpertExtensions.Classes;

namespace NetXpertExtensions.Xml
{
	#nullable disable
	[Serializable]
	public static partial class NetXpertXmlExtensions
	{
		#region XmlDocument extensions
		/// <summary>Returns all xml elements (nodes) from the <seealso cref="XmlDocument"/> with the specified tag-name and specifying a named attribute.</summary>
		/// <param name="elementName">The element (tag) name to search for.</param>
		/// <param name="attributeName">The attribute name to match.</param>
		/// <returns>An array of all <seealso cref="XmlNode"/>'s in the current document matching the search critera.</returns>
		public static XmlNode[] GetNamedElements(this XmlDocument doc, string elementName, string attributeName)
		{
			List<XmlNode> result = new List<XmlNode>();
			XmlNodeList nodes = doc.GetElementsByTagName(elementName);
			if (nodes.Count > 0)
				foreach (XmlNode node in nodes)
					if (node.Attributes[attributeName] != null) result.Add(node);

			return result.ToArray();
		}

		/// <summary>Fetches the first named XML element from the parent <seealso cref="XmlDocument"/> that has a matching attribute with a specified value.</summary>
		/// <param name="elementName">The element (tag) name to search for.</param>
		/// <param name="attributeName">The attribute to look for.</param>
		/// <param name="attributeValue">The attribute value to look for.</param>
		/// <returns>If found, the <seealso cref="XmlNode"/> object corresponding to the first named element with a matching attribute and value pair. Otherwise null.</returns>
		public static XmlNode GetFirstNamedElement(this XmlDocument doc, string elementName, string attributeName = "", string attributeValue = "")
		{
			XmlNode result = null;
			XmlNodeList nodes = doc.GetElementsByTagName(elementName);
			if (string.IsNullOrWhiteSpace(attributeName)) return (nodes.Count > 0) ? nodes[0] : null;

			int i = -1;
			if (nodes.Count > 0)
				while ( (++i < nodes.Count) && (result == null) )
					if ( nodes[ i ].HasAttribute( attributeName ) )
						result = (string.IsNullOrWhiteSpace( attributeValue ) || nodes[ i ].GetAttributeValue( attributeName ).Equals( attributeValue, StringComparison.OrdinalIgnoreCase )) ? nodes[ i ] : null;

			return result;
		}

		#region XmlDocument Extensions
		/// <summary>Extends the <seealso cref="XmlDocument"/> class to support loading remote content directly into the object.</summary>
		/// <param name="path">A <seealso cref="Uri"/> object specifying where the desired Xml content is to be found.</param>
		public static void XmlLoad( this XmlDocument doc, Uri path )
		{
			string source = Http.Get( path, false );
			if ( source.IsXml() ) doc.LoadXml( source );
		}

		/// <summary>Extends the <seealso cref="XmlDocument"/> class to support loading remote content directly into the object.</summary>
		/// <param name="path">A <seealso cref="Uri"/> object specifying where the desired Xml content is to be found.</param>
		public static void XmlLoad( this XmlDocument source, string path )
		{
			Uri uri = new( path );
			source.XmlLoad( uri );
		}
		#endregion


		/// <summary>Extends the <seealso cref="XmlDocument"/> class to add in the ability to produce formatted output via the <seealso cref="XDocument"/> class...</summary>
		/// <returns>A cleanly formatted string containing the contents of the current <seealso cref="XmlDocument"/> class.</returns>
		public static string ToFormattedString(this XmlDocument source) => 
			XDocument.Load(new XmlNodeReader(source)).ToString();

		/// <summary>Extends the XmlDocument class to support exporting it's XmlEntities</summary>
		/// <returns>An array of XmlEntities containing all of the namespaces and entities defined in this document.</returns>
		//public static XmlEntityCollection ExportEntities(this XmlDocument source) =>
		//	XmlEntityCollection.ExtractEntities(source.OuterXml);

		/// <summary>Defines a DocType + Entity declaration and assigns (overwrites) it to this XmlDocument.</summary>
		/// <param name="entities">The XmlEntityCollection to assign.</param>
		//public static void SetEntities(this XmlDocument source, XmlEntityCollection entities, string rootTagName = "")
		//{
		//	if (source.HasChildNodes && ((rootTagName is null) || (rootTagName.Trim().Length == 0)))
		//		rootTagName = source.FirstChild.LocalName;

		//	source.LoadXml(entities.ToString(rootTagName));
		//}

		/// <summary>Adds a collection of Entity declarations to this XmlDocument.</summary>
		/// <param name="entities">An XmlEntityCollection to import into this document.</param>
		//public static void ImportEntities(this XmlDocument source,  XmlEntityCollection entities) =>
		//	source.SetEntities( new XmlEntityCollection(source) + entities );

		/// <summary>Imports the first node with a specified tagName from another <seealso cref="XmlDocument"/>.</summary>
		/// <param name="tagName">A string specifying the tagName of the node to extract/import.</param>
		public static void ImportNode(this XmlDocument source, XmlDocument original, string tagName)
		{
			XmlNode node = original.GetFirstNamedElement(tagName);
			source.AppendChild(node);
		}

		/// <summary>Imports the first node with a specified tagName and id attribute from another <seealso cref="XmlDocument"/>.</summary>
		/// <param name="original">The <seealso cref="XmlDocument"/> containing the original node.</param>
		/// <param name="tagName">A string specifying the tagName of the node to extract/import.</param>
		/// <param name="id">A string specifying the Id attribute value to match.</param>
		public static void ImportNode(this XmlDocument source, XmlDocument original, string tagName, string id)
		{
			XmlNodeList nodes = original.GetElementsByTagName(tagName);
			foreach (XmlNode node in nodes)
				if (node.Attributes["id"].Value.Equals(id, StringComparison.InvariantCultureIgnoreCase))
					source.ImportNode(node, true);
		}

		/// <summary>Creates a new <seealso cref="XmlDocument"/> from this one, containing only the first instance of a single named node.</summary>
		/// <param name="tagName">A string specifying the tagName of the node to export.</param>
		/// <returns>A new <seealso cref="XmlDocument"/> containing the specified node.</returns>
		public static XmlDocument ExportNode(this XmlDocument source, string tagName)
		{
			XmlDocument doc = new XmlDocument(source.NameTable);
			doc.ImportNode(source, tagName);
			return doc;
		}

		/// <summary>Creates a new <seealso cref="XmlDocument"/> from this one, containing only the first instance of a single named node.</summary>
		/// <param name="tagName">A string specifying the tagName of the node to export.</param>
		/// <param name="id">A string specifying the Id attribute value to match.</param>
		/// <returns>A new <seealso cref="XmlDocument"/> containing the specified node.</returns>
		public static XmlDocument ExportNode(this XmlDocument source, string tagName, string id)
		{
			XmlDocument doc = new XmlDocument(source.NameTable);
			doc.ImportNode(source, tagName, id);
			return doc;
		}

		/// <summary>Simple boolean check to see if the loaded document contains a specified Xml node/element.</summary>
		/// <param name="elementName">A string specifying the (case-insensitive) element name to look for.</param>
		/// <param name="attributeName">The field attribute name to match.</param>
		/// <param name="attrValue">The value to match of the specified attribute.</param>
		/// <param name="caseSensitive">If set to TRUE, the search will be done with case sensitivity, otherwise it wont't.</param>
		/// <returns>TRUE if an element with the specified name exists in the current <seealso cref="XmlDocument"/>, otherwise FALSE.</returns>
		public static bool HasElement(this XmlDocument source, string elementName, string attributeName = "", string attributeValue = "", bool caseSensitive = false, bool recursive = true) =>
			(source is null) || (!source.HasChildNodes) ? false : (source.GetNamedElements(elementName, attributeName, attributeValue, false, recursive).Length > 0);
		#endregion
	}
}
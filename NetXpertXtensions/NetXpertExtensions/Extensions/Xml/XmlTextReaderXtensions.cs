using System.Xml;

namespace NetXpertExtensions.Xml
{
	#nullable disable

	public static partial class NetXpertXmlExtensions
	{
		#region XmlTextReader extensions
		/// <summary>Implements a means of converting an XmlTextReader object directly into an XmlDocument object.</summary>
		/// <returns>An XmlDocument class populated with the contents of the parent XmlTextReader object.</returns>
		public static XmlDocument ToXmlDocument(this XmlTextReader reader)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(reader);
			return doc;
		}

		/// <summary>Extends the XmlTextReader class to support fetching XML nodes via the tag (element) name.</summary>
		/// <param name="elementName">An array of strings specifying the element (tag) names to extract.</param>
		/// <returns>An XmlNodeList containing all of the objects whose tag names match the request.</returns>
		public static XmlNodeList[] GetNamedElements(this XmlTextReader reader, string[] elementNames)
		{
			List<XmlNodeList> nodes = new List<XmlNodeList>();
			XmlDocument doc = reader.ToXmlDocument();

			foreach (string element in elementNames)
			{
				XmlNodeList result = doc.GetElementsByTagName(element);
				if (result.Count > 0) nodes.Add(result);
			}

			return nodes.ToArray();
		}

		/// <summary>Extends the XmlTextReader class to support fetching XML nodes via the tag (element) name.</summary>
		/// <param name="elementName">A string specifying the element (tag) name to extract.</param>
		/// <returns>An XmlNodeList containing all of the objects whose tag names match the request.</returns>
		public static XmlNodeList[] GetNamedElements(this XmlTextReader reader, string elementName) =>
			reader.GetNamedElements(new string[] { elementName });

		/// <summary>Extends the XmlTextReader class to support fetching the first XML node with a matching tag (element) name.</summary>
		/// <param name="elementName">A string specifying the element (tag) name to extract.</param>
		/// <returns>An XmlNode object containing the first node found whose tag name matches the request or NULL if not found.</returns>
		public static XmlNode GetFirstNamedElement(this XmlTextReader reader, string[] elementNames)
		{
			XmlNodeList[] elems = reader.GetNamedElements(elementNames);
			return (elems.Count() > 0) && (elems[0].Count > 0) ? elems[0][0] : null;
		}

		public static XmlNode GetFirstNamedElement(this XmlTextReader reader, string elementName, string attributeName, string attributeValue) =>
			reader.ToXmlDocument().GetFirstNamedElement(elementName, attributeName, attributeValue);
		#endregion
	}
}
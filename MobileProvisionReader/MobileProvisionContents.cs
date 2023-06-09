﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace MobileProvisionReader
{
    class MobileProvisionContents
    {
		/// <summary>
		/// The contents of the provision
		/// </summary>
		XmlDocument Document;

		/// <summary>
		/// Map of key names to XML elements holding their values
		/// </summary>
		Dictionary<string, XmlElement> NameToValue = new Dictionary<string, XmlElement>();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="Document">XML file to create the mobile provision from. Call Read() to read from a signed file on disk.</param>
		public MobileProvisionContents(XmlDocument Document)
		{
			this.Document = Document;

			foreach (XmlElement KeyElement in Document.SelectNodes("/plist/dict/key"))
			{
				XmlNode ValueNode = KeyElement.NextSibling;
				while (ValueNode != null)
				{
					XmlElement ValueElement = ValueNode as XmlElement;
					if (ValueElement != null)
					{
						NameToValue[KeyElement.InnerText] = ValueElement;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets the unique id for this mobileprovision
		/// </summary>
		/// <returns>UUID for the provision</returns>
		public string GetUniqueId()
		{
			XmlElement UniqueIdElement;
			if (!NameToValue.TryGetValue("UUID", out UniqueIdElement))
			{
				throw new Exception("Missing UUID in MobileProvision");
			}
			return UniqueIdElement.InnerText;
		}

		/// <summary>
		/// Gets the bundle id for this mobileprovision
		/// </summary>
		/// <returns>Bundle Identifier for the provision</returns>
		public string GetBundleIdentifier()
		{
			XmlElement UniqueIdElement = null, UniqueIdEntitlement;
			if (!NameToValue.TryGetValue("Entitlements", out UniqueIdEntitlement) || UniqueIdEntitlement.Name != "dict")
			{
				throw new Exception("Missing Entitlements in MobileProvision");
			}

			foreach (XmlElement KeyElement in UniqueIdEntitlement.SelectNodes("key"))
			{
				Console.WriteLine("Found entitlement node:" + KeyElement.InnerText);
				if (!KeyElement.InnerText.Equals("application-identifier"))
				{
					continue;
				}
				UniqueIdElement = KeyElement.NextSibling as XmlElement;
				break;
			}


			if (UniqueIdElement == null)
			{
				throw new Exception("Missing Bundle Identifier in MobileProvision");
			}
			return UniqueIdElement.InnerText.Substring(UniqueIdElement.InnerText.IndexOf('.') + 1);
		}

		/// <summary>
		/// Gets the team unique id for this mobileprovision
		/// </summary>
		/// <param name="UniqueId">Receives the team unique id</param>
		/// <returns>True if the team unique ID was found, false otherwise</returns>
		public bool TryGetTeamUniqueId(out string UniqueId)
		{
			XmlElement UniqueIdElement;
			if (!NameToValue.TryGetValue("TeamIdentifier", out UniqueIdElement) || UniqueIdElement.Name != "array")
			{
				UniqueId = null;
				return false;
			}

			XmlElement ValueElement = UniqueIdElement.SelectSingleNode("string") as XmlElement;
			if (ValueElement == null)
			{
				UniqueId = null;
				return false;
			}

			UniqueId = ValueElement.InnerText;
			return true;
		}

		/// <summary>
		/// Reads a mobileprovision from a file on disk
		/// </summary>
		/// <param name="Location">Path to the file</param>
		/// <returns>New mobile provision instance</returns>
		public static MobileProvisionContents Read(string Location)
		{
			XmlDocument Document = ReadXml(Location);
			return new MobileProvisionContents(Document);
		}

		/// <summary>
		/// Reads the plist file inside a mobileprovision
		/// </summary>
		/// <param name="Location">Path to the file</param>
		/// <returns>XML plist extracted from the mobile provision</returns>
		public static XmlDocument ReadXml(string Location)
		{
			// Provision data is stored as PKCS7-signed file in ASN.1 BER format
			using (BinaryReader Reader = new BinaryReader(File.Open(Location, FileMode.Open, FileAccess.Read)))
			{
				long Length = Reader.BaseStream.Length;
				while (Reader.BaseStream.Position < Length)
				{
					Asn.FieldInfo Field = Asn.ReadField(Reader);
					if (Field.Tag == Asn.FieldTag.OBJECT_IDENTIFIER)
					{
						int[] Identifier = Asn.ReadObjectIdentifier(Reader, Field.Length);
						if (Enumerable.SequenceEqual(Identifier, Asn.ObjectIdentifier.Pkcs7_Data))
						{
							while (Reader.BaseStream.Position < Length)
							{
								Asn.FieldInfo NextField = Asn.ReadField(Reader);
								if (NextField.Tag == Asn.FieldTag.OCTET_STRING)
								{
									byte[] Data = Reader.ReadBytes(NextField.Length);

									XmlDocument Document = new XmlDocument();
									Document.Load(new MemoryStream(Data));
									return Document;
								}
								else
								{
									Asn.SkipValue(Reader, NextField);
								}
							}
						}
					}
					else
					{
						Asn.SkipValue(Reader, Field);
					}
				}
				throw new Exception("No PKCS7-Data section");
			}
		}
	}
}

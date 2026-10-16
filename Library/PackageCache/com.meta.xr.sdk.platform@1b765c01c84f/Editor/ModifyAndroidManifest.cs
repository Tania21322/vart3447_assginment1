// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

public class ModifyAndroidManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        // Path to the AndroidManifest.xml file
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

        if (File.Exists(manifestPath))
        {
            // Load the existing manifest
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(manifestPath);

            // Get the <manifest> element
            XmlElement manifestElement = xmlDoc.DocumentElement;

            // Add the xmlns:horizonos attribute if it doesn't exist
            if (manifestElement != null && !manifestElement.HasAttribute("xmlns:horizonos"))
            {
                manifestElement.SetAttribute("xmlns:horizonos", "http://schemas.horizonos/sdk");
            }

            // Check if the <horizonos:uses-horizonos-supplement> element exists
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("horizonos", "http://schemas.horizonos/sdk");
            XmlNode supplementNode = xmlDoc.SelectSingleNode("/manifest/horizonos:uses-horizonos-supplement", nsmgr);
            if (supplementNode != null)
            {
                // Remove the existing <horizonos:uses-horizonos-supplement> element
                manifestElement.RemoveChild(supplementNode);
            }

            // Create a new <horizonos:uses-horizonos-supplement> element
            XmlElement supplementElement = xmlDoc.CreateElement("horizonos", "uses-horizonos-supplement", "http://schemas.horizonos/sdk");
            supplementElement.SetAttribute("name", "horizonos-supplement-hzplatformclientcore");
            supplementElement.SetAttribute("minSupplementVersion", "1");
            supplementElement.SetAttribute("targetSupplementVersion", "1");

            // Append the new element to the manifest
            manifestElement.AppendChild(supplementElement);

            // Save the modified manifest back to the file
            xmlDoc.Save(manifestPath);
        }
        else
        {
            Debug.LogError("AndroidManifest.xml not found at: " + manifestPath);
        }
    }
}

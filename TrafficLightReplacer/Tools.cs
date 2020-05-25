﻿using ColossalFramework.IO;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UnityEngine;

namespace TrafficLightReplacer
{
    public class Tools
    {
        public static string BlankProp = "blankprop.blankprop_Data";
        public static void ShowErrorWindow(string header, string message)
        {
            ExceptionPanel panel = UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel");
            panel.SetMessage(header, message, false);
            panel.GetComponentInChildren<UISprite>().spriteName = "IconError";
        }
        public static void RefreshXMLPacks()
        {
            List<Pack> packList = GetPackList();

            TrafficLightReplacePanel.ResetDropdown(TrafficLightReplacePanel.instance.packDropdown);

            foreach (var xmlItem in packList)
            {
                TrafficLightReplacePanel.instance.packDropdown.AddItem(xmlItem.PackName);
            }

            Replacer.packList = packList;

            Debug.Log("TLRModSettings.instance.CurrentPackIndex: " + TLRModSettings.instance.CurrentPackIndex);
            //add check that current pack index matches name or something?
            TrafficLightReplacePanel.instance.packDropdown.selectedIndex = TLRModSettings.instance.CurrentPackIndex;

        }

        public static List<Pack> GetPackList()
        {
            List<string> files = new List<string>();
            files = Directory.GetFiles(Path.Combine(DataLocation.localApplicationData, "TLRLocal"), "*.xml").ToList();
            List<string> xmlPackNames = new List<string>();
            List<Pack> packList = new List<Pack>();

            //get xmls from ws asset folder
            for (uint i = 0; i < PrefabCollection<PropInfo>.LoadedCount(); i++)
            {
                var prefab = PrefabCollection<PropInfo>.GetLoaded(i);

                if (prefab == null)
                    continue;

                var asset = PackageManager.FindAssetByName(prefab.name);
                if (asset == null || asset.package == null)
                    continue;

                var crpPath = asset.package.packagePath;
                if (crpPath == null)
                    continue;

                var propDirectory = Directory.GetParent(crpPath);

                if (propDirectory.ToString() != DataLocation.assetsPath)
                {
                    var folderFiles = propDirectory.GetFiles();

                    foreach (var filePath in folderFiles)
                    {
                        var filename = Path.GetFileName(filePath.ToString());
                        if (filename == "TLRConfig.xml")
                        {
                            Debug.Log("Found TLRConfig at: " + filePath);
                            files.Add(filePath.ToString());
                        }
                    }
                }
            }

            //////////

            foreach (var xmlFilePath in files)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(TLRConfig));
                StreamReader reader = new StreamReader(xmlFilePath);
                TLRConfig XMLinput = (TLRConfig)serializer.Deserialize(reader);
                reader.Close();
                Debug.Log("packname: " + XMLinput.PackName);
                xmlPackNames.Add(XMLinput.PackName);
            }


            //adding xml file path and name to pack object
            for (int i = 0; i < xmlPackNames.Count; i++)
            {
                Pack item = new Pack();
                item.PackName = xmlPackNames[i];
                item.PackPath = files[i];
                packList.Add(item);
            }

            //sorts and removes square brackets from packname for sorter
            packList = packList.OrderBy(o => Regex.Replace(o.PackName, @"\[.*\] ", "")).ToList();

            //places vanilla lights xml to top
            var vanillatrafficindex = packList.FindIndex(a => a.PackName == "Vanilla Traffic Lights");
            List<Pack> tempPackList = new List<Pack>
            {
                packList[vanillatrafficindex]
            };
            for (int j = 0; j < xmlPackNames.Count; j++)
            {
                if (j != vanillatrafficindex)
                {
                    tempPackList.Add(packList[j]);
                }
            }
            packList = tempPackList;
            return packList;
        }

        public static void ExtractEmbeddedResource(string outputDir, string resourceLocation, List<string> files)
        {
            Directory.CreateDirectory(outputDir);

            foreach (string file in files)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation + @"." + file))
                {
                    if (!File.Exists(Path.Combine(outputDir, file)))
                    {
                        using (FileStream fileStream = new FileStream(Path.Combine(outputDir, file), FileMode.Create))
                        {
                            for (int i = 0; i < stream.Length; i++)
                            {

                                fileStream.WriteByte((byte)stream.ReadByte());
                            }
                            fileStream.Close();
                        }
                    }
                }
            }
        }
    }
}

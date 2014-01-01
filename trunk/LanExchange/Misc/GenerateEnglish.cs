﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using LanExchange.SDK;

namespace LanExchange.Misc
{
    [Localizable(false)]
    public static class GenerateEnglish
    {
        private static Dictionary<string, Phrase> s_Dict;

        public static void Generate()
        {
            s_Dict = new Dictionary<string, Phrase>();
            // get strings from addons
            ProcessAddons();
            // extract strings from *.exe and *.dll files
            var files = GetFiles();
            foreach (var file in files)
                try
                {
                    ProcessFile(file);
                }
                catch(Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            // generate English.po
            GeneratePO();
        }

        private static void ProcessAddons()
        {
            foreach (var fileName in App.FolderManager.GetAddonsFiles())
                try
                {
                    ProcessAddon(fileName);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
        }

        private static void ProcessAddon(string fileName)
        {
            // load addon from xml
            var addon = (SDK.Addon)SerializeUtils.DeserializeObjectFromXMLFile(fileName, typeof(SDK.Addon));
            var context = Path.GetFileName(fileName);
            foreach (var panelItem in addon.PanelItemTypes)
            {
                foreach(var menuItem in panelItem.ContextMenuStrip)
                {
                    if (menuItem.IsSeparator)
                        continue;
                    var value = menuItem.Text;
                    Phrase found;
                    if (!s_Dict.TryGetValue(value, out found))
                        s_Dict.Add(value, new Phrase(context, value));
                }
            }
        }

        private static IEnumerable<string> GetFiles()
        {
            var args = Environment.GetCommandLineArgs();
            var currentDir = Path.GetDirectoryName(args[0]);
            if (currentDir == null)
                return new string[] { };
            var files = Directory.GetFiles(currentDir, "*.exe");
            var result = new List<string>();
            //var origFile = args[0].EndsWith(".vshost.exe") ? args[0].Replace(".vshost.", ".") : "";
            foreach (var file in files)
                //if (!file.Equals(args[0]) && !file.Equals(origFile))
                result.Add(file);
            files = Directory.GetFiles(currentDir, "*.dll");
            foreach (var file in files)
                result.Add(file);
            return result.ToArray();
        }

        private static void ProcessFile(string fileName)
        {
            var assembly = Assembly.LoadFile(fileName);
            var resMan = new ResourceManager(string.Format("{0}.Properties.Resources", Path.GetFileNameWithoutExtension(fileName)), assembly);
            var resourceSet = resMan.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            var context = Path.GetFileName(fileName);
            foreach (DictionaryEntry entry in resourceSet)
            {
                var value = entry.Value as string;
                if (value != null)
                {
                    Phrase found;
                    if (!s_Dict.TryGetValue(value, out found))
                        s_Dict.Add(value, new Phrase(context, value));
                }
            }
        }

        private static string FormatGetText(string value)
        {
            return "\"" + value.Replace("\n", "\\n").Replace("\r", "") + "\"";
        }

        private static void GeneratePO()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# This file generated by \"LanExchange.exe /genenglish\"");
            sb.AppendLine();
            // add variables
            sb.AppendLine("msgctxt \"Variables\"");
            sb.AppendLine("msgid \"@LANGUAGE_NAME\"");
            sb.AppendLine("msgstr \"\"");
            sb.AppendLine();
            sb.AppendLine("msgctxt \"Variables\"");
            sb.AppendLine("msgid \"@AUTHOR\"");
            sb.AppendLine("msgstr \"\"");
            sb.AppendLine();
            // add strings
            foreach (var pair in s_Dict)
            {
                sb.AppendLine(string.Format("msgctxt \"{0}\"", pair.Value.Context));
                sb.AppendLine("msgid " + FormatGetText(pair.Value.ID));
                sb.AppendLine("msgstr \"\"");
                sb.AppendLine();
            }
            using (var writer = new StreamWriter(File.Create("English.po")))
            {
                writer.Write(sb.ToString());
            }
        }

    }
}
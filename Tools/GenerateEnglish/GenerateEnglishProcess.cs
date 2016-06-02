﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using LanExchange.Application.Interfaces;
using LanExchange.Base;
using LanExchange.SDK;

namespace GenerateEnglish
{
    internal sealed class GenerateEnglishProcess
    {
        private readonly IFolderManager folderManager;
        private Dictionary<string, Phrase> phrases;

        public GenerateEnglishProcess(IFolderManager folderManager)
        {
            if (folderManager == null) throw new ArgumentNullException(nameof(folderManager));

            this.folderManager = folderManager;
        }

        public void Execute()
        {
            phrases = new Dictionary<string, Phrase>();
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

        private void ProcessAddons()
        {
            foreach (var fileName in folderManager.GetAddonsFiles())
                try
                {
                    ProcessAddon(fileName);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
        }

        private void ProcessAddon(string fileName)
        {
            // load addon from xml
            var addon = (AddOn)SerializeUtils.DeserializeObjectFromXmlFile(fileName, typeof(AddOn));
            var context = Path.GetFileName(fileName);
            foreach (var panelItem in addon.ItemTypes)
            {
                foreach(var menuItem in panelItem.ContextMenu)
                {
                    if (menuItem.IsSeparator)
                        continue;
                    var value = menuItem.Text;
                    Phrase found;
                    if (!phrases.TryGetValue(value, out found))
                        phrases.Add(value, new Phrase(context, value));
                }
            }
        }

        private IEnumerable<string> GetFiles()
        {
            var args = Environment.GetCommandLineArgs();
            var currentDir = Path.GetDirectoryName(args[0]);
            if (currentDir == null)
                return new string[] { };
            var files = Directory.GetFiles(currentDir, "*.exe");
            var result = files.ToList();
            files = Directory.GetFiles(currentDir, "*.dll");
            result.AddRange(files);
            return result.ToArray();
        }

        private void ProcessFile(string fileName)
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
                    if (!phrases.TryGetValue(value, out found))
                        phrases.Add(value, new Phrase(context, value));
                }
            }
        }

        private string FormatGetText(string value)
        {
            return "\"" + value.Replace("\n", "\\n").Replace("\r", "") + "\"";
        }

        private void GeneratePO()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# This file generated by GenerateEnglish.exe program");
            sb.AppendLine(string.Format("# total phrases: {0}", phrases.Count));
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
            foreach (var pair in phrases)
            {
                sb.AppendLine(string.Format("msgctxt \"{0}\"", pair.Value.Context));
                sb.AppendLine("msgid " + FormatGetText(pair.Value.Id));
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
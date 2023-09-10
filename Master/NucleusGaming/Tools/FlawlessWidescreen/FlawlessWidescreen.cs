﻿using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace Nucleus.Gaming.Tools.FlawlessWidescreen
{
    public static class FlawlessWidescreen
    {
        public static void UseFlawlessWidescreen(GenericGameHandler genericGameHandler, GenericGameInfo genericGameInfo, GenericContext context, GameProfile profile, int i)
        {
            genericGameHandler.Log("Setting up Flawless Widescreen");

            bool pcIs64 = Environment.Is64BitOperatingSystem;
            string pcArch = pcIs64 ? "x64" : "x86";
            string utilFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "utils\\FlawlessWidescreen\\" + pcArch);

            if (genericGameInfo.FlawlessWidescreenOverrideDisplay)
            {
                List<PlayerInfo> players = profile.PlayersList;

                string setPath = utilFolder + "\\settings.xml";
                string backupPath = Path.GetDirectoryName(setPath) + "\\settings_NUCLEUS_BACKUP.xml";
                if (!File.Exists(backupPath))
                {
                    File.Copy(setPath, backupPath);
                }

                string text = File.ReadAllText(setPath);
                text = text.Replace("1010_FirstUse", "Nucleus_FirstUse");
                File.WriteAllText(setPath, text);

                genericGameHandler.Log($"Enabling display detection override and setting width:{players[i].MonitorBounds.Width.ToString()}, height:{players[i].MonitorBounds.Height.ToString()}");
                var setDoc = new XmlDocument();
                setDoc.Load(setPath);
                var nodes = setDoc.SelectNodes("Configuration/DisplayDetectionSettings/CustomSettings");
                foreach (XmlNode node in nodes)
                {
                    node.Attributes["Enabled"].Value = "true";
                    node.Attributes["Width"].Value = players[i].MonitorBounds.Width.ToString();
                    node.Attributes["Height"].Value = players[i].MonitorBounds.Height.ToString();
                }
                setDoc.Save(setPath);

                text = File.ReadAllText(setPath);
                text = text.Replace("Nucleus_FirstUse", "1010_FirstUse");
                File.WriteAllText(setPath, text);
            }


            string fwGameFolder = Path.Combine(utilFolder, "PluginCache\\FWS_Plugins\\Modules\\" + genericGameInfo.FlawlessWidescreen);
            if (genericGameInfo.FlawlessWidescreenPluginPath?.Length > 0)
            {
                fwGameFolder = Path.Combine(utilFolder, genericGameInfo.FlawlessWidescreenPluginPath + "\\" + genericGameInfo.FlawlessWidescreen);
            }

            if (!Directory.Exists(fwGameFolder))
            {
                MessageBox.Show("Nucleus could not find an installed plugin for \"" + genericGameInfo.FlawlessWidescreen + "\" in FlawlessWidescreen. FlawlessWidescreen will now open. Please make sure to install the plugin and make any required changes. When yo close FlawlessWidescreen, Nucleus will continue. Press OK to open FlawlessWidescreen", "Nucleus - Use Flawless Widescreen", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                //bool appRunning = false;
                Process[] runnProcs = Process.GetProcesses();
                foreach (Process proc in runnProcs)
                {
                    if (proc.ProcessName.ToLower() == "flawlesswidescreen")
                    {
                        proc.Kill();
                    }
                }

                genericGameHandler.Log("Starting Flawless Widescreen process");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = utilFolder;
                startInfo.FileName = Path.Combine(utilFolder, "FlawlessWidescreen.exe");
                Process util = Process.Start(startInfo);
                util.WaitForExit();
            }

            if (Directory.Exists(fwGameFolder))
            {
                if (File.Exists(Path.Combine(fwGameFolder, "Dependencies\\Scripts\\" + genericGameInfo.FlawlessWidescreen + ".lua")))
                {
                    List<string> otextChanges = new List<string>();
                    string oscriptPath = Path.Combine(fwGameFolder, "Dependencies\\Scripts\\" + genericGameInfo.FlawlessWidescreen + ".lua");

                    otextChanges.Add(context.FindLineNumberInTextFile(oscriptPath, "Process_WindowName = ", SearchType.StartsWith) + "|Process_WindowName = \"Removed\"");
                    context.ReplaceLinesInTextFile(oscriptPath, otextChanges.ToArray());
                }

                string newFwGameFolder = fwGameFolder + " - Nucleus Instance " + (i + 1);
                if (Directory.Exists(newFwGameFolder))
                {
                    Directory.Delete(newFwGameFolder, true);
                }
                Directory.CreateDirectory(newFwGameFolder);

                foreach (string dir in Directory.GetDirectories(fwGameFolder, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(Path.Combine(newFwGameFolder, dir.Substring(fwGameFolder.Length + 1)));
                }

                foreach (string file_name in Directory.GetFiles(fwGameFolder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Copy(file_name, Path.Combine(newFwGameFolder, file_name.Substring(fwGameFolder.Length + 1)));
                        genericGameHandler.Log("Copying file " + Path.GetFileName(file_name));
                    }
                    catch
                    {
                        //Log("ERROR - " + ex.Message);
                        genericGameHandler.Log("Using alternative copy method for " + file_name);
                        CmdUtil.ExecuteCommand(Path.GetDirectoryName(Path.Combine(newFwGameFolder, file_name.Substring(fwGameFolder.Length + 1))), out int exitCode, "copy \"" + file_name + "\" \"" + Path.Combine(newFwGameFolder, file_name.Substring(fwGameFolder.Length + 1)) + "\"", false);
                    }

                    if (file_name.EndsWith("Dependencies\\Scripts\\" + genericGameInfo.FlawlessWidescreen + ".lua"))
                    {
                        File.Move(Path.Combine(newFwGameFolder + "\\Dependencies\\Scripts\\", genericGameInfo.FlawlessWidescreen + ".lua"), Path.Combine(newFwGameFolder + "\\Dependencies\\Scripts\\", Path.GetFileNameWithoutExtension(file_name) + " - Nucleus Instance " + (i + 1) + ".lua"));
                    }
                }

                List<string> textChanges = new List<string>();
                string scriptPath = Path.Combine(newFwGameFolder, "Dependencies\\Scripts\\" + genericGameInfo.FlawlessWidescreen + " - Nucleus Instance " + (i + 1) + ".lua");

                textChanges.Add(context.FindLineNumberInTextFile(scriptPath, "Process_WindowName = ", SearchType.StartsWith) + "|Process_WindowName = \"" + "Nucleus Instance " + (i + 1) + "(" + genericGameInfo.Hook.ForceFocusWindowName.Replace("®", "%R") + ")\"");
                context.ReplaceLinesInTextFile(scriptPath, textChanges.ToArray());

                string path = Path.Combine(utilFolder, "Plugins\\FWS_Plugins.fws");
                path = Environment.ExpandEnvironmentVariables(path);

                var doc = new XmlDocument();
                doc.Load(path);
                var nodes = doc.SelectNodes("Plugin/Modules/Module");
                bool exists = false;
                XmlNode origNode = null;
                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes["NameSpace"].Value == genericGameInfo.FlawlessWidescreen)
                    {
                        origNode = node;
                    }
                    if (node.Attributes["NameSpace"].Value == genericGameInfo.FlawlessWidescreen + " - Nucleus Instance " + (i + 1))
                    {
                        exists = true;
                    }
                    if (origNode != null && exists)
                    {
                        break;
                    }
                }

                if (!exists)
                {
                    // Create a new node with the name of your new server
                    XmlNode newNode = doc.CreateElement("Module");

                    // set the inner xml of a new node to inner xml of original node
                    newNode.InnerXml = origNode.InnerXml;

                    XmlAttribute attr = doc.CreateAttribute("NameSpace");
                    attr.Value = genericGameInfo.FlawlessWidescreen + " - Nucleus Instance " + (i + 1);

                    newNode.Attributes.SetNamedItem(attr);
                    newNode["FriendlyName"].InnerText = newNode["FriendlyName"].InnerText + " - Nucleus Instance " + (i + 1);
                    doc.DocumentElement["Modules"].AppendChild(newNode);
                }

                doc.Save(path);

                if (i == (genericGameHandler.numPlayers - 1))
                {
                    //bool appRunning = false;
                    Process[] runnProcs = Process.GetProcesses();
                    foreach (Process proc in runnProcs)
                    {
                        if (proc.ProcessName.ToLower() == "flawlesswidescreen")
                        {
                            proc.Kill();
                        }
                    }

                    genericGameHandler.Log("Starting Flawless Widescreen process");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.UseShellExecute = true;
                    startInfo.WorkingDirectory = utilFolder;
                    startInfo.FileName = Path.Combine(utilFolder, "FlawlessWidescreen.exe");
                    Process.Start(startInfo);

                }

                genericGameHandler.Log("Flawless Widescreen setup complete");
            }
        }

        public static void KillFlawlessWidescreen(GenericGameHandler genericGameHandler, GenericGameInfo gen)
        {
            Process[] runnProcs = Process.GetProcesses();
            foreach (Process proc in runnProcs)
            {
                if (proc.ProcessName.ToLower() == "flawlesswidescreen")
                {
                    genericGameHandler.Log("Killing Flawless Widescreen app");
                    proc.Kill();
                }
            }

            if (gen.FlawlessWidescreenOverrideDisplay)
            {
                genericGameHandler.Log("Restoring back up Flawless Widescreen settings file");
                string utilFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "utils\\FlawlessWidescreen\\" + genericGameHandler.garch);
                string setPath = utilFolder + "\\settings.xml";
                string backupPath = Path.GetDirectoryName(setPath) + "\\settings_NUCLEUS_BACKUP.xml";
                if (File.Exists(backupPath))
                {
                    if (File.Exists(setPath))
                    {
                        File.Delete(setPath);
                    }

                    File.Move(backupPath, setPath);
                }
            }
        }
    }
}

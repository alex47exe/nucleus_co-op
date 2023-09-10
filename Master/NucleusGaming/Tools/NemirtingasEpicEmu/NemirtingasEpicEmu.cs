﻿using Newtonsoft.Json.Linq;
using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace Nucleus.Gaming.Tools.NemirtingasEpicEmu
{
    public static class NemirtingasEpicEmu
    {
        public static void UseNemirtingasEpicEmu(GenericGameHandler genericGameHandler, GenericGameInfo gen, string rootFolder, string linkFolder, int i, PlayerInfo player, bool setupDll)
        {
            if (setupDll)
            {
                genericGameHandler.Log("Starting Nemirtingas Epic Emu setup");
                string utilFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "utils\\NemirtingasEpicEmu");
                string x86dll = "EOSSDK-Win32-Shipping.dll";
                string x64dll = "EOSSDK-Win64-Shipping.dll";

                string dllrootFolder = string.Empty;
                string dllFolder = string.Empty;
                string instanceDllFolder = string.Empty;

                genericGameHandler.Log("Generating emulator settings folder");
                try
                {
                    if (!Directory.Exists(Path.Combine(genericGameHandler.instanceExeFolder, "nepice_settings")))
                    {
                        Directory.CreateDirectory((Path.Combine(genericGameHandler.instanceExeFolder, "nepice_settings")));
                        genericGameHandler.Log("Emulator settings folder generated");
                    }
                }
                catch (Exception)
                {
                    genericGameHandler.Log("Nucleus is unable to generate the required emulator settings folder");
                }

                try
                {
                    JObject emuSettings;

                    if (gen.AltEpicEmuArgs)
                    {
                        emuSettings = new JObject(
                        new JProperty("enable_overlay", false),
                        new JProperty("epicid", player.Nickname),
                        new JProperty("disable_online_networking", false),
                        new JProperty("enable_lan", true),
                        //new JProperty("log_level", log),
                        new JProperty("savepath", "appdata"),
                        new JProperty("unlock_dlcs", true),
                        new JProperty("language", GetEpicLanguage()),
                        new JProperty("username", player.Nickname)
                        );
                    }
                    else
                    {
                        emuSettings = new JObject(
                        new JProperty("enable_overlay", false),
                        //new JProperty("gamename", gen.GameName.ToLower()),
                        new JProperty("epicid", "0000000000000000000000000player" + (i + 1)),
                        new JProperty("disable_online_networking", false),
                        new JProperty("enable_lan", true),
                        // new JProperty("log_level", log),
                        new JProperty("savepath", "appdata"),
                        new JProperty("unlock_dlcs", true),
                        new JProperty("language", GetEpicLanguage()),
                        new JProperty("username", player.Nickname)
                        );
                    }

                    string jsonPath = Path.Combine(genericGameHandler.instanceExeFolder, "nepice_settings\\NemirtingasEpicEmu.json");

                    string oldjsonPath = Path.Combine(genericGameHandler.instanceExeFolder, "NemirtingasEpicEmu.json");//for older eos emu version

                    genericGameHandler.Log("Writing emulator settings NemirtingasEpicEmu.json");

                    File.WriteAllText(jsonPath, emuSettings.ToString());

                    File.WriteAllText(oldjsonPath, emuSettings.ToString());//for older eos emu version

                    if (setupDll)
                    {
                        genericGameHandler.addedFiles.Add(jsonPath);
                        genericGameHandler.addedFiles.Add(oldjsonPath);
                    }
                }
                catch (Exception)
                {
                    genericGameHandler.Log("Nucleus is unable to write the required NemirtingasEpicEmu.json file");
                }

                string[] steamDllFiles = Directory.GetFiles(rootFolder, "EOSSDK-Win*.dll", SearchOption.AllDirectories);
                foreach (string nameFile in steamDllFiles)
                {
                    genericGameHandler.Log("Found " + nameFile);
                    dllrootFolder = Path.GetDirectoryName(nameFile);

                    string tempRootFolder = rootFolder;
                    if (tempRootFolder.EndsWith("\\"))
                    {
                        tempRootFolder = tempRootFolder.Substring(0, tempRootFolder.Length - 1);
                    }
                    dllFolder = dllrootFolder.Remove(0, (tempRootFolder.Length));

                    instanceDllFolder = linkFolder.TrimEnd('\\') + "\\" + dllFolder.TrimStart('\\');

                    if (nameFile.EndsWith(x64dll, true, null))
                    {

                        FileUtil.FileCheck(genericGameHandler, gen, Path.Combine(instanceDllFolder, x64dll));
                        try
                        {
                            genericGameHandler.Log("Placing Epic Emu " + x64dll + " in instance dll folder " + instanceDllFolder);
                            File.Copy(Path.Combine(utilFolder, "x64\\" + x64dll), Path.Combine(instanceDllFolder, x64dll), true);
                        }
                        catch (Exception ex)
                        {
                            genericGameHandler.Log("ERROR - " + ex.Message);
                            genericGameHandler.Log("Using alternative copy method for " + x64dll);
                            CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, "x64\\" + x64dll) + "\" \"" + Path.Combine(instanceDllFolder, x64dll) + "\"");
                        }
                    }

                    if (nameFile.EndsWith(x86dll, true, null))
                    {
                        FileUtil.FileCheck(genericGameHandler, gen, Path.Combine(instanceDllFolder, x86dll));

                        try
                        {
                            genericGameHandler.Log("Placing Epic Emu " + x86dll + " in instance steam dll folder " + instanceDllFolder);
                            File.Copy(Path.Combine(utilFolder, "x86\\" + x86dll), Path.Combine(instanceDllFolder, x86dll), true);
                        }
                        catch (Exception ex)
                        {
                            genericGameHandler.Log("ERROR - " + ex.Message);
                            genericGameHandler.Log("Using alternative copy method for " + x86dll);
                            CmdUtil.ExecuteCommand(utilFolder, out int exitCode, "copy \"" + Path.Combine(utilFolder, "x86\\" + x86dll) + "\" \"" + Path.Combine(instanceDllFolder, x86dll) + "\"");
                        }
                    }
                }
            }

            genericGameHandler.Log("Epic Emu setup complete");
        }

        public static string GetEpicLanguage()
        {
            string epicLanguage = Globals.ini.IniReadValue("Misc", "EpicLang");
            string EpicLang = "";

            IDictionary<string, string> epiclangs = new Dictionary<string, string>
            {
                { "Arabic", "ar" },
                { "Brazilian", "pt-BR" },
                { "Bulgarian", "bg" },
                { "Chinese", "zh" },
                { "Czech", "cs" },
                { "Danish", "da" },
                { "Dutch", "nl" },
                { "English", "en" },
                { "Finnish", "fi" },
                { "French", "fr" },
                { "German", "de" },
                { "Greek", "el" },
                { "Hungarian", "hu" },
                { "Italian", "it" },
                { "Japanese", "ja" },
                { "Koreana", "ko" },
                { "Norwegian", "no" },
                { "Polish", "pl" },
                { "Portuguese", "pt" },
                { "Romanian", "ro" },
                { "Russian", "ru" },
                { "Spanish", "es" },
                { "Swedish", "sv" },
                { "Thai", "th" },
                { "Turkish", "tr" },
                { "Ukrainian", "uk" }
            };

            foreach (KeyValuePair<string, string> lang in epiclangs)
            {
                if (lang.Key != epicLanguage)
                {
                    continue;
                }

                EpicLang = lang.Value;
                break;
            }

            return EpicLang;
        }
    }
}

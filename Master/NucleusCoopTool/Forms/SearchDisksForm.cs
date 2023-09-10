﻿using Nucleus.Gaming;
using Nucleus.Gaming.Cache;
using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace Nucleus.Coop
{
    public partial class SearchDisksForm : UserControl, IDynamicSized
    {
        public struct SearchDriveInfo
        {
            public DriveInfo drive;
            public string text;

            public override string ToString()
            {
                return text;
            }
        }

        private float progress;
        private float lastProgress;

        private List<string> pathsToSearch;
        private List<Control> ctrls = new List<Control>();
        private bool searching;
        private bool closed;
        private MainForm main;
        private float fontSize;

        private Cursor hand_Cursor;
        private Cursor default_Cursor;

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
         int nLeftRect,     // x-coordinate of upper-left corner
         int nTopRect,      // y-coordinate of upper-left corner
         int nRightRect,    // x-coordinate of lower-right corner
         int nBottomRect,   // y-coordinate of lower-right corner
         int nWidthEllipse, // width of ellipse
         int nHeightEllipse // height of ellipse
        );

        private void controlscollect()
        {
            foreach (Control control in Controls)
            {
                ctrls.Add(control);
                foreach (Control container1 in control.Controls)
                {
                    ctrls.Add(container1);
                    foreach (Control container2 in container1.Controls)
                    {
                        ctrls.Add(container2);
                        foreach (Control container3 in container2.Controls)
                        {
                            ctrls.Add(container3);
                        }
                    }
                }
            }
        }

        public void button_Click(object sender, EventArgs e)
        {
            if (main.mouseClick)
                main.SoundPlayer(main.theme + "button_click.wav");
        }

        private void closeButton(object sender, EventArgs e)
        {
            txt_Stage.Visible = false;
            progressBar1.Visible = false;
            txt_Path.Visible = false;
            this.Visible = false;
        }

        public SearchDisksForm(MainForm main)
        {
            this.main = main;

            InitializeComponent();

            SuspendLayout();

            default_Cursor = main.default_Cursor;
            Cursor.Current = default_Cursor;
            hand_Cursor = main.hand_Cursor;
            Location = new Point(main.Location.X + main.Width / 2 - Width / 2, main.Location.Y + main.Height / 2 - Height / 2);

            fontSize = float.Parse(main.themeIni.IniReadValue("Font", "AutoSearchFontSize"));
            ForeColor = Color.FromArgb(int.Parse(main.rgb_font[0]), int.Parse(main.rgb_font[1]), int.Parse(main.rgb_font[2]));
            BackColor = BackColor = Color.FromArgb(int.Parse(main.themeIni.IniReadValue("Colors", "AutoSearchBackground").Split(',')[0]),
                                                   int.Parse(main.themeIni.IniReadValue("Colors", "AutoSearchBackground").Split(',')[1]),
                                                   int.Parse(main.themeIni.IniReadValue("Colors", "AutoSearchBackground").Split(',')[2]),
                                                   int.Parse(main.themeIni.IniReadValue("Colors", "AutoSearchBackground").Split(',')[3]));

            closeBtn.BackgroundImage = ImageCache.GetImage(main.theme + "title_close.png");

            btn_addSelection.BackColor = main.buttonsBackColor;
            btn_customPath.BackColor = main.buttonsBackColor;
            btnSearch.BackColor = main.buttonsBackColor;
            btn_delPath.BackColor = main.buttonsBackColor;
            btn_selectAll.BackColor = main.buttonsBackColor;
            btn_deselectAll.BackColor = main.buttonsBackColor;
    
            btn_addSelection.FlatAppearance.MouseOverBackColor = main.MouseOverBackColor;
            btn_customPath.FlatAppearance.MouseOverBackColor = main.MouseOverBackColor;
            btnSearch.FlatAppearance.MouseOverBackColor = main.MouseOverBackColor;
            btn_delPath.FlatAppearance.MouseOverBackColor = main.MouseOverBackColor;
            btn_selectAll.FlatAppearance.MouseOverBackColor = main.MouseOverBackColor;
            btn_deselectAll.FlatAppearance.MouseOverBackColor = main.MouseOverBackColor;

            controlscollect();

            foreach (Control control in ctrls)
            {
                control.Font = new Font(main.customFont, fontSize, FontStyle.Regular, GraphicsUnit.Pixel, 0);

                if (control.Name != "panel1")
                {
                    control.Cursor = hand_Cursor;
                }
                else
                {
                    control.Cursor = default_Cursor;
                }

                if (main.mouseClick)
                {
                    if (control is Button)
                    {
                        control.Click += new System.EventHandler(this.button_Click);
                    }
                }              
            }

            if (main.useButtonsBorder)
            {
                btn_addSelection.FlatAppearance.BorderSize = 1;
                btn_addSelection.FlatAppearance.BorderColor = main.ButtonsBorderColor;
                btn_customPath.FlatAppearance.BorderSize = 1;
                btn_customPath.FlatAppearance.BorderColor = main.ButtonsBorderColor;
                btnSearch.FlatAppearance.BorderSize = 1;
                btnSearch.FlatAppearance.BorderColor = main.ButtonsBorderColor;

                btn_delPath.FlatAppearance.BorderSize = 1;
                btn_delPath.FlatAppearance.BorderColor = main.ButtonsBorderColor;
                btn_selectAll.FlatAppearance.BorderSize = 1;
                btn_selectAll.FlatAppearance.BorderColor = main.ButtonsBorderColor;
                btn_deselectAll.FlatAppearance.BorderSize = 1;
                btn_deselectAll.FlatAppearance.BorderColor = main.ButtonsBorderColor;
            }

            ResumeLayout();

            closeBtn.Cursor = hand_Cursor;

            for (int x = 1; x <= 100; x++)
            {
                if (main.ini.IniReadValue("SearchPaths", x.ToString()) == "")
                {
                    break;
                }
                else
                {
                    disksBox.Items.Add(main.ini.IniReadValue("SearchPaths", x.ToString()), true);
                }
            }

            DPIManager.Register(this);
            DPIManager.Update(this);
        }

        public void UpdateSize(float scale)
        {
            if (IsDisposed)
            {
                DPIManager.Unregister(this);
                return;
            }

            SuspendLayout();

            if (scale > 1.0F)
            {
                var heightField = typeof(CheckedListBox).GetField(
                "scaledListItemBordersHeight",
                BindingFlags.NonPublic | BindingFlags.Instance
                );

                var addedHeight = 10 * (int)scale;

                heightField.SetValue(disksBox, addedHeight);
                heightField.SetValue(checkboxFoundGames, addedHeight);
            }
            else
            {
                var heightField = typeof(CheckedListBox).GetField(
               "scaledListItemBordersHeight",
               BindingFlags.NonPublic | BindingFlags.Instance
               );

                var addedHeight = 6;

                heightField.SetValue(disksBox, addedHeight);
                heightField.SetValue(checkboxFoundGames, addedHeight);

            }
          
            float textBoxFontSize = (Font.Size + 4) * scale;

            foreach (Control c in ctrls)
            {
                if (c.GetType() == typeof(CheckedListBox))
                {
                    c.Font = new Font(main.customFont, c.Font.Size, FontStyle.Regular, GraphicsUnit.Point, 0);
                }

                if (c.GetType() == typeof(TextBox))
                {
                    c.Font = new Font(main.customFont, textBoxFontSize, FontStyle.Regular, GraphicsUnit.Point, 0);
                }
            }

            ResumeLayout();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x020 || m.Msg == 0x0a0)//Do not reset our custom cursor when mouse hover over the Form background(needed because of the custom resizing/moving messges handling) 
            {
                Cursor.Current = default_Cursor;
                return;
            }

            base.WndProc(ref m);
        }

        private bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (btnSearch.Text == "Cancel")
            {
                closed = true;
                searching = false;
                btnSearch.Text = "Search";
                txt_Path.Text = "";
                txt_Stage.Text = "Cancelling";
                checkboxFoundGames.Items.Clear();
                Cancel();
                return;
            }

            if (searching || disksBox.CheckedItems.Count == 0)
            {
                return;
            }
            txt_Stage.Visible = true;
            progressBar1.Visible = true;
            txt_Path.Visible = true;
            btnSearch.Text = "Cancel";
            btn_customPath.Enabled = false;
            btn_delPath.Enabled = false;
            disksBox.Enabled = false;
            searching = true;
            closed = false;
            txt_Path.Text = "";
            txt_Stage.Text = "";
            label1.Enabled = false;
            checkboxFoundGames.Items.Clear();

            pathsToSearch = new List<string>();

            for (int i = 0; i < disksBox.CheckedItems.Count; i++)
            {
                pathsToSearch.Add(disksBox.CheckedItems[i].ToString());
            }

            ThreadPool.QueueUserWorkItem(SearchDrive, null);
        }

        private void UpdateProgress(float toAdd)
        {
            progress += toAdd;

            float dif = progress - lastProgress;
            if (dif > 0.005f || toAdd == 0) // only update after .5% or if the user has just requested an update
            {
                lastProgress = progress;
                if (IsDisposed || closing)
                {
                    return;
                }

                Invoke(new Action(delegate
                {
                    if (IsDisposed || progressBar1.IsDisposed || closing)
                    {
                        return;
                    }
                    progressBar1.Value = Math.Min(100, (int)(progress * 100));
                }));
            }
        }

        private IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                Application.DoEvents();
                if (closed)
                {
                    break;
                }

                path = queue.Dequeue();
                txt_Path.Text = path;
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        if (closed)
                        {
                            break;
                        }
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    if (closed)
                    {
                        break;
                    }
                    files = Directory.GetFiles(path, "*.exe");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (closed)
                        {
                            break;
                        }
                        yield return files[i];
                    }
                }
            }

            if (closed)
            {
                txt_Path.Text = "";
                yield return null;
            }
        }

        private void Cancel()
        {
            searching = false;
            btnSearch.Enabled = true;
            btn_customPath.Enabled = true;
            btn_delPath.Enabled = true;
            btn_addSelection.Enabled = false;
            btn_selectAll.Enabled = false;
            btn_deselectAll.Enabled = false;
            checkboxFoundGames.Enabled = false;
            checkboxFoundGames.Items.Clear();
            disksBox.Enabled = true;
            progressBar1.Value = 0;
            progress = 0;
            lastProgress = 0;
            UpdateProgress(0);
            label2.Enabled = false;
            label1.Enabled = true;
            txt_Stage.Text = "Cancelled";
            txt_Path.Text = "";
            txt_Stage.Visible = false;
            progressBar1.Visible = false;
            txt_Path.Visible = false;
        }

        private void SearchDrive(object state)
        {
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < pathsToSearch.Count; i++)
                {
                    txt_Stage.Text = i + 1 + " of " + pathsToSearch.Count;
                    string currentPath = pathsToSearch[i];

                    float totalDiskPc = 1 / (float)pathsToSearch.Count;
                    float thirdDiskPc = totalDiskPc / 3.0f;

                    // 1/3 done, we started the operation
                    UpdateProgress(thirdDiskPc);

                    List<string> result = new List<string>();

                    result = GetFiles(currentPath).ToList();


                    float increment = thirdDiskPc / result.Count;

                    foreach (string exeFilePath in result)
                    {
                        if (closed)
                        {
                            return;
                        }

                        //UpdateProgress(increment);
                        if (GameManager.Instance.User.Games.Any(c => c.ExePath.ToLower() == exeFilePath.ToLower()))
                        {
                            continue;
                        }

                        if (GameManager.Instance.AnyGame(Path.GetFileName(exeFilePath).ToLower()))
                        {
                            if (exeFilePath.Contains("$Recycle.Bin") ||
                                exeFilePath.Contains(@"\Instance"))
                            {
                                // noope
                                continue;
                            }

                            GenericGameInfo uinfo = GameManager.Instance.GetGame(exeFilePath);

                            if (uinfo != null)
                            {
#if RELEASE
                    if (uinfo.Game.Debug)
                    {
                        continue;
                    }
#endif
                                //LogManager.Log("> Found new game {0} on drive {1}", uinfo.Game.GameName, info.drive.Name);
                                Invoke(new Action(delegate
                                {
                                    bool exists = false;
                                    foreach (object item in checkboxFoundGames.Items)
                                    {
                                        if (item.ToString() == uinfo.GameName + " | " + exeFilePath)
                                        {
                                            exists = true;
                                        }
                                    }
                                    if (!exists)
                                    {
                                        checkboxFoundGames.Items.Add(uinfo.GameName + " | " + exeFilePath, true);
                                        checkboxFoundGames.Refresh();
                                    }
                                }));
                            }
                        }
                    }

                    if (closed)
                    {
                        return;
                    }
                }

                searching = false;
                btnSearch.Text = "Search";

                watch.Stop();

                long elapsedMs = watch.ElapsedMilliseconds / 1000;

                if (checkboxFoundGames.Items.Count == 0)
                {
                    btn_customPath.Enabled = true;
                    btn_delPath.Enabled = true;
                    disksBox.Enabled = true;

                    MessageBox.Show("Operation completed in " + elapsedMs + "s. No new games found.");
                    progressBar1.Value = 0;
                    return;
                }

                progress = 1;
                UpdateProgress(0);
                btn_addSelection.Enabled = true;
                btn_selectAll.Enabled = true;
                btn_deselectAll.Enabled = true;
                checkboxFoundGames.Enabled = true;
                btnSearch.Enabled = false;
                label2.Enabled = true;
                label1.Enabled = false;
                txt_Stage.Text = "Done";
                txt_Path.Text = "";
                Refresh();
                Invalidate();
                MessageBox.Show("Search has completed, operation took " + elapsedMs + "s. Select the games you wish to add from the right-hand side.", "Search finished");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private bool closing;
        private void SearchDisksForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Refresh();
            closing = true;
        }

        private void Btn_customPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    disksBox.Items.Add(fbd.SelectedPath, true);

                    int freeIndex = 1;
                    for (int x = 1; x <= 100; x++)
                    {
                        if (main.ini.IniReadValue("SearchPaths", x.ToString()) == "")
                        {
                            freeIndex = x;
                            break;
                        }
                    }

                    main.ini.IniWriteValue("SearchPaths", freeIndex.ToString(), fbd.SelectedPath);
                }
            }
        }

        private void Btn_addSelection_Click(object sender, EventArgs e)
        {
            if (checkboxFoundGames.CheckedItems.Count == 0)
            {
                DialogResult dialogResult = MessageBox.Show("No games have been selected. Do you wish to add any?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    return;
                }
            }

            if (checkboxFoundGames.CheckedItems.Count > 0)
            {
                List<string> gamesToAdd = new List<string>();
                int numAdded = 0;

                for (int i = 0; i < checkboxFoundGames.CheckedItems.Count; i++)
                {
                    string checkBoxFullname = checkboxFoundGames.CheckedItems[i].ToString();
                    string exePath = checkBoxFullname.Substring(checkBoxFullname.IndexOf(" | ") + " | ".Length);
                    gamesToAdd.Add(exePath);
                }

                foreach (string gameToAdd in gamesToAdd)
                {
                    UserGameInfo uinfo = GameManager.Instance.TryAddGame(gameToAdd);
                    if (uinfo != null)
                    {
                        main.NewUserGame(uinfo);
                        numAdded++;
                    }
                }

                MessageBox.Show(string.Format("{0}/{1} selected games added!", numAdded, checkboxFoundGames.CheckedItems.Count), "Games added");
                main.RefreshGames();
            }

            btnSearch.Enabled = true;
            btn_customPath.Enabled = true;
            btn_delPath.Enabled = true;
            btn_addSelection.Enabled = false;
            btn_selectAll.Enabled = false;
            btn_deselectAll.Enabled = false;
            checkboxFoundGames.Enabled = false;
            checkboxFoundGames.Items.Clear();
            disksBox.Enabled = true;
            progressBar1.Value = 0;
            progress = 0;
            lastProgress = 0;
            label2.Enabled = false;
            label1.Enabled = true;
            txt_Path.Text = "";
            txt_Stage.Text = "";
        }

        private void Btn_delPath_Click(object sender, EventArgs e)
        {
            if (disksBox.CheckedItems.Count == 0)
            {
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete the paths that are currently checked?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                foreach (string item in disksBox.CheckedItems.OfType<string>().ToList())
                {
                    disksBox.Items.Remove(item);
                    for (int x = 1; x <= 100; x++)
                    {
                        if (main.ini.IniReadValue("SearchPaths", x.ToString()) == item)
                        {
                            main.ini.IniWriteValue("SearchPaths", x.ToString(), "");
                            break;
                        }
                    }
                }
            }
        }

        private void Btn_selectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkboxFoundGames.Items.Count; i++)
            {
                checkboxFoundGames.SetItemChecked(i, true);
            }
        }

        private void Btn_deselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkboxFoundGames.Items.Count; i++)
            {
                checkboxFoundGames.SetItemChecked(i, false);
            }
        }

        private void closeBtn_MouseEnter(object sender, EventArgs e)
        {
            closeBtn.BackgroundImage = ImageCache.GetImage(main.theme + "title_close_mousehover.png");
        }

        private void closeBtn_MouseLeave(object sender, EventArgs e)
        {
            closeBtn.BackgroundImage = ImageCache.GetImage(main.theme + "title_close.png");
        }
    }
}
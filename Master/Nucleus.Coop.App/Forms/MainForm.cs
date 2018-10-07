﻿using Newtonsoft.Json;
using Nucleus.Coop.App.Controls;
using Nucleus.Coop.Controls;
using Nucleus.Gaming;
using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Coop.Handler;
using Nucleus.Gaming.Coop.Interop;
using Nucleus.Gaming.Package;
using Nucleus.Gaming.Windows;
using Nucleus.Gaming.Windows.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Nucleus.Coop.App.Forms
{
    /// <summary>
    /// Central UI class to the Nucleus Coop application
    /// </summary>
    public partial class MainForm : BaseForm
    {
        private int currentStepIndex;
        private bool formClosing;

        private GameManager gameManager;
        private Dictionary<UserGameInfo, GameControl> controls;

        private SearchDisksForm form;

        private GameControl selectedControl;

        private GameHandlerMetadata selectedHandler;
        private HandlerDataManager handlerDataManager;
        private HandlerData handlerData;
        private GameHandler handler;

        private GameProfile currentProfile;
        private bool noGamesPresent;
        private List<UserInputControl> stepsList;
        private UserInputControl currentStep;

        private PositionsControl positionsControl;
        private PlayerOptionsControl optionsControl;
        private JSUserInputControl jsControl;

        private Thread handlerThread;

        private GameRunningOverlay overlay;

        private GameHandlerMetadata[] currentHandlers;

        private DomainWebApiConnection apiConnection;
        private HandlerManagerForm pkgManager;

        private AppPage appPage = AppPage.None;

        protected override Size DefaultSize
        {
            get
            {
                return new Size(1070, 740);
            }
        }

        public MainForm(string[] args, GameManager gameManager, DomainWebApiConnection con)
        {
            this.apiConnection = con;
            this.gameManager = gameManager;

            InitializeComponent();

            overlay = new GameRunningOverlay();
            overlay.OnStop += Overlay_OnStop;

            this.titleBarControl1.Text = string.Format("Nucleus Coop v{0}", Globals.Version);

            controls = new Dictionary<UserGameInfo, GameControl>();

            positionsControl = new PositionsControl();
            optionsControl = new PlayerOptionsControl();
            jsControl = new JSUserInputControl();

            positionsControl.OnCanPlayUpdated += StepCanPlay;
            optionsControl.OnCanPlayUpdated += StepCanPlay;
            jsControl.OnCanPlayUpdated += StepCanPlay;

            // selects the list of games, so the buttons look equal
            list_games.Select();
            list_games.AutoScroll = false;
            list_games.SelectedChanged += list_Games_SelectedChanged;
            //int vertScrollWidth = SystemInformation.VerticalScrollBarWidth;
            //list_Games.Padding = new Padding(0, 0, vertScrollWidth, 0);

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string argument = args[i];
                    if (string.IsNullOrEmpty(argument))
                    {
                        continue;
                    }

                    string extension = Path.GetExtension(argument);
                    if (extension.ToLower().EndsWith("nc"))
                    {
                        // try installing the package in the arguments if user allows it
                        if (MessageBox.Show("Would you like to install " + argument + "?", "Question", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            gameManager.RepoManager.InstallPackage(argument);
                        }
                    }
                }
            }

            if (!gameManager.User.Options.RequestedToAssociateFormat)
            {
                gameManager.User.Options.RequestedToAssociateFormat = true;

                if (MessageBox.Show("Would you like to associate Nucleus Package Files (*.nc) to the application?", "Question", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string startLocation = Process.GetCurrentProcess().MainModule.FileName;
                    // TODO: abstract (windows exclusive code)
                    if (!FileAssociations.SetAssociation(".nc", "NucleusCoop", "Nucleus Package Files", startLocation))
                    {
                        MessageBox.Show("Failed to set association");
                        gameManager.User.Options.RequestedToAssociateFormat = false;
                    }
                }

                gameManager.User.Save();
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            this.TopMost = true;
            this.BringToFront();

            System.Diagnostics.Debug.WriteLine("Got Focus");
        }

        public void RefreshGames()
        {
            lock (controls)
            {
                foreach (var con in controls)
                {
                    if (con.Value != null)
                    {
                        con.Value.Dispose();
                    }
                }
                this.list_games.Controls.Clear();
                controls.Clear();

                List<GameHandlerMetadata> handlers = gameManager.User.InstalledHandlers;
                for (int i = 0; i < handlers.Count; i++)
                {
                    GameHandlerMetadata handler = handlers[i];
                    NewGameHandler(handler);
                }

                // make menu before games
                GameControl pkgManagerBtn = new GameControl(null);
                pkgManagerBtn.Width = list_games.Width;
                pkgManagerBtn.TitleText = "Package Manager";
                pkgManagerBtn.Image = Properties.Resources.icon;
                pkgManagerBtn.Click += PkgManagerBtn_Click;
                this.list_games.Controls.Add(pkgManagerBtn);

                HorizontalLineControl line = new HorizontalLineControl();
                line.LineHorizontalPc = 100;
                line.Width = list_games.Width;
                line.LineHeight = 2;
                line.LineColor = Color.FromArgb(255, 41, 45, 47);
                this.list_games.Controls.Add(line);

                GamesSeparator sep = new GamesSeparator();
                this.list_games.Controls.Add(sep);

                List<UserGameInfo> games = gameManager.User.Games;
                for (int i = 0; i < games.Count; i++)
                {
                    UserGameInfo game = games[i];
                    NewUserGame(game);
                }

                if (games.Count == 0)
                {
                    noGamesPresent = true;
                    appPage = AppPage.NoGamesInstalled;
                    GameControl con = new GameControl(null);
                    con.Click += Con_Click;
                    con.Width = list_games.Width;
                    con.TitleText = "No games";
                    this.list_games.Controls.Add(con);
                }
            }

            DPIManager.ForceUpdate();
            UpdatePage();

            gameManager.User.Save();
        }

        private void Con_Click(object sender, EventArgs e)
        {
            appPage = AppPage.NoGamesInstalled;
            UpdatePage();
        }

        private void PkgManagerBtn_Click(object sender, EventArgs e)
        {
            appPage = AppPage.PackageManager;
            UpdatePage();
        }

        private void UpdatePage()
        {
            //panel_pageTitle.Visible = false;

            // game
            panel_game.Visible = false;
            panel_gameBtns.Visible = false;

            // no games installed
            panel_noGames.Visible = false;

            // handler manager
            panel_handlerManager.Visible = false;

            switch (appPage)
            {
                case AppPage.NoGamesInstalled:
                    gameNameControl.UpdateText("No games installed");
                    panel_noGames.Visible = true;
                    break;
                case AppPage.GameHandler:
                    panel_game.Visible = true;
                    break;
                case AppPage.PackageManager:
                    gameNameControl.UpdateText("Package Manager");
                    panel_handlerManager.Visible = true;
                    break;
            }
        }

        public void NewGameHandler(GameHandlerMetadata metadata)
        {
            if (noGamesPresent)
            {
                noGamesPresent = false;
                RefreshGames();
                return;
            }

            // get all Repository Game Infos
            this.combo_handlers.Items.Add(metadata);

            //HandlerControl con = new HandlerControl(metadata);
            //con.Width = list_Games.Width;
        }

        public void NewUserGame(UserGameInfo game)
        {
            if (!game.IsGamePresent())
            {
                return;
            }

            if (noGamesPresent)
            {
                noGamesPresent = false;
                RefreshGames();
                return;
            }

            // get all Repository Game Infos
            GameControl con = new GameControl(game);
            con.Width = list_games.Width;

            controls.Add(game, con);
            this.list_games.Controls.Add(con);

            ThreadPool.QueueUserWorkItem(GetIcon, game);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshGames();

            DPIManager.ForceUpdate();
        }

        private void GetIcon(object state)
        {
            UserGameInfo game = (UserGameInfo)state;
            Icon icon = Shell32Interop.GetIcon(game.ExePath, false);

            Bitmap bmp = icon.ToBitmap();
            icon.Dispose();
            game.Icon = bmp;

            lock (controls)
            {
                if (controls.ContainsKey(game))
                {
                    GameControl control = controls[game];
                    control.Invoke((Action)delegate ()
                    {
                        control.Image = game.Icon;
                    });
                }
            }
        }

        private void list_Games_SelectedChanged(Control arg1, Control arg2)
        {
            selectedControl = (GameControl)arg1;

            if (selectedControl.UserGameInfo == null)
            {
                return;
            }

            panel_steps.Visible = true;

            UserGameInfo userGameInfo = selectedControl.UserGameInfo;
            string gameId = selectedControl.UserGameInfo.GameID;

            GameHandlerMetadata[] handlers = gameManager.RepoManager.GetInstalledHandlers(gameId);
            if (handlers.Length == 0)
            {
                // uninstalled package perhaps?
                return;
            }

            currentHandlers = handlers;

            combo_handlers.DataSource = handlers;
            combo_handlers.SelectedIndex = 0;
        }

        private void combo_Handlers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combo_handlers.SelectedIndex == -1)
            {
                return;
            }

            KillCurrentStep();

            if (handlerDataManager != null)
            {
                // dispose
                handlerDataManager.Dispose();
                handlerDataManager = null;
            }

            try
            {
                selectedHandler = currentHandlers[combo_handlers.SelectedIndex];
                handlerDataManager = gameManager.RepoManager.ReadHandlerDataFromInstalledPackage(selectedHandler);
                handlerData = handlerDataManager.HandlerData;

                btn_play.Enabled = false;

                stepsList = new List<UserInputControl>();
                stepsList.Add(positionsControl);
                stepsList.Add(optionsControl);
                if (handlerData.CustomSteps != null)
                {
                    for (int i = 0; i < handlerData.CustomSteps.Count; i++)
                    {
                        stepsList.Add(jsControl);
                    }
                }

                currentProfile = new GameProfile();
                currentProfile.InitializeDefault(handlerData);

                gameNameControl.GameInfo = selectedControl.UserGameInfo;

                GoToStep(0);
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
        }


        private void EnablePlay()
        {
            btn_play.Enabled = true;
        }

        private void StepCanPlay(UserControl obj, bool canProceed, bool autoProceed)
        {
            if (!canProceed)
            {
                btn_next.Enabled = false;
                return;
            }

            if (currentStepIndex + 1 > stepsList.Count - 1)
            {
                EnablePlay();
                return;
            }

            if (autoProceed)
            {
                GoToStep(currentStepIndex + 1);
            }
            else
            {
                btn_next.Enabled = true;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            GoToStep(currentStepIndex + 1);
        }

        private void KillCurrentStep()
        {
            currentStep?.Ended();
            this.panel_steps.Controls.Clear();
        }

        private void GoToStep(int step)
        {
            btn_previous.Enabled = step > 0;
            if (step >= stepsList.Count)
            {
                return;
            }

            if (step >= 2)
            {
                // Custom steps
                List<CustomStep> customSteps = handlerData.CustomSteps;
                int customStepIndex = step - 2;
                CustomStep customStep = customSteps[0];

                if (customStep.Required)
                {
                    jsControl.CustomStep = customStep;
                    jsControl.DataManager = handlerDataManager;
                }
                else
                {
                    EnablePlay();
                    return;
                }
            }

            KillCurrentStep();

            currentStepIndex = step;
            currentStep = stepsList[step];
            currentStep.Size = panel_steps.Size;
            currentStep.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

            currentStep.Initialize(handlerData, selectedControl.UserGameInfo, currentProfile);

            btn_next.Enabled = currentStep.CanProceed && step != stepsList.Count - 1;

            panel_steps.Controls.Add(currentStep);
            currentStep.Size = panel_steps.Size; // for some reason this line must exist or the PositionsControl get messed up

            lbl_stepTitle.Text = currentStep.Title;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            formClosing = true;
            if (handler != null)
            {
                handler.End();
            }
        }

        private void Overlay_OnStop()
        {
            overlay.DisableOverlay();

            if (handler != null)
            {
                handler.End();
                return;
            }
        }

        private void btn_Play_Click(object sender, EventArgs e)
        {
            this.overlay.EnableOverlay(this);

            handler = new GameHandler();
            handler.Initialize(handlerDataManager, selectedControl.UserGameInfo, GameProfile.CleanClone(currentProfile));
            handler.Ended += handler_Ended;

            gameManager.Play(handler);
            if (handlerData.HandlerInterval > 0)
            {
                handlerThread = new Thread(UpdateGameManager);
                handlerThread.Start();
            }

            WindowState = FormWindowState.Minimized;
        }

        private void handler_Ended()
        {
            handler = null;
            if (handlerThread != null)
            {
                handlerThread.Abort();
                handlerThread = null;
            }
        }

        private void UpdateGameManager(object state)
        {
            for (; ; )
            {
                try
                {
                    if (gameManager == null || formClosing || handler == null)
                    {
                        break;
                    }

                    string error = gameManager.Error;
                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        handler_Ended();
                        return;
                    }

                    handler.Tick(handlerData.HandlerInterval);
                    Thread.Sleep(TimeSpan.FromMilliseconds(handlerData.HandlerInterval));
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch { }
            }
        }

        private void arrow_Back_Click(object sender, EventArgs e)
        {
            currentStepIndex--;
            if (currentStepIndex < 0)
            {
                currentStepIndex = 0;
                return;
            }
            GoToStep(currentStepIndex);
        }

        private void arrow_Next_Click(object sender, EventArgs e)
        {
            currentStepIndex = Math.Min(currentStepIndex++, stepsList.Count - 1);
            GoToStep(currentStepIndex);
        }

        private void btn_Browse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog open = new OpenFileDialog())
            {
                open.Filter = "Game Executable Files|*.exe";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    string path = open.FileName;

                    List<GameHandlerMetadata> allGames = gameManager.User.InstalledHandlers;

                    GameList list = new GameList(allGames);
                    DPIManager.ForceUpdate();

                    if (list.ShowDialog() == DialogResult.OK)
                    {
                        GameHandlerMetadata selected = list.Selected;
                        UserGameInfo game = gameManager.TryAddGame(path, list.Selected);

                        if (game == null)
                        {
                            MessageBox.Show("Game already in your library!");
                        }
                        else
                        {
                            MessageBox.Show("Game accepted as ID " + game.GameID);
                            RefreshGames();
                        }
                    }
                }
            }
        }

        private void btnAutoSearch_Click(object sender, EventArgs e)
        {
            if (form != null)
            {
                return;
            }

            form = new SearchDisksForm(this);
            //DPIManager.AddForm(form);

            form.FormClosed += Form_FormClosed;
            form.Show();
            //SetUpForm(form);

            DPIManager.ForceUpdate();
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            form = null;
        }

        private void btnShowTaskbar_Click(object sender, EventArgs e)
        {
            User32Util.ShowTaskBar();
        }

        private void btn_Install_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog open = new OpenFileDialog())
            {
                open.Multiselect = true;
                open.Filter = "Nucleus Coop Package Files|*.nc";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    string[] paths = open.FileNames;
                    for (int i = 0; i < paths.Length; i++)
                    {
                        gameManager.RepoManager.InstallPackage(paths[i]);
                    }

                    RefreshGames();
                }
            }
        }

        private void imgBtn_handlers_Click(object sender, EventArgs e)
        {
            if (pkgManager != null)
            {
                if (!pkgManager.IsDisposed)
                {
                    return;
                }
            }

            pkgManager = new HandlerManagerForm(this.apiConnection);
            pkgManager.Show();
        }
    }
}
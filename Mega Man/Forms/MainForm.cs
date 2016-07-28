﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using MegaMan.Engine.Forms;
using MegaMan.IO.Xml;

namespace MegaMan.Engine
{
    public partial class MainForm : Form
    {
        #region Variables And Constants
        #region Variables
        private string settingsPath, currentGame, lastGameWithPath;
        private int widthZoom, heightZoom, width, height;
        private bool fullScreenToolStripMenuItem_IsMaximized;
        
        private bool menu, altKeyDown, gotFocus; // menu is either used when context menu or title bar menu is opened
                                                 // altKeyDown is exclusively used to know if it is the menu bar is activated by alt key

        ToolStripMenuItem previousScreenSizeSelection; // Remember previous screen selection to fullscreen option. Then when fullscreen is quitted, it goes back to this option

        public static bool pauseEngine;
        #endregion

        #region Constants
        private readonly CustomNtscForm customNtscForm = new CustomNtscForm();
        private readonly Keyboard keyform = new Keyboard();

        #region Code used by windows messages
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_INITMENUPOPUP = 0x0117;
        private const int WM_UNINITMENUPOPUP = 0x0125;
        #endregion
        #endregion
        #endregion

        #region Handle Engine pausing
        // Lots of functions used to determine what is happening and set engine activated/deactivated state

        /// <summary>
        /// Function which is called by events, checks conditions to know if engine is active/unactive
        /// </summary>
        private void HandleEngineActivation()
        {
            altKeyDown = false;

            if (menu || gotFocus == false || WindowState == FormWindowState.Minimized) Engine.Instance.Stop();
            else
            {
                Engine.Instance.Start();
                menu = false;   // If here, no more focus or Window is minimized, so menu is closed for surre
            }
        }

        /// <summary>
        /// A function is made because the event needs to be "triggered" at other places
        /// </summary>
        private void OnResizeCode()
        {
            if (!screenToolStripMenuItem.Pressed)
            {
                menu = false;
                HandleEngineActivation();
            }
        }

        /// <summary>
        /// Only event to be called when minimizing by clicking on tray icon.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            OnResizeCode();
        }

        /// <summary>
        /// Called on focus lost.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);

            gotFocus = menu = false; // Menu is sure to be closed
            HandleEngineActivation();
        }

        /// <summary>
        /// Called on focus
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            gotFocus = true;
            HandleEngineActivation();
        }

        /// <summary>
        /// Called on moving form
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);

            menu = false; // Menu is sure to be closed.
            Engine.Instance.Stop();
        }

        /// <summary>
        /// Used because it is called when moving is stopped.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);

            menu = false;
            gotFocus = true;
            Engine.Instance.Start();
        }

        /// <summary>
        /// If user happens to find a way to have game deactivated when it shouldn't be, user will have reflex to click
        /// the game image. So when it is clicked, we must restard engine.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xnaImage_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.XButton1 && e.Button != MouseButtons.XButton2)
            {
                menu = false;
                gotFocus = true;
                Engine.Instance.Start();
            }
        }

        /// <summary>
        /// Menu was selected but is no more
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStrip1_MenuDeactivate(object sender, EventArgs e)
        {
            menu = false;
            HandleEngineActivation();
        }

        /// <summary>
        /// Menubar is activated. It can be by a mouse click (any), alt key, etc...
        /// To prevent some problems, it is only used by alt key press.
        /// To know if this key is pressed, we check it using ProcessCmdKey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStrip1_MenuActivate(object sender, EventArgs e)
        {
            if (altKeyDown) menu = true;
            HandleEngineActivation();
        }

        /// <summary>
        /// Call by every menu clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStrip_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                menu = true;
                HandleEngineActivation();
            }
        }

        /// <summary>
        /// If a menu is dropped down, make sure engine is stopped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            menu = true;
            HandleEngineActivation();

            try
            {
                gravityFlipToolStripMenuItem.Checked = Game.CurrentGame.GetFlipGravity();
            }
            catch (Exception)
            {
                gravityFlipToolStripMenuItem.Checked = false;
            }
        }

        /// <summary>
        /// Interrupt Windows messages. Used to know if context menu is opened.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INITMENUPOPUP)
            {
                menu = true;
                HandleEngineActivation();
            }
            else if (m.Msg == WM_UNINITMENUPOPUP)
            {
                menu = false;
                HandleEngineActivation();
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Used to know if alt key is pressed.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((msg.Msg == WM_SYSKEYDOWN))
            {
                if (keyData == (Keys.Menu | Keys.Alt))
                {
                    altKeyDown = true;
                    if (fullScreenToolStripMenuItem.Checked)
                    {
                        menuStrip1.Visible = !menuStrip1.Visible;
                    }
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        /// <summary>
        /// Unpause engine.
        /// </summary>
        /// <remarks>
        /// This function is slightly different from when engine is off from pauseEngineToolStripMenuItem_Click.
        /// When engine is off and pauseEngineToolStripMenuItem_Click is called, it also call Engine.Instance.Start. pauseOff doesn't.
        /// pauseOff is for a case Engine.Instance.Start() is gonna be called but by something else. It's not good to keep calling Engine.Instance.Start when it is already started.
        /// </remarks>
        private void pauseOff()
        {
            if (pauseEngineToolStripMenuItem.Checked == true)
            {
                pauseEngineToolStripMenuItem.Checked = pauseEngine = false;

                if (Engine.Instance.SoundSystem.MusicEnabled != musicMenuItem.Checked)
                {
                    Engine.Instance.SoundSystem.MusicEnabled = musicMenuItem.Checked;
                }
            }
        }
        #endregion

        #region Form Events Openings/Closings
        public MainForm()
        {
            InitializeComponent();

            menu = gotFocus = altKeyDown = false;
            defaultConfigToolStripMenuItem.Checked = true;
            currentGame = "";
            lastGameWithPath = null;

#if !DEBUG
            debugBar.Hide();
            debugBar.Height = 0;
            menuStrip1.Items.Remove(debugToolStripMenuItem);
#endif

            widthZoom = heightZoom = 1;
            DefaultScreen();
            xnaImage.SetSize();

            Game.ScreenSizeChanged += Game_ScreenSizeChanged;
            Engine.Instance.GameLogicTick += Instance_GameLogicTick;

            Engine.Instance.OnException += Engine_Exception;

            customNtscForm.Apply += customNtscForm_ApplyFromForm;
            keyform.FormClosed += (s, e) => AutosaveConfig(null);
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Hide();

            try
            {
                string autoLoadGame = GetAutoLoadGame();
                var args = Environment.GetCommandLineArgs();

                base.OnLoad(e);

                if (args.Length > 1)
                {
                    var path = args[1];
                    var start = (args.Length > 2) ? args[2] : null;

                    LoadGame(path, args.Skip(2).ToList());
                }

                try
                {
                    LoadGlobalConfigValues();
                    LoadConfigFromXMLOrDefaultOneIfInvalidXML();

                    if (autoLoadGame != null)
                    {
                        if (!LoadGame(autoLoadGame, null, true))
                        {
                            // Game we try to autoload failed. Now set autoload to when no game is loaded
                            autoloadToolStripMenuItem.Checked = true;
                            SaveGlobalConfigValues();
                        }
                        else
                        {
                            OnGameLoaded();
                        }
                    }
                }
                catch (Exception x)
                {
                    MessageBox.Show(x.Message); // If a line in config file is wrong, this is gonna tell user.
                    MessageBox.Show("The config file could was not loaded successfully.");
                }
            }
            catch (Exception)
            {
            }
            this.Show();
        }

        /// <summary>
        /// Things to do when closing engine, no matter the way
        /// </summary>
        public void close()
        {
            AutosaveConfig();
            if (Game.CurrentGame != null) Game.CurrentGame.Unload();
        }

        protected override void OnClosed(EventArgs e)
        {
            close();
            base.OnClosed(e);
        }
        #endregion

        #region Menus
        #region File Menu
        #region First section
        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                AutosaveConfig();

                pauseOff();

                LoadGame(dialog.FileName);
            }
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Game.CurrentGame != null)
            {
                pauseOff();

                Game.CurrentGame.Reset();
                OnGameLoaded();
            }
        }

        #region Close Game
        private void CloseGame()
        {
            if (Game.CurrentGame != null)
            {
                AutosaveConfig();
                currentGame = "";
                lastGameWithPath = null;
                LoadConfigFromXML();

                Game.CurrentGame.Unload();
                this.xnaImage.Clear();
                Text = "Mega Man";

                OnGameLoadedChanged();
            }
        }

        private void closeGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseGame();
        }
        #endregion

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            close();
            Application.Exit();
        }
        #endregion

        #region Second section
        /// <summary>
        /// Pause engine or restart it depending on previous state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pauseEngineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pauseEngineToolStripMenuItem.Checked = !pauseEngineToolStripMenuItem.Checked;

            pauseEngine = pauseEngineToolStripMenuItem.Checked;

            if (pauseEngine) Engine.Instance.Stop();
            else
            {
                Engine.Instance.Start();
                if (Engine.Instance.SoundSystem.MusicEnabled != musicMenuItem.Checked)
                {
                    Engine.Instance.SoundSystem.MusicEnabled = musicMenuItem.Checked;
                }
            }
        }
        #endregion

        #region Third Section
        /// <summary>
        /// If true, on events where saving happens, no save (except if menu to save is picked).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autosaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autosaveToolStripMenuItem.Checked = !autosaveToolStripMenuItem.Checked;
            SaveGlobalConfigValues();
        }

        /// <summary>
        /// If defaultConfigToolStripMenuItem unchecked, uses a config specific to a game name in xml file for configs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void defaultConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutosaveConfig();
            defaultConfigToolStripMenuItem.Checked = !defaultConfigToolStripMenuItem.Checked;
            SaveGlobalConfigValues();
            LoadConfigFromXML();

            SetLayersVisibilityFromSettings();
        }

        private void saveConfigurationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveConfig();
        }
        #endregion

        #region Fourth Section
        /// <summary>
        /// True for a game it autoloads when application starts (if none, this option is checked when no game is loaded)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autoloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentGame == "") autoloadToolStripMenuItem.Checked = true;
            else
            {
                autoloadToolStripMenuItem.Checked = !autoloadToolStripMenuItem.Checked;
            }

            SaveGlobalConfigValues();
        }
        #endregion
        #endregion

        #region Input Menu
        private void keyboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            keyform.Show();
        }
        #endregion

        #region Screen Menu
        #region First Section
        /// <summary>
        /// 1X, 2X, 3X, 4X and NTSC are considered resolutions
        /// </summary>
        /// <param name="itemToKeepChecked"></param>
        private void AllScreenResolutionOffBut(ToolStripMenuItem itemToKeepChecked)
        {
            fullScreenToolStripMenuItem.Checked = screenNTSCMenu.Checked = false;
            screen1XMenu.Checked = screen2XMenu.Checked = screen3XMenu.Checked = screen4XMenu.Checked = false;

            itemToKeepChecked.Checked = true;
        }
        #region Size Selection
        private void ScreenSizeMultiple()
        {
            WindowState = FormWindowState.Normal;
            if (Game.CurrentGame == null)
            {
                DefaultScreen();
            }
            else
            {
                ResizeScreen();
            }

            xnaImage.NTSC = false;
        }

        /// <summary>
        /// Screen option X1, X2, X3, X4, they use mostly similar code.
        /// </summary>
        /// <param name="index"></param>
        private void screenSizeMenuSelected(int index)
        {
            Int16 ntscOption = -1;
            snes_ntsc_setup_t ntscOptionParameters = null;

            if (index == (Int16)UserSettingsEnums.Screen.NTSC)
            {
                #region NTSC
                if (ntscComposite.Checked)
                {
                    ntscOption = (Int16)UserSettingsEnums.NTSC_Options.Composite;
                    ntscOptionParameters = snes_ntsc_setup_t.snes_ntsc_composite;
                }
                else if (ntscSVideo.Checked)
                {
                    ntscOption = (Int16)UserSettingsEnums.NTSC_Options.S_Video;
                    ntscOptionParameters = snes_ntsc_setup_t.snes_ntsc_svideo;
                }
                else if (ntscRGB.Checked)
                {
                    ntscOption = (Int16)UserSettingsEnums.NTSC_Options.RGB;
                    ntscOptionParameters = snes_ntsc_setup_t.snes_ntsc_rgb;
                }
                else // (ntscCustom.Checked)
                {
                    ntscOption = (Int16)UserSettingsEnums.NTSC_Options.Custom;
                    ntscOptionParameters = NtscCustomFromForm();
                }

                if (ntscOption == -1 || ntscOptionParameters == null)
                {
                    // This case is a programming error, alert!
                    // If NTSC is selected, 3 parameters must be sent to current function.
                    Programming_Error_No_Shutdown("screenSizeMenuSelected called, NTSC selected, 3 parameters should be sent. ntscOption was " + ntscOption.ToString() + " ntscOptionParameters was " + ntscOptionParameters + ".");
                    return;
                }

                ntscOptionSet(ntscOption, ntscOptionParameters);
                #endregion
            }
            if (index == (Int16)UserSettingsEnums.Screen.X1)
            {
                previousScreenSizeSelection = screen1XMenu;
                widthZoom = heightZoom = 1;
                ScreenSizeMultiple();
                AllScreenResolutionOffBut(previousScreenSizeSelection);
            }
            if (index == (Int16)UserSettingsEnums.Screen.X2)
            {
                previousScreenSizeSelection = screen2XMenu;
                widthZoom = heightZoom = 2;
                ScreenSizeMultiple();
                AllScreenResolutionOffBut(previousScreenSizeSelection);
            }
            if (index == (Int16)UserSettingsEnums.Screen.X3)
            {
                previousScreenSizeSelection = screen3XMenu;
                widthZoom = heightZoom = 3;
                ScreenSizeMultiple();
                AllScreenResolutionOffBut(previousScreenSizeSelection);
            }
            if (index == (Int16)UserSettingsEnums.Screen.X4)
            {
                previousScreenSizeSelection = screen4XMenu;
                widthZoom = heightZoom = 4;
                ScreenSizeMultiple();
                AllScreenResolutionOffBut(previousScreenSizeSelection);
            }
        }

        private void screen1XMenu_Click(object sender, EventArgs e)
        {
            screenSizeMenuSelected((Int16)UserSettingsEnums.Screen.X1);
        }

        private void screen2XMenu_Click(object sender, EventArgs e)
        {
            screenSizeMenuSelected((Int16)UserSettingsEnums.Screen.X2);
        }

        private void screen3XMenu_Click(object sender, EventArgs e)
        {
            screenSizeMenuSelected((Int16)UserSettingsEnums.Screen.X3);
        }

        private void screen4XMenu_Click(object sender, EventArgs e)
        {
            screenSizeMenuSelected((Int16)UserSettingsEnums.Screen.X4);
        }

        #region NTSC Menu Clicked
        private void screenNTSCSelected()
        {
            previousScreenSizeSelection = screenNTSCMenu;

            if (width != 256 || height != 224) return;

            widthZoom = heightZoom = 1;
            ResizeScreen(602, 448);

            AllScreenResolutionOffBut(screenNTSCMenu);
            xnaImage.NTSC = true;
        }

        private void screenNTSCMenu_Click(object sender, EventArgs e)
        {
            screenSizeMenuSelected((Int16)UserSettingsEnums.Screen.NTSC);
        }
        #endregion
        #endregion

        #region NTSC Submenu
        #region NTSC Custom Functions
        /// <summary>
        /// Functions that prepares snes_ntsc_setup_t parameter and make function call to apply changes
        /// Build a snes_ntsc_setup_t item from form parameters
        /// </summary>
        private void customNtscForm_ApplyFromForm()
        {
            ntscOptionCode(ntscCustom, NtscCustomFromForm());

            AutosaveConfig();
        }

        private snes_ntsc_setup_t NtscCustomFromForm()
        {
            return new snes_ntsc_setup_t(customNtscForm.Hue, customNtscForm.Saturation, customNtscForm.Contrast, customNtscForm.Brightness,
                            customNtscForm.Sharpness, customNtscForm.Gamma, customNtscForm.Resolution, customNtscForm.Artifacts, customNtscForm.Fringing, customNtscForm.Bleed, true);
        }
        #endregion

        #region NTSC setting the option (once snes_ntsc_setup_t variable is build)
        private void NTSC_OptionsOffBut(ToolStripMenuItem itemToKeepChecked)
        {
            ntscComposite.Checked = ntscSVideo.Checked = ntscRGB.Checked = ntscCustom.Checked = false;

            itemToKeepChecked.Checked = true;
        }

        private void ntscOptionCode(ToolStripMenuItem menuClicked, snes_ntsc_setup_t snes_ntsc_type, bool setOption = true)
        {
            NTSC_OptionsOffBut(menuClicked);

            if (setOption)
            {
                screenNTSCSelected();
                xnaImage.ntscInit(snes_ntsc_type);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="NTSC_Option"></param>
        /// <param name="customParameters"></param>
        /// <param name="setOption">If not set, we only check the option</param>
        private void ntscOptionSet(Int16 NTSC_Option, snes_ntsc_setup_t ntscOption, bool setOption = true)
        {
            // Set parameters of Custom options
            if (NTSC_Option == (Int16)UserSettingsEnums.NTSC_Options.Composite) ntscOptionCode(ntscComposite, ntscOption, setOption);
            if (NTSC_Option == (Int16)UserSettingsEnums.NTSC_Options.S_Video) ntscOptionCode(ntscSVideo, ntscOption, setOption);
            if (NTSC_Option == (Int16)UserSettingsEnums.NTSC_Options.RGB) ntscOptionCode(ntscRGB, ntscOption, setOption);
            if (NTSC_Option == (Int16)UserSettingsEnums.NTSC_Options.Custom) ntscOptionCode(ntscCustom, ntscOption, setOption);
        }
        #endregion

        #region Button Click event of NTSC options
        private void ntscComposite_Click(object sender, EventArgs e)
        {
            ntscOptionSet((Int16)UserSettingsEnums.NTSC_Options.Composite, snes_ntsc_setup_t.snes_ntsc_composite);
        }

        private void ntscSVideo_Click(object sender, EventArgs e)
        {
            ntscOptionSet((Int16)UserSettingsEnums.NTSC_Options.S_Video, snes_ntsc_setup_t.snes_ntsc_svideo);
        }

        private void ntscRGB_Click(object sender, EventArgs e)
        {
            ntscOptionSet((Int16)UserSettingsEnums.NTSC_Options.RGB, snes_ntsc_setup_t.snes_ntsc_rgb);
        }

        private void ntscCustom_Click(object sender, EventArgs e)
        {
            customNtscForm.Show();
        }
        #endregion
        #endregion

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fullScreenToolStripMenuItem.Checked = !fullScreenToolStripMenuItem.Checked;

            if (fullScreenToolStripMenuItem.Checked)
            {
                fullScreenToolStripMenuItem_IsMaximized = (this.WindowState == FormWindowState.Maximized) ? true : false;

                AllScreenResolutionOffBut(fullScreenToolStripMenuItem);
                xnaImage.NTSC = false;
                this.TopMost = true;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                menuStrip1.Visible = false;
#if DEBUG
                debugBar.Visible = false;
#endif
            }
            else
            {
                AllScreenResolutionOffBut(previousScreenSizeSelection);
                this.TopMost = false;
                this.FormBorderStyle = FormBorderStyle.Sizable;

                if (fullScreenToolStripMenuItem_IsMaximized) this.WindowState = FormWindowState.Maximized;
                else this.WindowState = FormWindowState.Normal;

                menuStrip1.Visible = !hideMenuItem.Checked;
#if DEBUG
                debugBar.Visible = true;
#endif

                // NTSC has special specifications
                if (previousScreenSizeSelection.Name == screenNTSCMenu.Name)
                {
                    screenNTSCMenu_Click(sender, e);
                }
            }
        }
        #endregion

        #region Second Section
        #region Pixellated/Smoothed Section
        private void pixellatedVsSmoothedAllOffBut(ToolStripMenuItem itemToKeepChecked)
        {
            smoothedToolStripMenuItem.Checked = pixellatedToolStripMenuItem.Checked = false;

            itemToKeepChecked.Checked = true;
        }

        /// <summary>
        /// Function that execute code for Smoothed/Pixellated selection
        /// </summary>
        /// <param name="itemToKeepChecked"></param>
        /// <param name="samplerState"></param>
        private void pixellatedVsSmoothedCode(Int32 index)
        {
            if (index == (Int32)UserSettingsEnums.PixellatedOrSmoothed.Pixellated)
            {
                Engine.Instance.FilterState = Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp;
                pixellatedVsSmoothedAllOffBut(pixellatedToolStripMenuItem);
            }
            else if (index == (Int32)UserSettingsEnums.PixellatedOrSmoothed.Smoothed)
            {
                Engine.Instance.FilterState = Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp;
                pixellatedVsSmoothedAllOffBut(smoothedToolStripMenuItem);
            }
        }

        private void smoothedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pixellatedVsSmoothedCode((Int16)UserSettingsEnums.PixellatedOrSmoothed.Smoothed);
        }

        private void pixellatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pixellatedVsSmoothedCode((Int16)UserSettingsEnums.PixellatedOrSmoothed.Pixellated);
        }
        #endregion
        #endregion

        #region Third Section
        #region Code for Hide Menu
        private void hideMenu(bool hideMenu)
        {
            if (hideMenu)
            {
                hideMenuItem.Checked = true;
                Height -= menuStrip1.Height;
                menuStrip1.Visible = false;
            }
            else
            {
                hideMenuItem.Checked = false;
                menuStrip1.Visible = true;
                Height += menuStrip1.Height;
            }
        }

        private void hideMenuItem_Click(object sender, EventArgs e)
        {
            hideMenu(!hideMenuItem.Checked);
        }
        #endregion

        private void screenshotMenuItem_Click(object sender, EventArgs e)
        {
            var capDir = Path.Combine(Application.StartupPath, "screenshots");
            if (!Directory.Exists(capDir)) System.IO.Directory.CreateDirectory(capDir);

            string capPath;
            int capNum = 1;

            do
            {
                capPath = Path.Combine(capDir, String.Format("{0}.png", capNum));
                capNum++;
            } while (File.Exists(capPath));

            using (var stream = File.OpenWrite(capPath))
            {
                xnaImage.SaveCap(stream);
            }
        }
        #endregion
        #endregion
        
        #region Audio Menu
        #region First Section
        private void setMusic(bool value)
        {
            if (!pauseEngine) Engine.Instance.SoundSystem.MusicEnabled = musicMenuItem.Checked = value;
        }

        private void musicMenuItem_Click(object sender, EventArgs e)
        {
            setMusic(!musicMenuItem.Checked);
        }

        private void setSFX(bool value)
        {
            Engine.Instance.SoundSystem.SfxEnabled = sfxMenuItem.Checked = value; ;
        }

        private void sfxMenuItem_Click(object sender, EventArgs e)
        {
            setSFX(!sfxMenuItem.Checked);
        }
        #endregion

        #region Second Section
        private void setSq1(bool value)
        {
            Engine.Instance.SoundSystem.SquareOne = sq1MenuItem.Checked = value;
        }

        private void sq1MenuItem_Click(object sender, EventArgs e)
        {
            setSq1(!sq1MenuItem.Checked);
        }

        private void setSq2(bool value)
        {
            Engine.Instance.SoundSystem.SquareTwo = sq2MenuItem.Checked = value;
        }

        private void sq2MenuItem_Click(object sender, EventArgs e)
        {
            setSq2(!sq2MenuItem.Checked);
        }

        private void setTri(bool value)
        {
            Engine.Instance.SoundSystem.Triangle = triMenuItem.Checked = value;
        }

        private void triMenuItem_Click(object sender, EventArgs e)
        {
            setTri(!triMenuItem.Checked);
        }

        private void setNoise(bool value)
        {
            Engine.Instance.SoundSystem.Noise = noiseMenuItem.Checked = value;
        }

        private void noiseMenuItem_Click(object sender, EventArgs e)
        {
            setNoise(!noiseMenuItem.Checked);
        }
        #endregion

        #region Third Section
        #region Volume
        public void SetVolume(int value)
        {
            Engine.Instance.Volume = value;
        }

        private void increaseVolumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetVolume(Engine.Instance.Volume + 1);
        }

        private void decreaseVolumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetVolume(Engine.Instance.Volume - 1);
        }
        #endregion
        #endregion

        #endregion

        #region Debug Menu

        #region Debug Menu
        #region First Section
        private void setDebugBar(bool value)
        {
            debugBar.Visible = value;
            Height += debugBar.Height * (debugBar.Visible ? 1 : -1);
            debugBarToolStripMenuItem.Checked = value;
        }

        private void debugBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setDebugBar(!debugBarToolStripMenuItem.Checked);
        }
        #endregion

        #region Second Section
        private void setShowHitBoxes(bool value)
        {
            showHitboxesToolStripMenuItem.Checked = Engine.Instance.DrawHitboxes = value;
        }

        private void showHitboxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setShowHitBoxes(!showHitboxesToolStripMenuItem.Checked);
        }

        #region Cheat Submenu
        private void SetNoDamage(bool value)
        {
            noDamageToolStripMenuItem.Checked = Engine.Instance.NoDamage = value;
        }

        private void noDamageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetNoDamage(!noDamageToolStripMenuItem.Checked);
        }

        private void setInvincibility(bool value)
        {
            invincibilityToolStripMenuItem.Checked = Engine.Instance.Invincible = value;
        }

        private void invincibilityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setInvincibility(!invincibilityToolStripMenuItem.Checked);
        }

        private void gravityFlipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Game.CurrentGame == null) return;
            gravityFlipToolStripMenuItem.Checked = Game.CurrentGame.DebugFlipGravity();
        }

        private void emptyHealthMenuItem_Click(object sender, EventArgs e)
        {
            if (Game.CurrentGame != null)
            {
                if (Engine.Instance.Invincible)
                {
                    Engine.Instance.Invincible = false;
                    Game.CurrentGame.DebugEmptyHealth();
                    Engine.Instance.Invincible = true;
                }
                else Game.CurrentGame.DebugEmptyHealth();
            }
        }
        private void fillHealthMenuItem_Click(object sender, EventArgs e)
        {
            if (Game.CurrentGame != null)
            {
                Game.CurrentGame.DebugFillHealth();
            }
        }

        private void emptyWeaponMenuItem_Click(object sender, EventArgs e)
        {
            if (Game.CurrentGame != null)
            {
                Game.CurrentGame.DebugEmptyWeapon();
            }
        }

        private void fillWeaponMenuIem_Click(object sender, EventArgs e)
        {
            if (Game.CurrentGame != null)
            {
                Game.CurrentGame.DebugFillWeapon();
            }
        }
        #endregion

        #region Layer Submenu
        private void setLayerVisibility(Int16 index, bool value)
        {
            if (index == (Int16)UserSettingsEnums.Layers.Background)
            {
                Engine.Instance.SetLayerVisibility(index, value);
                backgroundToolStripMenuItem.Checked = value;
            }
            else if (index == (Int16)UserSettingsEnums.Layers.Sprite1)
            {
                Engine.Instance.SetLayerVisibility(index, value);
                sprites1ToolStripMenuItem.Checked = value;
            }
            else if (index == (Int16)UserSettingsEnums.Layers.Sprite2)
            {
                Engine.Instance.SetLayerVisibility(index, value);
                sprites2ToolStripMenuItem.Checked = value;
            }
            else if (index == (Int16)UserSettingsEnums.Layers.Sprite3)
            {
                Engine.Instance.SetLayerVisibility(index, value);
                sprites3ToolStripMenuItem.Checked = value;
            }
            else if (index == (Int16)UserSettingsEnums.Layers.Sprite4)
            {
                Engine.Instance.SetLayerVisibility(index, value);
                sprites4ToolStripMenuItem.Checked = value;
            }
            else if (index == (Int16)UserSettingsEnums.Layers.Foreground)
            {
                Engine.Instance.SetLayerVisibility(index, value);
                foregroundToolStripMenuItem.Checked = value;
            }
        }

        private void backgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Background, !backgroundToolStripMenuItem.Checked);
        }

        private void sprites1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite1, !sprites1ToolStripMenuItem.Checked);
        }

        private void sprites2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite2, !sprites2ToolStripMenuItem.Checked);
        }

        private void sprites3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite3, !sprites3ToolStripMenuItem.Checked);
        }

        private void sprites4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite4, !sprites4ToolStripMenuItem.Checked);
        }

        private void foregroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Foreground, !foregroundToolStripMenuItem.Checked);
        }

        private void activateAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!backgroundToolStripMenuItem.Checked) backgroundToolStripMenuItem_Click(sender, e);
            if (!sprites1ToolStripMenuItem.Checked) sprites1ToolStripMenuItem_Click(sender, e);
            if (!sprites2ToolStripMenuItem.Checked) sprites2ToolStripMenuItem_Click(sender, e);
            if (!sprites3ToolStripMenuItem.Checked) sprites3ToolStripMenuItem_Click(sender, e);
            if (!sprites4ToolStripMenuItem.Checked) sprites4ToolStripMenuItem_Click(sender, e);
            if (!foregroundToolStripMenuItem.Checked) foregroundToolStripMenuItem_Click(sender, e);
        }
        #endregion
        #endregion

        #region Third Section

        #region Framerate Setting
        private void SetFrameRate(int framerate)
        {
            Engine.Instance.FPS = framerate;

            if (Engine.Instance.FPS < Constants.EngineProperties.FramerateMin) Engine.Instance.FPS = Constants.EngineProperties.FramerateMin;
            if (Engine.Instance.FPS > Constants.EngineProperties.FramerateMax) Engine.Instance.FPS = Constants.EngineProperties.FramerateMax;

            fpsCapLabel.Text = "FPS Cap: " + Engine.Instance.FPS;
        }

        private void framerateUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetFrameRate(Engine.Instance.FPS + 10);
        }

        private void framerateDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetFrameRate(Engine.Instance.FPS - 10);
        }

        private void defaultFramerateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetFrameRate(ConfigFilesDefaultValues.Framerate);
        }
        #endregion

        #endregion
        #endregion

        #endregion
        #endregion

        #region Functions Used By Many
        private bool LoadGame(string path, List<string> pathArgs = null, bool silenceErrorMessages = false)
        {
            try
            {
                Game.Load(path, pathArgs);
                Text = Game.CurrentGame.Name;

                lastGameWithPath = path;
                currentGame = Path.GetFileName(path);
                LoadConfigFromXML();

                OnGameLoadedChanged();

                return true;
            }
            catch (GameXmlException ex)
            {
                if (silenceErrorMessages == false)
                {
                    // this builds a dialog message to tell the user where the error is in the XML file

                    StringBuilder message = new StringBuilder("There is an error in one of your game files.\n\n");
                    if (ex.File != null) message.Append("File: ").Append(ex.File).Append('\n');
                    if (ex.Line != 0) message.Append("Line: ").Append(ex.Line.ToString()).Append('\n');
                    if (ex.Entity != null) message.Append("Entity: ").Append(ex.Entity).Append('\n');
                    if (ex.Tag != null) message.Append("Tag: ").Append(ex.Tag).Append('\n');
                    if (ex.Attribute != null) message.Append("Attribute: ").Append(ex.Attribute).Append('\n');

                    message.Append("\n").Append(ex.Message);

                    MessageBox.Show(message.ToString(), "Game Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Game.CurrentGame.Unload();
            }
            catch (System.IO.FileNotFoundException ex)
            {
                if (silenceErrorMessages == false)
                {
                    MessageBox.Show("I'm sorry, I couldn't the following file. Perhaps the file path is incorrect?\n\n" + ex.Message, "C# MegaMan Engine", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Game.CurrentGame.Unload();
            }
            catch (XmlException ex)
            {
                if (silenceErrorMessages == false)
                {
                    MessageBox.Show("Your XML is badly formatted.\n\nFile: " + ex.SourceUri + "\n\nError: " + ex.Message, "C# MegaMan Engine", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Game.CurrentGame.Unload();
            }
            catch (Exception ex)
            {
                if (silenceErrorMessages == false)
                {
                    MessageBox.Show("There was an error loading the game.\n\n" + ex.Message, "C# MegaMan Engine", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Game.CurrentGame.Unload();
            }

            this.OnActivated(new EventArgs());

            return false;
        }
        
        /// <summary>
        /// Function used when loading a game.
        /// When loading a new game, it needs to be set again because those properties are reset.
        /// </summary>
        private void SetLayersVisibilityFromSettings()
        {
            Engine.Instance.SetLayerVisibility(0, backgroundToolStripMenuItem.Checked);
            Engine.Instance.SetLayerVisibility(1, sprites1ToolStripMenuItem.Checked);
            Engine.Instance.SetLayerVisibility(2, sprites2ToolStripMenuItem.Checked);
            Engine.Instance.SetLayerVisibility(3, sprites3ToolStripMenuItem.Checked);
            Engine.Instance.SetLayerVisibility(4, sprites4ToolStripMenuItem.Checked);
            Engine.Instance.SetLayerVisibility(5, foregroundToolStripMenuItem.Checked);
        }

        #region Configs Functions
        /// <summary>
        /// Returns UserSettings build from XML.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>
        /// OK: 
        ///  - Constants.Errors.GetUserSettingsFromXML_NoError
        /// Error: 
        /// - Constants.Errors.GetUserSettingsFromXML_FileNotFound
        /// - Constants.Errors.GetUserSettingsFromXML_CannotDeserialize
        /// </returns>
        Int16 GetUserSettingsFromXML(ref UserSettings var, string fileName = null)
        {
            try
            {
                if (fileName == null) fileName = Constants.Paths.SettingFile;

                settingsPath = Path.Combine(Application.StartupPath, fileName);
                if (File.Exists(settingsPath))
                {
                    var serializer = new XmlSerializer(typeof(UserSettings));
                    using (var file = File.Open(settingsPath, FileMode.Open))
                    {
                        var = (UserSettings)serializer.Deserialize(file);
                    }
                }
                else return Constants.Errors.GetUserSettingsFromXML_FileNotFound;
            }
            catch (Exception)
            {
                var = null;
                return Constants.Errors.GetUserSettingsFromXML_CannotDeserialize;
            }

            return Constants.Errors.GetUserSettingsFromXML_NoError;
        }

        /// <summary>
        /// This is kind of a bad patch but found no better way to do it.
        /// OnMove function is used when user is moving the form, and OnResizeEnd is used for when he finish.
        /// However, when moving it with coordinates, OnMove is called, but not OnResizeEnd.
        /// So we restart the engine after the move if it was running.
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        private void ChangeFormLocation(int X, int Y)
        {
            bool running = Engine.Instance.IsRunning;

            this.Location = new System.Drawing.Point(X, Y);

            if (running) OnResizeCode();
        }

        private Setting GetDefaultConfig()
        {
            Setting settings = new Setting();

            settings.GameFileName = "";

            #region Input Menu: Keys
            settings.Keys.Up = Keys.Up;
            settings.Keys.Down = Keys.Down;
            settings.Keys.Left = Keys.Left;
            settings.Keys.Right = Keys.Right;
            settings.Keys.Jump = Keys.A;
            settings.Keys.Shoot = Keys.S;
            settings.Keys.Start = Keys.Enter;
            settings.Keys.Select = Keys.Shift;
            #endregion

            #region Screen Menu
            settings.Screens.Size = ConfigFilesDefaultValues.Size;
            settings.Screens.NTSC_Options = ConfigFilesDefaultValues.NTSC_Option;
            settings.Screens.Maximized = false;
            //settings.Screens.NTSC_Custom
            settings.Screens.Pixellated = ConfigFilesDefaultValues.PixellatedOrSmoothed;
            settings.Screens.HideMenu = false;
            #endregion

            #region Audio Menu
            settings.Audio.Volume = 0;
            settings.Audio.Musics = true;
            settings.Audio.Sound = true;
            settings.Audio.Square1 = true;
            settings.Audio.Square2 = true;
            settings.Audio.Triangle = true;
            settings.Audio.Noise = true;
            #endregion

            #region Debug Menu
            settings.Debug.ShowMenu = true;
            settings.Debug.ShowHitboxes = false;
            settings.Debug.Framerate = ConfigFilesDefaultValues.Framerate;
            //settings.Debug.Cheat
            //settings.Debug.Layers =
            #endregion

            #region Miscellaneous
            settings.Miscellaneous.ScreenX_Coordinate = 0;
            settings.Miscellaneous.ScreenY_Coordinate = 0;
            #endregion

            return settings;
        }

        private void LoadConfigFromSetting(Setting settings)
        {
            #region Input Menu: Keys
            GameInputKeys.Up = settings.Keys.Up;
            GameInputKeys.Down = settings.Keys.Down;
            GameInputKeys.Left = settings.Keys.Left;
            GameInputKeys.Right = settings.Keys.Right;
            GameInputKeys.Jump = settings.Keys.Jump;
            GameInputKeys.Shoot = settings.Keys.Shoot;
            GameInputKeys.Start = settings.Keys.Start;
            GameInputKeys.Select = settings.Keys.Select;
            #endregion

            #region Screen Menu
            // NTSC option is set before. So if menu selected is NTSC, options are set.
            if (!Enum.IsDefined(typeof(UserSettingsEnums.NTSC_Options), settings.Screens.NTSC_Options))
            {
                WrongConfigAlert(ConfigFileInvalidValuesMessages.NTSC_Option);
                settings.Screens.NTSC_Options = ConfigFilesDefaultValues.NTSC_Option;
            }
            ntscOptionSet(
                (Int16)settings.Screens.NTSC_Options,
                new snes_ntsc_setup_t(
                    settings.Screens.NTSC_Custom.Hue,
                    settings.Screens.NTSC_Custom.Saturation,
                    settings.Screens.NTSC_Custom.Contrast,
                    settings.Screens.NTSC_Custom.Brightness,
                    settings.Screens.NTSC_Custom.Sharpness,
                    settings.Screens.NTSC_Custom.Gamma,
                    settings.Screens.NTSC_Custom.Resolution,
                    settings.Screens.NTSC_Custom.Artifacts,
                    settings.Screens.NTSC_Custom.Fringing,
                    settings.Screens.NTSC_Custom.Bleed,
                    settings.Screens.NTSC_Custom.Merge_Fields
                ),
                false
                );

            customNtscForm.Hue = settings.Screens.NTSC_Custom.Hue;
            customNtscForm.Saturation = settings.Screens.NTSC_Custom.Saturation;
            customNtscForm.Contrast = settings.Screens.NTSC_Custom.Contrast;
            customNtscForm.Brightness = settings.Screens.NTSC_Custom.Brightness;
            customNtscForm.Sharpness = settings.Screens.NTSC_Custom.Sharpness;
            customNtscForm.Gamma = settings.Screens.NTSC_Custom.Gamma;
            customNtscForm.Resolution = settings.Screens.NTSC_Custom.Resolution;
            customNtscForm.Artifacts = settings.Screens.NTSC_Custom.Artifacts;
            customNtscForm.Fringing = settings.Screens.NTSC_Custom.Fringing;
            customNtscForm.Bleed = settings.Screens.NTSC_Custom.Bleed;

            if (!Enum.IsDefined(typeof(UserSettingsEnums.Screen), settings.Screens.Size))
            {
                WrongConfigAlert(ConfigFileInvalidValuesMessages.Size);
                settings.Screens.Size = ConfigFilesDefaultValues.Size;
            }
            screenSizeMenuSelected(settings.Screens.Size);

            if (!Enum.IsDefined(typeof(UserSettingsEnums.PixellatedOrSmoothed), settings.Screens.Pixellated))
            {
                WrongConfigAlert(ConfigFileInvalidValuesMessages.PixellatedOrSmoothed);
                settings.Screens.Pixellated = ConfigFilesDefaultValues.PixellatedOrSmoothed;
            }
            pixellatedVsSmoothedCode(settings.Screens.Pixellated);

            hideMenu(settings.Screens.HideMenu);

            if (settings.Screens.Maximized) WindowState = FormWindowState.Maximized;
            #endregion

            #region Audio Menu
            SetVolume(settings.Audio.Volume);
            setMusic(settings.Audio.Musics);
            setSFX(settings.Audio.Sound);
            // setSq1(settings.Audio.Square1);
            // setSq2(settings.Audio.Square2);
            // setTri(settings.Audio.Triangle);
            // setNoise(settings.Audio.Noise);
            #endregion

            #region Debug Menu
            setDebugBar(settings.Debug.ShowMenu);
            setShowHitBoxes(settings.Debug.ShowHitboxes);
            SetFrameRate(settings.Debug.Framerate);

            #region Cheats
            setInvincibility(settings.Debug.Cheat.Invincibility);
            SetNoDamage(settings.Debug.Cheat.NoDamage);
            #endregion

            #region Layers
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Background, settings.Debug.Layers.Background);
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite1, settings.Debug.Layers.Sprites1);
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite2, settings.Debug.Layers.Sprites2);
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite3, settings.Debug.Layers.Sprites3);
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Sprite4, settings.Debug.Layers.Sprites4);
            setLayerVisibility((Int16)UserSettingsEnums.Layers.Foreground, settings.Debug.Layers.Foreground);
            #endregion
            #endregion

            #region Miscellaneou
            ChangeFormLocation(settings.Miscellaneous.ScreenX_Coordinate, settings.Miscellaneous.ScreenY_Coordinate);
            #endregion
        }

        private string GetAutoLoadGame(string XML_fileName = null)
        {
            UserSettings userSettings = null;

            try
            {
                GetUserSettingsFromXML(ref userSettings, XML_fileName);

                return userSettings.Autoload;
            }
            catch (Exception) { }
            return null;
        }
        
        /// <summary>
         /// 
         /// </summary>
         /// <param name="XML_fileName"></param>
         /// <returns></returns>
         /// <remarks>Does no valiation, if crashes just doesn't set param</remarks>
        private void LoadGlobalConfigValues(string XML_fileName = null)
        {
            UserSettings userSettings = null;

            try
            {
                GetUserSettingsFromXML(ref userSettings, XML_fileName);

                autosaveToolStripMenuItem.Checked = userSettings.AutosaveSettings;
                defaultConfigToolStripMenuItem.Checked = userSettings.UseDefaultSettings;
                autoloadToolStripMenuItem.Checked = lastGameWithPath == userSettings.Autoload ? true : false;
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="XML_fileName"></param>
        /// <returns>
        /// OK: 
        ///  - Constants.Errors.GetUserSettingsFromXML_NoError
        /// Error: 
        /// - Constants.Errors.GetUserSettingsFromXML_FileNotFound
        /// - Constants.Errors.GetUserSettingsFromXML_CannotDeserialize
        /// - Constants.Errors.LoadConfigFromXML_NoContentReadFromXML
        /// - Constants.Errors.LoadConfigFromXML_NoDefaultValueInXML
        /// </returns>
        private Int16 LoadConfigFromXML(string XML_fileName = null)
        {
            UserSettings settingsArray = null;
            Setting settings = null;

            Int16 errorCode = GetUserSettingsFromXML(ref settingsArray, XML_fileName);

            if (errorCode != Constants.Errors.LoadConfigFromXML_NoError) return errorCode;

            if (settingsArray == null) return Constants.Errors.LoadConfigFromXML_NoContentReadFromXML;

            if (defaultConfigToolStripMenuItem.Checked) settings = settingsArray.GetSettingsForGame();
            else settings = settingsArray.GetSettingsForGame(currentGame);

            if (settings == null) return Constants.Errors.LoadConfigFromXML_NoDefaultValueInXML;
            
            // Here, there is a config. If a value is not valid, a default one is loaded instead
            // It displays messages in such case but always complete well
            LoadConfigFromSetting(settings);

            return Constants.Errors.LoadConfigFromXML_NoError;
        }

        private Int16 LoadConfigFromXMLOrDefaultOneIfInvalidXML(string XML_fileName = null)
        {
            Int16 errorCode = LoadConfigFromXML(XML_fileName);
            string newFileName = null, currentFileWithPath;
            Setting settings = null;

            if (XML_fileName == null) XML_fileName = Constants.Paths.SettingFile;
            currentFileWithPath = Path.Combine(Application.StartupPath, XML_fileName);

            if (errorCode == 0) return 0;

            #region Error when loading config from XML

            // If file is not readable, rename it to create a new one.
            if (errorCode == Constants.Errors.LoadConfigFromXML_CannotDeserialize)
            {
                WrongConfigAlert(ConfigFileInvalidValuesMessages.CannotDeserializeXML);

                // Will rename file to create a new one. So user can check it if he wants
                newFileName = "Bad_" + DateTime.Now.Day.ToString("00") + "_" + DateTime.Now.Month.ToString("00") + "_" + DateTime.Now.Year.ToString("0000") + "_" + DateTime.Now.Hour.ToString("00") + "_" + DateTime.Now.Minute.ToString("00") + "_" + DateTime.Now.Second.ToString("00") + ".xml";

                newFileName = Path.Combine(Application.StartupPath, newFileName);

                File.Move(currentFileWithPath, newFileName);
            }

            // If here, it was not possible to load a config from XML. Load a default one
            settings = GetDefaultConfig();
            LoadConfigFromSetting(settings);

            // Save the default config we just loaded
            SaveConfig(XML_fileName, settings);

            return 0;
            #endregion
        }

        #region Functions to build datas for saving config
        private Int32 currentSize()
        {
            if (screen2XMenu.Checked) return (Int32)UserSettingsEnums.Screen.X2;
            if (screen3XMenu.Checked) return (Int32)UserSettingsEnums.Screen.X3;
            if (screen4XMenu.Checked) return (Int32)UserSettingsEnums.Screen.X4;
            if (screenNTSCMenu.Checked) return (Int32)UserSettingsEnums.Screen.NTSC;

            return (Int32)UserSettingsEnums.Screen.X1;
        }

        private Int32 currentNTSC_Option()
        {
            if (ntscComposite.Checked) return (Int32)UserSettingsEnums.NTSC_Options.Composite;
            if (ntscSVideo.Checked) return (Int32)UserSettingsEnums.NTSC_Options.S_Video;
            if (ntscRGB.Checked) return (Int32)UserSettingsEnums.NTSC_Options.RGB;

            return (Int32)UserSettingsEnums.NTSC_Options.Custom;
        }

        public Int32 currentPixellatedOrSmoothedOption()
        {
            if (pixellatedToolStripMenuItem.Checked) return (Int32)UserSettingsEnums.PixellatedOrSmoothed.Pixellated;
            return (Int32)UserSettingsEnums.PixellatedOrSmoothed.Smoothed;
        }
        #endregion

        private void SaveToConfigXML(UserSettings userSettings, string fileName = null)
        {
            if (fileName == null) fileName = Constants.Paths.SettingFile;

            var serializer = new XmlSerializer(typeof(UserSettings));
            
            XmlTextWriter writer = new XmlTextWriter(settingsPath, null)
            {
                Indentation = 1,
                IndentChar = '\t',
                Formatting = Formatting.Indented
            };

            serializer.Serialize(writer, userSettings);

            writer.Close();
        }

        private void SaveGlobalConfigValues(string fileName = null)
        {
            UserSettings userSettings = null;

            // This functions updates settingsPath
            GetUserSettingsFromXML(ref userSettings);

            userSettings.AutosaveSettings = autosaveToolStripMenuItem.Checked;
            userSettings.UseDefaultSettings = defaultConfigToolStripMenuItem.Checked;
            userSettings.Autoload = autoloadToolStripMenuItem.Checked == true ? lastGameWithPath : null;

            SaveToConfigXML(userSettings, fileName);
        }

        private void AutosaveConfig(string fileName = null)
        {
            if (autosaveToolStripMenuItem.Checked) SaveConfig();
        }

        /// <summary>
        /// When engine is opening, every cases of bad files are handled, then file is locked.
        /// It means that calling this function after this call, it will always be valid since only program can modify it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="settings"></param>
        private void SaveConfig(string fileName = null, Setting settings = null)
        {
            if (settings == null)
            {
                // Save current config
                #region Creation of variable to save
                settings = new Setting()
                {
                    GameFileName = defaultConfigToolStripMenuItem.Checked == true ? "" : currentGame,
                    Keys = new UserKeys()
                    {
                        Up = GameInputKeys.Up,
                        Down = GameInputKeys.Down,
                        Left = GameInputKeys.Left,
                        Right = GameInputKeys.Right,
                        Jump = GameInputKeys.Jump,
                        Shoot = GameInputKeys.Shoot,
                        Start = GameInputKeys.Start,
                        Select = GameInputKeys.Select
                    },
                    Screens = new LastScreen()
                    {
                        Size = currentSize(),
                        Maximized = WindowState == FormWindowState.Maximized ? true : false,
                        NTSC_Options = currentNTSC_Option(),
                        NTSC_Custom = new NTSC_CustomOptions()
                        {
                            Hue = customNtscForm.Hue,
                            Saturation = customNtscForm.Saturation,
                            Brightness = customNtscForm.Brightness,
                            Contrast = customNtscForm.Contrast,
                            Sharpness = customNtscForm.Sharpness,
                            Gamma = customNtscForm.Gamma,
                            Resolution = customNtscForm.Resolution,
                            Artifacts = customNtscForm.Artifacts,
                            Fringing = customNtscForm.Fringing,
                            Bleed = customNtscForm.Bleed,
                            Merge_Fields = true
                        },
                        Pixellated = currentPixellatedOrSmoothedOption(),
                        HideMenu = hideMenuItem.Checked
                    },
                    Audio = new LastAudio()
                    {
                        Volume = Engine.Instance.Volume,
                        Musics = musicMenuItem.Checked,
                        Sound = sfxMenuItem.Checked,
                        Square1 = sq1MenuItem.Checked,
                        Square2 = sq2MenuItem.Checked,
                        Triangle = triMenuItem.Checked,
                        Noise = noiseMenuItem.Checked
                    },
                    Debug = new LastDebug()
                    {
                        ShowMenu = debugBarToolStripMenuItem.Checked,
                        ShowHitboxes = showHitboxesToolStripMenuItem.Checked,
                        Framerate = Engine.Instance.FPS,
                        Cheat = new LastCheat()
                        {
                            Invincibility = invincibilityToolStripMenuItem.Checked,
                            NoDamage = noDamageToolStripMenuItem.Checked
                        },
                        Layers = new LastBackground()
                        {
                            Background = backgroundToolStripMenuItem.Checked,
                            Sprites1 = sprites1ToolStripMenuItem.Checked,
                            Sprites2 = sprites2ToolStripMenuItem.Checked,
                            Sprites3 = sprites3ToolStripMenuItem.Checked,
                            Sprites4 = sprites4ToolStripMenuItem.Checked,
                            Foreground = foregroundToolStripMenuItem.Checked
                        }
                    },
                    Miscellaneous = new LastMiscellaneous()
                    {
                        ScreenX_Coordinate = this.Location.X,
                        ScreenY_Coordinate = this.Location.Y
                    }
                };
                #endregion
            }

            UserSettings userSettings = null;

            // This functions updates settingsPath
            GetUserSettingsFromXML(ref userSettings);

            // If there is no file, or no config in file, it is null
            if (userSettings == null) userSettings = new UserSettings();

            userSettings.AddOrSetExistingSettingsForGame(settings);

            SaveToConfigXML(userSettings, fileName);
        }
        #endregion

        #region Errors
        private void Engine_Exception(Exception e)
        {
            MessageBox.Show(this, e.Message, "Game Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

            CloseGame();
        }

        private void Programming_Error_No_Shutdown(string message)
        {
            MessageBox.Show(this, message, "Programming Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
        }

        private void WrongConfigAlert(string message)
        {
            MessageBox.Show(this, message, "Config File Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
        }
        #endregion
        #endregion

        #region To sort!!!!!!
        /// <summary>
        /// Anytime game loaded is changed
        /// </summary>
        private void OnGameLoadedChanged()
        {
            LoadGlobalConfigValues();
            OnGameLoaded();
        }

        /// <summary>
        /// Things to do when a game is loaded
        /// </summary>
        private void OnGameLoaded()
        {
            SetLayersVisibilityFromSettings();
        }

        private void DefaultScreen()
        {
            width = Const.PixelsAcross;
            height = Const.PixelsDown;

            ResizeScreen();
        }
        
        void Game_ScreenSizeChanged(object sender, ScreenSizeChangedEventArgs e)
        {
            FormWindowState previousWindowState = WindowState;

            WindowState = FormWindowState.Normal;

            if (width != 256 || height != 224)
            {
                xnaImage.NTSC = false;
            }

            width = e.PixelsAcross;
            height = e.PixelsDown;
            
            SetXnaSize(width, height);

            if (xnaImage.NTSC)
            {
                ResizeScreen(602, 448);
            }
            else
            {
                // normal zoomed size
                ResizeScreen();
            }
            WindowState = previousWindowState;
        }

        private void ResizeScreen(int? newWidth = null, int? newHeight = null)
        {
            if (newWidth == null)
            {
                newWidth = width * widthZoom;
            }
            if (newHeight == null)
            {
                newHeight = height * heightZoom;
            }

            // tell the image not to get crushed by the form
            xnaImage.Dock = DockStyle.None;
            
            // tell the form to fit the image
            if (fullScreenToolStripMenuItem.Checked)
            {
                menuStrip1.Visible = false;
            }

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            xnaImage.Width = newWidth.Value;
            xnaImage.Height = newHeight.Value;
            // now remember the form size
            int tempheight = Height;
            int tempwidth = Width;
            // now un-autosize to re-enable resizing
            AutoSize = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            // reset the form size
            if (debugBar.Visible)
            {
                tempheight += debugBar.Height;
            }

            // for some reason menu height is always still shown when the image is undocked
            if (!menuStrip1.Visible)
            {
                tempheight -= menuStrip1.Height;
            }

            Height = tempheight;
            Width = tempwidth;
            // redock the image
            xnaImage.Dock = DockStyle.Fill;
        }

        private void SetXnaSize(int width, int height)
        {
            xnaImage.Dock = DockStyle.None;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            xnaImage.Width = width;
            xnaImage.Height = height;
            xnaImage.SetSize();

            AutoSize = false;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            xnaImage.Dock = DockStyle.Fill;
        }

        void Instance_GameLogicTick(GameTickEventArgs e)
        {
            float fps = 1 / e.TimeElapsed;
            fpsLabel.Text = "FPS: " + fps.ToString("N2");
            thinkLabel.Text = "Busy: " + (Engine.Instance.ThinkTime * 100).ToString("N0") + "%";
            entityLabel.Text = "Entities: " + Game.DebugEntitiesAlive();
            fpsCapLabel.Text = "FPS Cap: " + Engine.Instance.FPS;
        }
        #endregion
    }
}
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using WindowPrivacy.Fonts;

namespace WindowPrivacy
{
    internal class MainUI : ClickableTransparentOverlay.Overlay
    {
        public MainUI() : base(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
        {
        }
        static ImFontPtr PoppinsFont;
        public static unsafe void loadResources(MainUI _ref)
        {
            if (!File.Exists("config.json"))
                Config.createNewConfigFile();
            var io = ImGui.GetIO();

            _ref.ReplaceFont(config =>
            {
                var io = ImGui.GetIO();
                io.Fonts.AddFontFromFileTTF(@"Fonts\Poppins-Light.ttf", 16, config, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
                config->MergeMode = 1;
                config->OversampleH = 1;
                config->OversampleV = 1;
                config->PixelSnapH = 1;

                var custom2 = new ushort[] { 0xe005, 0xf8ff, 0x00 };
                fixed (ushort* p = &custom2[0])
                {
                    io.Fonts.AddFontFromFileTTF("Fonts\\fa-solid-900.ttf", 16, config, new IntPtr(p));
                }
            });


            PoppinsFont = io.Fonts.AddFontFromFileTTF(@"Fonts\Poppins-Light.ttf", 16);
            //IconFont = io.Fonts.AddFontFromFileTTF(@"Fonts\fa-solid-900.ttf", 16,, new ushort[] { 0xe005,
            //0xf8ff,0});
        }
        bool tipVisble;
        Vector2 tipLocation;
        processData processTip;
        void showOptionTip(processData pid)
        {
            tipLocation = ImGui.GetMousePos();
            processTip = pid;
            tipVisble = true;
        }
        static List<processData> processDataList = new List<processData>();
        void iterateProcesses()
        {
            List<Process> pList = Process.GetProcesses().ToList();
            foreach (Process p in pList)
            {
                List<nint> windows = Extentions.GetRootWindowsOfProcess(p.Id); //making sure process has windows if it doesn't we dont care about that process

                if (windows.Count == 0 || processDataList.Exists(x => x.Path == p.MainModule.FileName)) continue;
                Bitmap bmp_ico;
                Icon ico = Icon.ExtractAssociatedIcon(p.MainModule.FileName);

                nint handle = 0;
                if (ico != null)
                {
                    bmp_ico = new Bitmap(ico.ToBitmap(), new Size(64, 64));
                    AddOrGetImagePointer($"{p.MainModule.FileName}", bmp_ico.ToImageSharpImage<SixLabors.ImageSharp.PixelFormats.Rgba32>(), true, out handle);
                }

                lock (processDataList)
                {
                    processDataList.Add(new processData()
                    {
                        pid = p.Id,
                        Name = p.MainModule.ModuleName,
                        Path = p.MainModule.FileName,
                    });
                }

            }
            lock (processDataList)
            {
                processDataList.RemoveAll(x => !pList.Exists(y => y.Id == x.pid)); //removes all processes that were closed
                processDataList = processDataList.OrderBy(x => !x.hidden).ThenBy(x => x.Name).ToList(); //sorting by state HIDDEN/VISBLE then by name
            }
            refreshProcess.Restart();
        }
        public static bool visble = true;
        bool resourceLoaded = false;
        Stopwatch refreshProcess = new Stopwatch();
        int tab = 0;
        public static bool setOverlayAffinity = false;
        public void setAffinity()
        {
            Extentions.SetWindowDisplayAffinity(base.window.Handle, (uint)((!setOverlayAffinity) ? 0x00000000 : 0x00000001));
        }
        protected override void Render()
        {
            var style = ImGui.GetStyle();
            if (!resourceLoaded)
            {
                var notifyIcon = new NotifyIcon();
                notifyIcon.Icon = new Icon("icon.ico");
                notifyIcon.Text = "WindowPrivacy (click to show menu)";
                notifyIcon.Visible = true;
                notifyIcon.Click += (object Sender, EventArgs e) =>
                {
                    WindowPrivacy.MainUI.visble = true;
                };
                iterateProcesses();
                style.Colors[(int)ImGuiCol.Border] = Color.FromArgb(184, 58, 184).toVec4();
                style.WindowRounding = 8;
                loadResources(this);
                resourceLoaded = true;
            }
            int exStyle = (int)Extentions.GetWindowLong(base.window.Handle, (int)Extentions.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)Extentions.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            Extentions.SetWindowLong(base.window.Handle, (int)Extentions.GetWindowLongFields.GWL_EXSTYLE, exStyle);
            if (!visble) return;
            if (refreshProcess.ElapsedMilliseconds > 500)
            {
                new Thread(() =>
                {
                    iterateProcesses();
                }).Start();
                refreshProcess.Reset();
            }
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(535, 400));
            ImGui.Begin("WindowPrivacy", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
            ImGui.BeginChild("tabs", new Vector2(600, 30));
            if (ImGui.Button($"{FontAwesome5.Table} Window List")) tab = 0;
            ImGui.SameLine();
            if (ImGui.Button($"{FontAwesome5.Book} WhiteList")) tab = 1;
            ImGui.SameLine();
            if (ImGui.Checkbox("make entire screen black", ref setOverlayAffinity))
                setAffinity();
            ImGui.SameLine();
            if (ImGui.Checkbox("run on startup", ref Config.getConfig().runOnStartUp))
            {
                Extentions.SetStartup(Config.getConfig().runOnStartUp);
                Config.getConfig().saveConfig();
            }
            ImGui.SameLine();
            if (ImGui.Button($"{FontAwesome5.Minus}"))
            {
                visble = false;
            };
            ImGui.SameLine();
            if (ImGui.Button($"{FontAwesome5.Xmark}"))
            {
                Process.GetCurrentProcess().Kill();
            };
            ImGui.EndChild();
            ImGui.BeginChild("list", new System.Numerics.Vector2(525, 360));
            var childRect = new Rectangle(ImGui.GetWindowPos().toPoint(), new Size(ImGui.GetWindowSize().toPoint()) - new Size(0, 30));
            List<processData> data = processDataList;
            if (tab == 1)
                data = Config.getConfig().whiteList;
            List<processData> runningWhiteList = new List<processData>();
            foreach (processData pData in data)
            {
                AddOrGetImagePointer(pData.Path, new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(64, 64), true, out var imageHandle);
                if (!pData.hidden && Config.getConfig().whiteList.Exists(x => x.Path == pData.Path))
                {
                    runningWhiteList.Add(pData);
                }
                var text = (tab == 1) ? $"{pData.Path}" : pData.Name;
                float imageSize = ImGui.CalcTextSize(text).Y;
                var imageStartPos = ImGui.GetCursorPos() + ImGui.GetWindowPos() + new Vector2(0, -ImGui.GetScrollY());
                var imageEndPos = imageStartPos + new Vector2(imageSize, imageSize);
                if (imageHandle != 0 && childRect.Contains(imageStartPos.toPoint()))
                {
                    ImGui.GetForegroundDrawList().AddImage(imageHandle,
                        imageStartPos, imageEndPos);
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + imageSize + 4);
                    string state = (pData.hidden) ? $"{FontAwesome5.EyeLowVision} Hidden" : $"{FontAwesome5.Eye} Visble";
                    uint color = (pData.hidden) ? Color.Green.ToUint() : Color.Gray.ToUint();
                    if (tab != 1)
                        ImGui.GetForegroundDrawList().AddText(imageStartPos + new Vector2(450, 0), color, state);
                }

                ImGui.Selectable($"{text}");
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) &&
                    ImGui.IsMouseHoveringRect(imageStartPos, imageStartPos + new Vector2(ImGui.GetWindowSize().X, imageSize)))
                {
                    showOptionTip(pData);
                }

            }
            //checking whitelist
            foreach (processData pData in runningWhiteList)
            {
                lock (processDataList)
                {
                    Injection.injectAffinity(pData.pid, Injection.Affinity.WDA_MONITOR);
                    processDataList.RemoveAll(x => x.Path == pData.Path);
                    processDataList.Add(new processData()
                    {
                        pid = pData.pid,
                        hidden = !pData.hidden,
                        Name = pData.Name,
                        Path = pData.Path
                    });
                }
            }
            ImGui.EndChild();


            ImGui.End();
            if (tipVisble)
            {
                ImGui.SetNextWindowPos(tipLocation);
                ImGui.Begin("Options", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize);
                string text = (!processTip.hidden) ? $"{FontAwesome5.EyeLowVision} Hide" : $"{FontAwesome5.Eye} Show";
                if (ImGui.Selectable(text))
                {
                    if (processTip.hidden)
                    {
                        if (!Injection.injectAffinity(processTip.pid, Injection.Affinity.WDA_NONE))
                        {
                            MessageBox.Show("Error couldn't set affinity");
                        }
                    }
                    else
                    {
                        if (!Injection.injectAffinity(processTip.pid, Injection.Affinity.WDA_MONITOR))
                        {
                            MessageBox.Show("Error couldn't set affinity");
                        }
                    }
                    lock (processDataList)
                    {
                        processDataList.RemoveAll(x => x.Path == processTip.Path);
                        processTip.hidden = !processTip.hidden;
                        processDataList.Add(processTip);
                    }
                    tipVisble = false;
                }
                bool isWhiteListed = Config.getConfig().whiteList.Exists(x => x.Path == processTip.Path);
                text = isWhiteListed ? $"{FontAwesome5.Minus} Remove From Whitelist" : $"{FontAwesome5.Plus} Add To Whitelist";
                if (ImGui.Selectable(text))
                {
                    if (isWhiteListed)
                        Config.getConfig().removeFromList(processTip);
                    else
                        Config.getConfig().addToList(processTip);
                    tipVisble = false;
                }
                ImGui.End();
            }

        }
    }
}

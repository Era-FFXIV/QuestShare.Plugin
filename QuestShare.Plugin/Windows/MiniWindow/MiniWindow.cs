﻿using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using QuestShare.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Windowing.Window;

namespace QuestShare.Windows.MiniWindow
{
    internal class MiniWindow : Window
    {
        public MiniWindow()
        : base(Plugin.Name + "###MiniWindow", ImGuiWindowFlags.None)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(200, 100),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        private static ApiService ApiService => (ApiService)Plugin.GetService<ApiService>();
        private static ShareService ShareService => (ShareService)Plugin.GetService<ShareService>();

        public string ShareCode { get; private set; } = string.Empty;
        private Objects.Session? Session => ShareService.Sessions.FirstOrDefault(s => s.ShareCode == ShareCode);
        

        public void SetSession(string shareCode)
        {
            ShareCode = shareCode;
        }

        public override void OnOpen()
        {
            if (Session == null)
            {
                IsOpen = false;
                return;
            }
            WindowName = $"{Plugin.Name} - {Session.ShareCode}###MiniWindow";
        }

        public override void Draw()
        {
            if (Session == null) return;
            if (!ApiService.IsConnected)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, "Not connected to the server, check main window.");
                return;
            }
            ImGui.SetWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
            // set text wrapping
            ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
            UiService.MainWindow.DrawSessionDetails(Session, true);
            ImGui.PopTextWrapPos();
        }
    }
}

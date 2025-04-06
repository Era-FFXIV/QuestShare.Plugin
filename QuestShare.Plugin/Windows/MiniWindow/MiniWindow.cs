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
                MinimumSize = new Vector2(200, 200),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        private Objects.Session? Session;

        public void SetSession(Objects.Session session)
        {
            Session = session;
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
        public override void OnClose()
        {
            Session = null;
        }

        public override void Draw()
        {
            if (Session == null) return;
            ImGui.SetWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
            ImGui.SetWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
            // set text wrapping
            ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
            UiService.MainWindow.DrawSessionDetails(Session);
            ImGui.PopTextWrapPos();
        }

        public void Dispose()
        {
            Session = null;
        }
    }
}

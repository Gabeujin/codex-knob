using System;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("Codex Knob")]
[assembly: AssemblyDescription("Local keyboard knob bridge for Codex")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace CodexKnob
{
    internal static class Program
    {
        [STAThread] private static int Main(string[] args)
        {
            bool background = args.Length == 1 && args[0] == "--background";
            try
            {
                if (args.Length == 1 && args[0] == "--self-test") return SelfTests.Run();
                L.Set(Service.LoadLanguage());
                if (args.Length == 1 && args[0] == "--install-startup") { Service.SetStartup(true); return 0; }
                if (args.Length > 0 && !background) return 2;
                // 이전 버전과 같은 뮤텍스를 사용해 두 입력 훅이 동시에 변환하지 않도록 제한
                bool created;
                using (var show = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\CodexKnob.Show"))
                using (var mutex = new Mutex(true, "Local\\Ak47CodexKnob", out created))
                {
                    if (!created) { if (!background) show.Set(); return 0; }
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (var form = new MainForm(background, show)) Application.Run(form);
                    return 0;
                }
            }
            catch (Exception error)
            {
                if (!background) MessageBox.Show(L.T("Codex Knob을 시작하지 못했습니다.") + "\n" + L.T(error.Message), "Codex Knob", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}

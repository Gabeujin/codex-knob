using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CodexKnob
{
    public static class Service
    {
        public const string Version = "1.2.0";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunName = "CodexKnob";
        public static string InstallPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CodexKnob", Version, "CodexKnob.exe"); } }
        internal static string SettingsPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexKnob", "settings.json"); } }
        private static string LanguagePath { get { return Path.Combine(Path.GetDirectoryName(SettingsPath), "language.txt"); } }
        public static string LoadLanguage()
        {
            try
            {
                string code = File.ReadAllText(LanguagePath, new UTF8Encoding(false, true));
                return code == "en" || code == "ja" ? code : "ko";
            }
            catch (IOException) { return "ko"; }
            catch (UnauthorizedAccessException) { return "ko"; }
            catch (DecoderFallbackException) { return "ko"; }
        }
        public static void SaveLanguage(string code)
        {
            if (code != "ko" && code != "en" && code != "ja") throw new ArgumentException("Invalid language");
            string path = LanguagePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temp, code, new UTF8Encoding(false, true));
            if (File.ReadAllText(temp, new UTF8Encoding(false, true)) != code) throw new IOException("설정 저장 검증에 실패했습니다.");
            if (File.Exists(path)) { BackupFile(path); File.Replace(temp, path, null); }
            else File.Move(temp, path);
            if (File.ReadAllText(path, new UTF8Encoding(false, true)) != code) throw new IOException("설정 반영 검증에 실패했습니다.");
        }
        internal static string StartupCommand { get { return QuoteCommand(InstallPath); } }
        private static string PreviousStartupCommand
        {
            get { return QuoteCommand(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(InstallPath)), "1.1.0", "CodexKnob.exe")); }
        }
        private static bool IsOwnedStartup(string command)
        {
            return command == StartupCommand || command == PreviousStartupCommand;
        }
        public static bool IsInstalled { get { return File.Exists(InstallPath); } }
        internal static bool StartupNeedsUpgrade { get { return ReadStartupCommand() == PreviousStartupCommand; } }
        public static bool StartupEnabled { get { return IsOwnedStartup(ReadStartupCommand()); } }

        private static string ReadStartupCommand()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                return key == null ? null : key.GetValue(RunName) as string;
        }
        internal static string QuoteCommand(string path)
        {
            if (!Path.IsPathRooted(path) || path.IndexOf('"') >= 0 || path.IndexOf('\n') >= 0 || path.IndexOf('\r') >= 0 ||
                !String.Equals(Path.GetFullPath(path), path, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Invalid startup path");
            string command = "\"" + path + "\" --background";
            if (command.Length > 260) throw new ArgumentException("자동 시작 경로가 너무 깁니다.");
            return command;
        }
        internal static string Hash(string path)
        { using (var hash = SHA256.Create()) using (var file = File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(file)).Replace("-", ""); }
        public static void Install()
        {
            CopyExecutable();
            if (StartupNeedsUpgrade) SetStartup(true);
        }

        private static void CopyExecutable()
        {
            string source = Application.ExecutablePath;
            string target = InstallPath;
            // 같은 버전의 다른 파일은 덮어쓰지 않고 사용자 원본을 보존
            if (File.Exists(target))
            {
                if (!String.Equals(Hash(source), Hash(target), StringComparison.Ordinal)) throw new IOException("같은 버전의 다른 파일이 설치되어 있습니다. 기존 파일을 보존한 뒤 확인해 주세요.");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(source, target, false);
            if (Hash(source) != Hash(target)) throw new IOException("설치 파일 검증에 실패했습니다.");
        }
        public static void SetStartup(bool enable)
        {
            if (enable) CopyExecutable();
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                object stored = key.GetValue(RunName);
                string current = stored as string;
                // 다른 프로그램이 같은 이름을 쓴 경우 등록과 해제를 모두 중단
                if (stored != null && !IsOwnedStartup(current)) throw new IOException("같은 이름의 다른 자동 시작 항목이 있어 변경하지 않았습니다.");
                if (enable && current == StartupCommand || !enable && current == null) return;
                BackupRegistry(current);
                if (enable) key.SetValue(RunName, StartupCommand, RegistryValueKind.String);
                else key.DeleteValue(RunName, false);
                string expected = enable ? StartupCommand : null;
                if (!String.Equals(key.GetValue(RunName) as string, expected, StringComparison.Ordinal)) throw new IOException("자동 시작 설정을 확인하지 못했습니다.");
            }
        }
        internal static Mapping LoadMapping()
        {
            if (!File.Exists(SettingsPath)) return Mapping.Default;
            if (new FileInfo(SettingsPath).Length > 16384) throw new IOException("Settings too large");
            return ParseMapping(File.ReadAllText(SettingsPath, new UTF8Encoding(false, true)));
        }
        internal static Mapping ParseMapping(string json)
        {
            var result = new JavaScriptSerializer().Deserialize<Mapping>(json);
            if (result == null || !result.Valid) throw new FormatException("등록할 수 없는 신호 설정입니다.");
            return result;
        }
        public static void SaveMapping(Mapping mapping)
        {
            SaveMapping(mapping, SettingsPath);
        }

        internal static void SaveMapping(Mapping mapping, string path)
        {
            if (mapping == null || !mapping.Valid) throw new ArgumentException("등록할 수 없는 신호 설정입니다.");
            string json = new JavaScriptSerializer().Serialize(new
            {
                mapping.Version,
                Up = new { mapping.Up.Kind, mapping.Up.Code, mapping.Up.ScanCode, mapping.Up.Extended, mapping.Up.WheelDelta },
                Down = new { mapping.Down.Kind, mapping.Down.Code, mapping.Down.ScanCode, mapping.Down.Extended, mapping.Down.WheelDelta },
                mapping.UpOutput,
                mapping.DownOutput
            });
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // 설정은 임시 파일을 검증한 뒤 교체하고 기존 내용은 별도 백업에 보존
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temp, json, new UTF8Encoding(false, true));
            if (File.ReadAllText(temp, new UTF8Encoding(false, true)) != json) throw new IOException("설정 저장 검증에 실패했습니다.");
            if (File.Exists(path)) { BackupFile(path); File.Replace(temp, path, null); }
            else File.Move(temp, path);
            if (File.ReadAllText(path, new UTF8Encoding(false, true)) != json) throw new IOException("설정 반영 검증에 실패했습니다.");
        }
        private static string BackupDirectory()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Codex-Change-Backups", "CodexKnob", DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path); return path;
        }
        private static void BackupFile(string source)
        {
            string directory = BackupDirectory(); string target = Path.Combine(directory, Path.GetFileName(source));
            var info = new FileInfo(source); File.Copy(source, target, false);
            WriteIndex(directory, new { sessionId = "CodexKnob", task = "mapping-update", timestamp = DateTime.Now.ToString("o"), sourcePath = source, backupPath = target, size = info.Length, modifiedTime = info.LastWriteTimeUtc.ToString("o"), sha256 = Hash(source) });
        }
        private static void BackupRegistry(string value)
        {
            string directory = BackupDirectory();
            WriteIndex(directory, new { sessionId = "CodexKnob", task = "startup-update", timestamp = DateTime.Now.ToString("o"), registryPath = "HKCU\\" + RunKey, valueName = RunName, existed = value != null, previousValue = value });
        }
        private static void WriteIndex(string directory, object data)
        {
            string text = new JavaScriptSerializer().Serialize(data);
            string path = Path.Combine(directory, "index.json");
            File.WriteAllText(path, text, new UTF8Encoding(false, true));
            if (File.ReadAllText(path, new UTF8Encoding(false, true)) != text) throw new IOException("백업 기록 검증에 실패했습니다.");
        }
    }
}

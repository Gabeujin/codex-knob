using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CodexKnob
{
    internal static class SelfTests
    {
        private static int checks;

        internal static int Run()
        {
            checks = 0;
            CheckLearnableKeys();
            CheckMappings();
            CheckRouting();
            CheckPaths();
            CheckKeySequences();
            CheckLearningSequences();
            CheckWheel();
            CheckInteropAndStartup();
            Console.WriteLine("PASS: " + checks + " policy/state/serialization checks. No input injection, hooks, files, registry, or UI exercised.");
            return 0;
        }

        private static void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException("Self-test failed: " + name);
            checks++;
        }

        private static void Reject(Action action, string name)
        {
            bool rejected = false;
            try { action(); }
            catch (ArgumentException) { rejected = true; }
            catch (FormatException) { rejected = true; }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, name);
        }

        private static void CheckLearnableKeys()
        {
            var allowed = new HashSet<int>();
            foreach (int key in new[] { 33, 34, 35, 36, 37, 38, 39, 40,
                112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123,
                124, 125, 126, 127, 128, 129, 130, 131, 132, 133,
                173, 174, 175, 176, 177, 178, 179 }) allowed.Add(key);
            // 문자·숫자·보조 키와 출력용 F23/F24를 포함해 허용 목록 밖의 모든 VK 거부 확인
            for (int key = 0; key < 256; key++)
                Check(Policy.LearnableKey(key) == allowed.Contains(key), "learnable VK " + key);
            Check(!Policy.LearnableKey(-1) && !Policy.LearnableKey(256), "out-of-range VK rejected");
        }

        private static void CheckMappings()
        {
            Mapping defaults = Mapping.Default;
            Check(defaults.Valid, "default mapping valid");
            Check(defaults.Target(new Trigger { Kind = "key", Code = 175 }) == 134, "volume up maps F23");
            Check(defaults.Target(new Trigger { Kind = "key", Code = 174 }) == 135, "volume down maps F24");
            Check(defaults.Target(new Trigger { Kind = "key", Code = 173 }) == 0, "unmapped media key ignored");
            var mixed = Service.ParseMapping("{\"Version\":1,\"Up\":{\"Kind\":\"wheel\",\"Code\":1},\"Down\":{\"Kind\":\"key\",\"Code\":112}}");
            Check(mixed.Valid && mixed.Up.Code == 1 && mixed.Down.Code == 112, "mixed mapping parsed");
            Check(mixed.Target(new Trigger { Kind = "wheel", Code = 1 }) == 134, "wheel mapping target");
            // 설정 파일의 문법 오류와 등록 불가 신호를 실제 파서로 확인
            foreach (string json in new[] { "", "null", "{", "{}", "[]",
                "{\"Version\":2,\"Up\":{\"Kind\":\"key\",\"Code\":175},\"Down\":{\"Kind\":\"key\",\"Code\":174}}",
                "{\"Version\":1,\"Up\":null,\"Down\":{\"Kind\":\"key\",\"Code\":174}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"key\",\"Code\":175},\"Down\":{\"Kind\":\"key\",\"Code\":175}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"wheel\",\"Code\":1},\"Down\":{\"Kind\":\"wheel\",\"Code\":1}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"wheel\",\"Code\":0},\"Down\":{\"Kind\":\"key\",\"Code\":174}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"wheel\",\"Code\":120},\"Down\":{\"Kind\":\"key\",\"Code\":174}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"mouse\",\"Code\":1},\"Down\":{\"Kind\":\"key\",\"Code\":174}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"key\",\"Code\":65},\"Down\":{\"Kind\":\"key\",\"Code\":174}}",
                "{\"Version\":1,\"Up\":{\"Kind\":\"key\",\"Code\":134},\"Down\":{\"Kind\":\"key\",\"Code\":174}}" })
                Reject(delegate { Service.ParseMapping(json); }, "invalid mapping " + json);
            Check(!new Trigger { Kind = "key", Code = 135 }.Valid, "F24 cannot be mapped");
            Check(!new Trigger { Kind = null, Code = 175 }.Valid, "null trigger kind rejected");
            Check(new Trigger { Kind = "wheel", Code = -1 }.Valid, "negative wheel direction valid");
        }

        private static void CheckRouting()
        {
            Check(Policy.MayRoute(true, true, false, 0), "physical input allowed");
            Check(Policy.MayRoute(true, true, false, 0x11), "AK47 injected down allowed");
            Check(Policy.MayRoute(true, true, false, 0x91), "AK47 injected up flags allowed");
            Check(!Policy.MayRoute(false, true, false, 0), "paused rejected");
            Check(!Policy.MayRoute(true, false, false, 0), "other app rejected");
            Check(!Policy.MayRoute(true, true, true, 0x11), "modifier rejected");
            foreach (uint flags in new uint[] { 2, 3, 0x12, 0x13, 0x92, 0x93 })
                Check(!Policy.MayRoute(true, true, false, flags), "lower-integrity injection rejected " + flags);
        }

        private static void CheckPaths()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string package = root + "\\WindowsApps\\OpenAI.Codex_26.1106.6628.0_x64__2p2nqsd0c76g0\\app\\";
            Check(Policy.IsCodexPath(package + "ChatGPT.exe"), "packaged ChatGPT accepted");
            Check(Policy.IsCodexPath(package + "Codex.exe"), "packaged Codex accepted");
            Check(Policy.IsCodexPath((package + "Codex.exe").ToLowerInvariant()), "path casing accepted");
            foreach (string path in new[] { null, "", "C:\\temp\\Codex.exe", package + "codex.exe.bak",
                package + "resources\\codex.exe", package.Replace("2p2nqsd0c76g0", "otherpublisher") + "Codex.exe",
                package.Replace("OpenAI.Codex_", "Other.Codex_") + "Codex.exe" })
                Check(!Policy.IsCodexPath(path), "untrusted executable path rejected");
        }

        private static void CheckKeySequences()
        {
            var sent = new List<int>();
            Func<int, bool> send = delegate(int key) { sent.Add(key); return true; };
            var sequence = new KeySequence();
            Check(sequence.Handle(true, false, 134, send), "first mapped down consumed");
            Check(sequence.Handle(true, false, 134, send), "mapped repeat consumed");
            Check(sent.Count == 2, "normal repeat sends once");
            Check(sequence.Handle(true, false, 0, send), "focus loss keeps consumed sequence");
            Check(sent.Count == 2, "focus loss sends nothing");
            Check(sequence.Handle(false, true, 0, send), "consumed up matched");
            Check(!sequence.Handle(false, true, 0, send), "orphan up passes");
            // 최초 통과한 입력은 매핑이나 전경이 바뀌어도 해제까지 통과
            Check(!sequence.Handle(true, false, 0, send), "unmapped down passes");
            Check(!sequence.Handle(true, false, 135, send), "new mapping cannot consume prior down");
            Check(!sequence.Handle(false, true, 0, send), "passed up matched");
            Check(sent.Count == 2, "passed sequence emits nothing");
            Check(sequence.Handle(true, false, 135, send), "next press uses changed mapping");
            Check(sent[sent.Count - 1] == 135, "changed target emitted");
            Check(sequence.Handle(false, true, 0, send), "changed mapping release consumed");
            Check(!sequence.Handle(true, false, 134, delegate { return false; }), "initial failure passes");
            Check(!sequence.Handle(false, true, 0, send), "initial failure release passes");
            Check(sequence.Handle(true, false, 134, send), "new successful sequence consumed");
            Check(sequence.Handle(true, false, 134, delegate { return false; }), "repeat failure remains consumed");
            Check(sequence.Handle(false, true, 0, send), "repeat failure release consumed");
        }

        private static void CheckLearningSequences()
        {
            // 학습 성공 후 키를 놓기 전까지 추론 단축키 전송을 막는지 확인
            var sequence = new KeySequence();
            int captures = 0, outputs = 0;
            Func<int, bool> capture = delegate(int key) { captures++; Check(key == -1, "learning sentinel only"); return true; };
            Func<int, bool> send = delegate { outputs++; return true; };
            Check(sequence.Handle(true, false, -1, capture), "learning down consumed");
            Check(sequence.Handle(true, false, -1, capture), "learning repeat consumed");
            Check(sequence.Handle(true, false, 134, send), "post-learning held key consumed");
            Check(captures == 1 && outputs == 0, "learning repeat cannot emit output");
            Check(sequence.Handle(false, true, 0, send), "learning release consumed");
            Check(sequence.Handle(true, false, 134, send), "new press after learning routes");
            Check(outputs == 1, "new press emits once");
            Check(sequence.Handle(false, true, 0, send), "new press release consumed");
            Check(!sequence.Handle(true, false, -1, delegate { return false; }), "expired capture passes");
            Check(!sequence.Handle(true, false, 134, send), "expired capture repeat stays passed");
            Check(!sequence.Handle(false, true, 0, send), "expired capture release passes");
        }

        private static void CheckWheel()
        {
            var wheel = new WheelAccumulator();
            Check(wheel.Add(30) == 0 && wheel.Add(30) == 0 && wheel.Add(30) == 0, "partial positive wheel waits");
            Check(wheel.Add(30) == 1, "four positive quarters emit one step");
            Check(wheel.Add(-30) == 0 && wheel.Add(-30) == 0 && wheel.Add(-30) == 0, "partial negative wheel waits");
            Check(wheel.Add(-30) == -1, "four negative quarters emit one step");
            Check(wheel.Add(240) == 2 && wheel.Add(-240) == -2, "multi-step wheel preserved");
            Check(wheel.Add(90) == 0 && wheel.Add(-30) == 0, "reversal discards positive remainder");
            Check(wheel.Add(-90) == -1, "negative remainder accumulates after reversal");
            Check(wheel.Add(90) == 0, "remainder before reset");
            wheel.Reset();
            Check(wheel.Add(30) == 0, "reset discards prior remainder");
            wheel.Reset();
            Check(wheel.Add(0) == 0, "zero delta produces no step");
        }

        private static void CheckInteropAndStartup()
        {
            Check(IntPtr.Size == 8 && Marshal.SizeOf(typeof(Native.Input)) == 40, "x64 INPUT size");
            Check(Marshal.OffsetOf(typeof(Native.Input), "keyboard").ToInt32() == 8, "INPUT keyboard offset");
            Check(Marshal.SizeOf(typeof(Native.MouseEvent)) == 32, "x64 mouse hook structure size");
            Check(Native.Marker != UIntPtr.Zero, "output marker present");
            Check(Service.QuoteCommand("C:\\Local Apps\\CodexKnob.exe") == "\"C:\\Local Apps\\CodexKnob.exe\" --background", "startup path quoted");
            // 자동 시작 명령의 상대 경로와 명령문 분리 문자를 거부하는지 확인
            foreach (string path in new[] { "", "CodexKnob.exe", "C:CodexKnob.exe", "\\CodexKnob.exe",
                "C:\\App\".exe", "C:\\App\n.exe", "C:\\App\r.exe" })
                Reject(delegate { Service.QuoteCommand(path); }, "invalid startup path rejected " + path);
            Check(Service.QuoteCommand("C:\\" + new string('a', 242)).Length == 260, "startup length boundary accepted");
            Reject(delegate { Service.QuoteCommand("C:\\" + new string('a', 243)); }, "startup length overflow rejected");
        }
    }
}

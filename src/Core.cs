using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CodexKnob
{
    public sealed class Trigger
    {
        public string Kind { get; set; }
        public int Code { get; set; }
        public bool Valid { get { return Kind == "key" ? Policy.LearnableKey(Code) : Kind == "wheel" && (Code == 1 || Code == -1); } }
        public string DisplayName
        {
            get
            {
                if (Kind == "wheel") return Code > 0 ? "휠 위로" : "휠 아래로";
                if (Code == 0xAF) return "볼륨 증가";
                if (Code == 0xAE) return "볼륨 감소";
                if (Code == 0xAD) return "음소거";
                return ((Keys)Code).ToString();
            }
        }
        public bool Same(Trigger other) { return other != null && Kind == other.Kind && Code == other.Code; }
    }

    public sealed class Mapping
    {
        public int Version { get; set; }
        public Trigger Up { get; set; }
        public Trigger Down { get; set; }
        public static Mapping Default { get { return new Mapping { Version = 1, Up = new Trigger { Kind = "key", Code = 175 }, Down = new Trigger { Kind = "key", Code = 174 } }; } }
        public bool Valid { get { return Version == 1 && Up != null && Down != null && Up.Valid && Down.Valid && !Up.Same(Down); } }
        public int Target(Trigger trigger) { return Up.Same(trigger) ? 0x86 : Down.Same(trigger) ? 0x87 : 0; }
    }

    internal static class Policy
    {
        internal static bool LearnableKey(int key)
        {
            return (key >= 0x70 && key <= 0x85) || (key >= 0x21 && key <= 0x28) || (key >= 0xAD && key <= 0xB3);
        }
        internal static bool IsCodexPath(string path)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Regex.IsMatch(path ?? "", "^" + Regex.Escape(root) +
                @"\\WindowsApps\\OpenAI\.Codex_[0-9.]+_x64__2p2nqsd0c76g0\\app\\(?:ChatGPT|Codex)\.exe$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        internal static bool MayRoute(bool enabled, bool codex, bool modifiers, uint flags)
        { return enabled && codex && !modifiers && (flags & 2) == 0; }
    }

    internal sealed class KeySequence
    {
        private bool pressed, consumed, repeatOutput;
        internal bool Handle(bool down, bool up, int target, Func<int, bool> send)
        {
            if (up) { bool result = consumed; pressed = consumed = false; return result; }
            if (!down) return false;
            if (!pressed) { pressed = true; repeatOutput = target > 0; consumed = target != 0 && send(target); return consumed; }
            // 최초 눌림의 전달 여부를 해제까지 보존하고 학습 입력의 반복 전송을 제외
            if (consumed && repeatOutput && target > 0) send(target);
            return consumed;
        }
    }

    internal sealed class WheelAccumulator
    {
        private int remainder;
        internal void Reset() { remainder = 0; }
        internal int Add(int delta)
        {
            if (Math.Sign(delta) != Math.Sign(remainder)) remainder = 0;
            remainder += delta;
            int steps = remainder / 120;
            remainder %= 120;
            return steps;
        }
    }

    internal static class Native
    {
        internal static readonly UIntPtr Marker = new UIntPtr(0x434B4E42u);
        internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
        [StructLayout(LayoutKind.Sequential)] internal struct KeyEvent { internal uint key, scan, flags, time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct MouseEvent { internal int x, y; internal uint mouseData, flags, time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct KeyboardInput { internal ushort key, scan; internal uint flags, time; internal UIntPtr extra; }
        [StructLayout(LayoutKind.Explicit, Size = 40)] internal struct Input { [FieldOffset(0)] internal uint type; [FieldOffset(8)] internal KeyboardInput keyboard; }
        [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll")] internal static extern IntPtr OpenProcess(uint access, bool inherit, uint process);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref int size);
        [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
        internal static bool IsCodexForeground()
        {
            uint process; GetWindowThreadProcessId(GetForegroundWindow(), out process);
            IntPtr handle = OpenProcess(0x1000, false, process);
            if (handle == IntPtr.Zero) return false;
            try { var path = new StringBuilder(1024); int size = path.Capacity; return QueryFullProcessImageName(handle, 0, path, ref size) && Policy.IsCodexPath(path.ToString()); }
            finally { CloseHandle(handle); }
        }
        internal static bool ModifiersHeld()
        { return GetAsyncKeyState(0x10) < 0 || GetAsyncKeyState(0x11) < 0 || GetAsyncKeyState(0x12) < 0 || GetAsyncKeyState(0x5B) < 0 || GetAsyncKeyState(0x5C) < 0; }
        internal static bool SendFunctionKey(int key)
        {
            var down = new Input { type = 1, keyboard = new KeyboardInput { key = (ushort)key, extra = Marker } };
            var up = new Input { type = 1, keyboard = new KeyboardInput { key = (ushort)key, flags = 2, extra = Marker } };
            uint sent = SendInput(2, new[] { down, up }, Marshal.SizeOf(typeof(Input)));
            if (sent == 1) SendInput(1, new[] { up }, Marshal.SizeOf(typeof(Input)));
            return sent == 2;
        }
    }

    public sealed class BridgeEngine : IDisposable
    {
        private readonly IntPtr owner;
        private readonly Action<Action> dispatch;
        private readonly Action changed;
        private readonly Native.HookProc keyCallback, mouseCallback;
        private IntPtr keyHook, mouseHook, wheelWindow;
        private readonly KeySequence[] sequences = new KeySequence[256];
        private readonly WheelAccumulator wheel = new WheelAccumulator();
        private DateTime deadline;
        private bool enabled = true, learning, learningUp, pending;
        public bool Enabled { get { return enabled; } set { enabled = value; wheel.Reset(); CancelLearn(); } }
        public string Notice { get; private set; }
        public string LastSignal { get; private set; }
        public int UpCount { get; private set; }
        public int DownCount { get; private set; }
        public int SentCount { get; private set; }
        public Mapping Settings { get; private set; }
        public bool IsLearning { get { return learning; } }
        public bool LearningUp { get { return learningUp; } }
        public bool UsesWheel { get { return Settings.Up.Kind == "wheel" || Settings.Down.Kind == "wheel"; } }
        public bool IsCodex { get { return Native.IsCodexForeground(); } }
        public int SecondsRemaining { get { return learning ? Math.Max(0, (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds)) : 0; } }

        public BridgeEngine(IntPtr owner, Action<Action> dispatch, Action changed)
        {
            this.owner = owner; this.dispatch = dispatch; this.changed = changed;
            // 설정 오류는 원본을 유지한 채 기본 매핑으로 복구하고 화면에 안내
            try { Settings = Service.LoadMapping(); Notice = ""; }
            catch { Settings = Mapping.Default; Notice = "저장된 신호를 읽지 못해 기본 볼륨 신호를 사용합니다."; }
            LastSignal = "아직 입력 없음";
            keyCallback = OnKey; mouseCallback = OnMouse;
            keyHook = Native.SetWindowsHookEx(13, keyCallback, Native.GetModuleHandle(null), 0);
            mouseHook = Native.SetWindowsHookEx(14, mouseCallback, Native.GetModuleHandle(null), 0);
            if (keyHook == IntPtr.Zero || mouseHook == IntPtr.Zero)
            { int error = Marshal.GetLastWin32Error(); Dispose(); throw new Win32Exception(error); }
        }
        public void BeginLearn(bool increase)
        {
            if (pending) return;
            learning = true; learningUp = increase; deadline = DateTime.UtcNow.AddSeconds(8); wheel.Reset();
            Notice = increase ? "높이는 방향으로 노브를 한 칸 돌려 주세요." : "낮추는 방향으로 노브를 한 칸 돌려 주세요.";
            changed();
        }
        public void CancelLearn()
        { if (learning) { learning = false; Notice = "신호 감지를 취소했습니다."; } wheel.Reset(); }
        public void ResetMapping()
        { CancelLearn(); Save(Mapping.Default, "기본 볼륨 신호로 되돌렸습니다."); }
        public void Tick()
        {
            if (learning && (DateTime.UtcNow >= deadline || Native.GetForegroundWindow() != owner))
            { learning = false; Notice = "신호 감지가 끝났습니다. 이 창에서 다시 시도하세요."; }
            if (!Enabled || !IsCodex || Native.ModifiersHeld()) wheel.Reset();
        }
        private void Save(Mapping mapping, string message)
        {
            try { Service.SaveMapping(mapping); Settings = mapping; Notice = message; wheel.Reset(); }
            catch (Exception) { Notice = "신호를 저장하지 못했습니다. 다시 시도하세요."; }
            changed();
        }
        private bool Capture(Trigger trigger)
        {
            if (!learning || Native.GetForegroundWindow() != owner || DateTime.UtcNow >= deadline) return false;
            bool increase = learningUp;
            learning = false; pending = true;
            // 훅 콜백에서는 파일을 쓰지 않고 UI 메시지 처리로 저장을 넘김
            dispatch(delegate
            {
                pending = false;
                var candidate = new Mapping { Version = 1, Up = increase ? trigger : Settings.Up, Down = increase ? Settings.Down : trigger };
                if (!candidate.Valid) { Notice = "두 방향에 같은 신호를 등록할 수 없습니다."; changed(); return; }
                Save(candidate, "신호를 등록했습니다.");
            });
            return true;
        }
        private int Target(Trigger trigger, uint flags)
        {
            if (pending || learning || !Policy.MayRoute(Enabled, IsCodex, Native.ModifiersHeld(), flags)) return 0;
            return Settings.Target(trigger);
        }
        private bool Send(int key)
        {
            if (!Policy.MayRoute(Enabled, IsCodex, Native.ModifiersHeld(), 0)) return false;
            if (!Native.SendFunctionKey(key)) { enabled = false; Notice = "입력 전송에 실패해 일시 중지했습니다."; return false; }
            SentCount++; return true;
        }
        private void Observe(Trigger trigger)
        {
            int target = Settings.Target(trigger);
            if (target == 0) return;
            if (target == 0x86) UpCount++; else DownCount++;
            LastSignal = trigger.DisplayName;
        }
        private IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            if (code < 0) return Native.CallNextHookEx(keyHook, code, message, data);
            var input = (Native.KeyEvent)Marshal.PtrToStructure(data, typeof(Native.KeyEvent));
            if (input.extra == Native.Marker || (input.flags & 2) != 0 || input.key >= 256)
                return Native.CallNextHookEx(keyHook, code, message, data);
            int key = (int)input.key;
            bool down = message.ToInt64() == 0x100 || message.ToInt64() == 0x104;
            bool up = message.ToInt64() == 0x101 || message.ToInt64() == 0x105;
            // 실제 키별 상태를 유지해 학습과 매핑 변경 이후에도 해제 이벤트를 보존
            if (sequences[key] == null) sequences[key] = new KeySequence();
            var trigger = new Trigger { Kind = "key", Code = key };
            int target = 0;
            Func<int, bool> send = Send;
            if (down && learning && Native.GetForegroundWindow() == owner)
            {
                if (key == 27) { target = -1; send = delegate { CancelLearn(); return true; }; }
                else if (trigger.Valid && !Native.ModifiersHeld()) { target = -1; send = delegate { return Capture(trigger); }; }
                else Notice = "보호된 키나 보조 키 조합은 등록할 수 없습니다.";
            }
            else if (down && trigger.Valid) { Observe(trigger); target = Target(trigger, input.flags); }
            bool consumed = sequences[key].Handle(down, up, target, send);
            return consumed ? new IntPtr(1) : Native.CallNextHookEx(keyHook, code, message, data);
        }
        private IntPtr OnMouse(int code, IntPtr message, IntPtr data)
        {
            // 클릭·드래그는 학습과 변환 모두에서 제외하고 수직 휠만 검사
            if (code < 0 || message.ToInt64() != 0x20A) return Native.CallNextHookEx(mouseHook, code, message, data);
            var input = (Native.MouseEvent)Marshal.PtrToStructure(data, typeof(Native.MouseEvent));
            if (input.extra == Native.Marker || (input.flags & 2) != 0) return Native.CallNextHookEx(mouseHook, code, message, data);
            int delta = (short)(input.mouseData >> 16);
            if (delta == 0) return Native.CallNextHookEx(mouseHook, code, message, data);
            var trigger = new Trigger { Kind = "wheel", Code = Math.Sign(delta) };
            if (learning && !Native.ModifiersHeld() && Capture(trigger)) return new IntPtr(1);
            int target = Target(trigger, input.flags);
            if (target == 0) { wheel.Reset(); return Native.CallNextHookEx(mouseHook, code, message, data); }
            IntPtr foreground = Native.GetForegroundWindow();
            if (wheelWindow != foreground) { wheel.Reset(); wheelWindow = foreground; }
            Observe(trigger);
            int steps = Math.Abs(wheel.Add(delta));
            for (int i = 0; i < steps; i++) if (!Send(target)) { wheel.Reset(); break; }
            // 이미 일부 단계를 전송했을 수 있어 원래 휠 이벤트는 재전달하지 않음
            return new IntPtr(1);
        }
        public void Dispose()
        {
            if (keyHook != IntPtr.Zero) { Native.UnhookWindowsHookEx(keyHook); keyHook = IntPtr.Zero; }
            if (mouseHook != IntPtr.Zero) { Native.UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; }
        }
    }
}

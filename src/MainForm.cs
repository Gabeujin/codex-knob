using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodexKnob
{
    internal sealed class MainForm : Form
    {
        internal static readonly Color Ink = Color.FromArgb(29, 31, 30);
        internal static readonly Color Muted = Color.FromArgb(100, 104, 101);
        private readonly CheckBox enabled = new CheckBox(), startup = new CheckBox();
        private readonly Label state = MakeLabel(), upTitle = MakeLabel(), downTitle = MakeLabel();
        private readonly Label upSignal = MakeLabel(), downSignal = MakeLabel(), learning = MakeLabel();
        private readonly Label upOutputLabel = MakeLabel(), downOutputLabel = MakeLabel();
        private readonly ComboBox upOutput = new ComboBox(), downOutput = new ComboBox();
        private readonly Label installState = MakeLabel(), notice = MakeLabel(), settingsNotice = MakeLabel(), diagnostics = MakeLabel();
        private readonly RoundButton learnUp = new RoundButton(), learnDown = new RoundButton(), cancel = new RoundButton();
        private readonly RoundButton install = new RoundButton(), settings = new RoundButton(), help = new RoundButton();
        private readonly RoundButton hide = new RoundButton(), quit = new RoundButton(), reset = new RoundButton();
        private readonly RoundButton back = new RoundButton(), more = new RoundButton();
        private readonly Label[] helpRows = { MakeLabel(), MakeLabel(), MakeLabel() };
        private readonly Label[] safetyRows = { MakeLabel(), MakeLabel(), MakeLabel() };
        private readonly LanguageButton[] languages = new LanguageButton[3];
        private readonly Panel viewport = new Panel(), header = new Panel();
        private readonly Panel[] pages = { new Panel(), new Panel(), new Panel(), new Panel() };
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly ToolStripMenuItem trayOpen = new ToolStripMenuItem(), trayToggle = new ToolStripMenuItem(), trayQuit = new ToolStripMenuItem();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly EventWaitHandle showEvent;
        private BridgeEngine engine;
        private TransitionOverlay transition;
        private int pageIndex;
        private Control homeFocus;
        private bool showRequested, exiting, syncing, installedKnown, startupKnown, startupNeedsUpgrade, animating;
        private DateTime nextServiceRead = DateTime.MinValue;
        private string serviceNotice = "", serviceReadError = "", errorDetail = "";
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
        private static bool AnimationsEnabled() { bool enabled; return SystemParametersInfo(0x1042, 0, out enabled, 0) && enabled; }

        internal MainForm(bool background, EventWaitHandle showEvent)
        {
            // 고정 화면과 언어를 먼저 준비하고 숨김 실행에서는 창 표시를 차단
            this.showEvent = showEvent; showRequested = !background;
            try { L.Set(Service.LoadLanguage()); } catch { L.Set("ko"); }
            Text = "Codex Knob"; Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); Font = new Font("맑은 고딕", 10F); ForeColor = Ink;
            BackColor = Color.FromArgb(247, 247, 245); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(620, 380); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BuildLayout(); BuildTray();
            engine = new BridgeEngine(Handle, Dispatch, RefreshState);
            WireActions(); ApplyLanguage();
            timer.Interval = 200;
            timer.Tick += delegate {
                if (this.showEvent != null && this.showEvent.WaitOne(0)) OpenWindow();
                engine.Tick(); RefreshState();
            };
            timer.Start();
        }

        private void BuildLayout()
        {
            // 헤더는 고정하고 각 깊이의 내용만 같은 뷰포트에 배치해 스크롤을 없앰
            header.SetBounds(24, 16, 572, 42); header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var title = MakeLabel(); title.Text = "Codex Knob"; title.Font = new Font(Font.FontFamily, 18F, FontStyle.Bold); title.SetBounds(0, 0, 280, 40);
            header.Controls.Add(title);
            var languageRow = new FlowLayoutPanel { Location = new Point(444, 6), Size = new Size(128, 32), WrapContents = false, Margin = Padding.Empty };
            string[] codes = { "ko", "en", "ja" }, names = { "KO", "EN", "JP" };
            for (int i = 0; i < 3; i++)
            {
                var button = new LanguageButton { Text = names[i], Tag = codes[i], AccessibleName = i == 0 ? "한국어" : i == 1 ? "English" : "日本語" };
                button.CheckedChanged += LanguageChanged; languages[i] = button; languageRow.Controls.Add(button);
            }
            header.Controls.Add(languageRow); Controls.Add(header);
            viewport.SetBounds(24, 76, 572, 280); viewport.AutoScroll = false; Controls.Add(viewport);
            foreach (var page in pages) { page.Dock = DockStyle.Fill; page.AutoScroll = false; page.BackColor = BackColor; page.Visible = false; viewport.Controls.Add(page); }
            BuildMainPage(); BuildSettingsPage(); BuildHelpPages(); pages[0].Visible = true;
            back.SetBounds(0, 0, 84, 34); pages[1].Controls.Add(back); back.Click += delegate { Navigate(pageIndex == 3 ? 2 : 0, back, true); };
        }

        private void BuildMainPage()
        {
            // 입력 설정 두 행만 중심에 두고 설치와 사용 안내는 다음 깊이에서 제공
            var activeRow = Row(0, 36); enabled.SetBounds(0, 0, 230, 34); enabled.AutoSize = false;
            state.SetBounds(232, 0, 340, 34); state.TextAlign = ContentAlignment.MiddleRight;
            activeRow.Controls.Add(enabled); activeRow.Controls.Add(state); pages[0].Controls.Add(activeRow);
            var mappings = new RoundPanel { Location = new Point(0, 48), Size = new Size(572, 104) };
            mappings.Controls.Add(MappingRow(5, upTitle, upOutputLabel, upOutput, upSignal, learnUp));
            mappings.Controls.Add(MappingRow(55, downTitle, downOutputLabel, downOutput, downSignal, learnDown));
            pages[0].Controls.Add(mappings);
            var learnRow = Row(156, 38); learning.SetBounds(0, 0, 476, 34); learning.Font = new Font(Font.FontFamily, 9F);
            cancel.SetBounds(488, 3, 84, 34); learnRow.Controls.Add(learning); learnRow.Controls.Add(cancel); pages[0].Controls.Add(learnRow);
            notice.SetBounds(0, 198, 572, 32); notice.Font = new Font(Font.FontFamily, 9F); pages[0].Controls.Add(notice);
            var buttons = Row(244, 36);
            settings.SetBounds(0, 0, 84, 34); help.SetBounds(92, 0, 84, 34); hide.SetBounds(396, 0, 84, 34); quit.SetBounds(488, 0, 84, 34);
            buttons.Controls.Add(settings); buttons.Controls.Add(help); buttons.Controls.Add(hide); buttons.Controls.Add(quit); pages[0].Controls.Add(buttons);
        }

        private void BuildSettingsPage()
        {
            var installation = Row(44, 42); installState.SetBounds(0, 0, 476, 40); install.SetBounds(488, 3, 84, 34);
            installation.Controls.Add(installState); installation.Controls.Add(install); pages[1].Controls.Add(installation);
            var startRow = Row(90, 38); startup.SetBounds(0, 0, 572, 36); startRow.Controls.Add(startup); pages[1].Controls.Add(startRow);
            var resetRow = Row(132, 42); var label = MakeLabel(); label.Tag = "기본 볼륨 입력으로 복원"; label.SetBounds(0, 0, 476, 40);
            reset.SetBounds(488, 3, 84, 34); resetRow.Controls.Add(label); resetRow.Controls.Add(reset); pages[1].Controls.Add(resetRow);
            settingsNotice.SetBounds(0, 190, 572, 86); settingsNotice.Font = new Font(Font.FontFamily, 9F); pages[1].Controls.Add(settingsNotice);
        }

        private void BuildHelpPages()
        {
            for (int i = 0; i < 3; i++)
            {
                helpRows[i].SetBounds(0, 44 + i * 56, 572, 50); pages[2].Controls.Add(helpRows[i]);
                safetyRows[i].SetBounds(0, 44 + i * 50, 572, 46); pages[3].Controls.Add(safetyRows[i]);
            }
            more.SetBounds(488, 244, 84, 34); pages[2].Controls.Add(more);
            more.Click += delegate { Navigate(3, more, false); };
            diagnostics.SetBounds(0, 210, 572, 66); diagnostics.Font = new Font(Font.FontFamily, 9F); pages[3].Controls.Add(diagnostics);
        }

        private static Panel Row(int y, int height) { return new Panel { Location = new Point(0, y), Size = new Size(572, height), AutoScroll = false }; }
        private Control MappingRow(int y, Label title, Label outputLabel, ComboBox output, Label signal, Button button)
        {
            var row = new Panel { Location = new Point(12, y), Size = new Size(548, 44), BackColor = Color.White };
            title.SetBounds(4, 1, 132, 42); title.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
            outputLabel.SetBounds(142, 1, 84, 42); outputLabel.ForeColor = Muted;
            output.DropDownStyle = ComboBoxStyle.DropDownList;
            output.SetBounds(228, 9, 76, 28);
            output.MaxDropDownItems = 10;
            for (int key = Policy.FirstFunctionKey; key <= Policy.LastFunctionKey; key++)
                output.Items.Add(((Keys)key).ToString());
            signal.SetBounds(316, 1, 136, 42); signal.ForeColor = Muted;
            signal.TextAlign = ContentAlignment.MiddleRight;
            button.SetBounds(464, 5, 84, 34);
            row.Controls.AddRange(new Control[] { title, outputLabel, output, signal, button });
            return row;
        }

        private void BuildTray()
        {
            tray.Icon = Icon; var menu = new ContextMenuStrip();
            trayOpen.Click += delegate { OpenWindow(); };
            trayToggle.Click += delegate { ClearNotice(); engine.Enabled = !engine.Enabled; RefreshState(); };
            trayQuit.Click += delegate { Quit(); };
            menu.Items.Add(trayOpen); menu.Items.Add(trayToggle); menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(trayQuit);
            tray.ContextMenuStrip = menu; tray.DoubleClick += delegate { OpenWindow(); }; tray.Visible = true;
        }

        private void WireActions()
        {
            // 저장 성공 뒤 실제 상태를 재조회하고 오류 설명은 설정 화면 안에 유지
            enabled.CheckedChanged += delegate { if (!syncing) { ClearNotice(); engine.Enabled = enabled.Checked; RefreshState(); } };
            learnUp.Click += delegate { ClearNotice(); engine.BeginLearn(true); RefreshState(); cancel.Focus(); };
            learnDown.Click += delegate { ClearNotice(); engine.BeginLearn(false); RefreshState(); cancel.Focus(); };
            upOutput.SelectionChangeCommitted += delegate { ClearNotice(); engine.SetOutput(true, Policy.FirstFunctionKey + upOutput.SelectedIndex); };
            downOutput.SelectionChangeCommitted += delegate { ClearNotice(); engine.SetOutput(false, Policy.FirstFunctionKey + downOutput.SelectedIndex); };
            cancel.Click += delegate { ClearNotice(); engine.CancelLearn(); RefreshState(); };
            hide.Click += delegate { Hide(); }; quit.Click += delegate { Quit(); };
            settings.Click += delegate { Navigate(1, settings, false); }; help.Click += delegate { Navigate(2, help, false); };
            reset.Click += delegate { ClearNotice(); engine.ResetMapping(); RefreshState(); };
            install.Click += delegate {
                try { Service.Install(); serviceNotice = "설치했습니다."; errorDetail = ""; }
                catch (Exception error) { serviceNotice = "설치하지 못했습니다."; errorDetail = error.Message; }
                nextServiceRead = DateTime.MinValue; RefreshState();
            };
            startup.CheckedChanged += delegate {
                if (syncing) return;
                try { Service.SetStartup(startup.Checked); serviceNotice = startup.Checked ? "자동 시작을 켰습니다." : "자동 시작을 껐습니다."; errorDetail = ""; }
                catch (Exception error) { serviceNotice = "자동 시작을 변경하지 못했습니다."; errorDetail = error.Message; }
                nextServiceRead = DateTime.MinValue; RefreshState();
            };
        }

        private void LanguageChanged(object sender, EventArgs args)
        {
            var button = (RadioButton)sender; if (syncing || !button.Checked || animating) return;
            try { Service.SaveLanguage((string)button.Tag); L.Set((string)button.Tag); ClearNotice(); }
            catch (Exception error) { serviceNotice = "언어를 저장하지 못했습니다."; errorDetail = error.Message; }
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            // 컨트롤은 유지하고 문구와 라디오 상태만 바꿔 접근성과 후크 생명주기를 보존
            syncing = true;
            try { foreach (var button in languages) button.Checked = (string)button.Tag == L.Code; }
            finally { syncing = false; }
            enabled.Text = L.T("노브 사용"); startup.Text = L.T("로그인 시 자동 시작");
            upTitle.Text = "↑  " + L.T("높이기"); downTitle.Text = "↓  " + L.T("낮추기");
            learnUp.Text = learnDown.Text = L.T("감지"); cancel.Text = L.T("취소");
            learnUp.AccessibleName = L.T("높이기 입력 감지"); learnDown.AccessibleName = L.T("낮추기 입력 감지");
            upOutputLabel.Text = downOutputLabel.Text = L.T("출력 키");
            upOutput.AccessibleName = L.T("높이기 출력 키"); downOutput.AccessibleName = L.T("낮추기 출력 키");
            settings.Text = L.T("설정"); help.Text = L.T("사용 안내"); hide.Text = L.T("트레이"); quit.Text = L.T("종료");
            back.Text = "← " + L.T("뒤로"); more.Text = L.T("입력 보호"); reset.Text = L.T("초기화");
            trayOpen.Text = L.T("창 열기"); trayToggle.Text = L.T("사용 / 일시 중지"); trayQuit.Text = L.T("종료");
            for (int i = 0; i < 3; i++) { helpRows[i].Text = L.T("연결 단계 " + (i + 1)); safetyRows[i].Text = L.T("입력 안내 " + (i + 1)); }
            foreach (Control row in pages[1].Controls) foreach (Control item in row.Controls) if (item.Tag is string) item.Text = L.T((string)item.Tag);
            RefreshState();
        }

        private void RefreshState()
        {
            if (engine == null || IsDisposed) return;
            // 전송 횟수는 추론 변경 성공과 구분하고 저장소 조회는 별도 주기로 제한
            syncing = true;
            try { enabled.Checked = engine.Enabled; RefreshServiceState(); } finally { syncing = false; }
            state.Text = L.T(engine.IsLearning ? "입력 대기" : !engine.Enabled ? "일시 중지" : engine.IsCodex ? "Codex 연결" : "Codex 대기");
            state.ForeColor = engine.Enabled ? Color.FromArgb(40, 100, 66) : Muted;
            upSignal.Text = L.T(engine.Settings.Up.DisplayName); downSignal.Text = L.T(engine.Settings.Down.DisplayName);
            if (!upOutput.DroppedDown) upOutput.SelectedIndex = engine.Settings.UpOutput - Policy.FirstFunctionKey;
            if (!downOutput.DroppedDown) downOutput.SelectedIndex = engine.Settings.DownOutput - Policy.FirstFunctionKey;
            upOutput.Enabled = downOutput.Enabled = !engine.IsLearning;
            helpRows[1].Text = String.Format(L.T("연결 단계 2"), (Keys)engine.Settings.UpOutput, (Keys)engine.Settings.DownOutput);
            learnUp.Enabled = learnDown.Enabled = !engine.IsLearning; cancel.Visible = engine.IsLearning;
            settings.Enabled = help.Enabled = !engine.IsLearning; header.Enabled = !engine.IsLearning && !animating;
            learning.Text = engine.IsLearning ? String.Format(L.T("노브를 돌려주세요 · {0}초"), engine.SecondsRemaining) : engine.UsesWheel ? L.T("휠 연결됨: Codex의 일반 스크롤도 바뀝니다.") : "";
            learning.ForeColor = engine.UsesWheel ? Color.FromArgb(135, 66, 31) : Muted;
            string key = !String.IsNullOrEmpty(serviceReadError) ? "설정 상태를 확인하지 못했습니다." : !String.IsNullOrEmpty(serviceNotice) ? serviceNotice : engine.Notice;
            notice.Text = L.T(key ?? ""); settingsNotice.Text = notice.Text;
            string detail = !String.IsNullOrEmpty(serviceReadError) ? serviceReadError : errorDetail;
            if (!String.IsNullOrEmpty(detail)) settingsNotice.Text += Environment.NewLine + L.T(detail);
            notice.ForeColor = settingsNotice.ForeColor = String.IsNullOrEmpty(detail) ? Muted : Color.FromArgb(154, 54, 40);
            diagnostics.Text = String.Format(L.T("감지: 높이기 {0} / 낮추기 {1} · 전송 {2}"), engine.UpCount, engine.DownCount, engine.SentCount) + Environment.NewLine + L.T("최근 신호: ") + L.T(engine.LastSignal) + Environment.NewLine + L.T("전송 횟수는 추론 변경 성공을 뜻하지 않습니다.");
            tray.Text = "Codex Knob · " + L.T(engine.Enabled ? "사용 중" : "일시 중지");
        }

        private void RefreshServiceState()
        {
            // 읽기 실패 시 마지막 확인값을 유지하고 잘못된 상태로 추가 변경하지 않음
            if (DateTime.UtcNow >= nextServiceRead)
            {
                nextServiceRead = DateTime.UtcNow.AddSeconds(2);
                try
                {
                    bool installed = Service.IsInstalled, started = Service.StartupEnabled, upgrade = Service.StartupNeedsUpgrade;
                    installedKnown = installed; startupKnown = started; startupNeedsUpgrade = upgrade; serviceReadError = "";
                }
                catch (Exception error) { serviceReadError = error.Message; }
            }
            startup.Checked = startupKnown; startup.Enabled = String.IsNullOrEmpty(serviceReadError);
            install.Enabled = (!installedKnown || startupNeedsUpgrade) && String.IsNullOrEmpty(serviceReadError);
            install.Text = L.T(startupNeedsUpgrade ? "업데이트" : installedKnown ? "설치됨" : "설치");
            installState.Text = installedKnown ? "v" + Service.Version + " · " + L.T("설치됨") : L.T("이 컴퓨터에 설치");
        }

        private void Navigate(int target, Control source, bool backward)
        {
            if (animating || pageIndex == target) return;
            // 클릭 위치와 각 행의 거리로 지연을 정하고 실제 컨트롤 대신 스냅샷만 이동
            engine.CancelLearn(); RefreshState();
            Panel oldPage = pages[pageIndex], nextPage = pages[target];
            if (pageIndex == 0) homeFocus = source;
            Point focus = viewport.PointToClient(source.PointToScreen(new Point(source.Width / 2, source.Height / 2)));
            if (back.Parent != null) back.Parent.Controls.Remove(back);
            if (target != 0) pages[target].Controls.Add(back);
            nextPage.Visible = true; oldPage.BringToFront(); nextPage.PerformLayout();
            if (!AnimationsEnabled())
            { oldPage.Visible = false; pageIndex = target; nextPage.BringToFront(); FocusPage(backward); return; }
            try { transition = new TransitionOverlay(oldPage, nextPage, focus.Y, backward); }
            catch { oldPage.Visible = false; pageIndex = target; nextPage.BringToFront(); FocusPage(backward); return; }
            animating = true; header.Enabled = false;
            viewport.Controls.Add(transition); transition.Dock = DockStyle.Fill; transition.BringToFront();
            oldPage.Visible = nextPage.Visible = false;
            transition.Start(delegate {
                oldPage.Visible = false; nextPage.Visible = true; nextPage.BringToFront(); pageIndex = target;
                animating = false; header.Enabled = true;
                var finished = transition; transition = null; viewport.Controls.Remove(finished); finished.Dispose(); FocusPage(backward);
            });
        }

        private void FocusPage(bool backward) { if (pageIndex == 0) { if (backward && homeFocus != null) homeFocus.Focus(); else enabled.Focus(); } else back.Focus(); }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { return animating || base.ProcessCmdKey(ref msg, keyData); }
        private void ClearNotice() { serviceNotice = ""; errorDetail = ""; }
        private void Dispatch(Action action) { if (!IsDisposed && IsHandleCreated) BeginInvoke(action); }
        private void OpenWindow() { showRequested = true; Show(); if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal; Activate(); }
        private void Quit() { exiting = true; Close(); }
        protected override void SetVisibleCore(bool value) { base.SetVisibleCore(value && showRequested); }
        protected override void OnFormClosing(FormClosingEventArgs e) { if (!exiting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } base.OnFormClosing(e); }
        protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); if (transition != null) transition.Dispose(); if (engine != null) engine.Dispose(); tray.Visible = false; tray.Dispose(); } base.Dispose(disposing); }
        private static Label MakeLabel() { return new Label { TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = Padding.Empty, UseMnemonic = false }; }
    }

    internal sealed class TransitionOverlay : Control
    {
        private sealed class Shot { internal Bitmap Image; internal Rectangle Bounds; internal bool Incoming; internal double Delay; }
        private readonly List<Shot> shots = new List<Shot>();
        private readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();
        private readonly Stopwatch elapsed = new Stopwatch();
        private readonly bool backward;
        private Action done;
        internal TransitionOverlay(Panel previous, Panel next, int focusY, bool backward)
        {
            this.backward = backward; BackColor = previous.BackColor; TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            try { CaptureRows(previous, false, focusY); CaptureRows(next, true, focusY); }
            catch { foreach (var shot in shots) shot.Image.Dispose(); throw; }
            clock.Interval = 15;
            clock.Tick += delegate { if (elapsed.ElapsedMilliseconds >= 350) { clock.Stop(); done(); } else Invalidate(); };
        }
        private void CaptureRows(Panel page, bool incoming, int focusY)
        {
            foreach (Control control in page.Controls)
            {
                if (!control.Visible || control.Width < 1 || control.Height < 1) continue;
                var image = new Bitmap(control.Width, control.Height);
                try { control.DrawToBitmap(image, new Rectangle(Point.Empty, control.Size)); }
                catch { image.Dispose(); throw; }
                double distance = Math.Abs(control.Top + control.Height / 2 - focusY);
                shots.Add(new Shot { Image = image, Bounds = control.Bounds, Incoming = incoming, Delay = Math.Min(75, distance / 75 * 25) + (incoming ? 35 : 0) });
            }
        }
        internal void Start(Action complete) { done = complete; elapsed.Start(); clock.Start(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            // 행별 스냅샷을 축소·이동·페이드하여 원본 글꼴과 컨트롤 배치는 바꾸지 않음
            e.Graphics.Clear(BackColor); e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            foreach (var shot in shots)
            {
                double raw = Math.Max(0, Math.Min(1, (elapsed.ElapsedMilliseconds - shot.Delay) / 240.0));
                float t = (float)(1 - Math.Pow(1 - raw, 3));
                float scale = shot.Incoming ? (backward ? .94F + .06F * t : 1F) : (backward ? 1F : 1F - .06F * t);
                float offset = shot.Incoming ? (backward ? -20 : 60) * (1 - t) : (backward ? 60 : -20) * t;
                float alpha = shot.Incoming ? t : 1 - t;
                var bounds = shot.Bounds; float width = bounds.Width * scale, height = bounds.Height * scale;
                var target = new Rectangle((int)(bounds.X + (bounds.Width - width) / 2 + offset), (int)(bounds.Y + (bounds.Height - height) / 2), (int)width, (int)height);
                using (var attributes = new ImageAttributes())
                {
                    var matrix = new ColorMatrix(); matrix.Matrix33 = alpha; attributes.SetColorMatrix(matrix);
                    e.Graphics.DrawImage(shot.Image, target, 0, 0, shot.Image.Width, shot.Image.Height, GraphicsUnit.Pixel, attributes);
                }
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) { clock.Dispose(); foreach (var shot in shots) shot.Image.Dispose(); shots.Clear(); } base.Dispose(disposing); }
    }

    internal static class RoundedShape
    {
        internal static GraphicsPath Path(Rectangle rectangle)
        {
            var path = new GraphicsPath(); const int diameter = 16;
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure(); return path;
        }
    }

    internal sealed class RoundButton : Button
    {
        private bool hover, pressed;
        internal RoundButton()
        {
            Size = new Size(84, 34); MinimumSize = Size; MaximumSize = Size;
            Anchor = AnchorStyles.Top | AnchorStyles.Left; Margin = new Padding(4, 0, 0, 0); FlatStyle = FlatStyle.Flat;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            // 기본 Button의 클릭·키보드·접근성 동작은 유지하고 시각 형태만 공통 반경으로 표현
            e.Graphics.Clear(Parent == null ? SystemColors.Control : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedShape.Path(new Rectangle(1, 1, Width - 3, Height - 3)))
            using (var brush = new SolidBrush(!Enabled ? Color.FromArgb(239, 240, 237) : pressed ? Color.FromArgb(224, 226, 221) : hover ? Color.FromArgb(238, 239, 235) : Color.White))
            using (var pen = new Pen(Focused ? MainForm.Ink : Color.FromArgb(211, 214, 208), Focused ? 2F : 1F))
            {
                e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? MainForm.Ink : MainForm.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class LanguageButton : RadioButton
    {
        internal LanguageButton()
        {
            Size = new Size(40, 30); Margin = new Padding(2, 0, 0, 0); Appearance = Appearance.Button;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            // 라디오 선택 상태를 접근성에 유지하며 테두리 굵기와 밑줄로도 선택을 구분
            e.Graphics.Clear(Parent == null ? SystemColors.Control : Parent.BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedShape.Path(new Rectangle(1, 1, Width - 3, Height - 3)))
            using (var fill = new SolidBrush(Checked ? Color.White : Color.FromArgb(247, 247, 245)))
            using (var pen = new Pen(Checked || Focused ? MainForm.Ink : Color.FromArgb(211, 214, 208), Checked || Focused ? 2F : 1F))
            { e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(pen, path); }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, MainForm.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (Checked) using (var pen = new Pen(MainForm.Ink)) e.Graphics.DrawLine(pen, 14, Height - 6, Width - 14, Height - 6);
        }
    }

    internal sealed class RoundPanel : Panel
    {
        internal RoundPanel() { DoubleBuffered = true; BackColor = Color.White; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? SystemColors.Control : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedShape.Path(new Rectangle(1, 1, Width - 3, Height - 3)))
            using (var fill = new SolidBrush(Color.White))
            using (var pen = new Pen(Color.FromArgb(220, 222, 217)))
            { e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(pen, path); e.Graphics.DrawLine(pen, 16, Height / 2, Width - 17, Height / 2); }
        }
    }
}

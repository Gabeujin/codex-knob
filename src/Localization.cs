using System;
using System.Collections.Generic;

namespace CodexKnob
{
    internal static class L
    {
        internal static string Code { get; private set; }
        static L() { Code = "ko"; }
        private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "노브 사용", new[] { "노브 사용", "Enable knob", "ノブを使用" } },
            { "로그인 시 자동 시작", new[] { "로그인 시 자동 시작", "Start at sign-in", "ログイン時に起動" } },
            { "높이기", new[] { "높이기", "Increase", "上げる" } },
            { "낮추기", new[] { "낮추기", "Decrease", "下げる" } },
            { "감지", new[] { "감지", "Detect", "検出" } },
            { "취소", new[] { "취소", "Cancel", "キャンセル" } },
            { "높이기 입력 감지", new[] { "높이기 입력 감지", "Detect increase input", "上げる入力を検出" } },
            { "낮추기 입력 감지", new[] { "낮추기 입력 감지", "Detect decrease input", "下げる入力を検出" } },
            { "도움말", new[] { "도움말", "Help", "ヘルプ" } },
            { "트레이", new[] { "트레이", "Tray", "トレイ" } },
            { "종료", new[] { "종료", "Quit", "終了" } },
            { "창 열기", new[] { "창 열기", "Open window", "ウィンドウを開く" } },
            { "사용 / 일시 중지", new[] { "사용 / 일시 중지", "Enable / pause", "使用 / 一時停止" } },
            { "입력 대기", new[] { "입력 대기", "Listening", "入力待ち" } },
            { "일시 중지", new[] { "일시 중지", "Paused", "一時停止" } },
            { "Codex 연결", new[] { "Codex 연결", "Codex active", "Codex がアクティブ" } },
            { "Codex 대기", new[] { "Codex 대기", "Waiting for Codex", "Codex 待機中" } },
            { "사용 중", new[] { "사용 중", "Enabled", "使用中" } },
            { "노브를 돌려주세요 · {0}초", new[] { "노브를 돌려주세요 · {0}초", "Turn the knob · {0}s", "ノブを回してください · {0}秒" } },
            { "휠 연결됨: Codex의 일반 스크롤도 바뀝니다.", new[] { "휠 연결됨: Codex의 일반 스크롤도 바뀝니다.", "Wheel mapped: normal scrolling in Codex also changes.", "ホイール登録中：Codex の通常スクロールも変わります。" } },
            { "설정 상태를 확인하지 못했습니다.", new[] { "설정 상태를 확인하지 못했습니다.", "Could not read settings.", "設定を確認できませんでした。" } },
            { "설치됨", new[] { "설치됨", "Installed", "インストール済み" } },
            { "설치", new[] { "설치", "Install", "インストール" } },
            { "이 컴퓨터에 설치", new[] { "이 컴퓨터에 설치", "Install on this computer", "このコンピューターにインストール" } },
            { "설치했습니다.", new[] { "설치했습니다.", "Installed.", "インストールしました。" } },
            { "설치하지 못했습니다.", new[] { "설치하지 못했습니다.", "Installation failed.", "インストールできませんでした。" } },
            { "자동 시작을 켰습니다.", new[] { "자동 시작을 켰습니다.", "Start at sign-in enabled.", "ログイン時の起動を有効にしました。" } },
            { "자동 시작을 껐습니다.", new[] { "자동 시작을 껐습니다.", "Start at sign-in disabled.", "ログイン時の起動を無効にしました。" } },
            { "자동 시작을 변경하지 못했습니다.", new[] { "자동 시작을 변경하지 못했습니다.", "Could not change startup settings.", "起動設定を変更できませんでした。" } },
            { "언어를 저장하지 못했습니다.", new[] { "언어를 저장하지 못했습니다.", "Could not save the language.", "言語を保存できませんでした。" } },
            { "연결 안내", new[] { "연결 안내", "Connect to Codex", "Codex との接続" } },
            { "감지: 높이기 {0} / 낮추기 {1} · 전송 {2}", new[] { "감지: 높이기 {0} / 낮추기 {1} · 전송 {2}", "Detected: up {0} / down {1} · sent {2}", "検出：上げる {0} / 下げる {1} · 送信 {2}" } },
            { "최근 신호: ", new[] { "최근 신호: ", "Last input: ", "最近の入力：" } },
            { "전송 횟수는 추론 변경 성공을 뜻하지 않습니다.", new[] { "전송 횟수는 추론 변경 성공을 뜻하지 않습니다.", "Sent counts do not confirm that reasoning changed.", "送信回数は推論レベルの変更成功を示すものではありません。" } },
            { "오류: ", new[] { "오류: ", "Error: ", "エラー：" } },
            { "닫기", new[] { "닫기", "Close", "閉じる" } },
            { "초기화", new[] { "초기화", "Reset", "初期化" } },
            { "휠 위로", new[] { "휠 위로", "Wheel up", "ホイール上" } },
            { "휠 아래로", new[] { "휠 아래로", "Wheel down", "ホイール下" } },
            { "볼륨 증가", new[] { "볼륨 증가", "Volume up", "音量を上げる" } },
            { "볼륨 감소", new[] { "볼륨 감소", "Volume down", "音量を下げる" } },
            { "음소거", new[] { "음소거", "Mute", "ミュート" } },
            { "아직 입력 없음", new[] { "아직 입력 없음", "No input yet", "入力はまだありません" } },
            { "Codex에서만 추론을 조절합니다.", new[] { "Codex에서만 추론을 조절합니다.", "Changes reasoning only in Codex.", "Codex でのみ推論レベルを調整します。" } },
            { "저장된 신호를 읽지 못해 기본 볼륨 신호를 사용합니다.", new[] { "저장된 신호를 읽지 못해 기본 볼륨 신호를 사용합니다.", "Could not read saved inputs; using volume keys.", "保存した入力を読み込めないため、音量キーを使用します。" } },
            { "높이는 방향으로 노브를 한 칸 돌려 주세요.", new[] { "높이는 방향으로 노브를 한 칸 돌려 주세요.", "Turn the knob one step in the increase direction.", "上げる方向にノブを1段階回してください。" } },
            { "낮추는 방향으로 노브를 한 칸 돌려 주세요.", new[] { "낮추는 방향으로 노브를 한 칸 돌려 주세요.", "Turn the knob one step in the decrease direction.", "下げる方向にノブを1段階回してください。" } },
            { "신호 감지를 취소했습니다.", new[] { "신호 감지를 취소했습니다.", "Input detection canceled.", "入力の検出をキャンセルしました。" } },
            { "기본 볼륨 신호로 되돌렸습니다.", new[] { "기본 볼륨 신호로 되돌렸습니다.", "Restored the default volume inputs.", "既定の音量入力に戻しました。" } },
            { "신호 감지가 끝났습니다. 이 창에서 다시 시도하세요.", new[] { "신호 감지가 끝났습니다. 이 창에서 다시 시도하세요.", "Detection ended. Try again in this window.", "検出を終了しました。このウィンドウでもう一度お試しください。" } },
            { "두 방향에 같은 신호를 등록할 수 없습니다.", new[] { "두 방향에 같은 신호를 등록할 수 없습니다.", "The two directions need different inputs.", "両方向に同じ入力は登録できません。" } },
            { "신호를 등록했습니다.", new[] { "신호를 등록했습니다.", "Input saved.", "入力を登録しました。" } },
            { "신호를 저장하지 못했습니다. 다시 시도하세요.", new[] { "신호를 저장하지 못했습니다. 다시 시도하세요.", "Could not save the input. Try again.", "入力を保存できませんでした。もう一度お試しください。" } },
            { "입력 전송에 실패해 일시 중지했습니다.", new[] { "입력 전송에 실패해 일시 중지했습니다.", "Input sending failed. Paused.", "入力の送信に失敗したため、一時停止しました。" } },
            { "보호된 키나 보조 키 조합은 등록할 수 없습니다.", new[] { "보호된 키나 보조 키 조합은 등록할 수 없습니다.", "Protected keys and modifier combinations cannot be assigned.", "保護されたキーや修飾キーの組み合わせは登録できません。" } },
            { "자동 시작 경로가 너무 깁니다.", new[] { "자동 시작 경로가 너무 깁니다.", "The startup path is too long.", "起動パスが長すぎます。" } },
            { "같은 버전의 다른 파일이 설치되어 있습니다. 기존 파일을 보존한 뒤 확인해 주세요.", new[] { "같은 버전의 다른 파일이 설치되어 있습니다. 기존 파일을 보존한 뒤 확인해 주세요.", "A different file of this version is already installed. Keep it intact and check the installation.", "同じバージョンの別のファイルがインストールされています。既存のファイルを保持して確認してください。" } },
            { "설치 파일 검증에 실패했습니다.", new[] { "설치 파일 검증에 실패했습니다.", "Installation file verification failed.", "インストールファイルの検証に失敗しました。" } },
            { "같은 이름의 다른 자동 시작 항목이 있어 변경하지 않았습니다.", new[] { "같은 이름의 다른 자동 시작 항목이 있어 변경하지 않았습니다.", "A different startup entry uses this name. Nothing was changed.", "同じ名前の別の起動項目があるため、変更しませんでした。" } },
            { "자동 시작 설정을 확인하지 못했습니다.", new[] { "자동 시작 설정을 확인하지 못했습니다.", "Could not verify startup settings.", "起動設定を確認できませんでした。" } },
            { "등록할 수 없는 신호 설정입니다.", new[] { "등록할 수 없는 신호 설정입니다.", "This input mapping is not supported.", "この入力設定は登録できません。" } },
            { "설정 저장 검증에 실패했습니다.", new[] { "설정 저장 검증에 실패했습니다.", "Settings save verification failed.", "設定の保存検証に失敗しました。" } },
            { "설정 반영 검증에 실패했습니다.", new[] { "설정 반영 검증에 실패했습니다.", "Settings readback verification failed.", "反映した設定の検証に失敗しました。" } },
            { "백업 기록 검증에 실패했습니다.", new[] { "백업 기록 검증에 실패했습니다.", "Backup record verification failed.", "バックアップ記録の検証に失敗しました。" } },
            { "언어 설정 검증에 실패했습니다.", new[] { "언어 설정 검증에 실패했습니다.", "Language setting verification failed.", "言語設定の検証に失敗しました。" } },
            { "지원하지 않는 언어입니다.", new[] { "지원하지 않는 언어입니다.", "This language is not supported.", "この言語には対応していません。" } },
            { "Codex Knob을 시작하지 못했습니다.", new[] { "Codex Knob을 시작하지 못했습니다.", "Could not start Codex Knob.", "Codex Knob を起動できませんでした。" } },
            { "PageUp", new[] { "PageUp", "Page Up", "Page Up" } },
            { "Next", new[] { "Next", "Page Down", "Page Down" } },
            { "Prior", new[] { "Prior", "Page Up", "Page Up" } },
            { "PageDown", new[] { "PageDown", "Page Down", "Page Down" } },
            { "Left", new[] { "Left", "Left arrow", "左矢印" } },
            { "Right", new[] { "Right", "Right arrow", "右矢印" } },
            { "Up", new[] { "Up", "Up arrow", "上矢印" } },
            { "Down", new[] { "Down", "Down arrow", "下矢印" } },
            { "MediaNextTrack", new[] { "MediaNextTrack", "Next track", "次のトラック" } },
            { "MediaPreviousTrack", new[] { "MediaPreviousTrack", "Previous track", "前のトラック" } },
            { "MediaStop", new[] { "MediaStop", "Stop media", "メディア停止" } },
            { "MediaPlayPause", new[] { "MediaPlayPause", "Play / pause", "再生 / 一時停止" } },
            { "설정", new[] { "설정", "Settings", "設定" } },
            { "사용 안내", new[] { "사용 안내", "Guide", "使い方" } },
            { "뒤로", new[] { "뒤로", "Back", "戻る" } },
            { "입력 보호", new[] { "입력 보호", "Input safety", "入力保護" } },
            { "기본 볼륨 입력으로 복원", new[] { "기본 볼륨 입력으로 복원", "Restore volume inputs", "既定の音量入力に戻す" } },
            { "연결 단계 1", new[] { "1. Codex 설정 → 키보드 단축키에서 ‘추론’을 검색합니다.", "1. Open Codex Settings → Keyboard shortcuts. Search for “reasoning”.", "1. Codex の設定 → キーボードショートカットで、推論を検索します。" } },
            { "연결 단계 2", new[] { "2. 높이기에 F23, 낮추기에 F24를 저장합니다.", "2. Assign F23 to Increase reasoning effort and F24 to Decrease reasoning effort.", "2. 推論を上げる操作に F23、下げる操作に F24 を設定します。" } },
            { "연결 단계 3", new[] { "3. 기존 채팅 입력창에서 노브를 돌립니다. Codex가 앞에 있을 때만 전환합니다.", "3. Return to an existing chat composer and turn the knob. Changes apply only while Codex is in front.", "3. 既存チャットの入力欄に戻り、ノブを回します。Codex が前面にあるときだけ動作します。" } },
            { "입력 안내 1", new[] { "클릭·문자·Enter·Esc는 등록할 수 없습니다. 감지 중 Esc는 취소합니다.", "Clicks, typing, Enter and Esc cannot be assigned. Esc cancels input detection.", "クリック・文字・Enter・Esc は登録できません。検出中の Esc はキャンセルです。" } },
            { "입력 안내 2", new[] { "Ctrl·Alt·Shift·Windows 키를 누르면 전환하지 않습니다. 같은 신호를 보내는 다른 장치에도 적용됩니다.", "Holding Ctrl, Alt, Shift or Windows bypasses conversion. Other devices sending the same input are also affected.", "Ctrl・Alt・Shift・Windows キーを押している間は変換しません。同じ入力を送る他の機器にも適用されます。" } },
            { "입력 안내 3", new[] { "휠을 등록하면 Codex의 일반 마우스 스크롤도 바뀝니다. 노브가 지원한다면 볼륨 입력을 사용하세요.", "Mapping the wheel also changes normal mouse scrolling in Codex. Use volume input if your knob supports it.", "ホイールを登録すると、Codex の通常のマウススクロールも変わります。対応するノブでは音量入力を使用してください。" } },
            { "도움말 본문", new[] { "1. Codex 설정 → 키보드 단축키에서 ‘추론’을 검색합니다.\r\n2. 높이기에 F23, 낮추기에 F24를 저장합니다.\r\n3. 기존 채팅의 입력창에서 노브를 돌립니다.\r\n\r\nCodex가 앞에 있을 때만 전환합니다. 같은 신호를 보내는 다른 장치에도 적용됩니다.\r\n클릭·문자·Enter·Esc는 등록할 수 없습니다. 감지 중 Esc는 취소합니다.\r\nCtrl·Alt·Shift·Windows 키를 누르면 전환하지 않습니다.\r\n휠을 등록하면 Codex의 일반 마우스 스크롤도 바뀝니다.\r\n초기화는 입력을 기본 볼륨 증가·감소로 되돌립니다.", "1. In Codex Settings → Keyboard shortcuts, search for “reasoning”.\r\n2. Assign F23 to Increase reasoning effort and F24 to Decrease reasoning effort.\r\n3. Turn the knob in an existing chat composer.\r\n\r\nOnly works while Codex is in front. Other devices sending the same input are also affected.\r\nClicks, typing, Enter and Esc cannot be assigned. Esc cancels detection.\r\nHolding Ctrl, Alt, Shift or Windows bypasses conversion.\r\nMapping the wheel also changes normal mouse scrolling in Codex.\r\nReset restores the default volume up/down inputs.", "1. Codex の設定 → キーボードショートカットで推論を検索します。\r\n2. 推論を上げる操作に F23、下げる操作に F24 を設定します。\r\n3. 既存チャットの入力欄でノブを回します。\r\n\r\nCodex が前面にあるときだけ動作します。同じ入力を送る他の機器にも適用されます。\r\nクリック・文字・Enter・Esc は登録できません。検出中の Esc はキャンセルです。\r\nCtrl・Alt・Shift・Windows キーを押している間は変換しません。\r\nホイールを登録すると、Codex の通常のマウススクロールも変わります。\r\n初期化すると、既定の音量アップ・ダウンの入力に戻ります。" } }
        };
        internal static void Set(string code) { Code = code == "en" || code == "ja" ? code : "ko"; }
        internal static string T(string key)
        {
            if (String.IsNullOrEmpty(key)) return "";
            string[] values;
            return Texts.TryGetValue(key, out values) ? values[Code == "en" ? 1 : Code == "ja" ? 2 : 0] : key;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using unvell.ReoGrid;

namespace GeoChemistryNexus.Helpers
{
    /// <summary>
    /// 修复 ReoGrid WPF 中文/英文首键输入问题。
    /// <para>
    /// PreviewKeyDown 时提前 StartEdit 并聚焦编辑框，保证 IME 候选框位置正确；
    /// 但焦点切换会吃掉当前这次按键，因此需要 Handled 原按键后，把该键重投递给编辑框。
    /// </para>
    /// </summary>
    public static class ReoGridImeHelper
    {
        private const uint InputKeyboard = 1;
        private const uint KeyeventfKeyup = 0x0002;

        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached(
                "IsAttached",
                typeof(bool),
                typeof(ReoGridImeHelper),
                new PropertyMetadata(false));

        private static readonly DependencyProperty SuppressOrphanTextInputProperty =
            DependencyProperty.RegisterAttached(
                "SuppressOrphanTextInput",
                typeof(bool),
                typeof(ReoGridImeHelper),
                new PropertyMetadata(false));

        /// <summary>
        /// 为指定 ReoGrid 启用 IME/首键输入修复（幂等）。
        /// </summary>
        public static void Attach(ReoGridControl? grid)
        {
            if (grid == null)
                return;

            if ((bool)grid.GetValue(IsAttachedProperty))
                return;

            grid.SetValue(IsAttachedProperty, true);
            grid.PreviewKeyDown += OnPreviewKeyDown;
            TextCompositionManager.AddPreviewTextInputHandler(grid, OnPreviewTextInput);
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ReoGridControl grid)
                return;

            var worksheet = grid.CurrentWorksheet;
            if (worksheet == null || worksheet.IsEditing)
                return;

            if (!ShouldStartEditOnKey(e))
                return;

            if (!worksheet.StartEdit(string.Empty))
                return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var editBox = FindEditTextBox(grid);
            editBox?.Focus();

            // 原按键若继续沿 grid 传递，会变成“被挂起的首字符”；吃掉后由我们重投递给编辑框
            e.Handled = true;
            grid.SetValue(SuppressOrphanTextInputProperty, true);

            bool imeOn = InputMethod.Current?.ImeState == InputMethodState.On;
            if (!imeOn)
            {
                char? ch = TryGetCharFromKey(key);
                if (ch.HasValue)
                {
                    ApplyEditText(grid, worksheet, ch.Value.ToString());
                    return;
                }
            }

            Key? keyToRedeliver = key == Key.ImeProcessed ? FindPressedTextKey() : key;
            if (keyToRedeliver is null or Key.ImeProcessed)
                return;

            grid.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
            {
                var box = FindEditTextBox(grid) ?? editBox;
                box?.Focus();
                RedeliverKey(keyToRedeliver.Value);
                grid.SetValue(SuppressOrphanTextInputProperty, false);
            });
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not ReoGridControl grid || string.IsNullOrEmpty(e.Text))
                return;

            var worksheet = grid.CurrentWorksheet;
            if (worksheet == null)
                return;

            if (!worksheet.IsEditing)
            {
                if ((bool)grid.GetValue(SuppressOrphanTextInputProperty)
                    && e.Text.Length == 1
                    && IsAsciiLetterOrDigit(e.Text[0]))
                {
                    grid.SetValue(SuppressOrphanTextInputProperty, false);
                    e.Handled = true;
                    return;
                }

                if (worksheet.StartEdit(e.Text))
                {
                    MoveCaretToEnd(grid, e.Text);
                    e.Handled = true;
                }

                return;
            }

            if (string.IsNullOrEmpty(worksheet.CellEditText))
            {
                ApplyEditText(grid, worksheet, e.Text);
                e.Handled = true;
            }
        }

        private static bool ShouldStartEditOnKey(KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0)
                return false;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key >= Key.A && key <= Key.Z)
                return true;
            if (key >= Key.D0 && key <= Key.D9)
                return true;
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return true;

            return key switch
            {
                Key.OemPlus or Key.OemMinus or Key.OemComma or Key.OemPeriod
                    or Key.OemQuestion or Key.OemQuotes or Key.OemSemicolon or Key.OemTilde
                    or Key.OemOpenBrackets or Key.OemCloseBrackets or Key.OemPipe
                    or Key.OemBackslash or Key.Space or Key.Multiply or Key.Add
                    or Key.Subtract or Key.Decimal or Key.Divide
                    or Key.ImeProcessed => true,
                _ => false
            };
        }

        private static Key? FindPressedTextKey()
        {
            for (var k = Key.A; k <= Key.Z; k++)
            {
                if (Keyboard.IsKeyDown(k))
                    return k;
            }

            for (var k = Key.D0; k <= Key.D9; k++)
            {
                if (Keyboard.IsKeyDown(k))
                    return k;
            }

            for (var k = Key.NumPad0; k <= Key.NumPad9; k++)
            {
                if (Keyboard.IsKeyDown(k))
                    return k;
            }

            return null;
        }

        private static char? TryGetCharFromKey(Key key)
        {
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool caps = Keyboard.IsKeyToggled(Key.CapsLock);

            if (key >= Key.A && key <= Key.Z)
            {
                char ch = (char)('a' + (key - Key.A));
                if (shift ^ caps)
                    ch = char.ToUpperInvariant(ch);
                return ch;
            }

            if (key >= Key.D0 && key <= Key.D9 && !shift)
                return (char)('0' + (key - Key.D0));

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return (char)('0' + (key - Key.NumPad0));

            if (key == Key.Space)
                return ' ';

            return null;
        }

        private static void RedeliverKey(Key key)
        {
            int vk = KeyInterop.VirtualKeyFromKey(key);
            if (vk <= 0)
                return;

            var inputs = new INPUT[3];
            // 先抬起（物理键可能仍按着），再完整点按一次，确保 IME/TextBox 收到首字符
            inputs[0] = CreateKeyInput((ushort)vk, keyUp: true);
            inputs[1] = CreateKeyInput((ushort)vk, keyUp: false);
            inputs[2] = CreateKeyInput((ushort)vk, keyUp: true);
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        private static INPUT CreateKeyInput(ushort virtualKey, bool keyUp)
        {
            return new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = keyUp ? KeyeventfKeyup : 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        private static bool IsAsciiLetterOrDigit(char ch) =>
            (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9');

        private static void ApplyEditText(ReoGridControl grid, Worksheet worksheet, string text)
        {
            worksheet.CellEditText = text;
            MoveCaretToEnd(grid, text);
            grid.SetValue(SuppressOrphanTextInputProperty, false);
        }

        private static void MoveCaretToEnd(ReoGridControl grid, string text)
        {
            void Move()
            {
                var editBox = FindEditTextBox(grid);
                if (editBox == null)
                    return;

                if (editBox.Text != text)
                    editBox.Text = text;

                int end = editBox.Text?.Length ?? 0;
                editBox.CaretIndex = end;
                editBox.SelectionStart = end;
                editBox.SelectionLength = 0;
            }

            Move();
            grid.Dispatcher.BeginInvoke(DispatcherPriority.Input, Move);
        }

        private static TextBox? FindEditTextBox(ReoGridControl grid)
        {
            foreach (var textBox in FindVisualChildren<TextBox>(grid))
            {
                if (textBox.Visibility == Visibility.Visible)
                    return textBox;
            }

            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    yield return match;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        // 必须按最大成员对齐，否则 x64 下 SendInput 会失败/错乱
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
    }
}

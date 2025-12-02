using NovaLight.Core;
using Terminal.Gui;
using static System.Net.Mime.MediaTypeNames;
using static Terminal.Gui.View;
using Application = Terminal.Gui.Application;
using Color = Terminal.Gui.Color;
using Window = Terminal.Gui.Window;

namespace NovaLight.Console
{
    public delegate void MessageReceived(string message, int id);
    public delegate void InputReceived(string input);

    public static class ConsoleHandler
    {
        private static AssemblyContext? _assemblyContext;
        public static void SwitchAssemblyContext(AssemblyContext? assemblyContext, bool loadLogsHistroy = false)
        {
            if (_assemblyContext != null)
                _assemblyContext.Logger.OnLog -= OnLog;

            _assemblyContext = assemblyContext;
            if (_assemblyContext != null)
            {
                WriteMessage($"{Environment.NewLine}Switching the console to a different AssemblyContext has been completed.", Color.BrightYellow);
                _assemblyContext.Logger.OnLog += OnLog;

                if (loadLogsHistroy)
                    foreach (string message in _assemblyContext.Logger.Logs)
                        OnLog(message);
            }
        }

        private static void OnLog(string message) => WriteMessage(message);

        private static ColorTextView _colorTextView = null!;
        private static TextField _inputField = null!;
        private static Window _mainWindow = null!;

        public static void Init(bool showLogo = true)
        {
            Application.Init();

            Thread thread = new(() => Application.Run());
            thread.Start();

            System.Console.Clear();
            Toplevel top = Application.Top;

            ColorScheme baseColors = Colors.Base;
            baseColors.Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black);
            baseColors.Focus = Application.Driver.MakeAttribute(Color.White, Color.Black);
            baseColors.HotNormal = Application.Driver.MakeAttribute(Color.Gray, Color.Black);
            baseColors.HotFocus = Application.Driver.MakeAttribute(Color.White, Color.Black);
            baseColors.Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black);

            _mainWindow = new Window("Console")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(_mainWindow);

            _colorTextView = new ColorTextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            _mainWindow.Add(_colorTextView);

            _inputField = new TextField()
            {
                X = 0,
                Y = Pos.Bottom(_colorTextView),
                Width = Dim.Fill(),
                Height = 1
            };
            _mainWindow.Add(_inputField);

            _inputField.KeyPress += OnInput;

            if (showLogo)
            {
                //_colorTextView.WriteMessage("----------------------------------------------------------------------------------------------", Color.Cyan);
                //_colorTextView.WriteMessage("___  ___                 _____ _           _   _     _       _     _     _____                ", Color.Cyan);
                //_colorTextView.WriteMessage("|  \\/  |                /  __ \\ |         | | | |   (_)     | |   | |   /  __ \\               ", Color.Cyan);
                //_colorTextView.WriteMessage("| .  . | ___  __ _  __ _| /  \\/ |__   __ _| |_| |    _  __ _| |__ | |_  | /  \\/ ___  _ __ ___ ", Color.Cyan);
                //_colorTextView.WriteMessage("| |\\/| |/ _ \\/ _` |/ _` | |   | '_ \\ / _` | __| |   | |/ _` | '_ \\| __| | |    / _ \\| '__/ _ \\", Color.Cyan);
                //_colorTextView.WriteMessage("| |  | |  __/ (_| | (_| | \\__/\\ | | | (_| | |_| |___| | (_| | | | | |_  | \\__/\\ (_) | | |  __/", Color.Cyan);
                //_colorTextView.WriteMessage("\\_|  |_/\\___|\\__, |\\__,_|\\____/_| |_|\\__,_|\\__\\_____/_|\\__, |_| |_|\\__|  \\____/\\___/|_|  \\___|", Color.Cyan);
                //_colorTextView.WriteMessage("              __/ |                                     __/ |                                 ", Color.Cyan);
                //_colorTextView.WriteMessage("             |___/                                     |___/                                  ", Color.Cyan);
                //_colorTextView.WriteMessage("----------------------------------------------------------------------------------------------", Color.Cyan);

                WriteMessage("------------------------------------------------------------------------------------------------", Color.Cyan);
                WriteMessage(" ______ |\\                     ___     __         /\\      __    _________                       ", Color.Cyan);
                WriteMessage(" \\     \\| | ____ ___  ______  |   |   |__|  ____ |  |__ _/  |_  \\_   ___ \\  ____ _______  ____  ", Color.Cyan);
                WriteMessage(" /   |    |/ __ \\\\  \\/ /__  \\ |   |   |  | / ___\\|  |  \\\\   __\\ /    \\  \\/ / __ \\\\_  __ \\/ __ \\", Color.Cyan);
                WriteMessage("/    |\\   |  \\_\\ )\\   / / __ \\_   |___|  |/ /_/  \\      \\|  |   \\     \\____  \\_\\ )|  | \\/  ___/_", Color.Cyan);
                WriteMessage("\\____| \\  /\\____/  \\_/ (____  /______ \\__|\\___  /|___|  /|__|    \\______  /\\____/ |__|   \\___  /", Color.Cyan);
                WriteMessage("        \\/                  \\/       \\/  /_____/      \\/                \\/                   \\/ ", Color.Cyan);
                WriteMessage("------------------------------------------------------------------------------------------------", Color.Cyan);

                WriteMessage("Lightweight modular core for console applications", Color.Green);
                WriteMessage($"Developed by CDW Studio", Color.Green);
            }
        }

        private static void OnInput(KeyEventEventArgs key)
        {
            if (key.KeyEvent.Key == Key.Enter)
            {
                string? text = _inputField.Text.ToString();
                _inputField.Text = "";

                HandleInput(text);

                key.Handled = true;
            }
        }

        public static void HandleInput(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _colorTextView.WriteMessage($">> {text}", Color.Green);

            Task.Run(() =>
            {
                InputReceived?.Invoke(text);
            });
        }

        public static string[] Lines => _colorTextView.Lines;
        public static event MessageReceived? MessageReceived;
        public static event InputReceived? InputReceived;

        public static void WriteMessage(string text, Color color = Color.Gray)
        {
            if (text.Contains(Environment.NewLine))
            {
                string[] splittedText = text.Split(Environment.NewLine);
                splittedText.ToList().ForEach(x => WriteMessage(x, color));
                return;
            }

            Application.MainLoop.Invoke(() =>
            {
                int id = _colorTextView.WriteMessage(text, color);
                MessageReceived?.Invoke(text, id);
            });
        }
    }
}
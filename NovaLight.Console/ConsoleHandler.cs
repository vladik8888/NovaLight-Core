using Terminal.Gui;
using static Terminal.Gui.View;
using Application = Terminal.Gui.Application;
using Color = Terminal.Gui.Color;
using Window = Terminal.Gui.Window;

namespace NovaLight.Console
{
    public delegate void MessageReceived(string message, int lineId);
    public delegate void InputReceived(string input);

    public static class ConsoleHandler
    {
        private static Window _mainWindow = null!;
        private static ColorTextView _colorTextView = null!;
        private static TextField _inputField = null!;

        private static bool _initializated = false;
        public static void Init(bool showLogo = true)
        {
            if (_initializated)
                throw new InvalidOperationException("The ConsoleHandler is already initializated.");
            _initializated = true;

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
                WriteMessage("------------------------------------------------------------------------------------------------", Color.Cyan);
                WriteMessage(" ______ |\\                     ___     __         /\\      __    _________                       ", Color.Cyan);
                WriteMessage(" \\     \\| | ____ ___  ______  |   |   |__|  ____ |  |__ _/  |_  \\_   ___ \\  ____ _______  ____  ", Color.Cyan);
                WriteMessage(" /   |    |/ __ \\\\  \\/ /__  \\ |   |   |  | / ___\\|  |  \\\\   __\\ /    \\  \\/ / __ \\\\_  __ \\/ __ \\", Color.Cyan);
                WriteMessage("/    |\\   |  \\_\\ )\\   / / __ \\_   |___|  |/ /_/  \\      \\|  |   \\     \\____  \\_\\ )|  | \\/  ___/_", Color.Cyan);
                WriteMessage("\\____| \\  /\\____/  \\_/ (____  /______ \\__|\\___  /|___|  /|__|    \\______  /\\____/ |__|   \\___  /", Color.Cyan);
                WriteMessage("        \\/                  \\/       \\/  /_____/      \\/                \\/                   \\/ ", Color.Cyan);
                WriteMessage("------------------------------------------------------------------------------------------------", Color.Cyan);

                WriteMessage("Lightweight modular core for console applications", Color.Green);
                WriteMessage($"Developed by CDW Studio \n", Color.Green);
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

        public static void HandleInput(string? command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            _colorTextView.WriteMessage($">> {command}", Color.Green);
            Task.Run(() => InputReceived?.Invoke(command));
        }

        public static string[] Lines => _colorTextView.Lines;
        public static event MessageReceived? MessageReceived;
        public static event InputReceived? InputReceived;

        public static void WriteMessage(string text, Color color = Color.Gray)
        {
            string[] splittedText = text.Split("\n");
            if (splittedText.Length > 1)
            {
                splittedText.ToList().ForEach(x => WriteMessage(x, color));
                return;
            }

            Application.MainLoop.Invoke(() =>
            {
                int lineId = _colorTextView.WriteMessage(text, color);
                MessageReceived?.Invoke(text, lineId);
            });
        }
    }
}
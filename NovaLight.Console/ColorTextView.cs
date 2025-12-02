using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace NovaLight.Console
{
    internal class ColorTextView : View
    {
        public string[] Lines => [.. _lines.Select(x => x.Text)];

        private int _scrollOffset = 0;
        private readonly List<(string Text, Color Color)> _lines = [];

        public int WriteMessage(string text, Color color = Color.Gray)
        {
            int visibleHeight = Frame.Height > 0 ? Frame.Height : 1;
            int maxOffsetBefore = Math.Max(0, _lines.Count - visibleHeight);
            bool isAtBottom = _scrollOffset == maxOffsetBefore;

            _lines.Add((text, color));
            int id = _lines.Count - 1;

            if (isAtBottom)
            {
                int maxOffsetAfter = Math.Max(0, _lines.Count - visibleHeight);
                _scrollOffset = maxOffsetAfter;
            }

            ClampScrollOffset();
            SetNeedsDisplay();

            return id;
        }

        private void ClampScrollOffset()
        {
            int visibleHeight = Frame.Height > 0 ? Frame.Height : 1;
            int maxOffset = Math.Max(0, _lines.Count - visibleHeight);
            if (_scrollOffset > maxOffset)
                _scrollOffset = maxOffset;
            if (_scrollOffset < 0)
                _scrollOffset = 0;
        }

        public override bool ProcessKey(KeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Key.CursorUp:
                case Key.PageUp:
                    ScrollUp();
                    return true;
                case Key.CursorDown:
                case Key.PageDown:
                    ScrollDown();
                    return true;
            }
            return base.ProcessKey(keyEvent);
        }
        public override bool MouseEvent(MouseEvent mouse)
        {
            if (mouse.Flags.HasFlag(MouseFlags.WheeledUp))
            {
                ScrollUp();
                return true;
            }
            else if (mouse.Flags.HasFlag(MouseFlags.WheeledDown))
            {
                ScrollDown();
                return true;
            }
            return base.MouseEvent(mouse);
        }

        public void ScrollUp()
        {
            if (_scrollOffset > 0)
            {
                _scrollOffset--;
                SetNeedsDisplay();
            }
        }
        public void ScrollDown()
        {
            int visibleHeight = Frame.Height > 0 ? Frame.Height : 1;
            int maxOffset = Math.Max(0, _lines.Count - visibleHeight);
            if (_scrollOffset < maxOffset)
            {
                _scrollOffset++;
                SetNeedsDisplay();
            }
        }

        public override void Redraw(Rect bounds)
        {
            Driver.SetAttribute(ColorScheme.Normal);
            Clear();

            int visibleHeight = Frame.Height;
            int startLine = _scrollOffset;
            int endLine = Math.Min(_lines.Count, startLine + visibleHeight);

            int y = 0;
            for (int i = startLine; i < endLine; i++)
            {
                var (text, color) = _lines[i];
                var attr = new Attribute(color, Color.Black);
                Driver.SetAttribute(attr);
                Move(0, y);

                foreach (var ch in text)
                {
                    Driver.AddRune(new Rune(ch));
                }
                y++;
            }
        }
    }
}
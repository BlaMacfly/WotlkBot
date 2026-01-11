using System;
using System.Drawing;
using System.Windows.Forms;

namespace WotlkBotGui
{
    public static class Theme
    {
        // Color Palette "Obsidian"
        public static Color BackColor = Color.FromArgb(30, 30, 30);      // Main Background
        public static Color PanelColor = Color.FromArgb(37, 37, 38);     // Secondary Background (Cards, Sidebars)
        public static Color TextColor = Color.FromArgb(240, 240, 240);   // Primary Text
        public static Color TextDimColor = Color.FromArgb(160, 160, 160); // Secondary Text
        public static Color AccentColor = Color.FromArgb(0, 122, 204);   // Focus/Highlight Blue
        
        public static Color ButtonColor = Color.FromArgb(63, 63, 70);
        public static Color ButtonHoverColor = Color.FromArgb(80, 80, 80);
        public static Color ButtonPressColor = Color.FromArgb(0, 122, 204);

        public static Color SuccessColor = Color.FromArgb(87, 166, 74);  // Green
        public static Color WarningColor = Color.FromArgb(202, 81, 0);   // Orange/Red
        
        public static Font MainFont = new Font("Segoe UI", 9.75F, FontStyle.Regular);
        public static Font HeaderFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static Font SmallFont = new Font("Segoe UI", 8.25F, FontStyle.Regular);

        public static void Apply(Control ctrl)
        {
            // Recursively apply theme
            ApplyCtx(ctrl);
        }

        private static void ApplyCtx(Control ctrl)
        {
            ctrl.ForeColor = TextColor;
            
            // Default font if not specific
            if (ctrl.Font.FontFamily.Name != "Consolas") 
                ctrl.Font = MainFont;

            if (ctrl is Form form)
            {
                form.BackColor = BackColor;
            }
            else if (ctrl is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = ButtonColor;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = ButtonHoverColor;
                btn.FlatAppearance.MouseDownBackColor = ButtonPressColor;
                btn.Cursor = Cursors.Hand;
            }
            else if (ctrl is TextBox txt)
            {
                txt.BackColor = PanelColor;
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.ForeColor = TextColor;
            }
            else if (ctrl is Panel pnl)
            {
                pnl.BackColor = BackColor; // Default, can be overridden manually to PanelColor
            }
            else if (ctrl is Label lbl)
            {
                lbl.BackColor = Color.Transparent;
            }

            foreach (Control child in ctrl.Controls)
            {
                ApplyCtx(child);
            }
        }
    }
}

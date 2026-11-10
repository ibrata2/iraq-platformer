using System.Diagnostics;

namespace IraqPlatformer;

internal static class Credits
{
    public const string Creator = "إبراهيم الخزرجي";
    public const string Telegram = "https://t.me/ibraprogram";

    public static void Show(IWin32Window owner)
    {
        using var dialog = new Form
        {
            Text = "عن اللعبة",
            ClientSize = new Size(380, 125),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true
        };
        var name = new Label { Text = Creator, AutoSize = true, Location = new Point(20, 20) };
        var telegram = new LinkLabel
        {
            Text = Telegram,
            AutoSize = true,
            RightToLeft = RightToLeft.No,
            Location = new Point(20, 53)
        };
        telegram.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(Telegram) { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(dialog, Telegram, "Telegram");
            }
        };
        dialog.Controls.AddRange([name, telegram]);
        dialog.ShowDialog(owner);
    }
}

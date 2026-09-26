using System;
using System.Windows;

namespace FolderSizeViewer
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string startPath = args.Length > 0 ? args[0] : null;
            var app = new Application();
            app.Run(new MainWindow(startPath));
        }
    }
}

using System;

namespace MicControlX
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the WPF application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var app = new App();
            app.Run();
        }
    }
}
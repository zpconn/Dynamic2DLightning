using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Dynamic2DLighting
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Properties.Settings.Default.Reload();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Dynamic2DLightingForm form = new Dynamic2DLightingForm())
            {
                form.Run(form.Config.GetSetting<bool>("Windowed"),
                    form.Config.GetSetting<int>("DesiredWidth"),
                    form.Config.GetSetting<int>("DesiredHeight"),
                    form.Config.GetSetting<string>("WindowTitle"));
            }
        }
    }
}
﻿using log4net;
using System.IO;
using System.Windows;

namespace UndertaleModTool;

public static class Program
{
    public static string GetExecutableDirectory()
    {
        return Path.GetDirectoryName(Environment.ProcessPath);
    }

    // https://stackoverflow.com/questions/1025843/merging-dlls-into-a-single-exe-with-wpf
    [STAThreadAttribute]
    public static void Main()
    {
        try
        {
            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            // Handler for exceptions in threads behind forms.
            System.Windows.Forms.Application.ThreadException += GlobalThreadExceptionHandler;
            App.Main();
        }
        catch (Exception e)
        {
            File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash.txt"), e.ToString());
            MessageBox.Show(e.ToString());
        }
    }
    private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = default(Exception);
        ex = (Exception)e.ExceptionObject;
        ILog log = LogManager.GetLogger(typeof(Program));
        log.Error(ex.Message + "\n" + ex.StackTrace);
        File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash2.txt"), (ex.ToString() + "\n" + ex.Message + "\n" + ex.StackTrace));
    }

    private static void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        Exception ex = default(Exception);
        ex = e.Exception;
        ILog log = LogManager.GetLogger(typeof(Program)); //Log4NET
        log.Error(ex.Message + "\n" + ex.StackTrace);
        File.WriteAllText(Path.Combine(GetExecutableDirectory(), "crash3.txt"), (ex.Message + "\n" + ex.StackTrace));
    }
}

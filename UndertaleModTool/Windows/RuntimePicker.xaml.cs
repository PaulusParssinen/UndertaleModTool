﻿using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using UndertaleModLib;

namespace UndertaleModTool;

/// <summary>
/// Logika interakcji dla klasy RuntimePicker.xaml
/// </summary>
public partial class RuntimePicker : Window
{
    public class Runtime
    {
        public string Version { get; set; }
        public string Path { get; set; }
        public string DebuggerPath { get; set; }
    }

    public ObservableCollection<Runtime> Runtimes { get; private set; } = new ObservableCollection<Runtime>();
    public Runtime Selected { get; private set; } = null;

    public RuntimePicker()
    {
        DataContext = this;
        InitializeComponent();
    }
    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible || IsLoaded)
            return;

        if (Settings.Instance.EnableDarkMode)
            MainWindow.SetDarkTitleBarForWindow(this, true, false);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Selected = Picker.SelectedItem as Runtime;
        Close();
    }

    public void DiscoverRuntimes(string dataFilePath, UndertaleData data)
    {
        Runtimes.Clear();
        DiscoverGameExe(dataFilePath, data);
        DiscoverGMS2();
        DiscoverGMS1();
    }

    private void DiscoverGameExe(string dataFilePath, UndertaleData data)
    {
        string gameExeName = data?.GeneralInfo?.FileName?.Content;
        if (gameExeName == null)
            return;

        string gameExePath = Path.Combine(Path.GetDirectoryName(dataFilePath), gameExeName + ".exe");
        if (!File.Exists(gameExePath))
            return;

        Runtimes.Add(new Runtime() { Version = "Game EXE", Path = gameExePath });
    }

    private void DiscoverGMS1()
    {
        string studioRunner = Path.Combine(Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudioPath), "Runner.exe");
        if (!File.Exists(studioRunner))
            return;

        string studioDebugger = Path.Combine(Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudioPath), @"GMDebug\GMDebug.exe");
        if (!File.Exists(studioDebugger))
            studioDebugger = null;

        Runtimes.Add(new Runtime() { Version = "1.4.xxx", Path = studioRunner, DebuggerPath = studioDebugger });
    }

    private void DiscoverGMS2()
    {
        string runtimesPath = Environment.ExpandEnvironmentVariables(SettingsWindow.GameMakerStudio2RuntimesPath);
        if (!Directory.Exists(runtimesPath))
            return;

        Regex runtimePattern = new Regex(@"^runtime-(.*)$");
        foreach(var runtimePath in Directory.EnumerateDirectories(runtimesPath))
        {
            Match m = runtimePattern.Match(Path.GetFileName(runtimePath));
            if (!m.Success)
                continue;

            string runtimeRunner = Path.Combine(runtimePath, @"windows\Runner.exe");
            string runtimeRunnerX64 = Path.Combine(runtimePath, @"windows\x64\Runner.exe");
            if (Environment.Is64BitOperatingSystem && File.Exists(runtimeRunnerX64))
                runtimeRunner = runtimeRunnerX64;
            if (!File.Exists(runtimeRunner))
                continue;

            Runtimes.Add(new Runtime() { Version = m.Groups[1].Value, Path = runtimeRunner });
        }
    }

    public Runtime Pick(string dataFilePath, UndertaleData data)
    {
        DiscoverRuntimes(dataFilePath, data);
        if (Runtimes.Count == 0)
        {
            this.ShowError("Unable to find game EXE or any installed Studio runtime", "Run error");
            return null;
        }
        else if (Runtimes.Count == 1)
        {
            return Runtimes[0];
        }
        else
        {
            ShowDialog();
            return Selected;
        }
    }
}

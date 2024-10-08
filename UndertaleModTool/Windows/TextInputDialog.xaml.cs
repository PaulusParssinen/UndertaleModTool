﻿using System.ComponentModel;
using System.Windows;

namespace UndertaleModTool;

/// <summary>
/// Interaction logic for TextInputDialog.xaml
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public partial class TextInputDialog : Window
{

    public Visibility CancelButtonVisibility { get => PreventClose ? Visibility.Hidden : Visibility.Visible; }
    public string Message { get; set; } // text in the label
    public string MessageTitle { get; set; } // label of the window
    public string ButtonTitle { get; set; } // text inside the button
    public string CancelButtonTitle { get; set; }
    public string InputText { get; set; } // text in the textbox.
    public bool PreventClose { get; set; } // should the dialog prevent itself from closing?
    public bool IsMultiline { get; set; }

    public TextInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
    {
        IsMultiline = isMultiline;
        PreventClose = preventClose;
        MessageTitle = titleText;
        Message = labelText;
        ButtonTitle = submitButtonText;
        CancelButtonTitle = cancelButtonText;
        InputText = defaultInputBoxText;

        InitializeComponent();
        DataContext = this;
    }
    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible || IsLoaded)
            return;

        if (Settings.Instance.EnableDarkMode)
            MainWindow.SetDarkTitleBarForWindow(this, true, false);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = PreventClose;
    }

    public void TryHide()
    {
        Dispatcher.Invoke(() =>
        {
            if (IsVisible)
            {
                PreventClose = false;
                Hide();
            }
        });
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        PreventClose = false;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

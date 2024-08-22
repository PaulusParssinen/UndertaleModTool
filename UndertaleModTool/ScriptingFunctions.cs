﻿using System.ComponentModel;
using System.IO;
using System.Windows;
using UndertaleModLib.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace UndertaleModTool;

// Adding misc. scripting functions here
public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
{
    public bool RunUMTScript(string path)
    {
        // By Grossley
        if (!File.Exists(path))
        {
            ScriptError(path + " does not exist!");
            return false;
        }
        RunScript(path);
        if (!ScriptExecutionSuccess)
            ScriptError("An error of type \"" + ScriptErrorType + "\" occurred. The error is:\n\n" + ScriptErrorMessage, ScriptErrorType);
        return ScriptExecutionSuccess;
    }
    public void InitializeScriptDialog()
    {
        if (scriptDialog == null)
        {
            scriptDialog = new LoaderDialog("Script in progress...", "Please wait...");
            scriptDialog.Owner = this;
            scriptDialog.PreventClose = true;
        }
    }
    public bool LintUMTScript(string path)
    {
        // By Grossley
        if (!File.Exists(path))
        {
            ScriptError(path + " does not exist!");
            return false;
        }
        try
        {
            CancellationTokenSource source = new CancellationTokenSource(100);
            CancellationToken token = source.Token;
            object test = CSharpScript.EvaluateAsync(File.ReadAllText(path), scriptOptions, this, typeof(IScriptInterface), token);
        }
        catch (CompilationErrorException exc)
        {
            ScriptError(exc.Message, "Script compile error");
            ScriptExecutionSuccess = false;
            ScriptErrorMessage = exc.Message;
            ScriptErrorType = "CompilationErrorException";
            return false;
        }
        catch (Exception)
        {
            // Using the 100 MS timer it can time out before successfully running, compilation errors are fast enough to get through.
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
            ScriptErrorType = "";
            return true;
        }
        return true;
    }
}

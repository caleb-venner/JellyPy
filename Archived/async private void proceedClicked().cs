async private void proceedClicked()
{
    if (checkBox.Checked)
    {
        string cmd = ""; // some python command
        Task<string> pythonTask = runProcAsync(cmd);
    }

    // do some other processing

    if (checkBox.Checked)
    {
        var pythonResult = await pythonTask;
        // print result to a message box
    }
}

private async Task<string> runProcAsync(string cmd)
{
    ProcessStartInfo start = new ProcessStartInfo
    {
        FileName = "python.exe", // full path to python
        Arguments = cmd,
        UseShellExecute = false,
        RedirectStandardOutput = true,
    };
    using (Process process = Process.Start(start))
    {
        using (StreamReader reader = process.StandardOutput)
        {
            string result = await reader.ReadToEndAsync();
            return result;
        }
    }
}

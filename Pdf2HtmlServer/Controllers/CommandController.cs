using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pdf2HtmlServer.Models;

namespace Pdf2HtmlServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommandController: Controller
{
    private RunConfiguration RunConfiguration
    {
        get;
        set;
    }
    public CommandController(IOptions<RunConfiguration> ops)
    {
        RunConfiguration = ops.Value;
    }

    [HttpPost("html2layout/{guid}")]
    public async Task<IActionResult> Html2Layout(string guid)
    {
        var arguments = $"{RunConfiguration.Html2LayoutMeasurePath} {RunConfiguration.HtmlStoragePath}/{guid}.html {RunConfiguration.CSVStoragePath}/{guid}.csv";
        var process = new Process();
        SetProcess(process,RunConfiguration.Node,arguments);
        var errorMsg = string.Empty;
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorMsg += e.Data;
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        var exitCode = process.ExitCode;
        if (exitCode == 0)
        {
            return Ok(HtmlLayout.GetHtmlLayoutFromCsv($"{RunConfiguration.CSVStoragePath}/{guid}.csv"));
        }
        return BadRequest(errorMsg);
    }
    // 执行转化pdf 到 html,实时输出日志
    [HttpPost("pdf2htmlex/{guid}")]
    public async Task PostTask(string guid)
    {
        var arguments = $"{RunConfiguration.PdfStoragePath}/{guid}.pdf {RunConfiguration.HtmlStoragePath}/{guid}.html";
        var process = new Process();
        SetProcess(process,RunConfiguration.Pdf2HtmlEXRunPath,arguments);
        process.OutputDataReceived += async (sender, e) =>
        {
            if (e.Data != null)
            {
                await Response.WriteAsync(e.Data + "\n");
            }
        };
        process.ErrorDataReceived += async (sender, e) =>
        {
            if (e.Data != null)
            {
                await Response.WriteAsync(e.Data + "\n");
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        var exitCode = process.ExitCode;
        if (exitCode == 0)
        {
            await Response.WriteAsync($"complete!");
            await HttpContext.Response.CompleteAsync();
            return;
        }

        await Response.WriteAsync("Bad Request");
        await HttpContext.Response.CompleteAsync();
    }

    private static void SetProcess(Process process,string fileName, string arguments)
    {
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
    }
}
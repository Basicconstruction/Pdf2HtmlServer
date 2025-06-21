using System.Diagnostics;
using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pdf2HtmlServer.Models;

namespace Pdf2HtmlServer.Controllers;
[Route("api/[controller]")]
public class Pdf2HtmlController:Controller
{

    private AuthRef AuthRef
    {
        get;
        set;
    }
    public Pdf2HtmlController(IOptions<AuthRef> ops)
    {
        AuthRef = ops.Value;
    }

    // html 下载head
    [HttpHead("html/download/{guid}")]
    public IActionResult HttpHead(string guid)
    {
        // 构建文件的完整路径
        var fullPath = Path.Combine(AuthRef.HtmlStoragePath, $"{guid}.html");
        var inKey = Request.Headers.Authorization;
        if (inKey != AuthRef.AuthKey)
        {
            return BadRequest("密钥验证失败");
        }
        // 检查文件是否存在
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }
        Response.Headers.Append("Accept-Ranges", "bytes");
        return Ok();
    }

    
    // 通过guid 下载html文件
    [HttpGet("html/download/{guid}")]
    public IActionResult Download(string guid)
    {
        // 构建文件的完整路径
        var fullPath = Path.Combine(AuthRef.HtmlStoragePath, $"{guid}.html");
        // 检查文件是否存在
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        // 获取文件信息
        var fileInfo = new FileInfo(fullPath);
        var fileSize = fileInfo.Length;

        // 检查是否支持断点续传
        var isRangeRequest = Request.Headers.ContainsKey("Range");
        if (isRangeRequest)
        {
            // 解析 Range 头部，获取断点续传的起始位置和结束位置
            var rangeHeader = Request.Headers["Range"].ToString();
            var (startByte, endByte) = GetRange(rangeHeader, fileSize);

            // 设置响应头部
            Response.StatusCode = 206; // Partial Content
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.Headers.Append("Content-Range", $"bytes {startByte}-{endByte}/{fileSize}");
            Response.Headers.Append("Content-Type", MimeTypesMap.GetMimeType(fullPath));
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName($"{guid}.html")}\"");

            // 设置响应内容
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fileStream.Seek(startByte, SeekOrigin.Begin);
            using var fileStream0 = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            // 设置文件流的位置为起始字节位置
            fileStream0.Position = startByte;

            // 计算要读取的字节数
            var totalBytesToRead = endByte - startByte + 1;

            // 创建一个字节数组来存储读取的字节
            var buffer = new byte[totalBytesToRead];

            // 从文件流中读取字节
            fileStream.ReadExactly(buffer, 0, (int)totalBytesToRead);

            // 创建 FileStreamResult 对象并设置相关属性
            var result = new FileStreamResult(new MemoryStream(buffer), "application/octet-stream");
            return result;
        }

        // 设置响应头部
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName($"{guid}.html")}\"");
        Response.Headers.Append("Content-Type", MimeTypesMap.GetMimeType(fullPath));
        Response.Headers.Append("Content-Range", $"bytes {0}-{fileSize-1}/{fileSize}");
        // 设置响应内容
        var fileBytes = System.IO.File.ReadAllBytes(fullPath);
        return File(fileBytes, "application/octet-stream");
    }

    private (long startByte, long endByte) GetRange(string rangeHeader, long fileSize)
    {
        if (rangeHeader.Length <= 6) return (0, fileSize);
        var ranges = rangeHeader[6..].Split("-");
        try
        {
            if (ranges[1].Length > 0)
            {
                return (long.Parse(ranges[0]), long.Parse(ranges[1]));
            }
        }
        catch (Exception)
        {
            return (long.Parse(ranges[0]), fileSize-1);
        }

        return (long.Parse(ranges[0]), fileSize-1);
    }
    // 上传pdf文件,并返回guid
    [HttpPost("upload")]
    public async Task<IActionResult> PutTask(IFormFile file)
    {
        var inKey = Request.Headers.Authorization;
        if (inKey != AuthRef.AuthKey)
        {
            return Unauthorized("非法访问");
        }

        var guidName = Guid.NewGuid();
        var filePath = AuthRef.PdfStoragePath+"/"+guidName+".pdf";
        if (!Directory.Exists(AuthRef.PdfStoragePath))
        {
            Directory.CreateDirectory(AuthRef.PdfStoragePath);
        }
        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return Ok(new UploadResult() { FileName = guidName.ToString(),OriginalName = file.FileName});
    }

    // 执行转化pdf 到 html,实时输出日志
    [HttpPost("execute/{guid}")]
    public async Task PostTask(string guid)
    {
        var arguments = $"{AuthRef.PdfStoragePath}/{guid}.pdf {AuthRef.HtmlStoragePath}/{guid}.html";
        var process = new Process();
        process.StartInfo.FileName = AuthRef.RunPath;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

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
}

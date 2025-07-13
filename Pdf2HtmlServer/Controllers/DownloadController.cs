using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pdf2HtmlServer.Models;

namespace Pdf2HtmlServer.Controllers;
[ApiController]
[Route("api/[controller]")]
public class DownloadController: Controller
{
    private RunConfiguration RunConfiguration
    {
        get;
        set;
    }
    public DownloadController(IOptions<RunConfiguration> ops)
    {
        RunConfiguration = ops.Value;
    }
    // html 下载head
    [HttpHead("html/{guid}")]
    public IActionResult HtmlDownloadHead(string guid,string fileName)
    {
        // 构建文件的完整路径
        var fullPath = Path.Combine(RunConfiguration.HtmlStoragePath, $"{guid}.html");
        return HttpHead(fullPath);
    }

    // html 下载head
    [HttpHead("csv/{guid}")]
    public IActionResult CsvDownloadHead(string guid,string fileName)
    {
        // 构建文件的完整路径
        var fullPath = Path.Combine(RunConfiguration.CSVStoragePath, $"{guid}.csv");
        return HttpHead(fullPath);
    }
    
    // 通过guid 下载html文件
    [HttpGet("html/{guid}")]
    public IActionResult HtmlDownload(string guid,string fileName)
    {
        // 构建文件的完整路径
        var fullPath = Path.Combine(RunConfiguration.HtmlStoragePath, $"{guid}.html");
        return HttpDownload(fullPath,fileName);
    }

    [HttpGet("csv/{guid}")]
    public IActionResult CsvDownload(string guid, string fileName)
    {
        var fullPath = Path.Combine(RunConfiguration.CSVStoragePath, $"{guid}.csv");
        return HttpDownload(fullPath, fileName);
    }
    
    private IActionResult HttpDownload(string fullPath, string fileName)
    {
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
            SetPartDownloadHeaders(fullPath,fileName,fileSize,startByte,endByte);
            return PartDownload(fullPath,startByte,endByte);
            
        }

        return FileContentReturn(fullPath, fileName,fileSize);
    }
    public IActionResult HttpHead(string fullPath)
    {
        var inKey = Request.Headers.Authorization;
        if (inKey != RunConfiguration.AuthKey)
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

    private IActionResult FileContentReturn(string fullPath,string fileName,long fileSize)
    {
        // 设置响应头部
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName($"{fileName}")}\"");
        Response.Headers.Append("Content-Type", MimeTypesMap.GetMimeType(fullPath));
        Response.Headers.Append("Content-Range", $"bytes {0}-{fileSize-1}/{fileSize}");
        // 设置响应内容
        var fileBytes = System.IO.File.ReadAllBytes(fullPath);
        return File(fileBytes, "application/octet-stream");
    }
    private void SetPartDownloadHeaders(string fullPath,string fileName,long fileSize,long startByte,long endByte)
    {
        // 设置响应头部
        Response.StatusCode = 206; // Partial Content
        Response.Headers.Append("Accept-Ranges", "bytes");
        Response.Headers.Append("Content-Range", $"bytes {startByte}-{endByte}/{fileSize}");
        Response.Headers.Append("Content-Type", MimeTypesMap.GetMimeType(fullPath));
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{Path.GetFileName($"{fileName}")}\"");
        
    }
    private IActionResult PartDownload(string fullPath,long startByte, long endByte)
    {
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
}
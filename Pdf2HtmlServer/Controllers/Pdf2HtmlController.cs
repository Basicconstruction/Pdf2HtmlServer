using System.Diagnostics;
using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pdf2HtmlServer.Models;

namespace Pdf2HtmlServer.Controllers;
[Route("api/[controller]")]
public class Pdf2HtmlController:Controller
{

    private RunConfiguration RunConfiguration
    {
        get;
        set;
    }
    public Pdf2HtmlController(IOptions<RunConfiguration> ops)
    {
        RunConfiguration = ops.Value;
    }
    
    // 上传pdf文件,并返回guid
    [HttpPost("upload")]
    public async Task<IActionResult> PutTask(IFormFile file)
    {
        var inKey = Request.Headers.Authorization;
        if (inKey != RunConfiguration.AuthKey)
        {
            return Unauthorized("非法访问");
        }

        var guidName = Guid.NewGuid();
        var filePath = RunConfiguration.PdfStoragePath+"/"+guidName+".pdf";
        if (!Directory.Exists(RunConfiguration.PdfStoragePath))
        {
            Directory.CreateDirectory(RunConfiguration.PdfStoragePath);
        }
        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return Ok(new UploadResult() { FileName = guidName.ToString(),OriginalName = file.FileName});
    }

    
}

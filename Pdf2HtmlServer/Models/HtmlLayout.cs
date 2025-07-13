using System.Globalization;

namespace Pdf2HtmlServer.Models;

public class HtmlLayout
{
    public double PageWidth { get; set; }
    public double PageHeight { get; set; }
    public List<Layout> Layouts { get; set; }

    public static HtmlLayout GetHtmlLayoutFromCsv(string fullPath)
    {
        var htmlLayout = new HtmlLayout
        {
            Layouts = []
        };

        using var reader = new StreamReader(fullPath);
        // 读取表头
        var headerLine = reader.ReadLine();
        if (headerLine == null)
            throw new Exception("CSV文件为空");

        // 读取第二行，即PageWidth和PageHeight所在行（ID=0）
        var firstDataLine = reader.ReadLine();
        if (firstDataLine == null)
            throw new Exception("CSV文件缺少PageWidth和PageHeight数据");

        var parts = firstDataLine.Split(',');
        if (parts.Length < 5)
            throw new Exception("数据格式错误");

        // 解析PageWidth和PageHeight
        htmlLayout.PageWidth = double.Parse(parts[1], CultureInfo.InvariantCulture);
        htmlLayout.PageHeight = double.Parse(parts[2], CultureInfo.InvariantCulture);

        // 从第三行开始读取布局数据（排除ID=0这一行）
        while (reader.ReadLine() is { } line)
        {
            var p = line.Split(',');
            if (p.Length < 5)
                continue;

            var layout = new Layout
            {
                Id = int.Parse(p[0]),
                Width = double.Parse(p[1], CultureInfo.InvariantCulture),
                Height = double.Parse(p[2], CultureInfo.InvariantCulture),
                Left = double.Parse(p[3], CultureInfo.InvariantCulture),
                Top = double.Parse(p[4], CultureInfo.InvariantCulture)
            };

            htmlLayout.Layouts.Add(layout);
        }

        return htmlLayout;
    }
}

public class Layout
{
    public int Id { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using hdm.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string cwd = Directory.GetCurrentDirectory();

        // ---------- 扫描项目文件夹 ----------
        var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "result", ".git", ".idea", "vendor", "packages", "node_modules"
        };

        var projects = new List<string>();
        foreach (var dir in Directory.GetDirectories(cwd))
        {
            string name = Path.GetFileName(dir);
            if (name.StartsWith(".")) continue;
            if (exclude.Contains(name)) continue;
            projects.Add(name);
        }
        projects.Sort(StringComparer.OrdinalIgnoreCase);

        if (projects.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("当前目录下没有找到任何项目文件夹。");
            Console.ResetColor();
            return;
        }

        // ---------- 列项目 ----------
        Console.WriteLine();
        Console.WriteLine("📁 可用的项目：");
        for (int i = 0; i < projects.Count; i++)
            Console.WriteLine($"  {i + 1}. {projects[i]}");

        Console.Write($"\n请输入项目序号 (1~{projects.Count})，或输入 0 退出: ");
        string input = Console.ReadLine()?.Trim();

        if (!int.TryParse(input, out int idx) || idx < 0 || idx > projects.Count)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"无效的序号，请输入 0~{projects.Count}");
            Console.ResetColor();
            return;
        }
        if (idx == 0) { Console.WriteLine("退出程序。"); return; }

        string selected = projects[idx - 1];
        string projectDir = Path.Combine(cwd, selected);
        Console.WriteLine($"\n开始处理项目: {selected}");

        // ---------- 加载项目 ----------
        ProjectData data;
        var sw = Stopwatch.StartNew();
        try
        {
            data = ProjectData.Load(selected, projectDir);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"项目加载失败: {ex.Message}");
            Console.ResetColor();
            return;
        }
        sw.Stop();
        Console.WriteLine($"加载完成，耗时 {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"桩号总数: {data.Stations.Length}");

        // ---------- 输入桩号 ----------
        while (true)
        {
            Console.Write("\n请输入桩号（米，例如 1000），或输入 q 退出: ");
            string stInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(stInput) || stInput.Equals("q", StringComparison.OrdinalIgnoreCase))
                break;

            if (!double.TryParse(stInput, out double station))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("桩号格式错误，请输入数字。");
                Console.ResetColor();
                continue;
            }

            var t = Stopwatch.StartNew();
            ComputeResult r;
            try
            {
                r = data.Query(station);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"计算异常: {ex.Message}");
                Console.ResetColor();
                continue;
            }
            t.Stop();

            Console.WriteLine();
            Console.WriteLine("=========================");
            Console.WriteLine($" 桩号: {LL.hua_Num2K(station)}");
            if (!r.Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" 失败: {r.ErrorMessage}");
                Console.ResetColor();
            }
            else
            {
                var s = r.Result;
                Console.WriteLine($" 填方面积: {s.FillArea:F3} ㎡");
                Console.WriteLine($" 挖方面积: {s.CutArea:F3} ㎡");
                Console.WriteLine($" 清表面积: {s.ClearArea:F3} ㎡");
                Console.WriteLine($" 清表范围: X = [{s.MinX:F3}, {s.MaxX:F3}]");
                Console.WriteLine($" 中桩高程: {s.CenterY:F3}");
                Console.WriteLine(" 结构层面积:");
                foreach (var txt in s.LayerAreaTexts)
                    Console.WriteLine($"   {txt}");
            }
            Console.WriteLine($" 耗时: {t.Elapsed.TotalMilliseconds:F2} ms");
            Console.WriteLine("=========================");
        }

        Console.WriteLine("退出程序。");
    }
}
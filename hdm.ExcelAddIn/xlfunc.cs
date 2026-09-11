using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;
using hdm.Core;

namespace hdm
{
    public class MyFunctions : IExcelAddIn
    {
        public static string? xllPath = null;
        public static string folderTips = "【未找到有效项目文件夹】";

        // 项目缓存：projectName → ProjectData
        private static readonly Dictionary<string, ProjectData> _cache
            = new Dictionary<string, ProjectData>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        // ============================================================
        // 生命周期
        // ============================================================
        public void AutoOpen()
        {
            xllPath = Path.GetDirectoryName(ExcelDnaUtil.XllPath);
            IntelliSenseServer.Install();

            if (!string.IsNullOrEmpty(xllPath) && Directory.Exists(xllPath))
            {
                var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "bin", "obj", "result", ".git", ".idea", "vendor", "packages", "node_modules"
                };

                var folders = Directory.GetDirectories(xllPath)
                                       .Select(Path.GetFileName)
                                       .Where(f => !string.IsNullOrEmpty(f)
                                                   && !f!.StartsWith(".")
                                                   && !exclude.Contains(f!))
                                       .ToList();

                if (folders.Any())
                    folderTips = "当前可用项目: " + string.Join(" - ", folders);
            }
        }

        public void AutoClose()
        {
            IntelliSenseServer.Uninstall();
        }

        // ============================================================
        // 内部：取项目（带缓存）
        // ============================================================
        private static ProjectData GetProject(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                throw new ArgumentException("项目名称不能为空");

            lock (_lock)
            {
                if (_cache.TryGetValue(projectName, out var d))
                    return d;

                string baseDir = string.IsNullOrEmpty(xllPath)
                    ? Directory.GetCurrentDirectory()
                    : xllPath!;
                string projectDir = Path.Combine(baseDir, projectName);
                if (!Directory.Exists(projectDir))
                    throw new DirectoryNotFoundException($"项目文件夹不存在：{projectDir}");

                d = ProjectData.Load(projectName, projectDir);
                _cache[projectName] = d;
                return d;
            }
        }

        // ============================================================
        // UDF：核心查询 —— 返回 [填方, 挖方, 清表] 一行三列
        // ============================================================
        [ExcelFunction(
            Name = "LL_FillCut",
            Description = "根据项目名和桩号返回 [填方面积, 挖方面积, 清表面积]",
            Category = "测量计算")]
        public static object[,] LL_FillCut(
            [ExcelArgument(Name = "project", Description = "项目名称（.xll 同级文件夹名）")] string projectName,
            [ExcelArgument(Name = "station", Description = "桩号（米）")] double station)
        {
            try
            {
                var data = GetProject(projectName);
                var r = data.Query(station);

                if (!r.Success)
                {
                    var err = new object[1, 1];
                    err[0, 0] = r.ErrorMessage ?? "计算失败";
                    return err;
                }

                var s = r.Result;
                var result = new object[1, 3];
                result[0, 0] = s.FillArea;
                result[0, 1] = s.CutArea;
                result[0, 2] = s.ClearArea;
                return result;
            }
            catch (Exception ex)
            {
                var err = new object[1, 1];
                err[0, 0] = "错误: " + ex.Message;
                return err;
            }
        }

        // ============================================================
        // UDF：列出所有项目
        // ============================================================
        [ExcelFunction(
            Name = "LL_ListProjects",
            Description = "列出 .xll 同级目录下的所有项目文件夹",
            Category = "测量计算")]
        public static object[,] LL_ListProjects()
        {
            try
            {
                string baseDir = string.IsNullOrEmpty(xllPath)
                    ? Directory.GetCurrentDirectory()
                    : xllPath!;

                var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "bin", "obj", "result", ".git", ".idea", "vendor", "packages", "node_modules"
                };

                var names = Directory.GetDirectories(baseDir)
                                     .Select(Path.GetFileName)
                                     .Where(f => !string.IsNullOrEmpty(f)
                                                 && !f!.StartsWith(".")
                                                 && !exclude.Contains(f!))
                                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                     .ToList();

                if (names.Count == 0)
                {
                    var empty = new object[1, 1];
                    empty[0, 0] = "（没有项目文件夹）";
                    return empty;
                }

                var result = new object[names.Count, 1];
                for (int i = 0; i < names.Count; i++)
                    result[i, 0] = names[i];
                return result;
            }
            catch (Exception ex)
            {
                var err = new object[1, 1];
                err[0, 0] = "错误: " + ex.Message;
                return err;
            }
        }

        // ============================================================
        // UDF：清空缓存
        // ============================================================
        [ExcelFunction(
            Name = "LL_RefreshCache",
            Description = "清空项目缓存，下次调用会重新读取源文件",
            Category = "测量计算")]
        public static string LL_RefreshCache()
        {
            lock (_lock)
            {
                int n = _cache.Count;
                _cache.Clear();
                return $"已清空 {n} 个项目的缓存";
            }
        }

        // ============================================================
        // UDF：提示信息
        // ============================================================
        [ExcelFunction(
            Name = "LL_Tips",
            Description = "返回当前可用项目列表文本",
            Category = "测量计算")]
        public static string LL_Tips()
        {
            return folderTips;
        }
    }
}
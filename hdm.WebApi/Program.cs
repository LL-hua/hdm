using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using hdm.Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string RootDir = Directory.GetCurrentDirectory();

// ---------- 项目缓存 ----------
var cache = new Dictionary<string, ProjectData>(StringComparer.OrdinalIgnoreCase);
var lockObj = new object();

ProjectData GetProject(string projectName)
{
    if (string.IsNullOrWhiteSpace(projectName))
        throw new ArgumentException("项目名称不能为空");

    lock (lockObj)
    {
        if (cache.TryGetValue(projectName, out var d))
            return d;

        string projectDir = Path.Combine(RootDir, projectName);
        if (!Directory.Exists(projectDir))
            throw new DirectoryNotFoundException($"项目文件夹不存在：{projectDir}");

        d = ProjectData.Load(projectName, projectDir);
        cache[projectName] = d;
        return d;
    }
}

List<string> ListProjects()
{
    var exclude = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "wwwroot", "result", "Properties",
        ".git", ".idea", "vendor", "packages", "node_modules"
    };

    return Directory.GetDirectories(RootDir)
        .Select(Path.GetFileName)
        .Where(f => !string.IsNullOrEmpty(f)
                    && !f.StartsWith(".")
                    && !exclude.Contains(f))
        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
        .ToList();
}

// ---------- double[,] → double[][] ----------
static double[][] ToJagged(double[,] mat)
{
    if (mat == null) return Array.Empty<double[]>();
    int rows = mat.GetLength(0);
    int cols = mat.GetLength(1);
    var arr = new double[rows][];
    for (int i = 0; i < rows; i++)
    {
        arr[i] = new double[cols];
        for (int j = 0; j < cols; j++)
            arr[i][j] = mat[i, j];
    }
    return arr;
}

// ---------- 静态文件 ----------
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------- API ----------
app.MapGet("/api/projects", () =>
{
    try { return Results.Ok(new { ok = true, projects = ListProjects() }); }
    catch (Exception ex) { return Results.Ok(new { ok = false, error = ex.Message }); }
});

app.MapGet("/api/fillcut", (string project, double station) =>
{
    try
    {
        var data = GetProject(project);
        var r = data.Query(station);
        if (!r.Success)
            return Results.Ok(new { ok = false, error = r.ErrorMessage });

        var s = r.Result;

        var layerPolys = new List<double[][]>();
        if (s.Layers != null)
            foreach (var poly in s.Layers)
                layerPolys.Add(ToJagged(poly));

        return Results.Ok(new
        {
            ok = true,
            project,
            station,
            stationK = LL.hua_Num2K(station),

            // 位置
            centerY = s.CenterY,

            // 关键点（double[] 直接序列化成 JSON 数组）
            lOuter = s.LOuter,
            rOuter = s.ROuter,
            lToe = s.LToe,
            rToe = s.RToe,

            // 包围盒 [minX, minY, maxX, maxY]
            bounds = s.Bounds,

            // 面积
            fill = s.FillArea,
            cut = s.CutArea,
            clear = s.ClearArea,
            layers = s.LayerAreas,

            // 板块
            leftSlabs = ToJagged(s.LeftSlabs),
            rightSlabs = ToJagged(s.RightSlabs),

            // 几何
            geometry = new
{
    ground        = ToJagged(s.Ground),
    cleared       = ToJagged(s.Cleared),
    design        = ToJagged(s.Design),
    leftSubgrade  = ToJagged(s.LeftSubgrade),
    rightSubgrade = ToJagged(s.RightSubgrade),
    layerPolygons = layerPolys,
    leftSlopeRaw  = ToJagged(s.LeftSlopeRaw),
    leftSlopeTrim = ToJagged(s.LeftSlopeTrimmed),
    rightSlopeRaw = ToJagged(s.RightSlopeRaw),
    rightSlopeTrim= ToJagged(s.RightSlopeTrimmed)
}
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { ok = false, error = ex.Message });
    }
});

app.MapGet("/api/stations", (string project) =>
{
    try
    {
        var data = GetProject(project);
        var list = data.Stations.Select(s => new { value = s, label = LL.hua_Num2K(s) }).ToList();
        return Results.Ok(new { ok = true, stations = list });
    }
    catch (Exception ex) { return Results.Ok(new { ok = false, error = ex.Message }); }
});

app.MapGet("/api/refresh", (string project) =>
{
    lock (lockObj) { cache.Remove(project); }
    return Results.Ok(new { ok = true, message = $"已清空项目 {project} 的缓存" });
});

app.Run("http://0.0.0.0:5000");
# hdm 项目上下文（给 AI 助手）

> 下次继续开发时，把这份文件发给 AI 助手，可快速恢复上下文。

---

## 一句话

道路横断面土方计算工具。核心算法与宿主分离，同一套计算逻辑可挂接 Excel、AutoCAD、控制台。

**仓库**：https://github.com/LL-hua/hdm

---

## 架构

hdm/
├── hdm.Core/          纯计算类库，net8.0，无 I/O 平台依赖
├── hdm.Console/       控制台测试宿主，net8.0
├── hdm.ExcelAddIn/    Excel 加载项，net8.0-windows
└── hdm.sln

### 三层职责

| 层 | 项目 | 职责 |
|----|------|------|
| 计算核心 | hdm.Core | 算法、几何、解析、数据模型 |
| 宿主 | hdm.Console / hdm.ExcelAddIn / 未来 hdm.AutoCad | I/O、UI、平台适配 |

核心原则：hdm.Core 不读文件、不画图、不依赖任何宿主。所有 I/O、绘图、UI 都在外层。

---

## hdm.Core 的文件组织

| 文件 | 内容 |
|------|------|
| AppConfig.cs | 实例类，只留 3 个参数：ClearDepth / XyzRows / XyzCols |
| Models.cs | 所有数据类型（纯定义，无实现） |
| LL.cs | 工具库：线路计算、几何、面积、桩号、竖曲线 |
| Parsers.cs | 5 个 Manager 的实现 + DataReader |
| SectionCalculator.cs | 单桩号核心计算 |
| ProjectData.cs | 加载 + 缓存 + 查询入口 |

### Models.cs 里的类型

| 类型 | 用途 |
|------|------|
| DuanMianShuJu | 断面数据（桩号 + 板块几何） |
| JueDuiBanKuaiDian | 绝对板块点 |
| LuJiCheDaoJieGuoBao | 路基车道结果包（左右拐点） |
| BianPoDuanLuo | 边坡段落 |
| BianPoHouXuanBao | 边坡候选包（4 组绝对坐标） |
| CrossfallRecord | 横坡记录（struct） |
| LeftPoint2D / RightPoint2D | 结构层点 |
| LeftJiegoucengConfig / RightJiegoucengConfig | 结构层配置 |
| SectionResult | 单桩号计算结果（保留几何数据） |
| ComputeResult | 计算结果包装（成功/失败/错误） |

### Parsers.cs 里的 5 个 Manager

| 类 | 作用 | 输入文件 |
|----|------|----------|
| LuJiYaoSuYinQing | 路基宽度 - 左右车道拐点 | .左板块 / .右板块 |
| BianPoYinQing | 边坡段落 - 绝对坐标 | .边坡 |
| LumianSlopeManager | 横坡插值 | .左结构层横坡 / .右结构层横坡 |
| LeftJiegoucengManager | 左结构层几何 | .左结构层 |
| RightJiegoucengManager | 右结构层几何 | .右结构层 |
| DataReader | 通用文本读取 | 任意 |

每个 Manager 有 ParseFile(string filePath) 和 Parse(IEnumerable<string>) 双签名（方便测试）。

### SectionResult 的字段分组

故意保留全部几何数据，为以后 AutoCAD 导出留余地。

| 分组 | 字段 |
|------|------|
| 位置 | Station, CenterY |
| 路基外缘 | LOuterX/Y, ROuterX/Y, LeftCrossfall, RightCrossfall |
| 折线 | Ground, Cleared, FinalDesign, FinalFinished |
| 结构层 | LeftSubgrade, RightSubgrade, LayerPolygons |
| 面积 | FillArea, CutArea, ClearArea, LayerAreaTexts |
| 范围 | MinX, MaxX, MinY |
| 边坡 | LeftSlopePoints, RightSlopePoints, LeftToeX/Y, RightToeX/Y |

---

## 输入文件约定

每个项目是 .xll（或 .dll）同级目录下的一个文件夹，名字即项目名。

D:/hdm/
├── hdm-AddIn64.xll
├── lot1/
│   ├── lot1.pqx                 线路参数（8列）
│   ├── lot1.sqx                 竖曲线（3列）
│   ├── lot1.k                   桩号列表（1列）
│   ├── lot1.原地面              原地面点（3列，跳过首行）
│   ├── lot1.左板块              左路基宽度
│   ├── lot1.右板块              右路基宽度
│   ├── lot1.边坡                边坡段落（每5行一个段落）
│   ├── lot1.左结构层            左结构层配置
│   ├── lot1.右结构层            右结构层配置
│   ├── lot1.左结构层横坡        左横坡
│   ├── lot1.右结构层横坡        右横坡
│   └── lot1.config              可选
└── lot2/
    └── ...

配置文件 {项目名}.config，每行 Key = Value：

| 键 | 默认值 | 说明 |
|----|--------|------|
| ClearDepth | -0.3 | 清表深度（米） |
| XyzRows | 35 | 原地面网格行数（保留） |
| XyzCols | 60 | 原地面网格列数（保留） |

---

## 关键决策（为什么这么做）

| 决策 | 原因 |
|------|------|
| AppConfig 从静态改实例 | 避免多项目状态串味 |
| Parse 双签名 | 保留 ParseFile(filePath) 方便直接调，同时提供 Parse(IEnumerable) 方便测试 |
| SectionResult 保留几何数据 | 为以后 AutoCAD 导出留余地 |
| 删除 DXF/SVG/投影/GPS/四参数 | 用不到，砍掉减依赖 |
| LL 瘦身合并 | 一个人维护，集中比拆分方便 |
| 不加批量 UDF | Excel 双击填充已够快，加批量无意义 |
| 一个命名空间 hdm.Core | 一个人维护，越简单越好 |
| Nullable disable | 老代码风格，避免大量警告 |

---

## 当前进度

- [x] hdm.Core 重构（0 警告 0 错误）
- [x] hdm.Console 跑通（数值一致）
- [x] hdm.ExcelAddIn 跑通（1300 断面几乎瞬间）
- [x] README + CONTEXT 文档
- [x] GitHub 上线
- [ ] hdm.AutoCad 加载项（待做）
- [ ] 返回更多字段（中桩高程、清表边界、逐层面积）
- [ ] 桩号字符串输入（"K85+525"）

---

## 性能

| 场景 | 耗时 |
|------|------|
| 项目加载（首次） | ~260 ms |
| 单桩号查询（首次） | ~440 ms（含 JIT） |
| 单桩号查询（稳态） | 1~3 ms |
| 1300 桩号批量 | 几乎瞬间 |

性能已够，不必再优化。

---

## Excel UDF 清单

| 函数 | 说明 |
|------|------|
| LL_FillCut(project, station) | 返回 [填方, 挖方, 清表] 一行三列 |
| LL_ListProjects() | 列出所有项目 |
| LL_Tips() | 返回项目列表文本 |
| LL_RefreshCache() | 清空项目缓存 |

关键实现：AddIn.cs 里用 Dictionary<string, ProjectData> 缓存项目，lock 保护多线程。

---

## 编译注意

| 项目 | 目标框架 | Termux 能编 | Windows 能编 |
|------|----------|-------------|--------------|
| hdm.Core | net8.0 | 可以 | 可以 |
| hdm.Console | net8.0 | 可以 | 可以 |
| hdm.ExcelAddIn | net8.0-windows | 不行（缺 Windows Desktop SDK） | 可以 |

Termux 里只编 Core 和 Console：

dotnet build hdm.Core
dotnet run --project hdm.Console

Excel 加载项在 Windows 上编：

dotnet build hdm.ExcelAddIn -c Release

---

## 维护者偏好

- 一个人维护，不过度设计
- 优先简单、可读，其次才是性能
- 够用就好，不为了"以后可能用到"而加功能
- 术语不装，中文拼音 + 英文混用，看得懂就行
- 决策靠想清楚，不是靠跟风

---

## 下次继续的方向

### AutoCAD 挂接（首选）

- 需要确认 AutoCAD 版本（决定 .NET 目标框架）
  - 2025+ - .NET 8，hdm.Core 直接引用
  - 2019~2024 - .NET 4.7/4.8，hdm.Core 要改 netstandard2.0
  - 更老 - 更麻烦
- 新建 hdm.AutoCad 项目
- 用 [CommandMethod("HDM")] 定义命令
- 交互方式：命令行选项目 - 选桩号 - 画图
- 绘图数据源：SectionResult 里的 Ground / Cleared / FinalDesign / LayerPolygons

### 更多返回字段

- 现在 LL_FillCut 只返回 3 列（填/挖/清表）
- 可以加 LL_FullResult(project, station) 返回整行几何数据

### 桩号字符串输入

- 现在必须输 85525
- 可以加 LL_FillCutK(project, "K85+525") 解析字符串

---

## 如何与 AI 助手对话

推荐开场白：

看 https://github.com/LL-hua/hdm 的 docs/CONTEXT.md，我们继续做 AutoCAD。AutoCAD 版本是 2025。

或者直接把这份文档粘贴给 AI，然后说：

这是项目上下文，我们继续做 XXX。

---

## 更新约定

每次重大改动后，更新本文档的「当前进度」和「下次继续的方向」两节。

---

*最后更新：项目重构完成，Excel 加载项跑通。*


## hdm.WebApi

- **技术**：ASP.NET Core Minimal API + 原生 HTML/Canvas
- **端口**：5000
- **项目根目录**：`hdm.WebApi/`（`dotnet run` 所在目录）
- **接口**：
  - `GET /api/projects` — 列项目
  - `GET /api/fillcut?project=X&station=Y` — 查断面（含全部几何数据）
  - `GET /api/stations?project=X` — 列桩号
  - `GET /api/refresh?project=X` — 清缓存
- **前端**：`wwwroot/index.html`，单页
  - Canvas 画断面
  - `GestureController` 手势（拖拽、双指缩放、双击放大、双指点击缩小）
  - 坐标映射：`px = wx * scale + offsetX`，`py = -wy * scale + offsetY`
- **部署**：手机 Termux 里 `dotnet run`，局域网访问 `http://手机IP:5000`

### 已完成
- [x] hdm.WebApi + 网页版跑通
- [x] Canvas 手势（用 GestureController）
# TON Prediction Backend

## 0. 背景

你是 .NET 后端工程师，需要实现 **TON 网络的中心化 Prediction（涨跌竞猜）** 链下支撑服务，整体思路与 PancakeSwap Prediction 类似，但运行在 TON 生态，并由后端完全掌控回合生命周期、押注与结算流程。

> **核心流程**
>
> 1. **回合调度**：后台定时任务（`RoundScheduler`）按照固定周期（如 5 分钟）创建新回合，记录开盘价格，并在回合结束时记录收盘价格。
> 2. **价格来源**：通过外部聚合行情（如 CoinGecko / Binance API）或自建价格预言机服务，实时拉取目标币种价格。
> 3. **押注方式**：用户在 TON 钱包中向**主钱包**转账，并在 `message`/`comment` 字段备注 `Bull` 或 `Bear`，金额即为押注额。
> 4. **监听入账**：后台监听主钱包入账事件，将押注落库。
> 5. **结算流程**：回合结束→后台计算胜负→写入结果→等待用户调用 **领奖 API** → 后端再从主钱包向赢家地址转账。
> 6. **风险控制**：后台需校验入账交易的合法性、金额上下限、重复领奖等。

### 0.0 项目分层

| Layer          | Project                        | 技术栈 / 说明                                                |
| -------------- | ------------------------------ | ------------------------------------------------------------ |
| Presentation   | `TonPrediction.Api`            | ASP.NET Core 8 Minimal API + SignalR；注册 `IHostedService`：`RoundScheduler`（创建 / 关闭回合 + 定价）与 `TonEventListener`（监听转账与出账回执） |
| Application    | `TonPrediction.Application`    | 纯 C# 业务用例 / Service / 接口定义                          |
| Infrastructure | `TonPrediction.Infrastructure` | **TonSdk.NET**、SqlSugar（`QYQ.Base.SqlSugar`）、Redis —— Application 接口的具体实现，含价格抓取、钱包事件订阅等 |

> **注意**：仓库不包含前端代码，`/frontend` 目录（若存在）仅作示例演示；禁止 Codex 修改。

#### 0.1 SqlSugar 使用示例（仅作说明，禁止直接复制到业务代码）

```csharp
using QYQ.Base.SqlSugar;
...
public class ExampleRepository(ILogger<ExampleRepository> logger, IOptionsMonitor<DatabaseConfig> options)
    : BaseRepository<ExampleEntity>(logger, options.CurrentValue.Log)
{
    public async Task<ExampleEntity?> FindAsync(string id) =>
        await Db.Queryable<ExampleEntity>()
                 .Where(e => e.Id == id)
                 .FirstAsync();
}
```

#### 0.2 TonSdk.NET 监听示例（仅作说明）

```csharp
using TonSdk.Client;
...
var client = new LiteClient("<ENV_TON_NODE>");
await foreach (var tx in client.SubscribeTransactions(<MASTER_WALLET_ADDRESS>))
{
    // 根据 tx.Message.Comment 判断 Bull / Bear
}
```

------

## 1. 代码规范

### 1.1 .NET

- 目标框架：`net9.0`
- 必须通过 `dotnet format --verify-no-changes`（已在 pre‑commit 钩子）
- 公共 API 需带 XML 注释；Domain 层禁止直接引用 **TonSdk.NET**，只能通过 Application 层接口访问链上逻辑
- 不得把私钥、Token 写进源码，使用 `<ENV_*>` 占位，例如：`<ENV_MASTER_WALLET_PK>`、`<ENV_PRICE_API_KEY>`
- 注释使用**简体中文**
- API 接口统一返回 `ApiResult<T>`（`using QYQ.Base.Common.ApiResult`,*Application* 层关于控制器的service也需要返回ApiResult<T>,处理业务方面的返回码,而Api层需要处理参数规范等错误的返回码
- 输出 DTO 统一后缀 `Output`，放在 *Application* 层 `Output` 文件夹；复杂请求体统一后缀 `Input`，放在 `Input` 文件夹

### 1.2 通用命名

- 类 / 接口：PascalCase；私有字段 `_camelCase`
- 文件名 = 顶级类名
- 异步方法后缀 `Async`

------

## 2. 构建与依赖

```bash
# 拉取依赖
$ dotnet restore

# 链上 SDK
$ dotnet add package TonSdk.Net

# ORM
$ dotnet add package QYQ.Base.SqlSugar

# 本地服务
$ docker compose up -d db redis ton-node price-oracle
```

### 必备环境变量（示例）

| Key                         | 说明                                          |
| --------------------------- | --------------------------------------------- |
| `ENV_MASTER_WALLET_ADDRESS` | 主钱包地址                                    |
| `ENV_MASTER_WALLET_PK`      | 主钱包私钥（Base64 或 Hex，加密存储，勿提交） |
| `ENV_PRICE_API_KEY`         | 行情 API 密钥                                 |

------

## 3. 代码质量检查（必须全部通过）

> **先自动补注释，再跑质量检查**（注释需描述属性作用、方法功能，保证初学者可读）

```bash
$ dotnet format --verify-no-changes
$ dotnet build -c Release
$ dotnet test
```

------

## 4. PR 规范

- 分支：`feat/<模块>` | `fix/<问题>` | `chore/<任务>`
- PR 描述需包含：
  1. 变更摘要
  2. 关联 Issue / Task ID
  3. 测试结果或链上 Tx 链接

------

## 5. README.md 编写规范

- **语言**：必须使用**简体中文**。
- **时间格式**：如需写日期／时间，一律采用 **北京时间（UTC + 8）**，格式示例
  - 日期：`2025-07-03`
  - 日期时间：`2025-07-03 17:00 (UTC+8)`
- **推荐章节顺序**
  1. 项目简介
  2. 快速开始（本地启动脚本、必需环境变量）
  3. 架构概览（文字 + 图示）
  4. 常见命令（构建、测试、运行）
  5. 部署说明（Docker / Kubernetes）
- **保持同步**：新增环境变量或脚本时，请同步更新 README。Codex 在自动修改相关文件后，也需自动修订 README。

------

## 6. 禁改目录／文件

- `/frontend/**`
- `**/*-secret.json`
- `*.lock`（除非确有必要升级依赖）

> **Codex 温馨提示**：遇到不明确的需求，请在 PR 描述中提问，而不是自行猜测。
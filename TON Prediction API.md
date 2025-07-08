# TON Prediction API（价格预测接口）

> **版本：v1 / 更新时间：2025‑07‑03**
>
> - 所有金额字段均为 **字符串形式的 decimal(38,18)**，客户端需自行转换。
>
> - 时间使用 **Unix 时间戳（秒）**，默认时区 UTC+0。
>
> - 所有 REST 接口统一返回包裹：
>
>   ```json
>   {
>     "code": 0,
>     "message": "success",
>     "data": {}
>   }
>   ```
>
> | 错误码 | 含义              | 说明               |
> | ------ | ----------------- | ------------------ |
> | 0      | success           | 请求成功           |
> | 4001   | invalid_parameter | 参数格式或取值非法 |
> | 4004   | round_not_found   | 回合不存在或未开始 |
> | 5000   | internal_error    | 服务器内部错误     |
>
> SignalR Hub 路径：`http://localhost:5259/predictionHub`（对应 `app.MapHub<PredictionHub>("/predictionHub")`）。
> 推荐使用 `@microsoft/signalr` 进行连接，示例代码：
>
> ```
import * as signalR from "@microsoft/signalr";

const hub = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5259/predictionHub")
  .build();

hub.on("currentRound", data => { /* ... */ });
hub.on("roundStarted", data => { /* ... */ });
hub.on("roundLocked", data => { /* ... */ });
hub.on("settlementStarted", data => { /* ... */ });
hub.on("settlementEnded", data => { /* ... */ });
hub.on("roundEnded", data => { /* ... */ });

await hub.start();
```

> **说明**：回合相关响应均同时包含 `id` 与 `epoch` 字段，`id` 用于后续业务请求，`epoch` 用于展示回合期次。
------

## 1️⃣ 当前回合（`currentRound` • WS 推送）

- **SignalR 事件**：`currentRound`

- **推送频率**：每 5 秒（或当有下注 / 价格变化时实时推送）

| 字段名         | 类型            | 说明                                     |
| -------------- | --------------- | ---------------------------------------- |
| `id`      | int             | 回合唯一编号                                 |
| `epoch`   | int             | 期次（Epoch）                                 |
| `lockPrice`    | string(decimal) | 锁定价格                                 |
| `currentPrice` | string(decimal) | 最新价格                                 |
| `totalAmount`  | string(decimal) | 总下注金额                               |
| `bullAmount` | string(decimal) | 押 **上涨** 的金额                       |
| `bearAmount` | string(decimal) | 押 **下跌** 的金额                       |
| `rewardPool`   | string(decimal) | 扣除手续费后的可分配奖金池               |
| `endTime`      | int             | 回合结束时间（Unix 秒）                  |
| `bullOdds`  | string(decimal) | 上涨赔率 = `totalAmount / bullAmount`    |
| `bearOdds` | string(decimal) | 下跌赔率 = `totalAmount / bearAmount`   |
| `status`       | enum            | `upcoming` |

**示例：**

```json
{
  "id": 357690,
  "epoch": 357690,
  "lockPrice": "308.85000000",
  "currentPrice": "309.12500000",
  "totalAmount": "2500.00000000",
  "bullAmount": "1400.00000000",
  "bearAmount": "1100.00000000",
  "rewardPool": "2425.00000000",
  "endTime": 1710001234,
  "status": "live",
  "bullOdds": "1.78571428",
  "bearOdds": "2.20454545"
}
```

------

## 2️⃣ 回合开始通知（`roundStarted` • WS 广播）

- **SignalR 事件**：`roundStarted`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |
## 3️⃣ 回合锁定通知（`roundLocked` • WS 广播）

- **SignalR 事件**：`roundLocked`

- **推送内容**：

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| `id` | int | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |


## 4️⃣ 开始结算通知（`settlementStarted` • WS 广播）

- **SignalR 事件**：`settlementStarted`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |

## 5️⃣ 结束结算通知（`settlementEnded` • WS 广播）

- **SignalR 事件**：`settlementEnded`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |

## 6️⃣ 回合结束通知（`roundEnded` • WS 广播）

- **SignalR 事件**：`roundEnded`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |

**示例：**

```json
{ "id": 357690, "epoch": 357690 }
```

------

## 7️⃣ 历史回合列表

### `GET /api/rounds/history?limit=3`

- **说明**：返回最近 `limit` 个已结束回合，默认 3，最大 100。

| 字段名        | 类型            | 说明               |
| ------------- | --------------- | ------------------ |
| `id`     | int             | 回合唯一编号           |
| `epoch`  | int             | 期次（Epoch）           |
| `lockPrice`   | string(decimal) | 锁定价格           |
| `closePrice`  | string(decimal) | 收盘价格           |
| `totalAmount` | string(decimal) | 总下注金额         |
| `bullAmount`  | string(decimal) | 押 **上涨** 的金额 |
| `bearAmount`  | string(decimal) | 押 **下跌** 的金额 |
| `rewardPool`  | string(decimal) | 奖金池（扣手续费） |
| `endTime`     | int             | 结束时间           |
| `bullOdds`    | string(decimal) | 上涨赔率           |
| `bearOdds`    | string(decimal) | 下跌赔率           |

**示例：**

```json
[
  {
    "id": 357689,
    "lockPrice": "308.85000000",
    "closePrice": "309.75000000",
    "totalAmount": "1800.00000000",
    "bullAmount": "1000.00000000",
    "bearAmount": "800.00000000",
    "rewardPool": "1746.00000000",
    "endTime": 1709999999,
    "bullOdds": "1.80000000",
    "bearOdds": "2.25000000"
  }
]
```

------

## 8️⃣ 即将开始回合

### `GET /api/rounds/upcoming`

返回未来 **2** 个预告回合时间，使前端可倒计时。

| 字段名      | 类型 | 说明           |
| ----------- | ---- | -------------- |
| `id`   | int  | 回合唯一编号（可能为 0） |
| `epoch` | int  | 期次（Epoch）       |
| `startTime` | int  | 开始时间（秒） |
| `endTime`   | int  | 结束时间（秒） |

------

## 9️⃣ 价格走势图

### `GET /api/price/chart`

返回最近 **10** 分钟价格，每 20 秒 1 条。

| 字段名       | 类型              | 说明                |
| ------------ | ----------------- | ------------------- |
| `timestamps` | int[]             | Unix 秒数组（升序） |
| `prices`     | string(decimal)[] | 价格数组            |

------

## 1️⃣0️⃣ 我的下注记录

### mode = `round`

```
GET /api/predictions/round
```

| 参数名     | 类型   | 默认值 | 说明                        |
| ---------- | ------ | ------ | --------------------------- |
| `symbol`   | string | TONUSD | 币种对                      |
| `status`   | enum   | all    | all \| claimed \| unclaimed |
| `page`     | int    | 1      | 页码                        |
| `pageSize` | int    | 10     | 分页大小，<=100             |

**data[] 字段：**

| 字段名       | 类型            | 说明                    |
| ------------ | --------------- | ----------------------- |
| `id`    | int             | 回合唯一编号                |
| `epoch` | int             | 期次（Epoch）                |
| `position`   | enum            | `up` | `down`           |
| `amount`     | string(decimal) | 押注金额                |
| `lockPrice`  | string(decimal) | 锁定价格                |
| `closePrice` | string(decimal) | 收盘价格                |
| `reward`     | string(decimal) | 奖励（可能为 0）        |
| `claimed`    | bool            | 是否已领取              |
| `result`     | enum            | `win` | `lose` | `draw` |

### mode = `pnl`

```
GET /api/predictions/pnl
```

返回盈亏汇总，字段同现有文档，字段名保持不变。

------

## 1️⃣1️⃣ 排行榜

### `GET /api/leaderboard/list`

| 参数名     | 类型   | 默认值    | 说明                                       |
| ---------- | ------ | --------- | ------------------------------------------ |
| `symbol`   | string | TONUSD    | 币种对                                     |
| `rankBy`   | enum   | netProfit | rounds \| netProfit \| totalBet \| winRate |
| `page`     | int    | 1         | 当前页                                     |
| `pageSize` | int    | 10        | 分页大小，<=100                            |
| `address`  | string |           | 若传入则返回该地址在列表中的页码 & 排名    |
## 1️⃣2️⃣ 领奖

### `POST /api/claim`

请求体：
```json
{ "id": 123, "address": "EQ..." }
```

返回字段：
| 字段名 | 类型 | 说明 |
| ------- | ---- | ---- |
| `txHash` | string | 转账交易哈希 |
| `lt` | ulong | 账户逻辑时间 |
| `status` | enum | `pending`\|`confirmed`\|`failed` |
| `timestamp` | int | 交易时间（秒） |


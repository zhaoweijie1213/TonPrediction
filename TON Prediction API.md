# TON Prediction API（价格预测接口）

> **版本：v1 / 更新时间：2025‑07‑03**
>
> - 所有金额字段均为 **字符串形式的 decimal(38,18)**，客户端需自行转换。
>
> - 时间使用 **Unix 时间戳（秒）**，默认时区 UTC+0。
>
> - 转账下注时需在 Comment 中填写 `<回合ID> bull` 或 `<回合ID> bear`，表示看多或看空。
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
> ```typescript
>import * as signalR from "@microsoft/signalr";
>
>const hub = new signalR.HubConnectionBuilder()
>  .withUrl("http://localhost:5259/predictionHub")
>  .build();
>
>hub.on("currentRound", data => { /* ... */ });
>hub.on("nextRound", data => { /* ... */ });
>hub.on("roundStarted", data => { /* ... */ });
>hub.on("roundLocked", data => { /* ... */ });
>hub.on("settlementStarted", data => { /* ... */ });
>hub.on("settlementEnded", data => { /* ... */ });
>hub.on("roundEnded", data => { /* ... */ });
>
>await hub.start();
>// 用户在连接钱包后调用，确保能收到个人通知
>await hub.invoke("joinAddress", userAddress);
> ```
>
>**说明**：除 `currentRound` 与 `nextRound` 外，其他回合相关响应均同时包含 `id` 与 `epoch` 字段，`id` 用于后续业务请求，`epoch` 用于展示回合期次。

------
## 枚举说明

### 回合状态（RoundStatus）

| 值 | 名称 | 说明 |
| --- | --- | --- |
| 1 | Betting | 未开始，正在下注 |
| 2 | Locked | 已锁价，等待收盘 |
| 3 | Calculating | 结算中 |
| 4 | Completed | 结算完成，可领奖 |
| 5 | Cancelled | 回合取消或平盘 |

### 下注方向（Position）

| 值 | 名称 | 说明 |
| --- | --- | --- |
| 1 | Bull | 看涨 |
| 2 | Bear | 看跌 |
| 3 | Tie | 平盘或退还本金 |

### 领奖交易状态（ClaimStatus）

| 值 | 名称 | 说明 |
| --- | --- | --- |
| 0 | Pending | 交易已提交待确认 |
| 1 | Confirmed | 交易已确认 |
| 2 | Failed | 交易失败 |

### 下注记录状态（BetStatus）

| 值 | 名称 | 说明 |
| --- | --- | --- |
| 0 | Pending | 待确认 |
| 1 | Confirmed | 已确认 |
| 2 | Failed | 失败或超时 |

### 我的下注记录筛选（status）

| 枚举值 | 说明 |
| --- | --- |
| all | 全部记录 |
| claimed | 已领取 |
| unclaimed | 未领取 |

### 排行榜排序字段（rankBy）

| 枚举值 | 说明 |
| --- | --- |
| rounds | 参与回合数 |
| netProfit | 净收益 |
| totalBet | 累计下注 |
| winRate | 胜率 |
| totalReward | 总奖金 |

## 1️⃣ 当前回合（`currentRound` • WS 推送）

- **SignalR 事件**：`currentRound`

- **推送频率**：每 5 秒（或当有下注 / 价格变化时实时推送）

| 字段名       | 类型            | 说明                                   |
| ------------ | --------------- | -------------------------------------- |
| `id`         | int             | 回合唯一编号                           |
| `currentPrice` | string(decimal) | 最新价格                               |
| `totalAmount`  | string(decimal) | 总下注金额                             |
| `bullAmount`   | string(decimal) | 押 **上涨** 的金额                     |
| `bearAmount`   | string(decimal) | 押 **下跌** 的金额                     |
| `rewardPool`   | string(decimal) | 扣除手续费后的可分配奖金池             |
| `bullOdds`     | string(decimal) | 上涨赔率 = `totalAmount / bullAmount` |
| `bearOdds`     | string(decimal) | 下跌赔率 = `totalAmount / bearAmount` |

**示例：**

```json
{
  "id": 357690,
  "currentPrice": "309.125",
  "totalAmount": "2500",
  "bullAmount": "1400",
  "bearAmount": "1100",
  "rewardPool": "2425",
  "bullOdds": "1.78571428",
  "bearOdds": "2.20454545"
}
```

------

## 2️⃣ 当前下注回合（`nextRound` • WS 推送）

- **SignalR 事件**：`nextRound`

- **推送频率**：下注变动时实时推送

仅推送下个回合实时变化的字段：

| 字段名       | 类型            | 说明                                   |
| ------------ | --------------- | -------------------------------------- |
| `id`         | int             | 回合唯一编号                           |
| `totalAmount`| string(decimal) | 总下注金额                             |
| `bullAmount` | string(decimal) | 押 **上涨** 的金额                     |
| `bearAmount` | string(decimal) | 押 **下跌** 的金额                     |
| `rewardPool` | string(decimal) | 扣除手续费后的可分配奖金               |
| `bullOdds`   | string(decimal) | 上涨赔率 = `totalAmount / bullAmount` |
| `bearOdds`   | string(decimal) | 下跌赔率 = `totalAmount / bearAmount` |

------

## 3️⃣ 回合开始通知（`roundStarted` • WS 广播）

- **SignalR 事件**：`roundStarted`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |
## 4️⃣ 回合锁定通知（`roundLocked` • WS 广播）

- **SignalR 事件**：`roundLocked`

- **推送内容**：

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| `id` | int | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |


## 5️⃣ 开始结算通知（`settlementStarted` • WS 广播）

- **SignalR 事件**：`settlementStarted`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |

## 6️⃣ 结束结算通知（`settlementEnded` • WS 广播）

- **SignalR 事件**：`settlementEnded`

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `id` | int  | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |

## 7️⃣ 回合结束通知（`roundEnded` • WS 广播）

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

## 8️⃣ ~~历史回合列表~~

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
    "lockPrice": "308.85",
    "closePrice": "309.75",
    "totalAmount": "1800",
    "bullAmount": "1000",
    "bearAmount": "800",
    "rewardPool": "1746",
    "endTime": 1709999999,
    "bullOdds": "1.8",
    "bearOdds": "2.25"
  }
]
```

------

## 9️⃣ ~~即将开始回合~~

### `GET /api/rounds/upcoming`

返回下一回合的编号、期次和时间，供前端倒计时与下注使用。

| 字段名      | 类型 | 说明           |
| ----------- | ---- | -------------- |
| `id`   | int  | 回合唯一编号（可能为 0） |
| `epoch` | int  | 期次（Epoch）       |
| `startTime` | int  | 开始时间（秒） |
| `endTime`   | int  | 结束时间（秒） |

------

## 1️⃣0️⃣ 价格走势图

### `GET /api/price/chart`

返回最近 **10** 分钟价格，每 20 秒 1 条。

| 字段名       | 类型              | 说明                |
| ------------ | ----------------- | ------------------- |
| `timestamps` | int[]             | Unix 秒数组（升序） |
| `prices`     | string(decimal)[] | 价格数组            |

------

## 1️⃣1️⃣ 我的下注记录

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

| 字段名 | 类型 | 说明 |
| ------ | ---- | ---- |
| `id` | int | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |
| `lockPrice` | string(decimal) | 锁定价格 |
| `closePrice` | string(decimal) | 收盘价格 |
| `totalAmount` | string(decimal) | 总下注金额 |
| `bullAmount` | string(decimal) | 押 **上涨** 的金额 |
| `bearAmount` | string(decimal) | 押 **下跌** 的金额 |
| `rewardPool` | string(decimal) | 奖金池（扣手续费） |
| `startTime` | int | 开始时间 |
| `endTime` | int | 结束时间 |
| `status` | enum | 回合状态 |
| `bullOdds` | string(decimal) | 上涨赔率 |
| `bearOdds` | string(decimal) | 下跌赔率 |
| `winnerSide` | enum | 回合结果，获胜方 |
| `position` | enum | 用户下注方向，可能为 null |
| `betAmount` | string(decimal) | 用户下注金额 |
| `reward` | string(decimal) | 奖励金额 |
| `claimed` | bool | 是否已领取 |

### mode = `pnl`

```
GET /api/predictions/pnl
```

返回盈亏汇总：

| 字段名 | 类型 | 说明 |
| ------ | ---- | ---- |
| `totalBet` | string(decimal) | 累计下注金额 |
| `totalReward` | string(decimal) | 累计奖励金额 |
| `netProfit` | string(decimal) | 最终盈亏 |
| `rounds` | int | 参与回合数 |
| `winRounds` | int | 获胜回合数 |
| `loseRounds` | int | 失利回合数 |
| `winRate` | string | 胜率（0-1） |
| `averageBet` | string(decimal) | 平均投入/回合 |
| `averageReturn` | string(decimal) | 平均奖励/回合 |
| `bestRoundId` | int | 最佳回合编号 |
| `bestRoundProfit` | string(decimal) | 最佳回合收益 |

------

## 1️⃣2️⃣ 排行榜

### `GET /api/leaderboard/list`

| 参数名     | 类型   | 默认值    | 说明                                       |
| ---------- | ------ | --------- | ------------------------------------------ |
| `symbol`   | string | TONUSD    | 币种对                                     |
| `rankBy`   | enum   | netProfit | rounds \| netProfit \| totalBet \| winRate |
| `page`     | int    | 1         | 当前页                                     |
| `pageSize` | int    | 10        | 分页大小，<=100                            |
| `address`  | string |           | 若传入则返回该地址在列表中的页码 & 排名    |
## 1️⃣3️⃣ 领奖

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

## 1️⃣4️⃣ 下注上报

### `POST /api/bet/report`

用户在转账后调用此接口上报交易 BOC，后台根据用户地址轮询最近交易，匹配消息哈希后记录下注信息。
转账 Comment 格式为 `<回合ID> bull` 或 `<回合ID> bear`。

请求体：
```json
{ "address": "<用户地址>", "boc": "<交易BOC>" }
```

返回字段：
| 字段名 | 类型 | 说明 |
| ------ | ---- | ---- |
| `data` | string | 交易哈希 |

## 1️⃣5️⃣ 最近回合及下注信息

### `GET /api/rounds/recent?symbol=ton&limit=5&address=<ADDR>`

返回最近若干回合及（可选）指定地址的下注情况，未传地址时仅返回回合信息。

| 字段名 | 类型 | 说明 |
| ------ | ---- | ---- |
| `id` | int | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |
| `lockPrice` | string(decimal) | 锁定价格 |
| `closePrice` | string(decimal) | 收盘价格 |
| `totalAmount` | string(decimal) | 总下注金额 |
| `bullAmount` | string(decimal) | 押 **上涨** 的金额 |
| `bearAmount` | string(decimal) | 押 **下跌** 的金额 |
| `rewardPool` | string(decimal) | 奖金池（扣手续费） |
| `startTime` | int | 开始时间 |
| `endTime` | int | 结束时间 |
| `bullOdds` | string(decimal) | 上涨赔率 |
| `bearOdds` | string(decimal) | 下跌赔率 |
| `position` | enum | 用户下注方向，可能为 null |
| `betAmount` | string(decimal) | 用户下注金额 |
| `reward` | string(decimal) | 奖励金额 |
| `claimed` | bool | 是否已领取 |

## 1️⃣6️⃣ 下注成功通知（`betPlaced` • 单用户推送）

- **SignalR 事件**：`betPlaced`
- **触发时机**：监听到用户向主钱包转账并确认成功
- **推送内容**：

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| `id` | int | 回合唯一编号 |
| `epoch` | int | 期次（Epoch） |
| `amount` | string(decimal) | 下注金额 |
| `txHash` | string | 交易哈希 |

> 客户端连接 SignalR 后，需要在用户连接钱包时调用 `joinAddress` 方法并传入地址，服务器将基于该地址推送消息。

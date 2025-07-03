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
> WebSocket 统一入口：`wss://<host>/ws`，订阅消息需发送 JSON：`{"event":"<topic>"}`。

------

## 1️⃣ 当前回合（`currentRound` • WS 推送）

- **订阅方式**：

  ```json
  { "event": "currentRound" }
  ```

- **推送频率**：每 5 秒（或当有下注 / 价格变化时实时推送）

| 字段名         | 类型            | 说明                                     |
| -------------- | --------------- | ---------------------------------------- |
| `roundId`      | int             | 回合编号                                 |
| `lockPrice`    | string(decimal) | 锁定价格                                 |
| `currentPrice` | string(decimal) | 最新价格                                 |
| `totalAmount`  | string(decimal) | 总下注金额                               |
| `upAmount`     | string(decimal) | 押 **上涨** 的金额                       |
| `downAmount`   | string(decimal) | 押 **下跌** 的金额                       |
| `rewardPool`   | string(decimal) | 扣除手续费后的可分配奖金池               |
| `endTime`      | int             | 回合结束时间（Unix 秒）                  |
| `oddsUp`       | string(decimal) | 上涨赔率 = `totalAmount / upAmount`      |
| `oddsDown`     | string(decimal) | 下跌赔率 = `totalAmount / downAmount`    |
| `status`       | enum            | `upcoming` | `live` | `locked` | `ended` |

**示例：**

```json
{
  "roundId": 357690,
  "lockPrice": "308.85000000",
  "currentPrice": "309.12500000",
  "totalAmount": "2500.00000000",
  "upAmount": "1400.00000000",
  "downAmount": "1100.00000000",
  "rewardPool": "2425.00000000",
  "endTime": 1710001234,
  "status": "live",
  "oddsUp": "1.78571428",
  "oddsDown": "2.20454545"
}
```

------

## 2️⃣ 回合结束通知（`roundEnded` • WS 广播）

- **订阅方式**：

  ```json
  { "event": "roundEnded" }
  ```

- **推送内容**：

| 字段名    | 类型 | 说明     |
| --------- | ---- | -------- |
| `roundId` | int  | 回合编号 |

**示例：**

```json
{ "roundId": 357690 }
```

------

## 3️⃣ 历史回合列表

### `GET /api/rounds/history?limit=3`

- **说明**：返回最近 `limit` 个已结束回合，默认 3，最大 100。

| 字段名        | 类型            | 说明               |
| ------------- | --------------- | ------------------ |
| `roundId`     | int             | 回合编号           |
| `lockPrice`   | string(decimal) | 锁定价格           |
| `closePrice`  | string(decimal) | 收盘价格           |
| `totalAmount` | string(decimal) | 总下注金额         |
| `upAmount`    | string(decimal) | 押 **上涨** 的金额 |
| `downAmount`  | string(decimal) | 押 **下跌** 的金额 |
| `rewardPool`  | string(decimal) | 奖金池（扣手续费） |
| `endTime`     | int             | 结束时间           |
| `oddsUp`      | string(decimal) | 上涨赔率           |
| `oddsDown`    | string(decimal) | 下跌赔率           |

**示例：**

```json
[
  {
    "roundId": 357689,
    "lockPrice": "308.85000000",
    "closePrice": "309.75000000",
    "totalAmount": "1800.00000000",
    "upAmount": "1000.00000000",
    "downAmount": "800.00000000",
    "rewardPool": "1746.00000000",
    "endTime": 1709999999,
    "oddsUp": "1.80000000",
    "oddsDown": "2.25000000"
  }
]
```

------

## 4️⃣ 即将开始回合

### `GET /api/rounds/upcoming`

返回未来 **2** 个预告回合时间，使前端可倒计时。

| 字段名      | 类型 | 说明           |
| ----------- | ---- | -------------- |
| `roundId`   | int  | 回合编号       |
| `startTime` | int  | 开始时间（秒） |
| `endTime`   | int  | 结束时间（秒） |

------

## 5️⃣ 价格走势图（`chartData` • WS 推送）

- **订阅方式**：

  ```json
  { "event": "chartData" }
  ```

- **说明**：推送最近 10 分钟价格，每隔 20 秒采样 1 点。

| 字段名       | 类型              | 说明                |
| ------------ | ----------------- | ------------------- |
| `timestamps` | int[]             | Unix 秒数组（升序） |
| `prices`     | string(decimal)[] | 价格数组            |

------

## 6️⃣ 我的下注记录

### mode = `round`

```
GET /api/user/predictions/round
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
| `roundId`    | int             | 回合编号                |
| `position`   | enum            | `up` | `down`           |
| `amount`     | string(decimal) | 押注金额                |
| `lockPrice`  | string(decimal) | 锁定价格                |
| `closePrice` | string(decimal) | 收盘价格                |
| `reward`     | string(decimal) | 奖励（可能为 0）        |
| `claimed`    | bool            | 是否已领取              |
| `result`     | enum            | `win` | `lose` | `draw` |

### mode = `pnl`

```
GET /api/user/predictions/pnl
```

返回盈亏汇总，字段同现有文档，字段名保持不变。

------

## 7️⃣ 排行榜

### `GET /api/leaderboard/list`

| 参数名     | 类型   | 默认值    | 说明                                       |
| ---------- | ------ | --------- | ------------------------------------------ |
| `symbol`   | string | TONUSD    | 币种对                                     |
| `rankBy`   | enum   | netProfit | rounds \| netProfit \| totalBet \| winRate |
| `page`     | int    | 1         | 当前页                                     |
| `pageSize` | int    | 10        | 分页大小，<=100                            |
| `address`  | string |           | 若传入则返回该地址在列表中的页码 & 排名    |

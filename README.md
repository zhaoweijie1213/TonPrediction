# TON Prediction Backend

## 项目简介
TON 网络的中心化预测游戏后端，提供回合创建、下注结算与统计等服务。

## 快速开始
1. 安装 .NET 9.0 并执行 `dotnet restore`。
2. 配置下表环境变量后启动服务：

| Key | 说明 |
| --- | --- |
| `ENV_MASTER_WALLET_ADDRESS` | 主钱包地址 |
| `ENV_MASTER_WALLET_PK` | 主钱包私钥 |
| `ENV_PRICE_API_KEY` | 行情 API 密钥 |
| `ENV_TONCENTER_ENDPOINT` | TonCenter API 地址 |
| `ENV_TONCENTER_API_KEY` | TonCenter API 密钥 |
| `ENV_RABBITMQ_HOST` | RabbitMQ 地址 |
| `ENV_RABBITMQ_USER` | RabbitMQ 用户名 |
| `ENV_RABBITMQ_PASSWORD` | RabbitMQ 密码 |

3. 运行 `docker compose up -d db redis ton-node price-oracle`。
4. 执行 `dotnet run --project TonPrediction.Api`。

## 架构概览
后端包含 API 层、应用层及基础设施层，使用 BackgroundService 调度回合，CAP 与 RabbitMQ 负责事件发布与订阅。

## 常见命令
- `dotnet build -c Release`
- `dotnet test`

## 部署说明
可通过 Docker 或 Kubernetes 部署，需保证数据库、Redis 与 RabbitMQ 可用。

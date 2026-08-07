# DNCTECH
.NET Core Learning Demo & Core Concepts Breakdown

## 2FA 对照示例（新增）

| 项目 | 说明 | 运行 |
|------|------|------|
| [Identity2FA_Demo](./Identity2FA_Demo) | 原生 ASP.NET Core Identity + SQLite + Authenticator 2FA | `dotnet run --project Identity2FA_Demo` → https://localhost:5101 |
| [Custom2FA_Demo](./Custom2FA_Demo) | 自建用户系统，模仿 Identity 抽象与两段式 2FA（JWT mfa_ticket） | `dotnet run --project Custom2FA_Demo` → https://localhost:5201 |

建议先跑 Identity 版建立直觉，再读 Custom 版的 `Abstractions/Contracts.cs` 与 `Services/SignInService.cs` 对照学习。

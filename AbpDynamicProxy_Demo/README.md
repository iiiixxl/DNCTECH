# AbpDynamicProxy_Demo

模拟 **ABP 动态代理 + AuthorizationInterceptor** 的最小可运行 WebAPI。

**完整深度文档：** [ABP-DynamicProxy-DeepDive.md](./ABP-DynamicProxy-DeepDive.md)

文档第 1 章（置顶、最详细）：**Castle.Core 扩展链路与双适配器**

**适配器概念小灶（不依赖 Castle，纯接口）：** [AdapterConcept/README.md](./AdapterConcept/README.md)  
跑：`GET /api/adapter-concept/run` 与 `GET /api/adapter-concept/deny`，看返回的 `trace`。

## 怎么跑

```bash
cd AbpDynamicProxy_Demo
dotnet run
```

Swagger：`http://localhost:5183/swagger`  
请求头：`X-Permissions: Demo.Users,Demo.Users.Create`  
用例：`AbpDynamicProxy_Demo.http`

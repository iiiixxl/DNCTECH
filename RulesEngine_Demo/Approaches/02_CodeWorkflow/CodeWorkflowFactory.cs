using RulesEngine.Models;

namespace RulesEngine_Demo.Approaches.CodeWorkflow;

/// <summary>用纯 C# 对象树构建 Workflow（与 JSON 业务逻辑对齐）。</summary>
public static class CodeWorkflowFactory
{
    public static IReadOnlyList<Workflow> CreateAll() =>
    [
        CreateOrderDiscount(),
        CreateApprovalRouting()
    ];

    public static Workflow CreateOrderDiscount() => new()
    {
        WorkflowName = "OrderDiscount",
        Rules = new List<Rule>
        {
            new Rule
            {
                RuleName = "VipMemberDiscount",
                Enabled = true,
                Properties = Priority("100"),
                Expression = "customer.Level == 3",
                SuccessEvent = "VipDiscount15Off",
                Actions = Output("0.85")
            },
            new Rule
            {
                RuleName = "GoldMemberDiscount",
                Enabled = true,
                Properties = Priority("90"),
                Expression = "customer.Level == 2",
                SuccessEvent = "GoldDiscount10Off",
                Actions = Output("0.90")
            },
            new Rule
            {
                RuleName = "SilverMemberDiscount",
                Enabled = true,
                Properties = Priority("80"),
                Expression = "customer.Level == 1",
                SuccessEvent = "SilverDiscount5Off",
                Actions = Output("0.95")
            },
            new Rule
            {
                RuleName = "ElectronicsFullReduce",
                Enabled = true,
                Properties = Priority("70"),
                Operator = "And",
                SuccessEvent = "ElectronicsMinus200",
                Rules =
                [
                    new Rule { RuleName = "IsElectronics", Expression = "order.Category == \"Electronics\"" },
                    new Rule { RuleName = "AmountOver2000", Expression = "order.Amount >= 2000" },
                    new Rule { RuleName = "NotUsingCoupon", Expression = "order.UsedCoupon == false" }
                ],
                Actions = Output("-200")
            },
            new Rule
            {
                RuleName = "FirstOrderGift",
                Enabled = true,
                Properties = Priority("60"),
                Expression = "order.IsFirstOrder == true && order.Amount >= 99",
                SuccessEvent = "FirstOrderMinus50",
                Actions = Output("-50")
            },
            new Rule
            {
                RuleName = "CouponSave100",
                Enabled = true,
                Properties = Priority("50"),
                Expression = "order.UsedCoupon == true && order.CouponCode == \"SAVE100\" && order.Amount >= 500",
                SuccessEvent = "CouponSAVE100",
                Actions = Output("-100")
            }
        }
    };

    public static Workflow CreateApprovalRouting() => new()
    {
        WorkflowName = "ApprovalRouting",
        Rules = new List<Rule>
        {
            new Rule
            {
                RuleName = "CapExMustCeo",
                Enabled = true,
                Properties = Priority("1000"),
                Expression = "req.ExpenseType == \"CapEx\" || req.Amount >= 500000",
                SuccessEvent = "RouteCeo",
                Actions = Output("\"CEO -> FINANCE_VP -> DEPT_DIR\"")
            },
            new Rule
            {
                RuleName = "HighRiskVendorEscalation",
                Enabled = true,
                Properties = Priority("900"),
                Expression = "req.VendorRisk == \"High\" && req.Amount >= 10000 && req.Amount < 500000 && req.ExpenseType != \"CapEx\"",
                SuccessEvent = "RouteRiskCommittee",
                Actions = Output("\"RISK_COMMITTEE -> FINANCE_VP -> DEPT_MANAGER\"")
            },
            new Rule
            {
                RuleName = "MarketingLargeUrgent",
                Enabled = true,
                Properties = Priority("800"),
                Operator = "And",
                SuccessEvent = "RouteCmo",
                Rules =
                [
                    new Rule { RuleName = "IsMarketing", Expression = "req.ExpenseType == \"Marketing\"" },
                    new Rule { RuleName = "Over100k", Expression = "req.Amount >= 100000 && req.Amount < 500000" },
                    new Rule { RuleName = "NotHighRisk", Expression = "req.VendorRisk != \"High\"" }
                ],
                Actions = Output("\"CMO -> FINANCE_MANAGER -> DEPT_MANAGER\"")
            },
            new Rule
            {
                RuleName = "MidAmountDeptDirector",
                Enabled = true,
                Properties = Priority("500"),
                Expression = "req.Amount >= 20000 && req.Amount < 500000 && req.ExpenseType != \"CapEx\" && req.VendorRisk != \"High\" && !(req.ExpenseType == \"Marketing\" && req.Amount >= 100000)",
                SuccessEvent = "RouteDeptDir",
                Actions = Output("\"DEPT_DIR -> DEPT_MANAGER\"")
            },
            new Rule
            {
                RuleName = "SmallAmountManager",
                Enabled = true,
                Properties = Priority("100"),
                Expression = "req.Amount < 20000 && req.ExpenseType != \"CapEx\"",
                SuccessEvent = "RouteManager",
                Actions = Output("\"DEPT_MANAGER\"")
            },
            new Rule
            {
                RuleName = "UrgentFlag",
                Enabled = true,
                Properties = Priority("50"),
                Expression = "req.IsUrgent == true",
                SuccessEvent = "Sla4Hours"
            },
            new Rule
            {
                RuleName = "MissingContractFlag",
                Enabled = true,
                Properties = Priority("40"),
                Expression = "req.ExpenseType == \"Purchase\" && req.HasContract == false && req.Amount >= 5000",
                SuccessEvent = "RequireContractAttachment"
            }
        }
    };

    private static Dictionary<string, object> Priority(string value) =>
        new() { ["Priority"] = value };

    private static RuleActions Output(string expression) => new()
    {
        OnSuccess = new ActionInfo
        {
            Name = "OutputExpression",
            Context = new Dictionary<string, object> { ["Expression"] = expression }
        }
    };
}

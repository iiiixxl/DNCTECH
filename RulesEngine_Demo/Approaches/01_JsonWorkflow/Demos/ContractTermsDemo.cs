using RulesEngine.Models;
using RulesEngine_Demo.Approaches;
using RulesEngine_Demo.Infrastructure;
using RulesEngine_Demo.Models;

namespace RulesEngine_Demo.Approaches.JsonWorkflow;

/// <summary>
/// 场景4：B2B 合同商务条款。
/// 业务痛点：折扣、账期、质保由客户分层与年框承诺驱动，销售经常「特批」——规则引擎可收敛标准价与例外。
/// </summary>
public sealed class ContractTermsDemo : IJsonBusinessDemo
{
    public string Key => "4";
    public string Title => "B2B 合同商务条款";
    public string Description => "伙伴等级/年框/政企 → 标准折扣、账期天数、是否需特批";

    public async Task RunAsync(RuleEngineHost host, string rulesDir)
    {
        ConsoleUi.Title(Title);
        ConsoleUi.Info("Workflow: ContractTerms");
        ConsoleUi.Info("要点: LocalParams 抽取中间变量；多条规则拼出条款包（折扣/账期/特批标记）。");

        var cases = new (string Label, ContractInput Input)[]
        {
            ("普通渠道商小年框",
                new ContractInput
                {
                    PartnerId = "PT-01", PartnerTier = "Standard", AnnualCommitAmount = 200_000,
                    CooperationYears = 1, ProductLine = "SaaS", Region = "CN"
                }),

            ("战略伙伴大年框",
                new ContractInput
                {
                    PartnerId = "PT-02", PartnerTier = "Strategic", AnnualCommitAmount = 5_000_000,
                    CooperationYears = 5, ProductLine = "SaaS", Region = "CN"
                }),

            ("政企硬件项目",
                new ContractInput
                {
                    PartnerId = "PT-03", PartnerTier = "Preferred", AnnualCommitAmount = 1_200_000,
                    CooperationYears = 3, IsGovernment = true, ProductLine = "Hardware", Region = "CN"
                }),
        };

        foreach (var (label, input) in cases)
        {
            ConsoleUi.Section(label);
            Console.WriteLine(
                $"伙伴={input.PartnerId}  分层={input.PartnerTier}  年框={input.AnnualCommitAmount:C}  " +
                $"合作={input.CooperationYears}年  政企={input.IsGovernment}  产品线={input.ProductLine}");

            var results = await host.ExecuteAsync(
                "ContractTerms",
                new RuleParameter("c", input));

            RuleResultFormatter.Print(results);

            // 条款包：从各规则 Output 约定字符串 "discount:0.88" / "net:60" / "needSpecialApproval:true"
            var terms = ParseTerms(results);
            ConsoleUi.Success(
                $"建议条款 → 折扣={terms.Discount:P0}  账期=Net{terms.NetDays}  需特批={terms.NeedSpecialApproval}");
            if (terms.Tags.Count > 0)
                Console.WriteLine($"标签: {string.Join(", ", terms.Tags)}");
        }

        ConsoleUi.Section("落地提示");
        Console.WriteLine("· 标准条款走规则；「低于标准折扣」走特批流程（SuccessEvent=NeedSpecialApproval）。");
        Console.WriteLine("· 与报价系统集成时，规则结果应落库为合同草稿的只读建议，人工确认后再生效。");
    }

    private static TermsBundle ParseTerms(List<RuleResultTree> results)
    {
        var bundle = new TermsBundle { Discount = 1m, NetDays = 30 };
        foreach (var output in RuleResultFormatter.AllSuccessOutputs(results))
        {
            var text = output.ToString() ?? "";
            var parts = text.Split(':', 2);
            if (parts.Length != 2) continue;
            switch (parts[0].Trim().ToLowerInvariant())
            {
                case "discount" when decimal.TryParse(parts[1], out var d):
                    bundle.Discount = Math.Min(bundle.Discount, d);
                    break;
                case "net" when int.TryParse(parts[1], out var n):
                    bundle.NetDays = Math.Max(bundle.NetDays, n);
                    break;
                case "needspecialapproval" when bool.TryParse(parts[1], out var b):
                    bundle.NeedSpecialApproval |= b;
                    break;
            }
        }

        bundle.Tags.AddRange(RuleResultFormatter.CollectSuccessEvents(results));
        return bundle;
    }

    private sealed class TermsBundle
    {
        public decimal Discount { get; set; }
        public int NetDays { get; set; }
        public bool NeedSpecialApproval { get; set; }
        public List<string> Tags { get; } = [];
    }
}


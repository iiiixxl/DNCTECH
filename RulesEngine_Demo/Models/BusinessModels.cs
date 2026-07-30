namespace RulesEngine_Demo.Models;

/// <summary>会员等级 —— 电商/SaaS 常见分层。</summary>
public enum MemberLevel
{
    Normal = 0,
    Silver = 1,
    Gold = 2,
    Vip = 3
}

/// <summary>下单上下文：促销引擎输入。</summary>
public sealed class OrderInput
{
    public string OrderId { get; init; } = "";
    public decimal Amount { get; init; }
    public int ItemCount { get; init; }
    public string Category { get; init; } = ""; // Electronics / Fashion / Fresh
    public bool IsFirstOrder { get; init; }
    public bool UsedCoupon { get; init; }
    public string CouponCode { get; init; } = "";
    public DateTime OrderTime { get; init; } = DateTime.Now;
}

/// <summary>下单会员。</summary>
public sealed class CustomerInput
{
    public string CustomerId { get; init; } = "";
    public string Name { get; init; } = "";
    public MemberLevel Level { get; init; }
    public int Points { get; init; }
    public int OrderCountThisYear { get; init; }
    public decimal SpendThisYear { get; init; }
}

/// <summary>费用/采购审批单。</summary>
public sealed class ApprovalInput
{
    public string RequestId { get; init; } = "";
    public string ApplicantDept { get; init; } = ""; // Sales / R&D / Admin / Finance
    public string ExpenseType { get; init; } = "";   // Travel / Purchase / Marketing / CapEx
    public decimal Amount { get; init; }
    public bool IsUrgent { get; init; }
    public bool HasContract { get; init; }
    public string VendorRisk { get; init; } = "Low"; // Low / Medium / High
}

/// <summary>支付风控上下文。</summary>
public sealed class RiskInput
{
    public string PaymentId { get; init; } = "";
    public decimal Amount { get; init; }
    public decimal AvgOrderAmount30d { get; init; }
    public int AccountAgeDays { get; init; }
    public bool DeviceTrusted { get; init; }
    public bool IpCountryMatchesBilling { get; init; }
    public int FailedAttempts1h { get; init; }
    public bool IsNightTime { get; init; }
    public string Channel { get; init; } = "App"; // App / Web / POS
}

/// <summary>B2B 合同商务条款上下文。</summary>
public sealed class ContractInput
{
    public string PartnerId { get; init; } = "";
    public string PartnerTier { get; init; } = "Standard"; // Standard / Preferred / Strategic
    public decimal AnnualCommitAmount { get; init; }
    public int CooperationYears { get; init; }
    public bool IsGovernment { get; init; }
    public string Region { get; init; } = "CN";
    public string ProductLine { get; init; } = "SaaS"; // SaaS / Hardware / ProfessionalService
}

/// <summary>物流运费上下文。</summary>
public sealed class ShippingInput
{
    public string OrderId { get; init; } = "";
    public decimal OrderAmount { get; init; }
    public decimal WeightKg { get; init; }
    public int DistanceKm { get; init; }
    public string ShippingMethod { get; init; } = "Standard"; // Standard / Express / SameDay
    public bool IsRemoteArea { get; init; }
    public MemberLevel MemberLevel { get; init; }
}

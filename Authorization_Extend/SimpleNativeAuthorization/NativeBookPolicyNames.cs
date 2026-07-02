namespace Authorization_Extend.SimpleNativeAuthorization;

/// <summary>
/// 原生 Policy 名常量，避免魔法字符串。
/// </summary>
public static class NativeBookPolicyNames
{
    public const string View = "NativeBooks.View";
    public const string Create = "NativeBooks.Create";
    public const string Update = "NativeBooks.Update";
    public const string Delete = "NativeBooks.Delete";
}

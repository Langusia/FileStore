namespace Credo.Core.Shared.Messages;

public static class ExternalServiceNames
{
    public const string CssApi = "CssApi";
    public const string CreditInfoApi = "CreditInfoApi";
    public const string CashdeskApi = "CashdeskApi";
    public const string BillPaymentsApi = "BillPaymentsApi";
    public const string CmtsApi = "CmtsApi";
}

public static class ResponseErrorCode
{
    public const string BadRequest = "BAD_REQUEST";
    public const string NotFound = "NOT_FOUND";
    public const string ServiceCallError = "SERVICE_CALL_ERROR";
    public const string GeneralError = "GENERAL_ERROR";
}
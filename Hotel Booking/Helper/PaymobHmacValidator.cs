using System.Security.Cryptography;
using System.Text;

public static class PaymobHmacValidator
{
    public static bool Validate(PaymobTransactionObj obj, string receivedHmac, string secret)
    {
        if (string.IsNullOrWhiteSpace(receivedHmac))
            return false;

        var concatenated = string.Concat(
            obj.AmountCents,
            obj.CreatedAt,
            obj.Currency,
            obj.ErrorOccured.ToString().ToLower(),
            obj.HasParentTransaction.ToString().ToLower(),
            obj.Id,
            obj.IntegrationId,
            obj.Is3dSecure.ToString().ToLower(),
            obj.IsAuth.ToString().ToLower(),
            obj.IsCapture.ToString().ToLower(),
            obj.IsRefunded.ToString().ToLower(),
            obj.IsStandalonePayment.ToString().ToLower(),
            obj.IsVoided.ToString().ToLower(),
            obj.Order.Id,
            obj.Owner,
            obj.Pending.ToString().ToLower(),
            obj.SourceData.Pan,
            obj.SourceData.SubType,
            obj.SourceData.Type,
            obj.Success.ToString().ToLower());

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(concatenated);

        using var hmacSha512 = new HMACSHA512(keyBytes);
        var hashBytes = hmacSha512.ComputeHash(messageBytes);
        var computedHmac = Convert.ToHexString(hashBytes).ToLower();

        return computedHmac == receivedHmac.ToLower();
    }
}
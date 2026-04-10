namespace ERMS.Domain.Constants.Enterprise
{
    public static class PaymentOrderConstants
    {
        public static class Status
        {
            public const string Pending = "Pending";
            public const string Paid = "Paid";
            public const string Cancelled = "Cancelled";
            public const string Expired = "Expired";
        }

        public static class ActionType
        {
            public const string Renew = "Renew";
            public const string Upgrade = "Upgrade";
        }
    }
}

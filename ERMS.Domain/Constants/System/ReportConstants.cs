namespace ERMS.Domain.Constants.System
{
    public static class ReportConstants
    {
        public static class Status
        {
            public const string Pending = "Pending";
            public const string Reviewing = "Reviewing";
            public const string Resolved = "Resolved";
            public const string Dismissed = "Dismissed";
        }

        public static class Reason
        {
            public const string FraudulentInfo = "FraudulentInfo";
            public const string InappropriateContent = "InappropriateContent";
            public const string LaborLawViolation = "LaborLawViolation";
            public const string IllegalFeeCollection = "IllegalFeeCollection";
            public const string FakeContactInfo = "FakeContactInfo";
            public const string Other = "Other";
        }

        public static class EntityType
        {
            public const string JobPosting = "JobPosting";
            public const string Enterprise = "Enterprise";
        }

        public static class ActionTaken
        {
            public const string Dismissed = "Dismissed";
            public const string Resolved = "Resolved";
            public const string HidJobPosting = "HidJobPosting";
            public const string SuspendedEnterprise = "SuspendedEnterprise";
        }
    }
}

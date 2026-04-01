using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Domain.Constants.Training
{
    public static class TrainingRequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";

        public const string NeedRevision = "NeedRevision";  //   cần chỉnh sửa
        public const string Rejected = "Rejected";      //   từ chối hẳn

        public const string AddedToPlan = "AddedToPlan";
        public const string Completed = "Completed";
    }
}

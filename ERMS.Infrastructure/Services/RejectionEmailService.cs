using System.Net;
using System.Text;
using ERMS.Application.Interface;

namespace ERMS.Infrastructure.Services
{
    public sealed class RejectionEmailService : IRejectionEmailService
    {
        private readonly IEmailService _emailService;

        public RejectionEmailService(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendRejectionEmailAsync(RejectionEmailContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var subject = $"Kết quả ứng tuyển vị trí {context.JobTitle}";
            var htmlBody = BuildHtmlBody(context);

            await _emailService.SendEmailAsync(context.CandidateEmail, subject, htmlBody);
        }

        private static string BuildHtmlBody(RejectionEmailContext context)
        {
            var candidateName = HtmlEncode(context.CandidateName);
            var jobTitle = HtmlEncode(context.JobTitle);

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Kết quả ứng tuyển</title>
</head>
<body style='margin:0;padding:0;background-color:#f4f7fb;color:#163046;font-family:Segoe UI,Arial,sans-serif;'>
    <div style='width:100%;padding:32px 16px;background:linear-gradient(180deg,#eef4ff 0%,#f9fbff 100%);'>
        <div style='max-width:640px;margin:0 auto;background-color:#ffffff;border-radius:20px;overflow:hidden;border:1px solid #dbe5f0;box-shadow:0 10px 30px rgba(22,48,70,0.08);'>
            <div style='padding:36px 40px;background:linear-gradient(135deg,#0b5cab 0%,#0f7b6c 100%);color:#ffffff;'>
                <div style='font-size:13px;letter-spacing:1.4px;text-transform:uppercase;opacity:0.9;'>ERMS Recruitment</div>
                <h1 style='margin:14px 0 8px;font-size:28px;line-height:1.25;'>Kết quả ứng tuyển vị trí {jobTitle}</h1>
                <p style='margin:0;font-size:15px;line-height:1.7;opacity:0.95;'>Cảm ơn bạn đã dành thời gian tìm hiểu và ứng tuyển cùng chúng tôi.</p>
            </div>

            <div style='padding:36px 40px 20px;'>
                <p style='margin:0 0 16px;font-size:16px;line-height:1.8;'>Chào <strong>{candidateName}</strong>,</p>

                <p style='margin:0 0 16px;font-size:15px;line-height:1.8;color:#41556b;'>
                    Chúng tôi chân thành cảm ơn bạn đã quan tâm đến vị trí <strong>{jobTitle}</strong> và đã đầu tư thời gian cho quá trình ứng tuyển.
                </p>

                <p style='margin:0 0 22px;font-size:15px;line-height:1.8;color:#41556b;'>
                    Sau khi xem xét kỹ lưỡng hồ sơ và mức độ phù hợp với yêu cầu hiện tại của vị trí, chúng tôi rất tiếc phải thông báo rằng ở thời điểm này chúng tôi chưa thể tiếp tục đồng hành cùng bạn trong vòng tuyển dụng này.
                </p>

                <div style='margin:0 0 22px;padding:22px 24px;background-color:#f8fbff;border:1px solid #d8e6f3;border-radius:16px;'>
                    <h2 style='margin:0 0 14px;font-size:18px;color:#0b5cab;'>Một vài gợi ý để bạn tiếp tục phát triển</h2>
                    {BuildReasonMarkup(context)}
                    {BuildSkillGapMarkup(context.SkillGaps)}
                    {BuildConcernMarkup(context.Concerns, context.RejectionReason)}
                </div>

                <p style='margin:0 0 14px;font-size:15px;line-height:1.8;color:#41556b;'>
                    Chúng tôi tin rằng với sự chuẩn bị thêm ở một số khía cạnh phù hợp, bạn sẽ tiếp tục có nhiều cơ hội tích cực trong chặng đường nghề nghiệp sắp tới.
                </p>

                <p style='margin:0 0 30px;font-size:15px;line-height:1.8;color:#41556b;'>
                    Rất mong sẽ có dịp được kết nối lại với bạn trong những cơ hội phù hợp hơn trong tương lai. Chúc bạn nhiều sức khỏe, tự tin và thành công trên hành trình phía trước.
                </p>

                <p style='margin:0;font-size:15px;line-height:1.7;color:#163046;'>
                    Trân trọng,<br>
                    <strong>Bộ phận Tuyển dụng</strong><br>
                    ERMS System
                </p>
            </div>

            <div style='padding:18px 40px 28px;border-top:1px solid #e5edf5;font-size:12px;line-height:1.7;color:#70859a;background-color:#fbfdff;'>
                Email này được gửi tự động từ hệ thống ERMS. Vui lòng không trả lời trực tiếp email này.
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private static string BuildReasonMarkup(RejectionEmailContext context)
        {
            var improvementAreas = InferImprovementAreas(context.RejectionReason, context.SkillGaps, context.Concerns);
            var builder = new StringBuilder();

            builder.AppendLine(
                "<p style='margin:0 0 12px;font-size:14px;line-height:1.8;color:#41556b;'>Sau khi đánh giá tổng thể, chúng tôi nhận thấy hồ sơ của bạn hiện chưa thật sự khớp hoàn toàn với một số tiêu chí trọng yếu của vị trí này.</p>");

            if (improvementAreas.Count > 0)
            {
                builder.AppendLine(
                    $"<p style='margin:0;font-size:14px;line-height:1.8;color:#41556b;'>Trong thời gian tới, bạn có thể ưu tiên củng cố thêm {JoinWithConjunction(improvementAreas)} để tăng mức độ phù hợp với các cơ hội tương tự.</p>");
            }
            else
            {
                builder.AppendLine(
                    "<p style='margin:0;font-size:14px;line-height:1.8;color:#41556b;'>Trong thời gian tới, bạn có thể tiếp tục củng cố kinh nghiệm thực tiễn, chiều sâu chuyên môn và cách thể hiện thế mạnh của mình cho các vai trò tương tự.</p>");
            }

            return builder.ToString();
        }

        private static string BuildSkillGapMarkup(string[]? skillGaps)
        {
            var normalizedSkillGaps = NormalizeItems(skillGaps);
            if (normalizedSkillGaps.Count == 0)
            {
                return string.Empty;
            }

            var items = string.Join(
                string.Empty,
                normalizedSkillGaps.Select(skill =>
                    $"<li style='margin:0 0 8px;color:#41556b;'>{HtmlEncode(skill)}</li>"));

            return $@"
<div style='margin-top:16px;'>
    <p style='margin:0 0 10px;font-size:14px;font-weight:600;color:#163046;'>Những năng lực chuyên môn có thể ưu tiên bồi dưỡng thêm:</p>
    <ul style='margin:0;padding-left:20px;font-size:14px;line-height:1.7;'>
        {items}
    </ul>
</div>";
        }

        private static string BuildConcernMarkup(string[]? concerns, string rawReason)
        {
            var improvementAreas = InferImprovementAreas(rawReason, null, concerns);
            if (improvementAreas.Count == 0)
            {
                return string.Empty;
            }

            return $@"
<p style='margin:16px 0 0;font-size:14px;line-height:1.8;color:#41556b;'>
    Bên cạnh chuyên môn, bạn cũng có thể dành thêm thời gian để cải thiện {JoinWithConjunction(improvementAreas)}. Đây đều là những yếu tố được đánh giá cao trong môi trường làm việc thực tế.
</p>";
        }

        private static List<string> InferImprovementAreas(string rawReason, string[]? skillGaps, string[]? concerns)
        {
            var text = string.Join(
                " ",
                new[]
                {
                    rawReason,
                    string.Join(" ", skillGaps ?? []),
                    string.Join(" ", concerns ?? [])
                });

            var normalizedText = text.Trim().ToLowerInvariant();
            var areas = new List<string>();

            if (ContainsAny(normalizedText, "kinh nghiem", "experience", "thuc chien", "thuc te", "du an", "project"))
            {
                areas.Add("kinh nghiệm thực tiễn gắn với yêu cầu công việc");
            }

            if (ContainsAny(normalizedText, "giao tiep", "communication", "trinh bay", "dien dat", "collaboration", "teamwork"))
            {
                areas.Add("khả năng giao tiếp và phối hợp trong công việc");
            }

            if (ContainsAny(normalizedText, "problem", "tu duy", "phan tich", "giai quyet"))
            {
                areas.Add("tư duy phân tích và giải quyết vấn đề");
            }

            if (ContainsAny(normalizedText, "english", "tieng anh"))
            {
                areas.Add("khả năng sử dụng tiếng Anh trong môi trường chuyên môn");
            }

            if (ContainsAny(normalizedText, "attitude", "thai do", "culture", "van hoa", "chu dong"))
            {
                areas.Add("mức độ chủ động và khả năng thích nghi với môi trường làm việc");
            }

            if (areas.Count == 0 && NormalizeItems(skillGaps).Count > 0)
            {
                areas.Add("độ phủ kỹ năng chuyên môn phù hợp với vị trí");
            }

            return areas.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> NormalizeItems(IEnumerable<string>? items)
        {
            return items?
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static string JoinWithConjunction(IReadOnlyList<string> values)
        {
            if (values.Count == 0)
            {
                return string.Empty;
            }

            if (values.Count == 1)
            {
                return values[0];
            }

            return string.Join(", ", values.Take(values.Count - 1)) + " và " + values[^1];
        }

        private static string HtmlEncode(string value)
        {
            return WebUtility.HtmlEncode(value);
        }
    }
}


using INC_Care_App.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace INC_Care_App.Controllers
{
    public class ClaimController : Controller
    {
        public IActionResult Upload()
        {
            return View();
        }
        //[HttpPost]
        //public async Task<IActionResult> Upload(List<IFormFile> files)
        //{
        //    var claimList = new List<ClaimInfo>();

        //    foreach (var file in files)
        //    {
        //        using var stream = file.OpenReadStream();
        //        using var pdf = PdfDocument.Open(stream);
        //        string pdfText = string.Join("\n", pdf.GetPages().Select(p => p.Text));

        //        string source = pdfText.Contains("HERITAGE") ? "HERITAGE"
        //                      : pdfText.Contains("UNITED") ? "UHC"
        //                      : pdfText.Contains("RAKSHA") ? "RAKSHA"
        //                      : pdfText.Contains("MEDIASSIST") ? "MEDIASSIST"
        //                      : "OTHER";

        //        string proposer = ExtractValue(pdfText, @"Proposer/Employee Name\s*:\s*(.*?)\s*\(")
        //                    ?? ExtractValue(pdfText, @"Patient Name\s*:\s*(.*?)\s*\(");

        //        string patient = ExtractValue(pdfText, @"Patient Name\s*:\s*(.*?)\s*\(");


        //        string icardLine = ExtractValue(pdfText, @"I-Card No\.?\s*:\s*(.*?)\n", RegexOptions.IgnoreCase);
        //        string icard = icardLine?.Split("Relation")[0].Trim();

        //        string policyNo = ExtractValue(pdfText, @"Policy No\.?\s*:\s*(\d{10,})")
        //                   ?? ExtractValue(pdfText, @"Policy\s*Number\s*:\s*(\d{10,})");
        //        string hospital = ExtractValue(pdfText, @"Hospital Name\s*:\s*(.*?)\s*(\n|DOA|DOD)", RegexOptions.Singleline);

        //        string doa = ExtractValue(pdfText, @"DOA\s*:\s*(\d{2}/\d{2}/\d{4})");

        //        string amountClaimedRaw = ExtractValue(pdfText, @"Amount Claimed\s*:\s*\n?\s*([\d,\.]+)", RegexOptions.Singleline)
        //            ?? ExtractValue(pdfText, @"Tot\. of all Bills submitted\s*-\s*Rs\.?\s*([\d,\.]+)");

        //        string amountSettledRaw = ExtractValue(pdfText, @"Amount Settled\s*:\s*\n?\s*([\d,\.]+)", RegexOptions.Singleline)
        //            ?? ExtractValue(pdfText, @"Settled Amount\s*:\s*Rs\.?\s*([\d,\.]+)");

        //        decimal.TryParse(amountClaimedRaw?.Replace(",", ""), out var claimed);
        //        decimal.TryParse(amountSettledRaw?.Replace(",", ""), out var settled);
        //        var difference = claimed - settled;

        //        string deductionsRaw = ExtractValue(pdfText, @"Details of deductions\s*:\s*(.*?)(?=Sincerely yours|TEAM)", RegexOptions.Singleline)?? ExtractValue(pdfText, @"Disallowance Reason\s*:?\s*(.*?)\n", RegexOptions.Singleline | RegexOptions.IgnoreCase)
        //    ?? ExtractValue(pdfText, @"DISALLOWED REASONS\s*:?\s*(.*?)\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        //        string deductions = deductionsRaw?
        //            .Replace("\r\n", "<br>")
        //            .Replace("\n", "<br>")
        //            .Trim();


        //        //string deductions = deductionsRaw?.Replace("\r\n", "<br>").Replace("\n", "<br>").Trim();

        //        claimList.Add(new ClaimInfo
        //        {
        //            Source = source,
        //            Proposer = proposer,
        //            Patient = patient,
        //            ICardNumber = icard,
        //            PolicyNumber = policyNo,
        //            HospitalName = hospital,
        //            DOA = doa,
        //            AmountClaimed = claimed.ToString("N2"),
        //            AmountSettled = settled.ToString("N2"),
        //            Difference = difference.ToString("N2"),
        //            Deductions = deductions
        //        });
        //    }

        //    return View("ResultView", claimList);
        //}


        [HttpPost]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            var claimList = new List<ClaimInfo>();

            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                using var pdf = PdfDocument.Open(stream);
                string pdfText = string.Join("\n", pdf.GetPages().Select(p => p.Text));

                string source = GetSource(pdfText);

                ClaimInfo claim = source switch
                {
                    "HERITAGE" => ExtractFromHeritage(pdfText),
                    "ICICI" => ExtractFromICICI(pdfText),
                    "UNKNOWN" => ExtractFromFHPL(pdfText),
                    _ => new ClaimInfo()
                };

                claim.Source = source;
                claimList.Add(claim);
            }

            return View("ResultView", claimList);
        }

        private string GetSource(string pdfText)
        {
            return pdfText.Contains("HERITAGE") ? "HERITAGE"
                 : pdfText.Contains("ICICI") ? "ICICI"
                 : pdfText.Contains("FHPL") ? "FHPL"
                 : "UNKNOWN";
        }

        private ClaimInfo ExtractFromHeritage(string pdfText)
        {
            string proposer = ExtractValue(pdfText, @"Proposer/Employee Name\s*:\s*(.*?)\s*\(")
                            ;

            string patient = ExtractValue(pdfText, @"Patient Name\s*:\s*(.*?)\s*\(");


            string icardLine = ExtractValue(pdfText, @"I-Card No\.?\s*:\s*(.*?)\n", RegexOptions.IgnoreCase);
            string icard = icardLine?.Split("Relation")[0].Trim();

            string policyNo = ExtractValue(pdfText, @"Policy No\.?\s*:\s*(\d{10,})")
                       ;
            string hospital = ExtractValue(pdfText, @"Hospital Name\s*:\s*(.*?)\s*(\n|DOA|DOD)", RegexOptions.Singleline);

            string doa = ExtractValue(pdfText, @"DOA\s*:\s*(\d{2}/\d{2}/\d{4})");

            string amountClaimedRaw = ExtractValue(pdfText, @"Amount Claimed\s*:\s*\n?\s*([\d,\.]+)", RegexOptions.Singleline)
                ;

            string amountSettledRaw = ExtractValue(pdfText, @"Amount Settled\s*:\s*\n?\s*([\d,\.]+)", RegexOptions.Singleline)
                ;

            decimal.TryParse(amountClaimedRaw?.Replace(",", ""), out var claimed);
            decimal.TryParse(amountSettledRaw?.Replace(",", ""), out var settled);
            var difference = claimed - settled;

            string deductionsRaw = ExtractValue(pdfText, @"Details of deductions\s*:\s*(.*?)(?=Sincerely yours|TEAM)", RegexOptions.Singleline) ?? ExtractValue(pdfText, @"Disallowance Reason\s*:?\s*(.*?)\n", RegexOptions.Singleline | RegexOptions.IgnoreCase)
        ;
            string deductions = deductionsRaw?
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
                .Trim();


            //string deductions = deductionsRaw?.Replace("\r\n", "<br>").Replace("\n", "<br>").Trim();

            return new ClaimInfo
            {
                Proposer = proposer,
                Patient = patient,
                ICardNumber = icard,
                PolicyNumber = policyNo,
                DOA = doa,
                AmountClaimed = claimed.ToString("N2"),
                AmountSettled = settled.ToString("N2"),
                Difference = difference.ToString("N2"),
                Deductions = deductions?.Replace("\n", "<br>")
            };
        }

        private ClaimInfo ExtractFromICICI(string text)
        {
            // Extract block containing both names (2 lines after header)
            string namesBlock = ExtractValue(text, @"EMPLOYEE NAME\s+MEMBER NAME\s*\n([^\n]+)\n([^\n]+)", RegexOptions.IgnoreCase);

            string proposer = null;
            string patient = null;

            if (!string.IsNullOrWhiteSpace(namesBlock))
            {
                var lines = namesBlock.Split('\n');
                if (lines.Length >= 2)
                {
                    // First line: KRISHNA MOHAN     Adilakshmi
                    // Second line: ANANTHARAJU      [blank or continuation]
                    var firstLineParts = Regex.Split(lines[0].Trim(), @"\s{2,}");
                    var secondLineParts = Regex.Split(lines[1].Trim(), @"\s{2,}");

                    proposer = (firstLineParts.ElementAtOrDefault(0) + " " + secondLineParts.ElementAtOrDefault(0))?.Trim();
                    patient = (firstLineParts.ElementAtOrDefault(1) + " " + secondLineParts.ElementAtOrDefault(1))?.Trim();
                }
            }

            string icard = ExtractValue(text, @"CLAIM NO\s*:?=?\s*(\d+)", RegexOptions.IgnoreCase);
            string policyNo = ExtractValue(text, @"AL NO\s*:?=?\s*(\d+)", RegexOptions.IgnoreCase);

            string hospitalLine = ExtractValue(text, @"Name Of Hospital\s*:?=?\s*(.*?)\s+Address of the Hospital", RegexOptions.IgnoreCase)
                               ?? ExtractValue(text, @"HOSPITAL NAME\s*:?=?\s*(.*?)\s+(REQUESTED|NET)", RegexOptions.IgnoreCase);
            string hospital = hospitalLine?.Trim();

            string doa = ExtractValue(text, @"Date Of Admission\s*:?=?\s*(\d{2}-[A-Z]{3}-\d{2,4})", RegexOptions.IgnoreCase)
                      ?? ExtractValue(text, @"DOA\s*:?=?\s*(\d{2}-[A-Z]{3}-\d{2,4})", RegexOptions.IgnoreCase);

            string amountClaimedRaw = ExtractValue(text, @"Requested Amount in Rs\s*:?=?\s*([\d,]+)", RegexOptions.IgnoreCase)
                                    ?? ExtractValue(text, @"REQUESTED\s+AMOUNT\s*[:=]?[\s\n]*([\d,]+)", RegexOptions.IgnoreCase);

            string amountSettledRaw = ExtractValue(text, @"Final Amount Settled in Rs\.?\s*:?=?\s*([\d,]+)", RegexOptions.IgnoreCase)
                                    ?? ExtractValue(text, @"NET\s+SANCTIONED\s+AMOUNT\s*[:=]?[\s\n]*([\d,]+)", RegexOptions.IgnoreCase);

            decimal.TryParse(amountClaimedRaw?.Replace(",", ""), out var claimed);
            decimal.TryParse(amountSettledRaw?.Replace(",", ""), out var settled);
            var difference = claimed - settled;

            string deductionsRaw = ExtractValue(text, @"Sub Bill Breakup(.*?)Note:", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string deductions = deductionsRaw?.Replace("\r\n", "<br>").Replace("\n", "<br>").Trim();

            return new ClaimInfo
            {
                Proposer = proposer,
                Patient = patient,               
                PolicyNumber = policyNo,
                ClaimNumber = icard,
                DOA = doa,
                AmountClaimed = claimed.ToString("N2"),
                AmountSettled = settled.ToString("N2"),
                Difference = difference.ToString("N2"),
                Deductions = deductions
            };
        }

        private ClaimInfo ExtractFromFHPL(string text)
        {
            string patientName = null;
            var patientMatch = Regex.Match(text, @"Patient Name\s*([A-Za-z\s]+)Main Mem Name\s*([A-Za-z\s]+)", RegexOptions.IgnoreCase);
            if (patientMatch.Success)
            {
                patientName = $"{patientMatch.Groups[1].Value.Trim()}";
            }
            // Claim ID (Uhid No)
            string uhidNo = ExtractValue(text, @"Claim ID\s*:\s*(\d+)", RegexOptions.IgnoreCase);

            // Main UHID No
            string mainUhid = ExtractValue(text, @"Main Uhid No\s*(NIC\d+)", RegexOptions.IgnoreCase);

            // Hospital Name
            string hospital = ExtractValue(text, @"Hospital Name\s*(.*?)\s*Admission Date", RegexOptions.IgnoreCase);

            // Admission Date
            string doa = ExtractValue(text, @"Admission Date\s*(\d{1,2} \w{3,9} \d{4})", RegexOptions.IgnoreCase);

            // Billed Amount (this appears before Claimed Amount — be careful!)
            string billedAmountRaw = ExtractValue(text, @"Billed Amount\s*(\d+)", RegexOptions.IgnoreCase);

            // Settled Amount
            string settledAmountRaw = ExtractValue(text, @"Settled Amount\s*(\d+)", RegexOptions.IgnoreCase);

            // Disallowence Reason block (ends at "claim <claimno>" or similar punctuation)
            string deductions = ExtractValue(text, @"DisallowenceReason\s*(.+?)(?=Cheque Details|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Convert numbers
            decimal.TryParse(billedAmountRaw, out var billed);
            decimal.TryParse(settledAmountRaw, out var settled);

            return new ClaimInfo
            {
                Source="HDFC Bank",
                Patient = patientName,
                ClaimNumber = uhidNo,
               // PolicyNumber = mainUhid,
                DOA = doa,
                AmountClaimed = billed.ToString("N2"),
                AmountSettled = settled.ToString("N2"),
                Difference = (billed - settled).ToString("N2"),
                Deductions = deductions?.Trim()
            };
        }


        private string ExtractValue(string input, string pattern, RegexOptions options = RegexOptions.None)
        {
            var match = Regex.Match(input, pattern, options);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }


    }
}

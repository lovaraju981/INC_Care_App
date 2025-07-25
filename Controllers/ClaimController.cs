
using INC_Care_App.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static UglyToad.PdfPig.Core.PdfSubpath;

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


            List<string> fileSequence = new List<string>();
            fileSequence.Add("settlement letter");
            fileSequence.Add("final auth");
            fileSequence.Add("final bill");

            var claimList = new List<ClaimInfo>();
            ClaimInfo claim = null;
            foreach (var fileSeq in fileSequence)
            {
                var file = files.FirstOrDefault(x => x.FileName.Contains(fileSeq, StringComparison.OrdinalIgnoreCase));
                if (file != null)
                {
                    using var stream = file.OpenReadStream();
                    using var pdf = PdfDocument.Open(stream);
                    string pdfText = string.Join("\n", pdf.GetPages().Select(p => p.Text));

                    string sourceKey = GetSourceKey(pdfText);

                    claim = sourceKey switch
                    {
                        "HERITAGE" => ExtractFromHeritage(pdfText, claim),
                        "ICICI" => ExtractFromICICI(pdfText, claim),
                        "UNIVERSAL" => ExtractFromFHPL(pdfText, claim),
                        "UNKNOWN" => ExtractFromFHPL(pdfText, claim),
                        _ => new ClaimInfo()
                    };

                    claim.SourceKey = sourceKey;
                }

            }
            claimList.Add(claim);

            return View("ResultView", claimList);
        }

        private string GetSourceKey(string pdfText)
        {
            return pdfText.Contains("HERITAGE", StringComparison.OrdinalIgnoreCase) ? "HERITAGE"
                 : pdfText.Contains("ICICI", StringComparison.OrdinalIgnoreCase) ? "ICICI"
                 : pdfText.Contains("FHPL", StringComparison.OrdinalIgnoreCase) ? "FHPL"
                 : pdfText.Contains("UNIVERSAL", StringComparison.OrdinalIgnoreCase) ? "UNIVERSAL"
                 : "UNKNOWN";
        }

        private ClaimInfo ExtractFromHeritage(string pdfText, ClaimInfo output)
        {
            if (output == null)
                output = new ClaimInfo();

            string sourceName = ExtractValue(pdfText, @"TPA/Corporate\(1\)\s*:\s*([A-Z\s]+)", RegexOptions.IgnoreCase);

            string patient = ExtractValue(pdfText, @"Patient Name\s*:\s*(.*?)\s*\(");

            string icardLine = ExtractValue(pdfText, @"I-Card No\.?\s*:\s*(.*?)\n", RegexOptions.IgnoreCase);
            string icard = icardLine?.Split("Relation")[0].Trim();

            string policyNo = ExtractValue(pdfText, @"Policy No\.?\s*:\s*(\d{10,})");
            string hospital = ExtractValue(pdfText, @"Hospital Name\s*:\s*(.*?)\s*(\n|DOA|DOD)", RegexOptions.Singleline);

            string doa = ExtractValue(pdfText, @"DOA\s*:\s*(\d{2}/\d{2}/\d{4})");

            string amountClaimedRaw = ExtractValue(pdfText, @"Amount Claimed\s*:\s*\n?\s*([\d,\.]+)", RegexOptions.Singleline);

            string amountSettledRaw = ExtractValue(pdfText, @"Amount Settled\s*:\s*\n?\s*([\d,\.]+)", RegexOptions.Singleline);

            string authAmountWords = ExtractValue(
              pdfText, @"Total Authorized amount:-\s*Rs\.\s*([A-Za-z\s]+)\s*Only", RegexOptions.IgnoreCase);

            var extractor = new AmountExtractor();
            long? authNum = extractor.ConvertWordsToNumber(authAmountWords);
            string authSource = authNum?.ToString() ?? authAmountWords;

            decimal.TryParse(authSource?.Replace(",", ""), out var authorized);
            decimal.TryParse(amountClaimedRaw?.Replace(",", ""), out var claimed);
            decimal.TryParse(amountSettledRaw?.Replace(",", ""), out var settled);

            string finalBill = ExtractValue(pdfText, @"Total Discount\s+[\d,]+\.\d{2}\s+([\d,]+\.\d{2})");

            decimal.TryParse(finalBill?.Replace(",", ""), out var finalAmount);

            string deductionsRaw = ExtractValue(pdfText, @"Details of deductions\s*:\s*(.*?)(?=Sincerely yours|TEAM)", RegexOptions.Singleline) ?? ExtractValue(pdfText, @"Disallowance Reason\s*:?\s*(.*?)\n", RegexOptions.Singleline | RegexOptions.IgnoreCase)
        ;
            string deductions = deductionsRaw?
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
                .Trim();

            output.Source = string.IsNullOrWhiteSpace(output.Source) ? sourceName : output.Source;
            output.Patient = string.IsNullOrWhiteSpace(output.Patient) ? patient : output.Patient;
            output.ICardNumber = string.IsNullOrWhiteSpace(output.ICardNumber) ? icard : output.ICardNumber;
            output.PolicyNumber = string.IsNullOrWhiteSpace(output.PolicyNumber) ? policyNo : output.PolicyNumber;
            output.DOA = string.IsNullOrWhiteSpace(output.DOA) ? doa : output.DOA;
            //  output.AmountClaimed = string.IsNullOrWhiteSpace(output.AmountClaimed) ? claimed.ToString("N2") : output.AmountClaimed;
            output.FinalAmount = string.IsNullOrWhiteSpace(output.FinalAmount) || output.FinalAmount == "0.00" ? finalAmount.ToString("N2") : output.FinalAmount;
            output.AmountAuthorized = string.IsNullOrWhiteSpace(output.AmountAuthorized) || output.AmountAuthorized == "0.00" ? authorized.ToString("N2") : output.AmountAuthorized;
            output.AmountSettled = string.IsNullOrWhiteSpace(output.AmountSettled) ? settled.ToString("N2") : output.AmountSettled;
            decimal.TryParse(output.AmountAuthorized, out var authAmount);
            decimal.TryParse(output.AmountSettled, out var settledAmount);
            var difference = authAmount - settledAmount;
            output.Difference = difference.ToString();
            output.Deductions = string.IsNullOrWhiteSpace(output.Deductions) ? deductions : output.Deductions;

            return output;

        }

        private ClaimInfo ExtractFromICICI(string text, ClaimInfo output)
        {
            if (output == null)
                output = new ClaimInfo();

            string sourceName = ExtractValue(text, @"TPA/Corporate\(\d+\)\s*:\s*(.+?)(?:\r?\n|Bed\s|Ward\s|$)", RegexOptions.IgnoreCase);

            string patient = ExtractValue(text, @"Name of the Patient\s*:\s*([A-Z\s]+?)(?=UHID)");

            string claimId = ExtractValue(text, @"Claim Number:\s*(\d+)");

            string policyNo = ExtractValue(text, @"Policy No\s*:\s*([A-Z0-9/]+)");

            string uhidNo = ExtractValue(text, @"UHID Number:\s*(\d+)");

            string doa = ExtractValue(text, @"Date of Admission\s*:\s*(\d{2}-[A-Z]{3}-\d{4})");

            string authAmountWords = ExtractValue(text, @"\(in words\)\s*Rupees\s+([A-Z\s]+?)\s+only", RegexOptions.IgnoreCase);

            var extractor = new AmountExtractor();
            long? authNum = extractor.ConvertWordsToNumber(authAmountWords);
            string authSource = authNum?.ToString() ?? authAmountWords;

            decimal.TryParse(authSource?.Replace(",", ""), out var authorized);

            string finalBill = ExtractValue(text, @"Total Discount\s+[\d,]+\.\d{2}\s+([\d,]+\.\d{2})");

            decimal.TryParse(finalBill?.Replace(",", ""), out var finalAmount);

            string settledAmountRaw = ExtractValue(text, @"settled for\s+Rs\.?\s*([\d,]+)", RegexOptions.IgnoreCase);

            decimal.TryParse(settledAmountRaw?.Replace(",", ""), out var settled);

            string deductionsRaw = ExtractValue(text, @"Reason for Deductions?\s*[:\-]?\s*((?:.(?! {2}))+)",RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (!string.IsNullOrEmpty(deductionsRaw))
            {
                // Remove Rs. and any numbers
                deductionsRaw = Regex.Replace(deductionsRaw, @"Rs\.?\s*\d+|\d+", "", RegexOptions.IgnoreCase).Trim();

                // Start from first alphabetic sequence like "Deducted"
                var alphaStart = Regex.Match(deductionsRaw, @"[A-Za-z].*", RegexOptions.Singleline);
                if (alphaStart.Success)
                {
                    deductionsRaw = alphaStart.Value.Trim();
                }
            }

            string deductions = deductionsRaw?
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
                .Trim();

            output.Source = string.IsNullOrWhiteSpace(output.Source) ? sourceName : output.Source;
            output.Patient = string.IsNullOrWhiteSpace(output.Patient) ? patient : output.Patient;
            output.ClaimNumber = string.IsNullOrWhiteSpace(output.ClaimNumber) ? claimId : output.ClaimNumber;
            output.PolicyNumber = string.IsNullOrWhiteSpace(output.PolicyNumber) ? policyNo : output.PolicyNumber;
            output.DOA = string.IsNullOrWhiteSpace(output.DOA) ? doa : output.DOA;
            output.FinalAmount = string.IsNullOrWhiteSpace(output.FinalAmount) || output.FinalAmount == "0.00" ? finalAmount.ToString("N2") : output.FinalAmount;
            output.AmountAuthorized = string.IsNullOrWhiteSpace(output.AmountAuthorized) || output.AmountAuthorized == "0.00" ? authorized.ToString("N2") : output.AmountAuthorized;
            output.AmountSettled = string.IsNullOrWhiteSpace(output.AmountSettled) ? settled.ToString("N2") : output.AmountSettled;
            decimal.TryParse(output.AmountAuthorized, out var authAmount);
            decimal.TryParse(output.AmountSettled, out var settledAmount);
            var difference = authAmount - settledAmount;
            output.Difference = difference.ToString();
            output.Deductions = string.IsNullOrWhiteSpace(output.Deductions) ? deductions : output.Deductions;

            return output;
        }


        private ClaimInfo ExtractFromFHPL(string text, ClaimInfo output)
        {
            if (output == null)
                output = new ClaimInfo();
            string sourceName = ExtractValue(text, @"TPA/Corporate\(\d+\)\s*:\s*(.+?)(?:\r?\n|Bed\s|Ward\s|$)", RegexOptions.IgnoreCase);

            string patient = ExtractValue(text, @"PatientName\s*:\s*([A-Z]+)(?=[A-Z][a-zA-Z]*[:*])");

            string uhidNo = ExtractValue(text, @"ClaimRegistrationNumber\s*:\s*(\d+)");

            // Main UHID No
            string mainUhid = ExtractValue(text, @"Main Uhid No\s*(NIC\d+)", RegexOptions.IgnoreCase);

            // Admission Date
            string doa = ExtractValue(text, @"DateofAdmission\s*:\s*(\d{2}-\d{2}-\d{4})");

            string authAmount = ExtractValue(text, @"authorizedfinalamountofRs\.(\d+)");

            // Billed Amount (this appears before Claimed Amount — be careful!)
            string billedAmountRaw = ExtractValue(text, @"ClaimedAmount\s*:\s*(\d+)", RegexOptions.IgnoreCase);

            // Settled Amount
            string settledAmountRaw = ExtractValue(text, @"CashlessAuthorizedAmount\s*:\s*(\d+)", RegexOptions.IgnoreCase);

            // Disallowence Reason block (ends at "claim <claimno>" or similar punctuation)
            string deductions = ExtractValue(text, @"DisallowenceReason\s*(.+?)(?=Cheque Details|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            string finalBill = ExtractValue(text, @"Total Discount\s+[\d,]+\.\d{2}\s+([\d,]+\.\d{2})");

            decimal.TryParse(finalBill?.Replace(",", ""), out var finalAmount);

            // Convert numbers
            decimal.TryParse(billedAmountRaw, out var billed);
            decimal.TryParse(settledAmountRaw, out var settled);

            output.Source = string.IsNullOrWhiteSpace(output.Source) ? sourceName : output.Source;
            output.Patient = string.IsNullOrWhiteSpace(output.Patient) ? patient : output.Patient;
            output.ClaimNumber = string.IsNullOrWhiteSpace(output.ClaimNumber) ? uhidNo : output.ClaimNumber;
            output.PolicyNumber = string.IsNullOrWhiteSpace(output.PolicyNumber) ? mainUhid : output.PolicyNumber;
            output.DOA = string.IsNullOrWhiteSpace(output.DOA) ? doa : output.DOA;
            output.FinalAmount = string.IsNullOrWhiteSpace(output.FinalAmount) || output.FinalAmount == "0.00" ? finalBill : output.FinalAmount;
            output.AmountAuthorized = string.IsNullOrWhiteSpace(output.AmountAuthorized) || output.AmountAuthorized == "0.00" ? authAmount : output.AmountAuthorized;
            output.AmountSettled = string.IsNullOrWhiteSpace(output.AmountSettled) ? settled.ToString("N2") : output.AmountSettled;
            decimal.TryParse(output.AmountAuthorized, out var authorized);
            decimal.TryParse(output.AmountSettled, out var settledAmount);
            var difference = authorized - settledAmount;
            output.Difference = difference.ToString();
            output.Deductions = string.IsNullOrWhiteSpace(output.Deductions) ? deductions : output.Deductions;

            return output;
        }

        private string ExtractValue(string input, string pattern, RegexOptions options = RegexOptions.None)
        {
            var match = Regex.Match(input, pattern, options);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        public class AmountExtractor
        {
            public long? ConvertWordsToNumber(string words)
            {
                if (string.IsNullOrWhiteSpace(words))
                    return null;

                var dict = new Dictionary<string, long>
    {
        { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 },
        { "five", 5 }, { "six", 6 }, { "seven", 7 }, { "eight", 8 },
        { "nine", 9 }, { "ten", 10 }, { "eleven", 11 }, { "twelve", 12 },
        { "thirteen", 13 }, { "fourteen", 14 }, { "fifteen", 15 },
        { "sixteen", 16 }, { "seventeen", 17 }, { "eighteen", 18 },
        { "nineteen", 19 }, { "twenty", 20 }, { "thirty", 30 },
        { "forty", 40 }, { "fifty", 50 }, { "sixty", 60 },
        { "seventy", 70 }, { "eighty", 80 }, { "ninety", 90 },
        { "hundred", 100 }, { "thousand", 1000 }, { "lakhs", 100000 },
        { "lacs", 100000 }, { "lakh", 100000 }, { "crore", 10000000 }
    };

                long total = 0;
                long current = 0;

                var tokens = words.ToLower().Split(new[] { ' ', '-', ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var token in tokens)
                {
                    if (dict.TryGetValue(token, out long value))
                    {
                        if (value == 100 || value == 1000 || value == 100000 || value == 10000000)
                        {
                            current = current == 0 ? 1 : current;
                            total += current * value;
                            current = 0;
                        }
                        else
                        {
                            current += value;
                        }
                    }
                }

                return total + current;
            }
        }
    }
}



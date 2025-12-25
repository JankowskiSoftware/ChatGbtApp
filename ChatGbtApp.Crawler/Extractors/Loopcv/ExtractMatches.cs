// namespace ChatGgtApp.Crawler.Extractors.Loopcv;
//
// public class ExtractMatches
// {
//     public (string title, string url) GetMatchUrls(string html)
//     {
//         var doc = new HtmlDocument();
//         doc.LoadHtml(html);
//
//         // Select tooltip contents that hold "original job link" text
//         var tooltipDivs = doc.DocumentNode
//                               .SelectNodes("//div[@class='v-overlaycontent' and contains(normalize-space(.), 'original job link')]")
//                           ?? new HtmlNodeCollection(null);
//
//         var results = new List<JobLink>();
//
//         foreach (var tooltip in tooltipDivs)
//         {
//             var target = tooltip.GetAttributeValue("target", null);
//             if (string.IsNullOrWhiteSpace(target))
//                 continue;
//
//             var uri = new Uri(target, UriKind.Absolute);
//             var queryParams = HttpUtility.ParseQueryString(uri.Query);
//
//             // jobTitle is URL-encoded with 20 instead of spaces
//             var rawJobTitle = queryParams["jobTitle"];
//             if (string.IsNullOrEmpty(rawJobTitle))
//                 continue;
//
//             var decodedTitle = Uri.UnescapeDataString(rawJobTitle).Replace("20", " ");
//             results.Add(new JobLink(decodedTitle, target));
//         }
//
//         return results;
//     }
//     
//     
//     public static IReadOnlyList<JobLink> ExtractJobLinks(string html)
//     {
//        
//     }
// }
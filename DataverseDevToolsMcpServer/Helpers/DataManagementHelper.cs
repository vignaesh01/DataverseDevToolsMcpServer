using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DataverseDevToolsMcpServer.Helpers
{
    public class DataManagementHelper
    {
        public async static Task<EntityCollection> RetrieveAllRecordsAsync(ServiceClient serviceClient, QueryExpression queryExpression)
        {
            queryExpression = queryExpression ?? throw new ArgumentNullException(nameof(queryExpression));
            var entityCollection = new EntityCollection();
            // Enable paging
            queryExpression.PageInfo = new PagingInfo
            {
                PageNumber = 1,
                Count = 5000 // Maximum allowed per page
            };

            bool moreRecords = true;
            string pagingCookie = null;

            while (moreRecords)
            {
                if (!string.IsNullOrEmpty(pagingCookie))
                {
                    queryExpression.PageInfo.PagingCookie = pagingCookie;
                }

                var page = await serviceClient.RetrieveMultipleAsync(queryExpression);
                entityCollection.Entities.AddRange(page.Entities);

                moreRecords = page.MoreRecords;
                if (moreRecords)
                {
                    queryExpression.PageInfo.PageNumber++;
                    pagingCookie = page.PagingCookie;
                }
            }

            return entityCollection;
        }

        public static string InjectPagingIntoFetchXml(string fetchXml, int pageNumber, int recordsPerPage, string pagingCookie)
        {
            var pagingAttributes = $"count='{recordsPerPage}' page='{pageNumber}'";
            if (!string.IsNullOrEmpty(pagingCookie))
            {
                pagingAttributes += $" paging-cookie='{System.Security.SecurityElement.Escape(pagingCookie)}'";
            }

            // Find the <fetch ...> tag and inject attributes
            var fetchTagStart = fetchXml.IndexOf("<fetch", StringComparison.OrdinalIgnoreCase);
            var fetchTagEnd = fetchXml.IndexOf('>', fetchTagStart);
            if (fetchTagStart == -1 || fetchTagEnd == -1)
                throw new ArgumentException("Invalid FetchXml format.", nameof(fetchXml));

            var beforeTag = fetchXml.Substring(0, fetchTagEnd);
            var afterTag = fetchXml.Substring(fetchTagEnd);

            // Remove existing paging attributes if present
            beforeTag = System.Text.RegularExpressions.Regex.Replace(
                beforeTag,
                @"\s(count|page|paging-cookie)='[^']*'",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var newFetchTag = beforeTag + " " + pagingAttributes + afterTag;
            return newFetchTag;
        }

        public static string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            // Load document
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            if (cookie != null)
            {
                XmlAttribute pagingAttr = doc.CreateAttribute("paging-cookie");
                pagingAttr.Value = cookie;
                attrs.Append(pagingAttr);
            }

            XmlAttribute pageAttr = doc.CreateAttribute("page");
            pageAttr.Value = System.Convert.ToString(page);
            attrs.Append(pageAttr);

            XmlAttribute countAttr = doc.CreateAttribute("count");
            countAttr.Value = System.Convert.ToString(count);
            attrs.Append(countAttr);

            StringBuilder sb = new StringBuilder(1024);
            StringWriter stringWriter = new StringWriter(sb);

            XmlTextWriter writer = new XmlTextWriter(stringWriter);
            doc.WriteTo(writer);
            writer.Close();

            return sb.ToString();
        }
    }

    
}

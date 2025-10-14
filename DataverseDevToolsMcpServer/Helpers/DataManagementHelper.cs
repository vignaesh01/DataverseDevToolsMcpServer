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
     
        public static string CreateXml(string xml, string cookie, int page, int count)
        {
            StringReader stringReader = new StringReader(xml);
            var reader = new XmlTextReader(stringReader);

            // Load document
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            XmlAttributeCollection attrs = doc.DocumentElement.Attributes;

            // Check if 'top' attribute exists - cannot use paging with top
            if (attrs["top"] != null)
            {
                // Return original XML without modification if top attribute is present
                return xml;
            }

            // Check if 'aggregate' attribute is true - cannot use paging with aggregate
            var aggregateAttr = attrs["aggregate"];
            if (aggregateAttr != null && aggregateAttr.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                // Return original XML without modification if aggregate is true
                return xml;
            }

            // Check if paging attributes already exist - don't override them
            var hasCount = attrs["count"] != null;
            var hasPage = attrs["page"] != null;
            var hasPagingCookie = attrs["paging-cookie"] != null;

            // If any paging attributes exist, return original XML without modification
            if (hasCount || hasPage || hasPagingCookie)
            {
                return xml;
            }

            // Add paging attributes only if they don't exist
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

        public static string CleanRequestUrl(string requestUrl)
        {
            //if requesURl starts with /api/data/v9.*/ or api/data/v9.*/ pattern then remove it
            if (requestUrl.StartsWith("/api/data/v9.", StringComparison.OrdinalIgnoreCase))
            {
                int index = requestUrl.IndexOf('/', 10); // Find the next '/' after /api/data/v9.
                if (index != -1)
                {
                    requestUrl = requestUrl.Substring(index);
                }
                else
                {
                    requestUrl = string.Empty; // If there's no further '/', set to empty
                }
            }
            else if (requestUrl.StartsWith("api/data/v9.", StringComparison.OrdinalIgnoreCase))
            {
                int index = requestUrl.IndexOf('/', 9); // Find the next '/' after api/data/v9.
                if (index != -1)
                {
                    requestUrl = requestUrl.Substring(index);
                }
                else
                {
                    requestUrl = string.Empty; // If there's no further '/', set to empty
                }
            }


            if (requestUrl.EndsWith("/"))
            {
                requestUrl = requestUrl.Substring(0, requestUrl.Length - 1);
            }
            return requestUrl;
        }
    }

    
}

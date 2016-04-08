using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace Walker.Office365
{
    class MailHelper
    {
        internal async Task ComposeAndSendMailAsync(string subject, string bodyContent, string recipients)
        {
            string[] splitter = { ";" };
            var splitRecipientsString = recipients.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            string recipientsJSON = null;

            int n = 0;
            foreach (string recipient in splitRecipientsString)
            {
                if (n == 0)
                    recipientsJSON += "{'EmailAddress':{'Address':'" + recipient.Trim() + "'}}";
                else
                    recipientsJSON += ", {'EmailAddress':{'Address':'" + recipient.Trim() + "'}}";
                n++;
            }
            try
            {
                HttpClient client = new HttpClient();
                var token = await AuthenticationHelper.GetTokenHelperAsync();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                string postBody = "{'Message':{"
                    + "'Body':{ "
                    + "'Content': '" + bodyContent + "',"
                    + "'ContentType':'HTML'},"
                    + "'Subject':'" + subject + "',"
                    + "'ToRecipients':[" + recipientsJSON + "]},"
                    + "'SaveToSentItems':true}";

                var emailBody = new StringContent(postBody, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(new Uri("https://graph.microsoft.com/v1.0/me/microsoft.graph.SendMail"), emailBody);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("We could not send the message: " + response.StatusCode.ToString());
            }
            catch (Exception e)
            {
                throw new Exception("We could not send the message: " + e.Message);
            }
        }
    }
}
//********************************************************* 
// 
//O365-UWP-Microsoft-Graph-Connect, https://github.com/OfficeDev/O365-UWP-Microsoft-Graph-Connect
//
//Copyright (c) Microsoft Corporation
//All rights reserved. 
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// ""Software""), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:

// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
//********************************************************* 
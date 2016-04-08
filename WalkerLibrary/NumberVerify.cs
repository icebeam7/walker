using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WalkerLibrary
{
    public static class Nexmo
    {
        public static string NexmoUrlApi = "https://api.nexmo.com";
        public static string NexmoUrlRest = "https://rest.nexmo.com";
        public static string NexmoApiKey = "";
        public static string NexmoApiSecret = "";
        public static string NexmoBrand = "Walker";
        public static string NexmoFrom = "";
    }

    public class VerifyResponse
    {
        public string request_id { get; set; }
        public string status { get; set; }
        public string error_text { get; set; }
    }

    public class VerifyRequest
    {
        public string api_key { get; set; }
        public string api_secret { get; set; }
        public string number { get; set; }
        public string brand { get; set; }
    }

    public class CheckRequest
    {
        public string api_key { get; set; }
        public string api_secret { get; set; }
        public string request_id { get; set; }
        public string code { get; set; }
    }

    public class CheckResponse
    {
        public string event_id { get; set; }
        public string status { get; set; }
        public string price { get; set; }
        public string currency { get; set; }
        public string error_text { get; set; }
    }

    public class ControlRequest
    {
        public string api_key { get; set; }
        public string api_secret { get; set; }
        public string request_id { get; set; }
        public string cmd { get; set; }
    }

    public class ControlResponse
    {
        public string status { get; set; }
        public string command { get; set; }
    }

    public class NexmoMessage
    {
        public string api_key { get; set; }
        public string api_secret { get; set; }
        public string to { get; set; }
        public string from { get; set; }
        public string text { get; set; }
    }

    public class NexmoSMSVoice
    {
        public async Task Send(string number, string text, int type)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri((type == 0) ? Nexmo.NexmoUrlApi : Nexmo.NexmoUrlRest);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                NexmoMessage message = new NexmoMessage()
                {
                    api_key = Nexmo.NexmoApiKey,
                    api_secret = Nexmo.NexmoApiSecret,
                    from = Nexmo.NexmoFrom, 
                    to = number, 
                    text = text
                };

                string json = JsonConvert.SerializeObject(message);
                string path = String.Format("{0}/json", (type == 0) ? "tts" : "sms");
                var response = await client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    
                }
                else
                {
                }
            }
            catch(Exception ex)
            {

                
            }
        }
    }

    public class NumberVerify
    {
        public async Task<VerifyResponse> Verify(string number)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(Nexmo.NexmoUrlApi);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var verify = new VerifyRequest()
                {
                    api_key = Nexmo.NexmoApiKey,
                    api_secret = Nexmo.NexmoApiSecret,
                    brand = Nexmo.NexmoBrand,
                    number = number
                };

                string json = JsonConvert.SerializeObject(verify);
                var response = await client.PostAsync("verify/json", new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<VerifyResponse>(jsonResponse);
                }
                else
                {
                    return new VerifyResponse()
                    {
                        status = response.StatusCode.ToString(),
                        error_text = response.ReasonPhrase,
                        request_id = "-1"
                    };
                }
            }
            catch (Exception ex)
            {
                return new VerifyResponse()
                {
                    status = "Exception",
                    error_text = ex.Message,
                    request_id = "-1"
                };
            }
        }

        public async Task<CheckResponse> Check(string requestID, string code)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(Nexmo.NexmoUrlApi);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var verify = new CheckRequest()
                {
                    api_key = Nexmo.NexmoApiKey,
                    api_secret = Nexmo.NexmoApiSecret,
                    request_id = requestID,
                    code = code
                };

                string json = JsonConvert.SerializeObject(verify);
                var response = await client.PostAsync("verify/check/json", new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CheckResponse>(jsonResponse);
                }
                else
                {
                    return new CheckResponse()
                    {
                        status = response.StatusCode.ToString(),
                        error_text = response.ReasonPhrase
                    };
                }
            }
            catch (Exception ex)
            {
                return new CheckResponse()
                {
                    status = "Exception",
                    error_text = ex.Message
                };
            }
        }

        public async Task<ControlResponse> Control(string requestID, string cmd = "trigger_next_event")
        {
            try
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(Nexmo.NexmoUrlApi);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var verify = new ControlRequest()
                {
                    api_key = Nexmo.NexmoApiKey,
                    api_secret = Nexmo.NexmoApiSecret,
                    request_id = requestID,
                    cmd = cmd
                };

                string json = JsonConvert.SerializeObject(verify);
                var response = await client.PostAsync("verify/control/json", new StringContent(json, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ControlResponse>(jsonResponse);
                }
                else
                {
                    return new ControlResponse()
                    {
                        status = response.StatusCode.ToString(),
                        command = response.ReasonPhrase
                    };
                }
            }
            catch (Exception ex)
            {
                return new ControlResponse()
                {
                    status = "Exception",
                    command = ex.Message
                };
            }
        }
    }
}

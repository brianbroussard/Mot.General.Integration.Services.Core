using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace MotWebApiLib
{
    //public class WebObjectBase
    //{
        [Serializable]
        internal class Token : IDisposable
        {
            [JsonProperty("access_token")]
            public string AccessToken;

            [JsonProperty("token_type")]
            public string TokenType;

            [JsonProperty("expires_in")]
            public int ExpiresIn;

            [JsonProperty("refresh_token")]
            public static string RefreshToken;

            public void StartTimer()
            {
                if (_refreshTokenTimer == null)
                {
                    _refreshTokenTimer = new System.Timers.Timer(ExpiresIn * 60)
                    {
                        Interval = ExpiresIn * 60,
                        Enabled = true
                    };

                    _refreshTokenTimer.Elapsed += RefreshCurrentTokenTimer;
                    _refreshTokenTimer.Start();
                }
                else
                {
                    RestartTimer();
                }
            }

            public void RestartTimer()
            {
                if (_refreshTokenTimer != null)
                {
                    _refreshTokenTimer.Stop();
                    _refreshTokenTimer.Interval = ExpiresIn; // Wait 10s
                    _refreshTokenTimer.Start();
                }
            }

            public void PauseTimer()
            {
                if (_refreshTokenTimer != null)
                {
                    _refreshTokenTimer.Stop();
                }
            }

            public void TerminateTimer()
            {
                if (_refreshTokenTimer != null)
                {
                    _refreshTokenTimer.Stop();
                    _refreshTokenTimer.Dispose();
                }
            }

            private System.Timers.Timer _refreshTokenTimer;

            public Token(string username, string password)
            {
                //StartTimer();
            }

            public Token()
            {
                //StartTimer();
            }

            public string BaseUri { get; set; }

            private void RefreshCurrentTokenTimer(object state, object e)
            {
                try
                {
                    this.PauseTimer();

                    Task.Run(async () =>
                    {
                        using (var client = new HttpClient())
                        {
                            client.BaseAddress = new Uri($"{BaseUri}/token");

                            using (var request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress))
                            {
                                request.Headers.Add("Accept", "application/json");
                                request.Headers.Add("Cache-Control", "no-cache");

                                request.Content = new StringContent(
                                    $"grant_type=refresh_token&refresh_token={RefreshToken}",
                                    Encoding.UTF8,
                                    "application/x-www-form-urlencoded");


                                using (var response = client.SendAsync(request).Result)
                                {
                                    response.EnsureSuccessStatusCode();
                                    var content = await response.Content.ReadAsStringAsync();
                                    this.RestartTimer();
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to refresh token {ex}");
                    throw;
                }
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    TerminateTimer();
                }
            }

            public void Dispose()
            {
                Dispose(true);
                AccessToken = null;
                RefreshToken = null;
                GC.SuppressFinalize(this);
            }

            ~Token()
            {
                Dispose();
            }
        }

        [Serializable]
        public class ServerContext : IDisposable
        {
            private Token _contextToken;
            private bool _loggedIn;

            public string BaseUri;

            public string ApiRoot { get; set; }
        
            public string AccessToken
            {
                get { return _contextToken.AccessToken; }
            }

            public string TokenType
            {
                get { return _contextToken.TokenType; }
            }

            public ServerContext(string uri, string apiRoot, string username, string password)
            {
                try
                {
                    ApiRoot = apiRoot;
                    BaseUri = uri;
                    Login(username, password);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Connect to server failed! : {ex.Message}");
                }
            }

            private void Login(string user, string password)
            {
                if (user == null || password == null)
                {
                    throw new ArgumentNullException($@"connect: user or password is null");
                }

                try
                {
                    if (_loggedIn)
                    {
                        return;
                    }

                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri($"{BaseUri}/token");

                        using (var request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress))
                        {
                            //request.Headers.Add("Accept", "application/json");
                            request.Content = new StringContent(
                                $"grant_type=password&username={user}&password={password}",
                                Encoding.UTF8,
                                "application/x-www-form-urlencoded");

                            using (var response = client.SendAsync(request).Result)
                            {
                                response.EnsureSuccessStatusCode();

                                var content = response.Content.ReadAsStringAsync().Result;

                                _contextToken = JsonConvert.DeserializeObject<Token>(content);
                                _contextToken.BaseUri = BaseUri;
                                _loggedIn = true;

                                // Kick the token refresher onto another thread, we shouldn't need it
                                var watcher = new Thread(() => _contextToken.StartTimer())
                                {
                                    Name = "refreshHandler"
                                };
                                watcher.Start();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public void Logout()
            {
                _loggedIn = false;
                _contextToken.Dispose();
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _contextToken.Dispose();
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        [Serializable]
        public class MotJsonObjectBase : HttpClient
        {
            //private HttpClient client;
            private static string _baseUri;
            private static string _apiRoot;
            private Type _typeParameterType;
            
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _context.Dispose();
                    base.Dispose();
                }
            }

            public static string BaseUri
            {
                get => _baseUri;

                set
                {
                    try
                    {
                        if (value == null)
                        {
                            throw new ArgumentNullException(nameof(value));
                        }

                        if (value.Last().ToString() == "/")
                        {
                            _baseUri = value.Substring(0, value.Length - 1);
                        }
                        else
                        {
                            _baseUri = value;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }

            private readonly ServerContext _context;

            protected MotJsonObjectBase(ServerContext context)
            {
                _context = context ?? throw new ArgumentNullException($"Invalid Context");
                BaseUri = context.BaseUri;
                _apiRoot = context.ApiRoot;
            }
    
            private static string NormalizeUri(string route, string id = null)
            {
                if (route.Substring(0, 1) == "/")
                {
                    route = route.Substring(1);
                }

                if (id != null)
                {
                    if (!route.Contains(_apiRoot))
                    {
                        return Uri.EscapeUriString($"{BaseUri}/{_apiRoot}/{route}/{{" + id + "}");
                    }
                    else
                    {
                        return Uri.EscapeUriString($"{BaseUri}/{route}/{{" + id + "}");
                    }
                }
                else
                {
                    if (!route.Contains(_apiRoot))
                    {
                        return Uri.EscapeUriString($"{BaseUri}/{_apiRoot}/{route}");
                    }
                    else
                    {
                        return Uri.EscapeUriString($"{BaseUri}/{route}");
                    }
                }
            }

            // CRUDL
            public async Task<T> Post<T>(T data, string route)
            {
                if (data == null || route == null)
                {
                    throw new ArgumentNullException($@"post: data or route is null");
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(NormalizeUri(route))))
                        {
                            request.Headers.Add("Authorization", $"{_context.TokenType} {_context.AccessToken}");
                            request.Headers.Add("Accept", "application/json");
                            request.Content = new StringContent(JsonConvert.SerializeObject(data, Converter.Settings));

                            using (var response = client.SendAsync(request).Result)
                            {
                                response.EnsureSuccessStatusCode();
                                var content = response.Content.ReadAsStringAsync().Result;
                                return (T)FromJson<T>(content);
                            }
                        }
                    }
                }
                catch (HttpRequestException hex)
                {
                    Console.WriteLine(hex.Message);
                    return default(T);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public async Task<T> Put<T>(T data, string route, string id)
            {
                try
                {
                    if (data == null || route == null)
                    {
                        throw new ArgumentNullException($@"put: data or route is null");
                    }

                    using (var client = new HttpClient())
                    {
                        using (var request =
                            new HttpRequestMessage(HttpMethod.Put, new Uri(NormalizeUri(route + $"/{id}"))))
                        {
                            request.Headers.Add("Authorization", $"{_context.TokenType} {_context.AccessToken}");
                            request.Headers.Add("Accept", "application/json");
                            request.Content = new StringContent(JsonConvert.SerializeObject(data));

                            using (var response = client.SendAsync(request).Result)
                            {
                                response.EnsureSuccessStatusCode();
                                var content = response.Content.ReadAsStringAsync().Result;
                                return (T)FromJson<T>(content);
                            }
                        }
                    }
                }
                catch (HttpRequestException hex)
                {
                    Console.WriteLine(hex.Message);
                    return default(T);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public async Task<T> Get<T>(string route, string id)
            {
                if (route == null)
                {
                    throw new ArgumentNullException($@"get: route is null");
                }

                _typeParameterType = typeof(T);
                
                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Get, id == null ? new Uri(NormalizeUri(route)) : new Uri(NormalizeUri(route + $"{id}"))))
                        {
                            request.Headers.Add("Authorization", $"{_context.TokenType} {_context.AccessToken}");
                            request.Headers.Add("Accept", "application/json");
                   
                            using (var response = client.SendAsync(request).Result)
                            {
                                response.EnsureSuccessStatusCode();
                                var content = response.Content.ReadAsStringAsync().Result;
                                
                               // Debug 
                               if (_typeParameterType.Name == "String")
                               {
                                    var conv = TypeDescriptor.GetConverter(typeof(T));
                                    return (T) conv.ConvertFromString(content);
                               }
                                
                                var returnValue = FromJson<T>(content);
                                return returnValue;
                            }
                        }
                    }
                }
                catch (HttpRequestException hex)
                {
                    Console.WriteLine(hex.Message);
                    return default(T);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public async Task<T> Delete<T>(string route, string id)
            {
                if (route == null)
                {
                    throw new ArgumentNullException($@"get: route is null");
                }

                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Delete, id == null ? new Uri(NormalizeUri(route)) : new Uri(NormalizeUri(route + $"/{id}"))))
                        {
                            request.Headers.Add("Authorization", $"{_context.TokenType} {_context.AccessToken}");
                            request.Headers.Add("Accept", "application/json");

                            using (var response = client.SendAsync(request).Result)
                            {
                                response.EnsureSuccessStatusCode();
                                var content = response.Content.ReadAsStringAsync().Result;
                                var list = FromJson<T>(content);
                                return list;
                            }
                        }
                    }
                }
                catch (HttpRequestException hex)
                {
                    Console.WriteLine(hex.Message);
                    return default(T);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public static class Converter
            {
                public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                    DateParseHandling = DateParseHandling.DateTime,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            private static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, Converter.Settings);

            public static class Serialize
            {
                public static string ToJson<T>(T self) => JsonConvert.SerializeObject(self, Converter.Settings);
            }
        }
    //}
}

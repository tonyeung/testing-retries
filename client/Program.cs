using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System.Net;

namespace client
{
    class Program
    {
        private static CookieContainer CookieContainer;
        static void Main(string[] args)
        {
            CookieContainer = new CookieContainer();
            
            using (var handler = new HttpClientHandler() { CookieContainer = CookieContainer })
            using (var client = new HttpClient(new RetryDelegatingHandler(handler)))
            {
                //var myResult = client.PostAsync(yourUri, yourHttpContent).Result;
            }

        }
    }

    public static class CustomRetryPolicyFactory
    {
        public static RetryPolicy MakeHttpRetryPolicy()
        {
            var strategy = new HttpTransientErrorDetectionStrategy();
            return Exponential(strategy);
        }

        private static RetryPolicy Exponential(ITransientErrorDetectionStrategy strategy)
        {
            var retryCount = 3;
            var minBackoff = TimeSpan.FromSeconds(1);
            var maxBackoff = TimeSpan.FromSeconds(10);
            var deltaBackoff = TimeSpan.FromSeconds(5);

            var exponentialBackoff = new ExponentialBackoff(retryCount, minBackoff, maxBackoff, deltaBackoff);

            return new RetryPolicy(strategy, exponentialBackoff);
        }
    }

    public class HttpTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            if (ex != null)
            {
                HttpRequestExceptionWithStatus httpException;

                if ((httpException = ex as HttpRequestExceptionWithStatus) != null)
                {
                    if (httpException.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }
    }

    public class RetryDelegatingHandler : DelegatingHandler
    {
        public RetryPolicy RetryPolicy { get; set; }

        public RetryDelegatingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            RetryPolicy = CustomRetryPolicyFactory.MakeHttpRetryPolicy();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage responseMessage = null;
            var currentRetryCount = 0;

            RetryPolicy.Retrying += (sender, args) =>
            {
                currentRetryCount = args.CurrentRetryCount;
            };

            try
            {
                await RetryPolicy.ExecuteAsync(async () =>
                {
                    responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if ((int)responseMessage.StatusCode > 500)
                    {
                        throw new HttpRequestExceptionWithStatus(string.Format("Response status code {0} indicates server error", (int)responseMessage.StatusCode))
                        {
                            StatusCode = responseMessage.StatusCode,
                            CurrentRetryCount = currentRetryCount
                        };
                    }

                    return responseMessage;

                }, cancellationToken).ConfigureAwait(false);

                return responseMessage;
            }
            catch (HttpRequestExceptionWithStatus exception)
            {
                if (exception.CurrentRetryCount >= 3)
                {
                    //write to log
                }

                if (responseMessage != null)
                {
                    return responseMessage;
                }

                throw;
            }
            catch (Exception)
            {
                if (responseMessage != null)
                {
                    return responseMessage;
                }

                throw;
            }
        }
    }

    public class HttpRequestExceptionWithStatus : HttpRequestException
    {
        public HttpRequestExceptionWithStatus() : base() { }

        public HttpRequestExceptionWithStatus(string message) : base(message) { }

        public HttpRequestExceptionWithStatus(string message, Exception inner) : base(message, inner) { }

        public HttpStatusCode StatusCode { get; set; }

        public int CurrentRetryCount { get; set; }
    }
}

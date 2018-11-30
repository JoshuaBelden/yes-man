using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;

namespace YesMan.Controllers
{
    class Template
    {
        public readonly string Id;
        public readonly string PathMatchExpression;
        public readonly string MethodMatchExpression;
        public readonly string HeaderNameMatchExpression;
        public readonly string HeaderValueMatchExpression;
        public readonly string BodyMatchExpression;
        public readonly string ResponseBody;
        public readonly string ErrorMessage;
        public readonly HttpStatusCode ResponseStatusCode;

        public Template(
            string id,
            string pathMatchExpression = null,
            string methodMatchExpression = null,
            string headerNameMatchExpression = null,
            string headerValueMatchExpression = null,
            string bodyMatchExpression = null,
            string responseBody = "",
            string errorMessage = "",
            HttpStatusCode responseStatusCode = HttpStatusCode.OK)
        {
            Id = id;
            PathMatchExpression = pathMatchExpression;
            MethodMatchExpression = methodMatchExpression;
            HeaderNameMatchExpression = headerNameMatchExpression;
            HeaderValueMatchExpression = headerValueMatchExpression;
            BodyMatchExpression = bodyMatchExpression;
            ResponseBody = responseBody;
            ErrorMessage = errorMessage;
            ResponseStatusCode = responseStatusCode;
        }

        public static Template DefaultOk() =>
            new Template("default-ok");

        public bool IsMatch(HttpRequest request, string requestBody)
        {
            return
                IsPathMatch() &&
                IsMethodMatch() &&
                IsHeaderNameMatch() &&
                IsHeaderValueMatch() &&
                IsBodyMatch();

            bool IsPathMatch() => PathMatchExpression == null || Regex.IsMatch(request.Path, PathMatchExpression, RegexOptions.IgnoreCase);
            bool IsMethodMatch() => MethodMatchExpression == null || Regex.IsMatch(request.Method, MethodMatchExpression, RegexOptions.IgnoreCase);
            bool IsHeaderNameMatch() => HeaderNameMatchExpression == null || request.Headers.Any(head => Regex.IsMatch(head.Key, HeaderNameMatchExpression, RegexOptions.IgnoreCase));
            bool IsHeaderValueMatch() => HeaderValueMatchExpression == null || request.Headers.Any(head => Regex.IsMatch(head.Value, HeaderValueMatchExpression, RegexOptions.IgnoreCase));
            bool IsBodyMatch() => BodyMatchExpression == null || Regex.IsMatch(requestBody, BodyMatchExpression, RegexOptions.IgnoreCase);
        }
    }
}
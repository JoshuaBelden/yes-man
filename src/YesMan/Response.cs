using System.Net;

namespace YesMan.Controllers
{
    class Response
    {
        public readonly string TemplateId;
        public readonly string ResponseBody;
        public readonly string ErrorMessage;
        public readonly HttpStatusCode ResponseStatusCode;

        public Response(
            string templateId,
            string responseBody,
            string errorMessage,
            HttpStatusCode responseStatusCode)
        {
            TemplateId = templateId;
            ResponseBody = responseBody;
            ErrorMessage = errorMessage;
            ResponseStatusCode = responseStatusCode;
        }
    }
}
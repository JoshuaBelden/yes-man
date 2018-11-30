using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YesMan.Controllers
{
    public class ResponseController : ControllerBase
    {
        readonly IEnumerable<Template> Templates;
        readonly string LogDirectory;

        public ResponseController()
        {
            var templateFileName = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\response-templates.json";
            if (!System.IO.File.Exists(templateFileName))
                throw new InvalidOperationException($"The mock api controller requires a template file. Expected to find {templateFileName}");

            try
            {
                Templates = JsonConvert.DeserializeObject<IEnumerable<Template>>(System.IO.File.ReadAllText(templateFileName));
            }
            catch (Exception exception)
            {
                throw new Exception("Exception parsing mock-templates.json.", exception);
            }

            try
            {
                LogDirectory = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\request-logs\\";
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);
            }
            catch (Exception exception)
            {
                throw new Exception("Exception setting up request log directory.", exception);
            }
        }

        [Route("{*url}")] // This is a catch all route.
        public ActionResult Respond()
        {
            string content = null;
            
            using (var reader = new StreamReader(Request.Body))
                content = reader.ReadToEnd();

            var response = BuildResponse(MatchTemplate(Request, content), Request, content);

            var correlationId = Guid.NewGuid();
            LogRequest(correlationId, Request, content);
            LogResponse(correlationId, response);

            return BuildResult(response);
        }

        Template MatchTemplate(HttpRequest request, string content) =>
            Templates.FirstOrDefault(t => t.IsMatch(request, content)) ?? Template.DefaultOk();

        Response BuildResponse(Template template, HttpRequest request, string content)
        {
            var document = JObject.Parse(!string.IsNullOrWhiteSpace(content) ? content : "{}");

            return new Response(
                templateId: template.Id,
                responseBody: TemplateValue(template.ResponseBody, request, document),
                errorMessage: TemplateValue(template.ErrorMessage, request),
                responseStatusCode: template.ResponseStatusCode);
        }

        string TemplateValue(string value, HttpRequest request, JObject document = null)
        {
            var result = value?.Replace("{Request.Method}", request.Method) ?? string.Empty;

            foreach (var header in request.Headers)
                result = result?.Replace($"{{Request.Header.{header.Key}}}", header.Value) ?? string.Empty;

            if (document != null)
                foreach (var prop in document.Properties())
                    result = result.Replace($"{{Request.Body.{prop.Name}}}", (string)document[prop.Name]);

            return result;
        }

        ActionResult BuildResult(Response response)
        {
            switch (response.ResponseStatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return BadRequest(response.ErrorMessage);

                case HttpStatusCode.NotFound:
                    return NotFound();

                case HttpStatusCode.OK:
                    return Ok(response.ResponseBody != null 
                        ? JsonConvert.DeserializeObject(response.ResponseBody)
                        : string.Empty);

                default:
                    return StatusCode((int)response.ResponseStatusCode, response.ErrorMessage);
            }
        }

        void LogRequest(Guid correlationId, HttpRequest request, string content) =>
            WriteLog(
                filename: BuildFilename("request", correlationId),
                contents: JsonConvert.SerializeObject(
                    value: new
                    {
                        path = request.Path,
                        queryString = request.QueryString,
                        method = request.Method,
                        contentType = request.ContentType,
                        headers = request.Headers,
                        cookies = request.Cookies,
                        body = content
                    },
                    formatting: Formatting.Indented));

        void LogResponse(Guid correlationId, Response response) =>
            WriteLog(
                filename: BuildFilename("response", correlationId),
                contents: JsonConvert.SerializeObject(
                    value: response,
                    formatting: Formatting.Indented));

        void WriteLog(string filename, string contents) =>
            System.IO.File.WriteAllText(Path.Combine(LogDirectory, filename), contents);

        string BuildFilename(string type, Guid correlationId) =>
            $"{correlationId.ToString().Substring(0, 8)}-{type}-{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.json";
    }
}

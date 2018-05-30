using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CustomActions.Helpers;
using Microsoft.Deployment.WindowsInstaller;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomActions
{
	enum HTTPMethod
	{
		GET,
		PUT,
		POST,
		DELETE
	}

	enum HTTPContentType
	{
		json,
		app
	}

	public class CustomActions
	{
		private static X509Certificate2 SENSE_CERT = GetCertificate();

		private static Tuple<HttpStatusCode, string> MakeQrsRequest(string path, HTTPMethod method, HTTPContentType contentType = HTTPContentType.json, byte[] body = null)
		{
			// Fix Path
			if (!path.StartsWith("/"))
			{
				path = '/' + path;
			}
			if (path.EndsWith("/"))
			{
				path = path.Substring(0, path.Length - 1);
			}
			int indexOfSlash = path.LastIndexOf('/');
			int indexOfQuery = path.LastIndexOf('?');
			if (indexOfQuery <= indexOfSlash)
			{
				path += "?";
			}
			else
			{
				path += "&";
			}

			string responseString = "";
			HttpStatusCode responseCode = 0;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

			//Create the HTTP Request and add required headers and content in xrfkey
			string xrfkey = "0123456789abcdef";
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"https://localhost:4242/qrs" + path + "xrfkey=" + xrfkey);
			request.Method = method.ToString();
			request.Accept = "application/json";
			request.Headers.Add("X-Qlik-xrfkey", xrfkey);
			request.Headers.Add("X-Qlik-User", @"UserDirectory=internal;UserId=sa_api");
			// Add the certificate to the request and provide the user to execute as
			request.ClientCertificates.Add(SENSE_CERT);

			if (method == HTTPMethod.POST || method == HTTPMethod.PUT)
			{
				// Set Headers
				if (contentType == HTTPContentType.json)
				{
					request.ContentType = "application/json";

				}
				else if (contentType == HTTPContentType.app)
				{
					request.ContentType = "application/vnd.qlik.sense.app";
				}
				else
				{
					throw new ArgumentException("Content type '" + contentType.ToString() + "' is not supported.");
				}

				// Set Body
				if (body == null)
				{
					request.ContentLength = 0;
				}
				else
				{
					request.ContentLength = body.Length;
					Stream requestStream = request.GetRequestStream();
					requestStream.Write(body, 0, body.Length);
					requestStream.Close();
				}
			}

			// make the web request and return the content
			try
			{
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					responseCode = response.StatusCode;
					using (Stream stream = response.GetResponseStream())
					{
						using (StreamReader reader = new StreamReader(stream))
						{
							// if response is invalid, throw exception and expect it to be handled in calling function (4xx errors)
							responseString = JsonConvert.SerializeObject(reader.ReadToEnd());
						}
					}
				}
			}
			catch (WebException webEx)
			{
				return new Tuple<HttpStatusCode, string>(HttpStatusCode.ServiceUnavailable, webEx.Message);
			}
			catch (Exception e)
			{
				return new Tuple<HttpStatusCode, string>(HttpStatusCode.InternalServerError, e.Message);

			}

			return new Tuple<HttpStatusCode, string>(responseCode, responseString);
		}


		[CustomAction]
		public static ActionResult IsRepositoryRunning(Session session)
		{
			Tuple<HttpStatusCode, string> response = MakeQrsRequest("/about", HTTPMethod.GET);
			if (response.Item1 == HttpStatusCode.OK)
			{
				return ActionResult.Success;
			}
			else
			{
				session.Message(InstallMessage.Error, new Record() { FormatString = "Cannot install the Telemetry Dashboard as the installer could not contact the 'Qlik Repository Service'." });
				return ActionResult.Failure;
			}
		}

		[CustomAction]
		public static ActionResult ImportApp(Session session)
		{
			Tuple<HttpStatusCode, string> hasAppResponse = MakeQrsRequest("/app/count?filter=name eq 'Telemetry Dashboard'", HTTPMethod.GET);
			if (hasAppResponse.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasAppResponse.Item2))["value"].ToObject<int>() == 1)
			{
				return ActionResult.NotExecuted;
			}

			// import app
			Tuple<HttpStatusCode, string> importAppResponse = MakeQrsRequest("/app/upload?name=Telemetry Dashboard", HTTPMethod.POST, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult ImportExtensions(Session session)
		{
			// Extension 1
			Tuple<HttpStatusCode, string> hasExtension1 = MakeQrsRequest("/extension/count?filter=name eq 'cl-container'", HTTPMethod.GET);
			if (hasExtension1.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasExtension1.Item2))["value"].ToObject<int>() == 0)
			{
				Tuple<HttpStatusCode, string> importExtensionResponse = MakeQrsRequest("/extension/upload", HTTPMethod.POST, HTTPContentType.json, Properties.Resources.cl_container);
				if (importExtensionResponse.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			// Extension 2
			Tuple<HttpStatusCode, string> hasExtension2 = MakeQrsRequest("/extension/count?filter=name eq 'cl-kpi'", HTTPMethod.GET);
			if (hasExtension2.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasExtension2.Item2))["value"].ToObject<int>() == 0)
			{
				Tuple<HttpStatusCode, string> importExtensionResponse = MakeQrsRequest("/extension/upload", HTTPMethod.POST, HTTPContentType.json, Properties.Resources.cl_kpi);
				if (importExtensionResponse.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			// Extension 3
			Tuple<HttpStatusCode, string> hasExtension3 = MakeQrsRequest("/extension/count?filter=name eq 'qsSimpleKPI'", HTTPMethod.GET);
			if (hasExtension3.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasExtension3.Item2))["value"].ToObject<int>() == 0)
			{
				Tuple<HttpStatusCode, string> importExtensionResponse = MakeQrsRequest("/extension/upload", HTTPMethod.POST, HTTPContentType.json, Properties.Resources.qsSimpleKPI);
				if (importExtensionResponse.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			// Extension 4
			Tuple<HttpStatusCode, string> hasExtension4 = MakeQrsRequest("/extension/count?filter=name eq 'swr-sense-navigation'", HTTPMethod.GET);
			if (hasExtension4.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasExtension4.Item2))["value"].ToObject<int>() == 0)
			{
				Tuple<HttpStatusCode, string> importExtensionResponse = MakeQrsRequest("/extension/upload", HTTPMethod.POST, HTTPContentType.json, Properties.Resources.sense_navigation);
				if (importExtensionResponse.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult CreateTasks(Session session)
		{
			string installDir = session.CustomActionData["InstallDir"];

			// External Task
			Tuple<HttpStatusCode, string> hasExtension4 = MakeQrsRequest("/externalprogramtask/count?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HTTPMethod.GET);
			if (hasExtension4.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasExtension4.Item2))["value"].ToObject<int>() == 0)
			{
				string body = JsonConvert.SerializeObject(new
					{
						path = "..\\ServiceDispatcher\\Node\\node.exe",
						parameters = "\"" + installDir + "fetchMetadata.js\"",
						name = "TelemetryDashboard-1-Generate-Metadata",
						taskType = 1,
						enabled = true,
						taskSessionTimeout = 1440,
						maxRetries = 0,
						impactSecurityAccess = false,
						schemaPath = "ExternalProgramTask"
					});
				Tuple<HttpStatusCode, string> importExtensionResponse = MakeQrsRequest("/externalprogramtask", HTTPMethod.POST, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (importExtensionResponse.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult RemoveTasks(Session session)
		{
			return ActionResult.Success;
		}

		private static X509Certificate2 GetCertificate()
		{
			var clientPem = File.ReadAllText(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem");
			var clientKeyPem = File.ReadAllText(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem");
			byte[] certBuffer = HelperFunctions.GetBytesFromPEM(clientPem, Helpers.PemStringType.Certificate);
			byte[] certKeyBuffer = HelperFunctions.GetBytesFromPEM(clientKeyPem, Helpers.PemStringType.RsaPrivateKey);

			X509Certificate2 cert = new X509Certificate2(certBuffer);

			RSACryptoServiceProvider provider = Crypto.DecodeRsaPrivateKey(certKeyBuffer);
			cert.PrivateKey = provider;
			return cert;
		}
	}
}

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
		private static string OUTPUT_FOLDER = "TelemetryDashboard";
		private static string JS_LIBRARY_FOLDER = "MetadataGenerater";
		private static string METADATA_OUTPUT = "MetadataOutput";

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
		public static ActionResult GetInstallDir(Session session)
		{
			Tuple<HttpStatusCode, string> response = MakeQrsRequest("/servicecluster/full", HTTPMethod.GET);
			if (response.Item1 == HttpStatusCode.OK)
			{
				string installDirFolder = JArray.Parse((string)JsonConvert.DeserializeObject(response.Item2))[0]["settings"]["sharedPersistenceProperties"]["rootFolder"].ToObject<string>();
				session["INSTALLFOLDER"] = installDirFolder + "\\" + OUTPUT_FOLDER + JS_LIBRARY_FOLDER;
				session["CERTSFOLDER"] = installDirFolder + "\\" + OUTPUT_FOLDER + JS_LIBRARY_FOLDER + "\\certs";
				session["CONFIGFOLDER"] = installDirFolder + "\\" + OUTPUT_FOLDER + JS_LIBRARY_FOLDER + "\\config";
				return ActionResult.Success;
			}
			else
			{
				session.Message(InstallMessage.Error, new Record() { FormatString = "Cannot get the Qlik Sense share folder. The Telemetry Dashboard can only be installed on a shared persistence installation." });
				return ActionResult.Failure;
			}
		}

		[CustomAction]
		public static ActionResult SetOutputDir(Session session)
		{
			string installDir = session.CustomActionData["InstallDir"];

			string text = File.ReadAllText(installDir + "\\config\\config.js");
			text = text.Replace("outputFolderPlaceholder", installDir + "..\\" + METADATA_OUTPUT);
			File.WriteAllText(installDir + "\\config\\config.js", text);

			return ActionResult.Success;
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
			MakeQrsRequest("/app/upload?name=Telemetry Dashboard", HTTPMethod.POST, HTTPContentType.app, Properties.Resources.Telemetry_Dashboard);
			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult CreateTasks(Session session)
		{
			string installDir = session.CustomActionData["InstallDir"];

			string externalTaskID = "";
			// External Task
			Tuple<HttpStatusCode, string> hasExternalTask = MakeQrsRequest("/externalprogramtask/count?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HTTPMethod.GET);
			if (hasExternalTask.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}
			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasExternalTask.Item2))["value"].ToObject<int>() == 0)
			{
				installDir = installDir.Replace("\\", "\\\\");
				string body = @"
				{
					'path': '..\\ServiceDispatcher\\Node\\node.exe',
					'parameters': '""" + installDir + @"fetchMetadata.js""',
					'name': 'TelemetryDashboard-1-Generate-Metadata',
					'taskType': 1,
					'enabled': true,
					'taskSessionTimeout': 1440,
					'maxRetries': 0,
					'impactSecurityAccess': false,
					'schemaPath': 'ExternalProgramTask'
				}";
				Tuple<HttpStatusCode, string> createExternalTask = MakeQrsRequest("/externalprogramtask", HTTPMethod.POST, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createExternalTask.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
				else
				{
					externalTaskID = JObject.Parse((string)JsonConvert.DeserializeObject(createExternalTask.Item2))["id"].ToString();
				}
			}
			else
			{
				Tuple<HttpStatusCode, string> getExternalTaskId = MakeQrsRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HTTPMethod.GET);
				externalTaskID = JArray.Parse((string)JsonConvert.DeserializeObject(getExternalTaskId.Item2))[0]["id"].ToString();

			}

			// Reload Task
			Tuple<HttpStatusCode, string> hasReloadTask = MakeQrsRequest("/reloadtask/count?filter=name eq 'TelemetryDashboard-2-Reload-Dashboard'", HTTPMethod.GET);
			if (hasReloadTask.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}

			if (JObject.Parse((string)JsonConvert.DeserializeObject(hasReloadTask.Item2))["value"].ToObject<int>() == 0)
			{
				// Get AppID for Telemetry Dashboard App
				Tuple<HttpStatusCode, string> getAppID = MakeQrsRequest("/app?filter=name eq 'Telemetry Dashboard'", HTTPMethod.GET);
				string appId = JArray.Parse((string)JsonConvert.DeserializeObject(getAppID.Item2))[0]["id"].ToString();

				string body = @"
					{
						'compositeEvents': [
						{
							'compositeRules': [
							{
								'externalProgramTask': {
									'id': '" + externalTaskID + @"',
									'name': 'TelemetryDashboard-1-Generate-Metadata'
								},
								'ruleState': 1
							}
							],
							'enabled': true,
							'eventType': 1,
							'name': 'telemetry-metadata-trigger',
							'privileges': [
								'read',
								'update',
								'create',
								'delete'
							],
							'timeConstraint': {
								'days': 0,
								'hours': 0,
								'minutes': 360,
								'seconds': 0
							}
						}
						],
						'schemaEvents': [],
						'task': {
							'app': {
								'id': '" + appId + @"',
								'name': 'Telemetry Dashboard'
							},
							'customProperties': [],
							'enabled': true,
							'isManuallyTriggered': false,
							'maxRetries': 0,
							'name': 'TelemetryDashboard-2-Reload-Dashboard',
							'tags': [],
							'taskSessionTimeout': 1440,
							'taskType': 0
						}
					}";

				Tuple<HttpStatusCode, string> importExtensionResponse = MakeQrsRequest("/reloadtask/create", HTTPMethod.POST, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (importExtensionResponse.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult CopyCertificates(Session session)
		{
			string installDir = session.CustomActionData["InstallDir"];
			File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\root.pem", Path.Combine(installDir, "certs\\root.pem"), true);
			File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem", Path.Combine(installDir, "certs\\client.pem"), true);
			File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem", Path.Combine(installDir, "certs\\client_key.pem"), true);

			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult RemoveTasks(Session session)
		{
			Tuple<HttpStatusCode, string> getReloadTaskId = MakeQrsRequest("/reloadtask?filter=name eq 'TelemetryDashboard-2-Reload-Dashboard'", HTTPMethod.GET);
			if (getReloadTaskId.Item1 == HttpStatusCode.OK)
			{
				JArray reloadTasks = JArray.Parse((string)JsonConvert.DeserializeObject(getReloadTaskId.Item2));
				foreach (JToken t in reloadTasks)
				{
					MakeQrsRequest("/reloadtask/" + t["id"], HTTPMethod.DELETE);
				}
			}

			Tuple<HttpStatusCode, string> getExternalTaskId = MakeQrsRequest("/externalprogramtask?filter=name eq 'TelemetryDashboard-1-Generate-Metadata'", HTTPMethod.GET);
			if (getExternalTaskId.Item1 == HttpStatusCode.OK)
			{
				JArray externalTasks = JArray.Parse((string)JsonConvert.DeserializeObject(getExternalTaskId.Item2));
				foreach (JToken t in externalTasks)
				{
					MakeQrsRequest("/externalprogramtask/" + t["id"], HTTPMethod.DELETE);
				}
			}

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

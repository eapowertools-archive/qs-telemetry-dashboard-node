using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
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
		private static Lazy<X509Certificate2> SENSE_CERT = new Lazy<X509Certificate2>(SetTLSandGetCertificate);
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
			request.ClientCertificates.Add(SENSE_CERT.Value);

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
		public static ActionResult ValidateInstallDir(Session session)
		{
			string installDir = session["INSTALLFOLDER"];

			if (!installDir.EndsWith("\\"))
			{
				installDir += "\\";
			}

			try
			{
				if (!Regex.IsMatch(installDir.Substring(0, 3), "\\\\[a-zA-Z0-9]"))
				{
					throw new ArgumentException("Installer path must be a network locattion (start with \"\\\\\").");
				}

				if (!installDir.EndsWith("\\" + OUTPUT_FOLDER + "\\"))
				{
					throw new ArgumentException("Telemetry Dashboard but be installed to \"" + OUTPUT_FOLDER + "\" folder on share (installer directory must end with \"\\" + OUTPUT_FOLDER + "\").");
				}

				installDir = installDir.Substring(0, installDir.Length - (OUTPUT_FOLDER.Length + 1));

				string[] dirs = Directory.GetDirectories(installDir);
				for (int i = 0; i < dirs.Length; i++)
				{
					dirs[i] = dirs[i].Substring(installDir.Length);
				}

				if (!(dirs.Contains("Apps") || dirs.Contains("ArchivedLogs") || dirs.Contains("CustomData") || dirs.Contains("StaticContent")))
				{
					session.Message(InstallMessage.Warning, new Record() { FormatString = "Installer did not find an 'Apps', 'StaticContent', 'ArchivedLogs' or 'StaticContent' folder. Install will proceed but Telemetry Dashboard may not function if not installed in root Qlik Sense share folder." });
				}
			}
			catch (ArgumentException e)
			{
				session.Message(InstallMessage.Error, new Record() { FormatString = "The install directory was not valid:\n" + e.Message });
				return ActionResult.Failure;
			}
			catch (Exception e)
			{
				session.Message(InstallMessage.Error, new Record() { FormatString = "The install directory validation failed:\n" + e.Message });
				return ActionResult.Failure;
			}

			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult SetOutputDir(Session session)
		{
			string installDir = session.CustomActionData["InstallDir"];
			string outputDir = Path.Combine(installDir, METADATA_OUTPUT);

			outputDir = outputDir.Replace('\\', '/');
			if (!outputDir.EndsWith("/"))
			{
				outputDir += '/';
			}
			string text = File.ReadAllText(installDir + JS_LIBRARY_FOLDER + "\\config\\config.js");
			text = text.Replace("outputFolderPlaceholder", outputDir);
			File.WriteAllText(installDir + JS_LIBRARY_FOLDER + "\\config\\config.js", text);

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
			if (!installDir.EndsWith("\\"))
			{
				installDir += "\\";
			}

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
					'parameters': '""" + Path.Combine(installDir, JS_LIBRARY_FOLDER) + @"\\fetchMetadata.js""',
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
		public static ActionResult CreateDataConnections(Session session)
		{
			string installDir = session.CustomActionData["InstallDir"];

			// Add TelemetryMetadata dataconnection
			Tuple<HttpStatusCode, string> dataConnections = MakeQrsRequest("/dataconnection?filter=name eq 'TelemetryMetadata'", HTTPMethod.GET);
			if (dataConnections.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}
			JArray listOfDataconnections = JArray.Parse((string)JsonConvert.DeserializeObject(dataConnections.Item2));
			if (listOfDataconnections.Count == 0)
			{
				string body = @"
				{
					'name': 'TelemetryMetadata',
					'connectionstring': '" + installDir + METADATA_OUTPUT + @"\\',
					'type': 'folder',
					'username': ''
				}";


				Tuple<HttpStatusCode, string> createdConnection = MakeQrsRequest("/dataconnection", HTTPMethod.POST, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createdConnection.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}
			else
			{
				installDir = installDir.Replace("\\\\", "\\");
				listOfDataconnections[0]["connectionstring"] = installDir + METADATA_OUTPUT + "\\";
				listOfDataconnections[0]["modifiedDate"] = DateTime.UtcNow.ToString("s") + "Z";
				string appId = listOfDataconnections[0]["id"].ToString();
				Tuple<HttpStatusCode, string> updatedConnection = MakeQrsRequest("/dataconnection/" + appId, HTTPMethod.PUT, HTTPContentType.json, Encoding.UTF8.GetBytes(listOfDataconnections[0].ToString()));
				if (updatedConnection.Item1 != HttpStatusCode.Created)
				{
					return ActionResult.Failure;
				}
			}

			// Add EngineSettings dataconnection
			Tuple<HttpStatusCode, string> engineSettingDataconnection = MakeQrsRequest("/dataconnection?filter=name eq 'EngineSettingsFolder'", HTTPMethod.GET);
			if (dataConnections.Item1 != HttpStatusCode.OK)
			{
				return ActionResult.Failure;
			}
			listOfDataconnections = JArray.Parse((string)JsonConvert.DeserializeObject(engineSettingDataconnection.Item2));
			if (listOfDataconnections.Count == 0)
			{
				string body = @"
				{
					'name': 'EngineSettingsFolder',
					'connectionstring': 'C:\\ProgramData\\Qlik\\Sense\\Engine\\',
					'type': 'folder',
					'username': ''
				}";

				Tuple<HttpStatusCode, string> createdConnection = MakeQrsRequest("/dataconnection", HTTPMethod.POST, HTTPContentType.json, Encoding.UTF8.GetBytes(body));
				if (createdConnection.Item1 != HttpStatusCode.Created)
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
			File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\root.pem", Path.Combine(installDir, JS_LIBRARY_FOLDER, "certs\\root.pem"), true);
			File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client.pem", Path.Combine(installDir, JS_LIBRARY_FOLDER, "certs\\client.pem"), true);
			File.Copy(@"C:\ProgramData\Qlik\Sense\Repository\Exported Certificates\.Local Certificates\client_key.pem", Path.Combine(installDir, JS_LIBRARY_FOLDER, "certs\\client_key.pem"), true);

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

		private static X509Certificate2 SetTLSandGetCertificate()
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
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

using System.IO;
using Microsoft.Deployment.WindowsInstaller;

namespace CustomActions
{
	public class QrsCustomActions
	{
		[CustomAction]
		public static ActionResult CustomAction1(Session session)
		{
			File.Create(@"c:\installed.txt");

			return ActionResult.Success;
		}
	}
}

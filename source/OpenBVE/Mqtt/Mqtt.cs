using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace OpenBve.Mqtt
{
	public static class Mqtt
	{
		private static MqttClient client = new MqttClient("192.168.0.15");
		private static int cpt = 0;

		static Mqtt()
		{
			string clientId = Guid.NewGuid().ToString();
			client.Connect(clientId);
		}
		public static void Publish(String msg)
		{
			cpt += 1;
			if (cpt == 60)
			{
				Console.WriteLine(msg);

				client.Publish("/train/currentSpeed", Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
				cpt = 0;
				return;
			}
			else
			{
				return;
			}
		}
	}
}

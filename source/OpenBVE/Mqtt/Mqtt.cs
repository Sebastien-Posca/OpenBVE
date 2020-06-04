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
		//private static int cpt = 0;

		static Mqtt()
		{
			client.MqttMsgPublishReceived += client_recievedMessage;
			string clientId = Guid.NewGuid().ToString();
			client.Connect(clientId);
			client.Subscribe(new String[] { "/train/actuators/PowerUp" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

		}
		public static void Publish(String topic, String msg)
		{
			client.Publish(topic, Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

		}

		static void client_recievedMessage(object sender, MqttMsgPublishEventArgs e)
		{
			if (!TrainManager.PlayerTrain.Handles.SingleHandle)
			{
				int p = TrainManager.PlayerTrain.Handles.Power.Driver;
				if (p < TrainManager.PlayerTrain.Handles.Power.MaximumNotch)
				{
					TrainManager.PlayerTrain.ApplyNotch(1, true, 0, true);
				}
			}
			TrainManager.PlayerTrain.Handles.Power.ContinuousMovement = true;
		}
	}
}

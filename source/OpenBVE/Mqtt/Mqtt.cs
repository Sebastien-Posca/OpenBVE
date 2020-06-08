using OpenBveApi.Runtime;
using System;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace OpenBve.Mqtt
{
	public static class Mqtt
	{
		private static MqttClient client = new MqttClient("192.168.0.15");
		private static Boolean isSound1Playing = false;
		private static Boolean isSound2Playing = false;

		const string powerUp = "/train/actuators/powerUp";
		const string powerDown = "/train/actuators/powerDown";
		const string brakeIncrease = "/train/actuators/brakeIncrease";
		const string brakeDecrease = "/train/actuators/brakeDecrease";
		const string reverserForward = "/train/actuators/reverserForward";
		const string reverserBackward = "/train/actuators/reverserBackward";

		const string doorsLeft = "/train/actuators/doorsLeft";
		const string doorsRight = "/train/actuators/doorsRight";

		const string sound1 = "/train/actuators/sound1";
		const string sound2 = "/train/actuators/sound2";



		static Mqtt()
		{
			client.MqttMsgPublishReceived += client_recievedMessage;
			string clientId = Guid.NewGuid().ToString();
			client.Connect(clientId);
			client.Subscribe(new String[] { powerUp, powerDown, brakeIncrease, brakeDecrease, reverserForward, reverserBackward, doorsLeft, doorsRight, sound1, sound2 }, new byte[] { 0,0,0,0,0,0,0,0,0,0 });

		}
		public static void Publish(String topic, String msg)
		{
			client.Publish(topic, Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
		}

		public static void client_recievedMessage(object sender, MqttMsgPublishEventArgs e)
		{
			switch (e.Topic)
			{
				case powerUp:
					if (!TrainManager.PlayerTrain.Handles.SingleHandle)
					{
						int p = TrainManager.PlayerTrain.Handles.Power.Driver;
						if (p < TrainManager.PlayerTrain.Handles.Power.MaximumNotch)
						{
							TrainManager.PlayerTrain.ApplyNotch(1, true, 0, true);
						}
					}
					TrainManager.PlayerTrain.Handles.Power.ContinuousMovement = true;
					break;
				case powerDown:
					if (!TrainManager.PlayerTrain.Handles.SingleHandle)
					{
						int p = TrainManager.PlayerTrain.Handles.Power.Driver;
						if (p > 0)
						{
							TrainManager.PlayerTrain.ApplyNotch(-1, true, 0, true);
						}
					}
					TrainManager.PlayerTrain.Handles.Power.ContinuousMovement = true;
					break;
				case brakeIncrease:
					if (!TrainManager.PlayerTrain.Handles.SingleHandle)
					{
						if (TrainManager.PlayerTrain.Handles.Brake is TrainManager.AirBrakeHandle)
						{
							if (TrainManager.PlayerTrain.Handles.HasHoldBrake &
								TrainManager.PlayerTrain.Handles.Brake.Driver ==
								(int)TrainManager.AirBrakeHandleState.Release &
								!TrainManager.PlayerTrain.Handles.HoldBrake.Driver)
							{
								TrainManager.PlayerTrain.ApplyHoldBrake(true);
							}
							else if (TrainManager.PlayerTrain.Handles.HoldBrake.Driver)
							{
								TrainManager.PlayerTrain.ApplyAirBrakeHandle(TrainManager.AirBrakeHandleState.Lap);
								TrainManager.PlayerTrain.ApplyHoldBrake(false);
							}
							else if (TrainManager.PlayerTrain.Handles.Brake.Driver ==
									 (int)TrainManager.AirBrakeHandleState.Lap)
							{
								TrainManager.PlayerTrain.ApplyAirBrakeHandle(TrainManager.AirBrakeHandleState.Service);
							}
							else if (TrainManager.PlayerTrain.Handles.Brake.Driver ==
									 (int)TrainManager.AirBrakeHandleState.Release)
							{
								TrainManager.PlayerTrain.ApplyAirBrakeHandle(TrainManager.AirBrakeHandleState.Lap);
							}
						}
						else
						{
							int b = TrainManager.PlayerTrain.Handles.Brake.Driver;
							if (TrainManager.PlayerTrain.Handles.HasHoldBrake & b == 0 &
								!TrainManager.PlayerTrain.Handles.HoldBrake.Driver)
							{
								TrainManager.PlayerTrain.ApplyHoldBrake(true);
							}
							else if (b < TrainManager.PlayerTrain.Handles.Brake.MaximumNotch)
							{
								TrainManager.PlayerTrain.ApplyNotch(0, true, 1, true);
								TrainManager.PlayerTrain.ApplyHoldBrake(false);
							}
						}
					}
					TrainManager.PlayerTrain.Handles.Brake.ContinuousMovement = true;
					break;
				case brakeDecrease:
					if (!TrainManager.PlayerTrain.Handles.SingleHandle)
					{
						if (TrainManager.PlayerTrain.Handles.Brake is TrainManager.AirBrakeHandle)
						{
							if (TrainManager.PlayerTrain.Handles.EmergencyBrake.Driver)
							{
								TrainManager.PlayerTrain.UnapplyEmergencyBrake();
							}
							else if (TrainManager.PlayerTrain.Handles.HasHoldBrake &
									 TrainManager.PlayerTrain.Handles.Brake.Driver ==
									 (int)TrainManager.AirBrakeHandleState.Lap &
									 !TrainManager.PlayerTrain.Handles.HoldBrake.Driver)
							{
								TrainManager.PlayerTrain.ApplyHoldBrake(true);
							}
							else if (TrainManager.PlayerTrain.Handles.HoldBrake.Driver)
							{
								TrainManager.PlayerTrain.ApplyAirBrakeHandle(TrainManager.AirBrakeHandleState.Release);
								TrainManager.PlayerTrain.ApplyHoldBrake(false);
							}
							else if (TrainManager.PlayerTrain.Handles.Brake.Driver ==
									 (int)TrainManager.AirBrakeHandleState.Lap)
							{
								TrainManager.PlayerTrain.ApplyAirBrakeHandle(TrainManager.AirBrakeHandleState.Release);
							}
							else if (TrainManager.PlayerTrain.Handles.Brake.Driver ==
									 (int)TrainManager.AirBrakeHandleState.Service)
							{
								TrainManager.PlayerTrain.ApplyAirBrakeHandle(TrainManager.AirBrakeHandleState.Lap);
							}
						}
						else
						{
							int b = TrainManager.PlayerTrain.Handles.Brake.Driver;
							if (TrainManager.PlayerTrain.Handles.EmergencyBrake.Driver)
							{
								TrainManager.PlayerTrain.UnapplyEmergencyBrake();
							}
							else if (b == 1 & TrainManager.PlayerTrain.Handles.HasHoldBrake)
							{
								TrainManager.PlayerTrain.ApplyNotch(0, true, 0, false);
								TrainManager.PlayerTrain.ApplyHoldBrake(true);
							}
							else if (TrainManager.PlayerTrain.Handles.HoldBrake.Driver)
							{
								TrainManager.PlayerTrain.ApplyHoldBrake(false);
							}
							else if (b > 0)
							{
								TrainManager.PlayerTrain.ApplyNotch(0, true, -1, true);
							}
						}
					}
					TrainManager.PlayerTrain.Handles.Brake.ContinuousMovement = true;
					break;
				case reverserForward:
					if (TrainManager.PlayerTrain.Handles.Reverser.Driver < TrainManager.ReverserPosition.Forwards)
					{
						TrainManager.PlayerTrain.ApplyReverser(1, true);
					}
					break;
				case reverserBackward:
					// reverser backward
					if (TrainManager.PlayerTrain.Handles.Reverser.Driver > TrainManager.ReverserPosition.Reverse)
					{
						TrainManager.PlayerTrain.ApplyReverser(-1, true);
					}
					break;
				case doorsLeft:
					if ((TrainManager.GetDoorsState(TrainManager.PlayerTrain, true, false) &
						 TrainManager.TrainDoorState.Opened) == 0)
					{
						if (TrainManager.PlayerTrain.Specs.DoorOpenMode != TrainManager.DoorMode.Automatic)
						{
							TrainManager.OpenTrainDoors(TrainManager.PlayerTrain, true, false);
						}
					}
					else
					{
						if (TrainManager.PlayerTrain.Specs.DoorOpenMode != TrainManager.DoorMode.Automatic)
						{
							TrainManager.CloseTrainDoors(TrainManager.PlayerTrain, true, false);
						}
					}
					if (TrainManager.PlayerTrain.Plugin != null)
					{
						TrainManager.PlayerTrain.Plugin.KeyDown(VirtualKeys.LeftDoors);
					}
					TrainManager.PlayerTrain.Cars[TrainManager.PlayerTrain.DriverCar].Doors[0].ButtonPressed = true;
					break;
				case doorsRight:
					if ((TrainManager.GetDoorsState(TrainManager.PlayerTrain, false, true) &
						 TrainManager.TrainDoorState.Opened) == 0)
					{
						if (TrainManager.PlayerTrain.Specs.DoorOpenMode != TrainManager.DoorMode.Automatic)
						{
							TrainManager.OpenTrainDoors(TrainManager.PlayerTrain, false, true);
						}
					}
					else
					{
						if (TrainManager.PlayerTrain.Specs.DoorOpenMode != TrainManager.DoorMode.Automatic)
						{
							TrainManager.CloseTrainDoors(TrainManager.PlayerTrain, false, true);
						}
					}
					if (TrainManager.PlayerTrain.Plugin != null)
					{
						TrainManager.PlayerTrain.Plugin.KeyDown(VirtualKeys.RightDoors);
					}
					TrainManager.PlayerTrain.Cars[TrainManager.PlayerTrain.DriverCar].Doors[1].ButtonPressed = true;
					break;
				case sound1:
					Console.WriteLine(Encoding.UTF8.GetString(e.Message));
					if (isSound1Playing)
					{
						if (Encoding.UTF8.GetString(e.Message) == "i")
						{
							isSound1Playing = false;
							Publish("/train/infos/sound1", isSound1Playing.ToString());
							int d = TrainManager.PlayerTrain.DriverCar;
							if (TrainManager.PlayerTrain.Cars[d].Horns.Length > 2)
							{
								TrainManager.PlayerTrain.Cars[d].Horns[2].Play();
								if (TrainManager.PlayerTrain.Plugin != null)
								{
									TrainManager.PlayerTrain.Plugin.HornBlow(OpenBveApi.Runtime.HornTypes.Music);
								}
							}
							int a = TrainManager.PlayerTrain.DriverCar;
							TrainManager.PlayerTrain.Cars[a].Horns[2].Stop();
						}
					}
					else
					{
						if (Encoding.UTF8.GetString(e.Message) == "m")
						{
							isSound1Playing = true;
							Publish("/train/infos/sound1", isSound1Playing.ToString());
							int d = TrainManager.PlayerTrain.DriverCar;
							if (TrainManager.PlayerTrain.Cars[d].Horns.Length > 2)
							{
								TrainManager.PlayerTrain.Cars[d].Horns[2].Play();
								if (TrainManager.PlayerTrain.Plugin != null)
								{
									TrainManager.PlayerTrain.Plugin.HornBlow(OpenBveApi.Runtime.HornTypes.Music);
								}
							}
							int a = TrainManager.PlayerTrain.DriverCar;
							TrainManager.PlayerTrain.Cars[a].Horns[2].Stop();
						}
					}
					
					break;
				case sound2:
					if (isSound2Playing)
					{
						if (Encoding.UTF8.GetString(e.Message) == "i")
						{
							isSound2Playing = false;
							Publish("/train/infos/sound2", isSound2Playing.ToString());
							int q = TrainManager.PlayerTrain.DriverCar;
							if (TrainManager.PlayerTrain.Cars[q].Horns.Length > 1)
							{
								TrainManager.PlayerTrain.Cars[q].Horns[1].Play();
								if (TrainManager.PlayerTrain.Plugin != null)
								{
									TrainManager.PlayerTrain.Plugin.HornBlow(OpenBveApi.Runtime.HornTypes.Music);
								}
							}
							int f = TrainManager.PlayerTrain.DriverCar;
							TrainManager.PlayerTrain.Cars[f].Horns[1].Stop();
						}
							
					}
					else
					{
						if (Encoding.UTF8.GetString(e.Message) == "m")
						{
							isSound2Playing = true;
							Publish("/train/infos/sound2", isSound2Playing.ToString());
							int q = TrainManager.PlayerTrain.DriverCar;
							if (TrainManager.PlayerTrain.Cars[q].Horns.Length > 1)
							{
								TrainManager.PlayerTrain.Cars[q].Horns[1].Play();
								if (TrainManager.PlayerTrain.Plugin != null)
								{
									TrainManager.PlayerTrain.Plugin.HornBlow(OpenBveApi.Runtime.HornTypes.Music);
								}
							}
							int f = TrainManager.PlayerTrain.DriverCar;
							TrainManager.PlayerTrain.Cars[f].Horns[1].Stop();
						}
					}
					
					break;
				default:
					break;
			}
		}
			
	}
}

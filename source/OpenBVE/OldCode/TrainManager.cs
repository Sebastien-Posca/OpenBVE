using System;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using OpenBveApi.Trains;

namespace OpenBve
{
	/// <summary>The TrainManager is the root class containing functions to load and manage trains within the simulation world.</summary>
	public static partial class TrainManager
	{
		private static int cpt = 0;
		private static int cpt1 = 0;


		// trains
		/// <summary>The list of trains available in the simulation.</summary>
		internal static Train[] Trains = new Train[] { };
		/// <summary>A reference to the train of the Trains element that corresponds to the player's train.</summary>
		internal static Train PlayerTrain = null;
		/// <summary>The list of TrackFollowingObject available on other tracks in the simulation.</summary>
		internal static TrackFollowingObject[] TFOs = new TrackFollowingObject[] { };
		
		/// <summary>This method should be called once a frame to update the position, speed and state of all trains within the simulation</summary>
		/// <param name="TimeElapsed">The time elapsed since the last call to this function</param>
		internal static void UpdateTrains(double TimeElapsed)
		{
			if (PlayerTrain != null)
			{
				double nr = PlayerTrain.CurrentRouteLimit;
				double ns = PlayerTrain.CurrentSectionLimit;
				double n = nr < ns ? nr : ns;
				double mass = 0;
				Mqtt.Mqtt.Publish("train/infos/speedLimit", (Math.Abs(n) * 3.6).ToString("0.0"));
				Mqtt.Mqtt.Publish("train/sensors/currentAvgAcc", PlayerTrain.Specs.CurrentAverageAcceleration.ToString("0.0000"));
				Mqtt.Mqtt.Publish("train/sensors/airPressure", PlayerTrain.Specs.CurrentAirPressure.ToString("0.0000"));
				Mqtt.Mqtt.Publish("train/sensors/currentSpeed", (Math.Abs(PlayerTrain.CurrentSpeed) * 3.6).ToString("0.00"));
				Mqtt.Mqtt.Publish("train/sensors/power", PlayerTrain.Handles.Power.Driver.ToString());
				Mqtt.Mqtt.Publish("train/sensors/doorsLeft", GetDoorsState(PlayerTrain, true, false).ToString());
				Mqtt.Mqtt.Publish("train/sensors/doorsRight", GetDoorsState(PlayerTrain, false, true).ToString());
				Mqtt.Mqtt.Publish("train/sensors/brake", PlayerTrain.Handles.Brake.Driver.ToString());
				Mqtt.Mqtt.Publish("train/sensors/reverser", PlayerTrain.Handles.Reverser.Driver.ToString());
				for (int i = 0; i < TrainManager.PlayerTrain.Cars.Length; i++)
				{
					mass += TrainManager.PlayerTrain.Cars[i].CurrentMass;
				}
				Mqtt.Mqtt.Publish("train/sensors/positionX", (TrainManager.PlayerTrain.Cars[0].FrontAxle.Follower.WorldPosition.X).ToString());
				Mqtt.Mqtt.Publish("train/sensors/positionY", (TrainManager.PlayerTrain.Cars[0].FrontAxle.Follower.WorldPosition.Y).ToString());
				Mqtt.Mqtt.Publish("train/sensors/positionZ", (TrainManager.PlayerTrain.Cars[0].FrontAxle.Follower.WorldPosition.Z).ToString());

				Mqtt.Mqtt.Publish("train/sensors/load", mass.ToString());
				cpt++;
				if (cpt == 30)
				{
					int length = Program.CurrentRoute.Stations.Length;
					double p0 = 0;
					double p1 = 0;
					double m = 0;
					int k = 0;
					StringBuilder sb = new StringBuilder("", 256);
					for (int i = 0; i < length; i++)
					{
						n = Program.CurrentRoute.Stations[i].GetStopIndex(PlayerTrain.NumberOfCars);
						p0 = PlayerTrain.FrontCarTrackPosition();
						p1 = Program.CurrentRoute.Stations[i].Stops.Length > 0 ? Program.CurrentRoute.Stations[i].Stops[k].TrackPosition : Program.CurrentRoute.Stations[i].DefaultTrackPosition;
						m = p1 - p0;
						if (i != length - 1)
						{
							sb.Append(Program.CurrentRoute.Stations[i].Name + " : " + m.ToString("0.0") + " ; ");
						}
						else
						{
							sb.Append(Program.CurrentRoute.Stations[i].Name + " : " + m.ToString("0.0"));
						}
					}
					Mqtt.Mqtt.Publish("train/infos/distanceToNextStation", sb.ToString());
					cpt = 0;
				}

				cpt1++;
				if (cpt1 == 30)
				{
					double[] trainsPositions = new double[Trains.Length - 1];
					System.Threading.Tasks.Parallel.For(0, Trains.Length, i =>
					{
						if (Trains[i].State == TrainState.Available)
						{
							if (!Trains[i].IsPlayerTrain)
							{
								double a = Trains[i].RearCarTrackPosition();
								trainsPositions[i] = a - PlayerTrain.FrontCarTrackPosition();
							}
						}
					});
					double min = trainsPositions.Min();
					Mqtt.Mqtt.Publish("train/infos/distanceToTrain", min.ToString("0.0"));
					cpt1 = 0;

				}

			}

			for (int i = 0; i < Trains.Length; i++) {
				Trains[i].Update(TimeElapsed);
			}

			foreach (var Train in TFOs)
			{
				Train.Update(TimeElapsed);
			}

			// detect collision
			if (!Game.MinimalisticSimulation & Interface.CurrentOptions.Collisions)
			{
				
				//for (int i = 0; i < Trains.Length; i++) {
				System.Threading.Tasks.Parallel.For(0, Trains.Length, i =>
				{
					// with other trains
					if (Trains[i].State == TrainState.Available)
					{
						double a = Trains[i].FrontCarTrackPosition();
						double b = Trains[i].RearCarTrackPosition();
						for (int j = i + 1; j < Trains.Length; j++)
						{
							if (Trains[j].State == TrainState.Available)
							{
								double c = Trains[j].FrontCarTrackPosition();
								double d = Trains[j].RearCarTrackPosition();
								if (a > d & b < c)
								{
									if (a > c)
									{
										// i > j
										int k = Trains[i].Cars.Length - 1;
										if (Trains[i].Cars[k].CurrentSpeed < Trains[j].Cars[0].CurrentSpeed)
										{
											double v = Trains[j].Cars[0].CurrentSpeed - Trains[i].Cars[k].CurrentSpeed;
											double s = (Trains[i].Cars[k].CurrentSpeed*Trains[i].Cars[k].CurrentMass +
														Trains[j].Cars[0].CurrentSpeed*Trains[j].Cars[0].CurrentMass)/
													   (Trains[i].Cars[k].CurrentMass + Trains[j].Cars[0].CurrentMass);
											Trains[i].Cars[k].CurrentSpeed = s;
											Trains[j].Cars[0].CurrentSpeed = s;
											double e = 0.5*(c - b) + 0.0001;
											Trains[i].Cars[k].FrontAxle.Follower.UpdateRelative(e, false, false);
											Trains[i].Cars[k].RearAxle.Follower.UpdateRelative(e, false, false);
											Trains[j].Cars[0].FrontAxle.Follower.UpdateRelative(-e, false, false);

											Trains[j].Cars[0].RearAxle.Follower.UpdateRelative(-e, false, false);
											if (Interface.CurrentOptions.Derailments)
											{
												double f = 2.0/ (Trains[i].Cars[k].CurrentMass + Trains[j].Cars[0].CurrentMass);
												double fi = Trains[j].Cars[0].CurrentMass*f;
												double fj = Trains[i].Cars[k].CurrentMass*f;
												double vi = v*fi;
												double vj = v*fj;
												if (vi > Trains[i].CriticalCollisionSpeedDifference)
													Trains[i].Derail(k, TimeElapsed);
												if (vj > Trains[j].CriticalCollisionSpeedDifference)
													Trains[j].Derail(i, TimeElapsed);
											}
											// adjust cars for train i
											for (int h = Trains[i].Cars.Length - 2; h >= 0; h--)
											{
												a = Trains[i].Cars[h + 1].FrontAxle.Follower.TrackPosition -
													Trains[i].Cars[h + 1].FrontAxle.Position + 0.5*Trains[i].Cars[h + 1].Length;
												b = Trains[i].Cars[h].RearAxle.Follower.TrackPosition -
													Trains[i].Cars[h].RearAxle.Position - 0.5*Trains[i].Cars[h].Length;
												d = b - a - Trains[i].Cars[h].Coupler.MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[i].Cars[h].FrontAxle.Follower.UpdateRelative(-d, false, false);
													Trains[i].Cars[h].RearAxle.Follower.UpdateRelative(-d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/ (Trains[i].Cars[h + 1].CurrentMass + Trains[i].Cars[h].CurrentMass);
														double fi = Trains[i].Cars[h + 1].CurrentMass*f;
														double fj = Trains[i].Cars[h].CurrentMass*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Trains[i].CriticalCollisionSpeedDifference)
															Trains[i].Derail(h + 1, TimeElapsed);
														if (vj > Trains[j].CriticalCollisionSpeedDifference)
															Trains[i].Derail(h, TimeElapsed);
													}
													Trains[i].Cars[h].CurrentSpeed =
														Trains[i].Cars[h + 1].CurrentSpeed;
												}
											}
											// adjust cars for train j
											for (int h = 1; h < Trains[j].Cars.Length; h++)
											{
												a = Trains[j].Cars[h - 1].RearAxle.Follower.TrackPosition -
													Trains[j].Cars[h - 1].RearAxle.Position - 0.5*Trains[j].Cars[h - 1].Length;
												b = Trains[j].Cars[h].FrontAxle.Follower.TrackPosition -
													Trains[j].Cars[h].FrontAxle.Position + 0.5*Trains[j].Cars[h].Length;
												d = a - b - Trains[j].Cars[h - 1].Coupler.MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[j].Cars[h].FrontAxle.Follower.UpdateRelative(d, false, false);
													Trains[j].Cars[h].RearAxle.Follower.UpdateRelative(d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/ (Trains[j].Cars[h - 1].CurrentMass + Trains[j].Cars[h].CurrentMass);
														double fi = Trains[j].Cars[h - 1].CurrentMass*f;
														double fj = Trains[j].Cars[h].CurrentMass*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Trains[j].CriticalCollisionSpeedDifference)
															Trains[j].Derail(h -1, TimeElapsed);
														if (vj > Trains[j].CriticalCollisionSpeedDifference)
															Trains[j].Derail(h, TimeElapsed);
													}
													Trains[j].Cars[h].CurrentSpeed =
														Trains[j].Cars[h - 1].CurrentSpeed;
												}
											}
										}
									}
									else
									{
										// i < j
										int k = Trains[j].Cars.Length - 1;
										if (Trains[i].Cars[0].CurrentSpeed > Trains[j].Cars[k].CurrentSpeed)
										{
											double v = Trains[i].Cars[0].CurrentSpeed -
													   Trains[j].Cars[k].CurrentSpeed;
											double s = (Trains[i].Cars[0].CurrentSpeed*Trains[i].Cars[0].CurrentMass +
														Trains[j].Cars[k].CurrentSpeed*Trains[j].Cars[k].CurrentMass)/
													   (Trains[i].Cars[0].CurrentMass + Trains[j].Cars[k].CurrentMass);
											Trains[i].Cars[0].CurrentSpeed = s;
											Trains[j].Cars[k].CurrentSpeed = s;
											double e = 0.5*(a - d) + 0.0001;
											Trains[i].Cars[0].FrontAxle.Follower.UpdateRelative(-e, false, false);
											Trains[i].Cars[0].RearAxle.Follower.UpdateRelative(-e, false, false);
											Trains[j].Cars[k].FrontAxle.Follower.UpdateRelative(e, false, false);
											Trains[j].Cars[k].RearAxle.Follower.UpdateRelative(e, false, false);
											if (Interface.CurrentOptions.Derailments)
											{
												double f = 2.0/ (Trains[i].Cars[0].CurrentMass + Trains[j].Cars[k].CurrentMass);
												double fi = Trains[j].Cars[k].CurrentMass*f;
												double fj = Trains[i].Cars[0].CurrentMass*f;
												double vi = v*fi;
												double vj = v*fj;
												if (vi > Trains[i].CriticalCollisionSpeedDifference)
													Trains[i].Derail(0, TimeElapsed);
												if (vj > Trains[j].CriticalCollisionSpeedDifference)
													Trains[j].Derail(k, TimeElapsed);
											}
											// adjust cars for train i
											for (int h = 1; h < Trains[i].Cars.Length; h++)
											{
												a = Trains[i].Cars[h - 1].RearAxle.Follower.TrackPosition -
													Trains[i].Cars[h - 1].RearAxle.Position - 0.5*Trains[i].Cars[h - 1].Length;
												b = Trains[i].Cars[h].FrontAxle.Follower.TrackPosition -
													Trains[i].Cars[h].FrontAxle.Position + 0.5*Trains[i].Cars[h].Length;
												d = a - b - Trains[i].Cars[h - 1].Coupler.MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[i].Cars[h].FrontAxle.Follower.UpdateRelative(d, false, false);
													Trains[i].Cars[h].RearAxle.Follower.UpdateRelative(d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/ (Trains[i].Cars[h - 1].CurrentMass + Trains[i].Cars[h].CurrentMass);
														double fi = Trains[i].Cars[h - 1].CurrentMass*f;
														double fj = Trains[i].Cars[h].CurrentMass*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Trains[i].CriticalCollisionSpeedDifference)
															Trains[i].Derail(h -1, TimeElapsed);
														if (vj > Trains[i].CriticalCollisionSpeedDifference)
															Trains[i].Derail(h, TimeElapsed);
													}
													Trains[i].Cars[h].CurrentSpeed =
														Trains[i].Cars[h - 1].CurrentSpeed;
												}
											}
											// adjust cars for train j
											for (int h = Trains[j].Cars.Length - 2; h >= 0; h--)
											{
												a = Trains[j].Cars[h + 1].FrontAxle.Follower.TrackPosition -
													Trains[j].Cars[h + 1].FrontAxle.Position + 0.5*Trains[j].Cars[h + 1].Length;
												b = Trains[j].Cars[h].RearAxle.Follower.TrackPosition -
													Trains[j].Cars[h].RearAxle.Position - 0.5*Trains[j].Cars[h].Length;
												d = b - a - Trains[j].Cars[h].Coupler.MinimumDistanceBetweenCars;
												if (d < 0.0)
												{
													d -= 0.0001;
													Trains[j].Cars[h].FrontAxle.Follower.UpdateRelative(-d, false, false);
													Trains[j].Cars[h].RearAxle.Follower.UpdateRelative(-d, false, false);
													if (Interface.CurrentOptions.Derailments)
													{
														double f = 2.0/ (Trains[j].Cars[h + 1].CurrentMass + Trains[j].Cars[h].CurrentMass);
														double fi = Trains[j].Cars[h + 1].CurrentMass*f;
														double fj = Trains[j].Cars[h].CurrentMass*f;
														double vi = v*fi;
														double vj = v*fj;
														if (vi > Trains[i].CriticalCollisionSpeedDifference)
															Trains[j].Derail(h + 1, TimeElapsed);
														if (vj > Trains[j].CriticalCollisionSpeedDifference)
															Trains[j].Derail(h, TimeElapsed);
													}
													Trains[j].Cars[h].CurrentSpeed =
														Trains[j].Cars[h + 1].CurrentSpeed;
												}
											}
										}
									}
								}
							}

						}
					}
					// with buffers
					if (Trains[i].IsPlayerTrain)
					{
						double a = Trains[i].Cars[0].FrontAxle.Follower.TrackPosition - Trains[i].Cars[0].FrontAxle.Position +
								   0.5*Trains[i].Cars[0].Length;
						double b = Trains[i].Cars[Trains[i].Cars.Length - 1].RearAxle.Follower.TrackPosition -
								   Trains[i].Cars[Trains[i].Cars.Length - 1].RearAxle.Position - 0.5*Trains[i].Cars[0].Length;
						for (int j = 0; j < Program.CurrentRoute.BufferTrackPositions.Length; j++)
						{
							if (a > Program.CurrentRoute.BufferTrackPositions[j] & b < Program.CurrentRoute.BufferTrackPositions[j])
							{
								a += 0.0001;
								b -= 0.0001;
								double da = a - Program.CurrentRoute.BufferTrackPositions[j];
								double db = Program.CurrentRoute.BufferTrackPositions[j] - b;
								if (da < db)
								{
									// front
									Trains[i].Cars[0].UpdateTrackFollowers(-da, false, false);
									if (Interface.CurrentOptions.Derailments &&
										Math.Abs(Trains[i].Cars[0].CurrentSpeed) > Trains[i].CriticalCollisionSpeedDifference)
									{
										Trains[i].Derail(0, TimeElapsed);
									}
									Trains[i].Cars[0].CurrentSpeed = 0.0;
									for (int h = 1; h < Trains[i].Cars.Length; h++)
									{
										a = Trains[i].Cars[h - 1].RearAxle.Follower.TrackPosition -
											Trains[i].Cars[h - 1].RearAxle.Position - 0.5*Trains[i].Cars[h - 1].Length;
										b = Trains[i].Cars[h].FrontAxle.Follower.TrackPosition -
											Trains[i].Cars[h].FrontAxle.Position + 0.5*Trains[i].Cars[h].Length;
										double d = a - b - Trains[i].Cars[h - 1].Coupler.MinimumDistanceBetweenCars;
										if (d < 0.0)
										{
											d -= 0.0001;
											Trains[i].Cars[h].UpdateTrackFollowers(d, false, false);
											if (Interface.CurrentOptions.Derailments &&
												Math.Abs(Trains[i].Cars[h].CurrentSpeed) >
												Trains[j].CriticalCollisionSpeedDifference)
											{
												Trains[i].Derail(h, TimeElapsed);
											}
											Trains[i].Cars[h].CurrentSpeed = 0.0;
										}
									}
								}
								else
								{
									// rear
									int c = Trains[i].Cars.Length - 1;
									Trains[i].Cars[c].UpdateTrackFollowers(db, false, false);
									if (Interface.CurrentOptions.Derailments &&
										Math.Abs(Trains[i].Cars[c].CurrentSpeed) > Trains[i].CriticalCollisionSpeedDifference)
									{
										Trains[i].Derail(c, TimeElapsed);
									}
									Trains[i].Cars[c].CurrentSpeed = 0.0;
									for (int h = Trains[i].Cars.Length - 2; h >= 0; h--)
									{
										a = Trains[i].Cars[h + 1].FrontAxle.Follower.TrackPosition -
											Trains[i].Cars[h + 1].FrontAxle.Position + 0.5*Trains[i].Cars[h + 1].Length;
										b = Trains[i].Cars[h].RearAxle.Follower.TrackPosition -
											Trains[i].Cars[h].RearAxle.Position - 0.5*Trains[i].Cars[h].Length;
										double d = b - a - Trains[i].Cars[h].Coupler.MinimumDistanceBetweenCars;
										if (d < 0.0)
										{
											d -= 0.0001;
											Trains[i].Cars[h].UpdateTrackFollowers(-d, false, false);
											if (Interface.CurrentOptions.Derailments &&
												Math.Abs(Trains[i].Cars[h].CurrentSpeed) >
												Trains[i].CriticalCollisionSpeedDifference)
											{
												Trains[i].Derail(h, TimeElapsed);
											}
											Trains[i].Cars[h].CurrentSpeed = 0.0;
										}
									}
								}
							}
						}
					}
				});
			}
			// compute final angles and positions
			//for (int i = 0; i < Trains.Length; i++) {
			System.Threading.Tasks.Parallel.For(0, Trains.Length, i =>
			{
				if (Trains[i].State != TrainState.Disposed & Trains[i].State != TrainState.Bogus)
				{
					for (int j = 0; j < Trains[i].Cars.Length; j++)
					{
						Trains[i].Cars[j].FrontAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].FrontBogie.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].FrontBogie.RearAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].RearAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].RearBogie.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Trains[i].Cars[j].RearBogie.RearAxle.Follower.UpdateWorldCoordinates(true);
						if (TimeElapsed == 0.0 | TimeElapsed > 0.5)
						{
							//Don't update the toppling etc. with excessive or no time
							continue;
						}
						Trains[i].Cars[j].UpdateTopplingCantAndSpring(TimeElapsed);
						Trains[i].Cars[j].FrontBogie.UpdateTopplingCantAndSpring();
						Trains[i].Cars[j].RearBogie.UpdateTopplingCantAndSpring();
					}
				}
			});

			System.Threading.Tasks.Parallel.For(0, TFOs.Length, i =>
			{
				if (TFOs[i].State != TrainState.Disposed & TFOs[i].State != TrainState.Bogus)
				{
					foreach (var Car in TFOs[i].Cars)
					{
						Car.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Car.FrontBogie.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Car.FrontBogie.RearAxle.Follower.UpdateWorldCoordinates(true);
						Car.RearAxle.Follower.UpdateWorldCoordinates(true);
						Car.RearBogie.FrontAxle.Follower.UpdateWorldCoordinates(true);
						Car.RearBogie.RearAxle.Follower.UpdateWorldCoordinates(true);
						if (TimeElapsed == 0.0 | TimeElapsed > 0.5)
						{
							//Don't update the toppling etc. with excessive or no time
							continue;
						}
						Car.UpdateTopplingCantAndSpring(TimeElapsed);
						Car.FrontBogie.UpdateTopplingCantAndSpring();
						Car.RearBogie.UpdateTopplingCantAndSpring();
					}
				}
			});
		}
	}
}

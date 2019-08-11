﻿using OpenBveApi.Colors;
using OpenBveApi.Interface;
using OpenBve.RouteManager;
using OpenBveApi;
using OpenBveApi.Routes;
using OpenBveApi.Trains;

namespace OpenBve
{
	internal static partial class TrackManager
	{
		/// <summary>Is called when a train changes from one signalling section to another</summary>
		internal class SectionChangeEvent : GeneralEvent<AbstractTrain, AbstractCar>
		{
			/// <summary>The index of the previous signalling section</summary>
			internal readonly int PreviousSectionIndex;
			/// <summary>The index of the next signalling section</summary>
			internal readonly int NextSectionIndex;

			internal SectionChangeEvent(double TrackPositionDelta, int PreviousSectionIndex, int NextSectionIndex)
			{
				this.TrackPositionDelta = TrackPositionDelta;
				this.DontTriggerAnymore = false;
				this.PreviousSectionIndex = PreviousSectionIndex;
				this.NextSectionIndex = NextSectionIndex;
			}
			public override void Trigger(int Direction, EventTriggerType TriggerType, AbstractTrain Train, AbstractCar Car)
			{
				if (Train != null)
				{
					if (TriggerType == EventTriggerType.FrontCarFrontAxle)
					{
						if (Direction < 0)
						{
							if (this.NextSectionIndex >= 0)
							{
								CurrentRoute.Sections[this.NextSectionIndex].TrainReachedStopPoint = false;
							}
							UpdateFrontBackward(Train, true);
						}
						else if (Direction > 0)
						{
							UpdateFrontForward(Train, true, true);
						}
					}
					else if (TriggerType == EventTriggerType.RearCarRearAxle)
					{
						if (Direction < 0)
						{
							UpdateRearBackward(Train, true);
						}
						else if (Direction > 0)
						{
							if (this.PreviousSectionIndex >= 0)
							{
								CurrentRoute.Sections[this.PreviousSectionIndex].TrainReachedStopPoint = false;
							}
							UpdateRearForward(Train, true);
						}
					}
				}
			}
			private void UpdateFrontBackward(AbstractTrain Train, bool UpdateTrain)
			{
				// update sections
				if (this.PreviousSectionIndex >= 0)
				{
					CurrentRoute.Sections[this.PreviousSectionIndex].Enter(Train);
					CurrentRoute.Sections[this.PreviousSectionIndex].Update(Game.SecondsSinceMidnight);
				}
				if (this.NextSectionIndex >= 0)
				{
					CurrentRoute.Sections[this.NextSectionIndex].Leave(Train);
					CurrentRoute.Sections[this.NextSectionIndex].Update(Game.SecondsSinceMidnight);
				}
				if (UpdateTrain)
				{
					// update train
					if (this.PreviousSectionIndex >= 0)
					{
						if (!CurrentRoute.Sections[this.PreviousSectionIndex].Invisible)
						{
							Train.CurrentSectionIndex = this.PreviousSectionIndex;
						}
					}
					else
					{
						Train.CurrentSectionLimit = double.PositiveInfinity;
						Train.CurrentSectionIndex = -1;
					}
				}
			}
			private void UpdateFrontForward(AbstractTrain Train, bool UpdateTrain, bool UpdateSection)
			{
				if (UpdateTrain)
				{
					// update train
					if (this.NextSectionIndex >= 0)
					{
						if (!CurrentRoute.Sections[this.NextSectionIndex].Invisible)
						{
							if (CurrentRoute.Sections[this.NextSectionIndex].CurrentAspect >= 0)
							{
								Train.CurrentSectionLimit = CurrentRoute.Sections[this.NextSectionIndex].Aspects[CurrentRoute.Sections[this.NextSectionIndex].CurrentAspect].Speed;
							}
							else
							{
								Train.CurrentSectionLimit = double.PositiveInfinity;
							}
							Train.CurrentSectionIndex = this.NextSectionIndex;
						}
					}
					else
					{
						Train.CurrentSectionLimit = double.PositiveInfinity;
						Train.CurrentSectionIndex = -1;
					}
					// messages
					if (this.NextSectionIndex < 0 || !CurrentRoute.Sections[this.NextSectionIndex].Invisible)
					{
						if (Train.CurrentSectionLimit == 0.0 && Game.MinimalisticSimulation == false)
						{
							Game.AddMessage(Translations.GetInterfaceString("message_signal_stop"), MessageDependency.PassedRedSignal, GameMode.Normal, MessageColor.Red, double.PositiveInfinity, null);
						}
						else if (Train.CurrentSpeed > Train.CurrentSectionLimit)
						{
							Game.AddMessage(Translations.GetInterfaceString("message_signal_overspeed"), MessageDependency.SectionLimit, GameMode.Normal, MessageColor.Orange, double.PositiveInfinity, null);
						}
					}
				}
				if (UpdateSection)
				{
					// update sections
					if (this.NextSectionIndex >= 0)
					{
						CurrentRoute.Sections[this.NextSectionIndex].Enter(Train);
						CurrentRoute.Sections[this.NextSectionIndex].Update(Game.SecondsSinceMidnight);
					}
				}
			}
			private void UpdateRearBackward(AbstractTrain Train, bool UpdateSection)
			{
				if (UpdateSection)
				{
					// update sections
					if (this.PreviousSectionIndex >= 0)
					{
						CurrentRoute.Sections[this.PreviousSectionIndex].Enter(Train);
						CurrentRoute.Sections[this.PreviousSectionIndex].Update(Game.SecondsSinceMidnight);
					}
				}
			}
			private void UpdateRearForward(AbstractTrain Train, bool UpdateSection)
			{
				if (UpdateSection)
				{
					// update sections
					if (this.PreviousSectionIndex >= 0)
					{
						CurrentRoute.Sections[this.PreviousSectionIndex].Leave(Train);
						CurrentRoute.Sections[this.PreviousSectionIndex].Update(Game.SecondsSinceMidnight);
					}
					if (this.NextSectionIndex >= 0)
					{
						CurrentRoute.Sections[this.NextSectionIndex].Enter(Train);
						CurrentRoute.Sections[this.NextSectionIndex].Update(Game.SecondsSinceMidnight);
					}
				}
			}
		}
	}
}

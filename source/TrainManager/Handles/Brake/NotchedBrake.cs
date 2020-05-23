﻿namespace TrainManager.Handles
{
	/// <summary>A brake handle</summary>
	public class BrakeHandle : NotchedHandle
	{
		public BrakeHandle(int max, int driverMax, EmergencyHandle eb, double[] delayUp, double[] delayDown)
		{
			this.MaximumNotch = max;
			this.MaximumDriverNotch = driverMax;
			this.EmergencyBrake = eb;
			this.DelayUp = delayUp;
			this.DelayDown = delayDown;
			this.DelayedChanges = new HandleChange[] { };
		}

		/// <summary>Provides a reference to the associated EB handle</summary>
		private readonly EmergencyHandle EmergencyBrake;

		public override void Update(double CurrentTime)
		{
			int sec = EmergencyBrake.Safety ? MaximumNotch : Safety;
			if (DelayedChanges.Length == 0)
			{
				if (sec < Actual)
				{
					AddChange(Actual - 1, GetDelay(false) + CurrentTime);
				}
				else if (sec > Actual)
				{
					AddChange(Actual + 1, GetDelay(true) + CurrentTime);
				}
			}
			else
			{
				int m = DelayedChanges.Length - 1;
				if (sec < DelayedChanges[m].Value)
				{
					AddChange(sec, GetDelay(false) + CurrentTime);
				}
				else if (sec > DelayedChanges[m].Value)
				{
					AddChange(sec, GetDelay(true) + CurrentTime);
				}
			}
			if (DelayedChanges.Length >= 1)
			{
				if (DelayedChanges[0].Time <= CurrentTime)
				{
					Actual = DelayedChanges[0].Value;
					RemoveChanges(1);
				}
			}
		}
	}
}

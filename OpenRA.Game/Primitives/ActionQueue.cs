#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	/// <summary>
	/// A thread-safe action queue, suitable for passing units of work between threads.
	/// </summary>
	public class ActionQueue
	{
		readonly List<DelayedAction> actions = new List<DelayedAction>();

		public void Add(Action a, int desiredTime)
		{
			if (a == null)
				throw new ArgumentNullException("a");

			lock (actions)
			{
				var action = new DelayedAction(a, desiredTime);
				var index = Index(action);
				actions.Insert(index, action);
			}
		}

		public void PerformActions(int currentTime)
		{
			DelayedAction[] pendingActions;
			lock (actions)
			{
				var dummyAction = new DelayedAction(null, currentTime);
				var index = Index(dummyAction);
				if (index <= 0)
					return;

				pendingActions = new DelayedAction[index];
				actions.CopyTo(0, pendingActions, 0, index);
				actions.RemoveRange(0, index);
			}

			foreach (var delayedAction in pendingActions)
				delayedAction.Action();
		}

		int Index(DelayedAction action)
		{
			// Returns the index of the next action with a strictly greater time.
			var index = actions.BinarySearch(action);
			if (index < 0)
				return ~index;
			while (index < actions.Count && action.CompareTo(actions[index]) >= 0)
				index++;
			return index;
		}
	}

	struct DelayedAction : IComparable<DelayedAction>
	{
		public readonly int Time;
		public readonly Action Action;

		public DelayedAction(Action action, int time)
		{
			Action = action;
			Time = time;
		}

		public int CompareTo(DelayedAction other)
		{
			return Time.CompareTo(other.Time);
		}

		public override string ToString()
		{
			return "Time: " + Time + " Action: " + Action;
		}
	}
}

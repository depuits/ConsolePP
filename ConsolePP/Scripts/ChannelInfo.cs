using System;
using UnityEngine;

namespace ConsolePP
{
	[Serializable]
	public class ChannelInfo
	{
		public string name = "";
		public bool enabled = true;
		public Color color = Color.white;
		
		public ChannelInfo(string n)
			: this(n, GeneralUtil.RandomColor())
		{
		}
		public ChannelInfo(string n, Color c)
		{
			name = n;
			color = c;
		}
		public ChannelInfo(ChannelInfo c)
			: this(c.name, c.color)
		{
		}
		
		public override bool Equals(System.Object obj)
		{
			// If parameter is null return false.
			if (obj == null)
				return false;
			
			// If parameter cannot be cast to Point return false.
			ChannelInfo ci = obj as ChannelInfo;
			if ((System.Object)ci == null)
				return false;
			
			// Return true if the fields match:
			return name == ci.name;
		}
		
		public bool Equals(ChannelInfo ci)
		{
			// If parameter is null return false:
			if ((object)ci == null)
				return false;
			
			// Return true if the fields match:
			return name == ci.name;
		}
		
		public override int GetHashCode()
		{
			return name.GetHashCode();
		}
	}
}

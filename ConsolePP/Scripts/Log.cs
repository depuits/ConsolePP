using System;
using ConsolePP;
using UnityEngine;

public static class Log 
{
	static Action<LogEntry> onLogAdded;

	public static event Action<LogEntry> OnLogAdded 
	{
		add { onLogAdded += value; }
		remove { onLogAdded -= value; }
	}

	public static void Trace(string id, string msg)
	{
		Add(id, LogEntry.Level.Trace, msg);
	}
	public static void Debug(string id, string msg)
	{
		Add(id, LogEntry.Level.Debug, msg);
	}
	public static void Info(string id, string msg)
	{
		Add(id, LogEntry.Level.Info, msg);
	}
	public static void Warning(string id, string msg)
	{
		Add(id, LogEntry.Level.Warning, msg);
	}
	public static void Error(string id, string msg)
	{
		Add(id, LogEntry.Level.Error, msg);
	}
	public static void Command(string id, string msg)
	{
		Add(id, LogEntry.Level.Command, msg);
	}

	public static void Trace(string id, string msg, params System.Object[] pars)
	{
		Trace( id, string.Format( msg, pars ) );
	}
	public static void Debug(string id, string msg, params System.Object[] pars)
	{
		Debug( id, string.Format( msg, pars ) );
	}
	public static void Info(string id, string msg, params System.Object[] pars)
	{
		Info( id, string.Format( msg, pars ) );
	}
	public static void Warning(string id, string msg, params System.Object[] pars)
	{
		Warning( id, string.Format( msg, pars ) );
	}
	public static void Error(string id, string msg, params System.Object[] pars)
	{
		Error( id, string.Format( msg, pars ) );
	}
	public static void Command(string id, string msg, params System.Object[] pars)
	{
		Command( id, string.Format( msg, pars ) );
	}

	public static void Add(string id, LogEntry.Level type, string msg)
	{
		if( onLogAdded != null )
			onLogAdded( new LogEntry(id, type, msg) );

		if( !Application.isEditor )
		{
			switch( type )
			{
			case LogEntry.Level.Error:
				UnityEngine.Debug.LogError( string.Format( "{0}: {1}", id, msg ) );
				break;
			case LogEntry.Level.Warning:
				UnityEngine.Debug.LogWarning( string.Format( "{0}: {1}", id, msg ) );
				break;
			default:
				UnityEngine.Debug.Log( string.Format( "{0}: {1}", id, msg ) );
				break;
			}
		}
	}
}

#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace OpenRA
{
	public class Download
	{
		readonly object syncObject = new object();
		WebClient wc;

		public static string FormatErrorMessage(Exception e)
		{
			var ex = e as WebException;
			if (ex == null)
				return e.Message;

			switch (ex.Status)
			{
				case WebExceptionStatus.RequestCanceled:
					return "Cancelled";
				case WebExceptionStatus.NameResolutionFailure:
					return "DNS lookup failed";
				case WebExceptionStatus.Timeout:
					return "Connection timeout";
				case WebExceptionStatus.ConnectFailure:
					return "Cannot connect to remote server";
				case WebExceptionStatus.ProtocolError:
					return "File not found on remote server";
				default:
					return ex.Message;
			}
		}

		public Download(string url, string path, Action<DownloadProgressChangedEventArgs> onProgress, Action<AsyncCompletedEventArgs> onComplete)
		{
			Console.WriteLine("Do we get here?! and url is: " + url );
			lock (syncObject)
			{
				wc = new WebClient { Proxy = null };
				wc.DownloadProgressChanged += (_, a) => onProgress(a);
				wc.DownloadFileCompleted += (_, a) => { DisposeWebClient(); onComplete(a); };
				wc.DownloadFileAsync(new Uri(url), path);
			}
		}

		public Download(string url, Action<DownloadProgressChangedEventArgs> onProgress, Action<DownloadDataCompletedEventArgs> onComplete)
		{
			Console.WriteLine("This is where mirror-list is downloaded, url is: " + url);
			lock (syncObject)
			{

				Console.WriteLine("IPv4: ", Socket.OSSupportsIPv4);
				Uri uri_test = new Uri(url);
				Console.WriteLine("This is the host: " + uri_test.Host);

				// This works around the real problem.
				// otherwise it throws SocketException and it isnt just WebClient-functionality that is affected.
				// whatever logic that is sidestepped here needs to be side stepped "globally", at the moment this just happens
				// when we download something.
				var field = typeof(System.Net.Sockets.Socket).GetField("s_SupportsIPv4", BindingFlags.Static|BindingFlags.NonPublic);
				field.SetValue(null, true);
				//
				
				//Console.WriteLine("IPv4: ", Socket.SupportsIPv4);
				Console.WriteLine("IPv4: ", Socket.OSSupportsIPv4);

				IPHostEntry host = Dns.GetHostEntry(uri_test.Host);
				Console.WriteLine(host.AddressList);

				wc = new WebClient { Proxy = null };
				wc.DownloadProgressChanged += (_, a) => onProgress(a);
				wc.DownloadDataCompleted += (_, a) => { DisposeWebClient(); onComplete(a); };

				wc.DownloadDataAsync(new Uri(url));
			}
		}

		void DisposeWebClient()
		{
			lock (syncObject)
			{
				wc.Dispose();
				wc = null;
			}
		}

		public void CancelAsync()
		{
			lock (syncObject)
				if (wc != null)
					wc.CancelAsync();
		}
	}
}

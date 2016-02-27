using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NLog;
using TwitchTallyShared;

namespace TwitchTally.WorkerComm {
	public class MasterServerOld {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private Socket listeningSock;
		private ArrayList clientInfoList = ArrayList.Synchronized(new ArrayList());
		private AsyncCallback asyncWorkerCallBack;

		/// <summary>
		/// Starts the Server listening on Config.Instance.CommInfoPort.
		/// </summary>
		public void StartListening() {
			listeningSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listeningSock.Bind(new IPEndPoint(IPAddress.Any, Properties.Settings.Default.MinionCommPort));
			listeningSock.Listen(4);
			listeningSock.BeginAccept(new AsyncCallback(OnWorkerConnect), null);
			Logger.Info("Master Server now listening on port {0}.", Properties.Settings.Default.MinionCommPort);
		}

		/// <summary>
		/// Triggered when a Client attempts to Connect. Accepts the connection and begins waiting for Data
		/// </summary>
		/// <param name="asyn">Associated IAsyncResult object</param>
		private void OnWorkerConnect(IAsyncResult asyn) {
			WorkerClient tempWorkerClient = new WorkerClient(listeningSock.EndAccept(asyn));
			clientInfoList.Add(tempWorkerClient);
			WaitForData(tempWorkerClient);
			Logger.Info("Client has connected.");
			listeningSock.BeginAccept(new AsyncCallback(OnWorkerConnect), null);
		}

		/// <summary>
		/// An always-active function that waits for the client to send data to the server. Eventually trigers OnDataReceived().
		/// </summary>
		/// <param name="i_WorkerClient">Associated ClientInfo object</param>
		private void WaitForData(WorkerClient i_WorkerClient) {
			if (asyncWorkerCallBack == null) { asyncWorkerCallBack = new AsyncCallback(OnDataReceived); }
			SocketEventArgs tempSocketEventArgs = new SocketEventArgs(i_WorkerClient);
			i_WorkerClient.Socket.BeginReceive(
				tempSocketEventArgs.DataBuffer,
				0,
				tempSocketEventArgs.DataBuffer.Length,
				SocketFlags.None,
				asyncWorkerCallBack,
				tempSocketEventArgs
			);
		}

		/// <summary>
		/// Triggered when data is received from the Client. Takes care of closing dead Sockets and
		/// passing data to Client.OnRecieveData().
		/// </summary>
		/// <param name="i_AsyncResult">Required associated IAsyncResult</param>
		private void OnDataReceived(IAsyncResult i_AsyncResult) {
			SocketEventArgs SocketEventArgs = (SocketEventArgs)i_AsyncResult.AsyncState;
			try {
				int receiveLen = 0;
				receiveLen = SocketEventArgs.Socket.EndReceive(i_AsyncResult);
				if (receiveLen == 0) { CloseClientInfoConnection(SocketEventArgs.WorkerIndex); } else {
					char[] receiveCharsOld = new char[receiveLen];
					int charLength = Encoding.UTF8.GetChars(SocketEventArgs.DataBuffer, 0, receiveLen, receiveCharsOld, 0);
					char[] receiveChars = new char[charLength];
					Array.Copy(receiveCharsOld, receiveChars, charLength);
					String receiveData = new String(receiveChars);
					SocketEventArgs.WorkerClient.DataBuffer += receiveData;
					// See if multiple messages were sent in the same packet, if so call OnReceiveData for all of them.
					if (Functions.OccurancesInString(SocketEventArgs.WorkerClient.DataBuffer, "\x4") >= 1) {
						String[] splitIncommingData = SocketEventArgs.WorkerClient.DataBuffer.Split(("\x4").ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
						for (int i = 0; i < splitIncommingData.Length; i++) {
							SocketEventArgs.WorkerClient.DataBuffer = SocketEventArgs.WorkerClient.DataBuffer.Remove(0, splitIncommingData[i].Length + 1);
							ClientInfoIndexToClientInfo(SocketEventArgs.WorkerIndex).OnReceiveData(splitIncommingData[i]);
						}
					}
					if (SocketEventArgs.WorkerClient.Socket.Connected == true) {
						WaitForData(ClientInfoIndexToClientInfo(SocketEventArgs.WorkerIndex));
					}
				}
			} catch (ObjectDisposedException) {
				CloseClientInfoConnection(SocketEventArgs.WorkerIndex);
			} catch (NullReferenceException) {
				CloseClientInfoConnection(SocketEventArgs.WorkerIndex);
			} catch (SocketException Se) {
				if (Se.ErrorCode == 10054) {
					CloseClientInfoConnection(SocketEventArgs.WorkerIndex);
				}
			}
		}

		/// <summary>
		/// Closes a server connection based on the Client Index.
		/// </summary>
		/// <param name="i_WorkerIndex">Client Index to Close</param>
		private void CloseClientInfoConnection(int i_WorkerIndex) {
			WorkerClient tempWorkerClient = ClientInfoIndexToClientInfo(i_WorkerIndex);
			clientInfoList.Remove(tempWorkerClient);
			Logger.Info("Worker has disconnected.");
		}

		/// <summary>
		/// Returns a Client object associated with a specific Client Index
		/// </summary>
		/// <param name="i_WorkerClientIndex">The Client Index being searched for.</param>
		/// <returns>Client Object</returns>
		private WorkerClient ClientInfoIndexToClientInfo(int i_WorkerClientIndex) {
			for (int i = 0; i < clientInfoList.Count; i++) {
				if (((WorkerClient)clientInfoList[i]).Index == i_WorkerClientIndex) {
					return (WorkerClient)clientInfoList[i];
				}
			}
			return null;
		}
	}
}
